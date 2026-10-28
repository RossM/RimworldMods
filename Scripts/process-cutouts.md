# Xenotype cutout processing

`process-cutouts.py` reads the top-level 384x384 RGBA PNGs in `Art/Cutouts` and
writes 768x768 RGBA PNGs with the same names in `Art/Cutouts/Processed`.
It preserves the originals and replaces processed files on subsequent runs.
Paths default relative to the script's repository, independent of the current directory.

## Setup

Requires Python 3.10+, OpenCV, NumPy, and a Vulkan-capable GPU. Install the Python
dependencies in your preferred virtual environment:

```powershell
python -m pip install -r Scripts/requirements-cutouts.txt
```

Download the portable Windows package from the author's official release:

- [Real-ESRGAN repository](https://github.com/xinntao/Real-ESRGAN)
- [Release v0.2.5.0](https://github.com/xinntao/Real-ESRGAN/releases/tag/v0.2.5.0)
- [Windows executable and models](https://github.com/xinntao/Real-ESRGAN/releases/download/v0.2.5.0/realesrgan-ncnn-vulkan-20220424-windows.zip)

Extract the archive contents into `Scripts/Tools/realesrgan`. The executable
must be `Scripts/Tools/realesrgan/realesrgan-ncnn-vulkan.exe`, alongside its
`models` directory, including `realesrgan-x4plus-anime.bin` and `.param`.
Keep the accompanying DLLs and licenses. `Scripts/Tools` is ignored by Git.
The downloaded Windows archive used for validation had SHA-256:

```text
abc02804e17982a3be33675e4d471e91ea374e65b70167abc09e31acb412802d
```

The portable package includes its neural network runtime and pretrained models;
PyTorch and CUDA installation are unnecessary. All image processing and model
inference run locally. The processor itself never downloads anything or uploads
images. The package is already installed in this checkout.

## Run

```powershell
python Scripts/process-cutouts.py
```

For the virtual environment used to validate this checkout:

```powershell
temp/cutouts-venv/Scripts/python.exe Scripts/process-cutouts.py
```

Options:

- `--input PATH`: source directory; only its immediate PNG files are read.
- `--output PATH`: destination directory; defaults to `INPUT/Processed`.
- `--gap-radius 2`: morphological closing radius in source pixels.
- `--outline-radius 1`: black underlay expansion radius in source pixels.
- `--upscaler PATH`: another local `realesrgan-ncnn-vulkan` executable;
  its `models` directory must be beside it.

Set either radius to zero to disable that repair operation.

## Why this pipeline

The original cubic/median/Gaussian/bilateral approach softened line transitions
while leaving their staircase geometry visible. Stronger classical shock-filter
trials introduced artifacts or lost detail. Local waifu2x was smoother, but
Real-ESRGAN's anime illustration model gave sharper lines on these samples.

1. Close narrow transparency seams and expand a black underlay by one source
   pixel, preserving large openings rather than flood-filling the silhouette.
2. Composite the original colors over that underlay in premultiplied form.
   Hidden RGB in fully transparent pixels contributes nothing.
3. Run both color and a separate grayscale coverage image through the local
   `realesrgan-x4plus-anime` model at its native 4x scale. Processing coverage as
   RGB avoids ordinary interpolation of the alpha channel.
4. Remove near-black/near-white residuals in the coverage image, reduce both
   reconstructed images to 2x with area sampling, and restore straight alpha.
   Limit coverage to within two source pixels of the repaired silhouette so
   distant background residuals stay fully transparent.

There is no final blur filter. Like other learned restoration models, this
model can change fine details; reconstructed stitches and scales are estimates
of the source shapes. Review the PNGs at 100% size when evaluating line quality.

Dependencies were verified against primary sources:

- [Real-ESRGAN's anime model documentation](https://github.com/xinntao/Real-ESRGAN/blob/master/docs/anime_model.md)
- [OpenCV Python distribution](https://pypi.org/project/opencv-python-headless/)
- [NumPy](https://numpy.org/about/)
