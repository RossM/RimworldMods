"""Reconstruct psycast PSDs as transparent 256px PNGs for the gene compositors.

Run: python Scripts/process-psycasts.py
Requires Pillow and the cutout-processing dependencies in requirements-cutouts.txt.
Uses the same local Real-ESRGAN executable/model and alpha-aware reconstruction as
process-cutouts.py. Run this when source art changes; compositors only read the
finished art and never invoke ESRGAN themselves.

Inputs default to Art/Rimworld art/Psycasts/*.psd. Outputs go to its Processed/
directory. PSDs remain the originals; temporary color/mask PNGs are cleaned up
after inference. All processing is local, with no downloads or network calls.
"""

import argparse
from pathlib import Path

from PIL import Image

from rgba_upscaler import DEFAULT_UPSCALER, cv2, np, upscale_bgra, write_png


ROOT = Path(__file__).resolve().parents[1]
SOURCE_SIZE = 64
OUTPUT_SIZE = 256


def read_psycast(path):
    with Image.open(path) as source:
        if source.size != (SOURCE_SIZE, SOURCE_SIZE):
            raise ValueError(f"Expected a {SOURCE_SIZE}x{SOURCE_SIZE} psycast PSD: {path}")
        return cv2.cvtColor(np.array(source.convert("RGBA")), cv2.COLOR_RGBA2BGRA)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--input", type=Path, default=ROOT / "Art/Rimworld art/Psycasts", help="Source PSD directory")
    parser.add_argument("--output", type=Path, help="Destination directory (default: INPUT/Processed)")
    parser.add_argument("--upscaler", type=Path, default=DEFAULT_UPSCALER, help="Local realesrgan-ncnn-vulkan executable")
    args = parser.parse_args()
    if not args.input.is_dir():
        parser.error(f"Input directory does not exist: {args.input}")
    output = args.output or args.input / "Processed"
    if output.resolve() == args.input.resolve():
        parser.error("Keep processed art in a separate directory from the originals")
    paths = sorted(path for path in args.input.iterdir() if path.is_file() and path.suffix.lower() == ".psd")
    if not paths:
        parser.error(f"No psycast PSDs found in {args.input}")
    try:
        images = [read_psycast(path) for path in paths]
        processed = upscale_bgra(images, OUTPUT_SIZE, args.upscaler)
        output.mkdir(parents=True, exist_ok=True)
        for path, image in zip(paths, processed):
            destination = output / (path.stem + ".png")
            write_png(destination, image)
            print(f"Saved {destination}: {OUTPUT_SIZE} x {OUTPUT_SIZE}")
    except (OSError, ValueError, cv2.error) as error:
        parser.error(str(error))


if __name__ == "__main__":
    main()
