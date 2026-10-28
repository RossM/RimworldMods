"""Repair and smooth the 384px xenotype cutouts into transparent 768px PNGs.

Requires Python 3.10+. Install dependencies and run from the repository root:
    python -m pip install -r Scripts/requirements-cutouts.txt
    python Scripts/process-cutouts.py

Paths default to Art/Cutouts and Art/Cutouts/Processed, relative to this script's
repository, so execution from another working directory also works. Originals
are preserved; rerunning replaces the processed PNGs with the same filenames.

Requires the portable Real-ESRGAN executable and anime illustration model;
see Scripts/process-cutouts.md for setup. All inference runs LOCALLY on a
Vulkan-capable GPU. The script makes no network requests or model downloads.

Morphological closing/dilation create a thin black underlay over narrow alpha
seams. Real-ESRGAN reconstructs both the premultiplied color and the alpha mask
at 4x; area downsampling produces the final 2x PNG. Processing alpha separately
avoids leaving a pixelated silhouette around the reconstructed interior lines.
No Gaussian blur, median filter, or bilateral filter is applied to the output.

Tuning example (radii are SOURCE pixels):
    python Scripts/process-cutouts.py --gap-radius 3 --outline-radius 1

Verified dependencies and documentation:
    https://github.com/xinntao/Real-ESRGAN
    https://pypi.org/project/opencv-python-headless/
    https://numpy.org/about/
    https://docs.opencv.org/4.x/d9/d61/tutorial_py_morphological_ops.html
    https://docs.opencv.org/4.x/d4/d86/group__imgproc__filter.html
"""

import argparse
from pathlib import Path
import subprocess
import tempfile

try:
    import cv2
    import numpy as np
except ImportError as error:
    raise SystemExit(
        "Missing image-processing dependency. Install with:\n"
        f'  python -m pip install -r "{Path(__file__).with_name("requirements-cutouts.txt")}"'
    ) from error


ROOT = Path(__file__).resolve().parents[1]
SOURCE_SIZE = 384
OUTPUT_SIZE = SOURCE_SIZE * 2
MODEL_NAME = "realesrgan-x4plus-anime"
DEFAULT_UPSCALER = ROOT / "Scripts/Tools/realesrgan/realesrgan-ncnn-vulkan.exe"


def ellipse(radius):
    return cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * radius + 1, 2 * radius + 1))


def repair_cutout(bgra, outline_radius=1, gap_radius=2):
    """Return premultiplied BGR and alpha as separate 8-bit images."""
    alpha = bgra[:, :, 3]
    underlay = alpha.copy()
    if gap_radius:
        # Closing bridges narrow seams without filling every enclosed opening.
        underlay = cv2.morphologyEx(underlay, cv2.MORPH_CLOSE, ellipse(gap_radius))
    if outline_radius:
        underlay = cv2.dilate(underlay, ellipse(outline_radius))

    alpha = alpha.astype(np.float32) / 255
    underlay = underlay.astype(np.float32) / 255
    repaired_alpha = alpha + underlay * (1 - alpha)
    # Black contributes opacity but no color. Ignore hidden RGB in clear pixels
    # so resizing cannot pull arbitrary source colors into the visible fringe.
    colors = np.rint(bgra[:, :, :3].astype(np.float32) * alpha[:, :, None]).astype(np.uint8)
    return colors, np.rint(repaired_alpha * 255).astype(np.uint8)


def reconstruct_cutout(color_path, alpha_path, original_alpha):
    """Combine separately reconstructed color and coverage without color halos."""
    colors = cv2.imdecode(np.fromfile(color_path, dtype=np.uint8), cv2.IMREAD_COLOR)
    alpha = cv2.imdecode(np.fromfile(alpha_path, dtype=np.uint8), cv2.IMREAD_GRAYSCALE)
    expected = SOURCE_SIZE * 4
    if colors is None or alpha is None or colors.shape != (expected, expected, 3) or alpha.shape != (expected, expected):
        raise ValueError(f"Upscaler did not produce the expected {expected}px images for {color_path.name}")
    # Neural inference leaves tiny residuals even in constant black/white areas.
    # Restore exact transparency/opacity before computing final edge coverage.
    alpha[alpha < 4] = 0
    alpha[alpha > 251] = 255
    colors = cv2.resize(colors.astype(np.float32), (OUTPUT_SIZE, OUTPUT_SIZE), interpolation=cv2.INTER_AREA)
    alpha = cv2.resize(alpha.astype(np.float32), (OUTPUT_SIZE, OUTPUT_SIZE), interpolation=cv2.INTER_AREA)
    # Restrict any reconstructed fringe to the neighborhood of the source art;
    # this also keeps distant background residuals completely transparent.
    support = cv2.dilate((original_alpha > 0).astype(np.uint8), ellipse(2))
    support = cv2.resize(support, (OUTPUT_SIZE, OUTPUT_SIZE), interpolation=cv2.INTER_NEAREST)
    alpha[support == 0] = 0
    straight = np.zeros_like(colors)
    np.divide(colors * 255, alpha[:, :, None], out=straight, where=alpha[:, :, None] > 0)
    straight = np.rint(np.clip(straight, 0, 255)).astype(np.uint8)
    alpha = np.rint(alpha).astype(np.uint8)
    straight[alpha == 0] = 0
    return np.dstack((straight, alpha))


