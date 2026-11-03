"""Upscale the 384px xenotype cutouts into transparent 768px PNGs.

Requires Python 3.10+. Install dependencies and run from the repository root:
    python -m pip install -r Scripts/requirements-cutouts.txt
    python Scripts/process-cutouts.py

Paths default to Art/Cutouts and Art/Cutouts/Processed, relative to this script's
repository, so execution from another working directory also works. Originals
are preserved; rerunning replaces the processed PNGs with the same filenames.

Uses the portable executable in Scripts/Tools/realesrgan/ with the
realesrgan-x4plus-anime .param/.bin files in its models/ directory. Use --upscaler
for another installation. All inference runs locally on a Vulkan-capable GPU,
with no network requests or model downloads. rgba_upscaler.py shares the same
alpha-aware reconstruction with process-psycasts.py so both pipelines stay consistent.

Real-ESRGAN reconstructs both the premultiplied color and the alpha mask
at 4x; area downsampling produces the final 2x PNG. Processing alpha separately
avoids leaving a pixelated silhouette around the reconstructed interior lines.
No Gaussian blur, median filter, or bilateral filter is applied to the output.

Verified dependencies and documentation:
    https://github.com/xinntao/Real-ESRGAN
    https://pypi.org/project/opencv-python-headless/
    https://numpy.org/about/
    https://docs.opencv.org/4.x/d4/d86/group__imgproc__filter.html
"""

import argparse
from pathlib import Path

from rgba_upscaler import DEFAULT_UPSCALER, cv2, np, upscale_bgra, write_png


ROOT = Path(__file__).resolve().parents[1]
SOURCE_SIZE = 384
OUTPUT_SIZE = SOURCE_SIZE * 2


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
    parser.add_argument("--upscaler", type=Path, default=DEFAULT_UPSCALER,
                        help="Local realesrgan-ncnn-vulkan executable")
    args = parser.parse_args()
    if not args.input.is_dir():
        parser.error(f"Input directory does not exist: {args.input}")
    output = args.output or args.input / "Processed"
    if output.resolve() == args.input.resolve():
        parser.error("Output directory must differ from input to preserve originals")
    paths = sorted(path for path in args.input.iterdir() if path.is_file() and path.suffix.lower() == ".png")
    if not paths:
        parser.error(f"No PNG cutouts found in {args.input}")
    try:
        # Validate the whole batch before replacing any existing processed images.
        cutouts = [(path, read_cutout(path)) for path in paths]
        for path, _ in cutouts:
            if (output / path.name).resolve() == path.resolve():
                raise ValueError(f"Output would overwrite original: {path}")
        processed = upscale_bgra([image for _, image in cutouts], OUTPUT_SIZE, args.upscaler)
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