def write_png(path, image):
    success, encoded = cv2.imencode(".png", image)
    if not success:
        raise ValueError(f"Cannot encode PNG: {path}")
    encoded.tofile(path)


def read_cutout(path):
    # imdecode/fromfile also handles Windows paths containing non-ASCII names.
    image = cv2.imdecode(np.fromfile(path, dtype=np.uint8), cv2.IMREAD_UNCHANGED)
    if image is None:
        raise ValueError(f"Cannot decode PNG: {path}")
    if image.dtype != np.uint8 or image.shape != (SOURCE_SIZE, SOURCE_SIZE, 4):
        raise ValueError(f"Expected an 8-bit {SOURCE_SIZE}x{SOURCE_SIZE} RGBA PNG: {path}")
    return image


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--input", type=Path, default=ROOT / "Art/Cutouts", help="Source PNG directory")
    parser.add_argument("--output", type=Path, help="Destination directory (default: INPUT/Processed)")
    parser.add_argument("--outline-radius", type=int, default=1,
                        help="Black underlay expansion in source pixels; 0 disables (default: %(default)s)")
    parser.add_argument("--gap-radius", type=int, default=2,
                        help="Closing radius in source pixels; 0 disables (default: %(default)s)")
    parser.add_argument("--upscaler", type=Path, default=DEFAULT_UPSCALER,
                        help="Local realesrgan-ncnn-vulkan executable (see process-cutouts.md)")
    args = parser.parse_args()
    if args.outline_radius < 0 or args.gap_radius < 0:
        parser.error("--outline-radius and --gap-radius must be nonnegative")
    if not args.input.is_dir():
        parser.error(f"Input directory does not exist: {args.input}")
    output = args.output or args.input / "Processed"
    if output.resolve() == args.input.resolve():
        parser.error("Output directory must differ from input to preserve originals")
    paths = sorted(path for path in args.input.iterdir() if path.is_file() and path.suffix.lower() == ".png")
    if not paths:
        parser.error(f"No PNG cutouts found in {args.input}")
    upscaler = args.upscaler.resolve()
    models = upscaler.parent / "models"
    if not upscaler.is_file() or not all((models / f"{MODEL_NAME}.{extension}").is_file() for extension in ("bin", "param")):
        parser.error("Missing local Real-ESRGAN executable/model; see Scripts/process-cutouts.md")
    try:
        # Validate the whole batch before replacing any existing processed images.
        cutouts = [(path, read_cutout(path)) for path in paths]
        for path, _ in cutouts:
            if (output / path.name).resolve() == path.resolve():
                raise ValueError(f"Output would overwrite original: {path}")
        with tempfile.TemporaryDirectory(prefix="rimworld-cutouts-") as temporary:
            workspace = Path(temporary)
            prepared, enlarged = workspace / "prepared", workspace / "enlarged"
            prepared.mkdir()
            enlarged.mkdir()
            masks = []
            for index, (_, cutout) in enumerate(cutouts):
                colors, alpha = repair_cutout(cutout, args.outline_radius, args.gap_radius)
                write_png(prepared / f"{index}-color.png", colors)
                # The executable normally resizes RGBA alpha with bicubic. Give
                # the mask its own RGB input so the network reconstructs it too.
                write_png(prepared / f"{index}-alpha.png", cv2.cvtColor(alpha, cv2.COLOR_GRAY2BGR))
                masks.append(alpha)
            print(f"Reconstructing {len(cutouts)} cutouts locally with {MODEL_NAME}...", flush=True)
            result = subprocess.run([str(upscaler), "-i", str(prepared), "-o", str(enlarged),
                                     "-m", str(models), "-n", MODEL_NAME, "-s", "4"],
                                    capture_output=True, text=True, errors="replace")
            if result.returncode:
                raise ValueError(f"Local Real-ESRGAN failed ({result.returncode}):\n{result.stderr[-4000:]}")
            # Validate and reconstruct the whole batch before replacing outputs.
            processed = [reconstruct_cutout(enlarged / f"{index}-color.png", enlarged / f"{index}-alpha.png", alpha)
                         for index, alpha in enumerate(masks)]
            output.mkdir(parents=True, exist_ok=True)
            for (path, _), image in zip(cutouts, processed):
                destination = output / path.name
                write_png(destination, image)
                print(f"Saved {destination}: {OUTPUT_SIZE} x {OUTPUT_SIZE}")
    except (OSError, ValueError, cv2.error) as error:
        parser.error(str(error))
    print(f"Processed {len(cutouts)} cutouts into {output.resolve()}")


if __name__ == "__main__":
    main()
