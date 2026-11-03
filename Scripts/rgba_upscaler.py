"""Shared alpha-aware Real-ESRGAN preprocessing for cutouts and psycast source art.

The executable's ordinary alpha resize leaves stair-stepped silhouettes. Feed
premultiplied colors and the coverage mask through the same anime model separately,
then unpremultiply to preserve clean transparent edges. Keep this offline step
separate from compositing so rendering stays fast and does not require a GPU.
"""

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
MODEL_NAME = "realesrgan-x4plus-anime"
DEFAULT_UPSCALER = ROOT / "Scripts/Tools/realesrgan/realesrgan-ncnn-vulkan.exe"
NETWORK_SCALE = 4


def write_png(path, image):
    success, encoded = cv2.imencode(".png", image)
    if not success:
        raise ValueError(f"Cannot encode PNG: {path}")
    encoded.tofile(path)


def prepare_bgra(bgra):
    alpha = bgra[:, :, 3]
    # Hidden RGB in transparent pixels must not bleed into reconstructed edges.
    coverage = alpha.astype(np.float32) / 255
    colors = np.rint(bgra[:, :, :3].astype(np.float32) * coverage[:, :, None]).astype(np.uint8)
    return colors, alpha


def reconstruct_bgra(color_path, alpha_path, original_alpha, output_size):
    colors = cv2.imdecode(np.fromfile(color_path, dtype=np.uint8), cv2.IMREAD_COLOR)
    alpha = cv2.imdecode(np.fromfile(alpha_path, dtype=np.uint8), cv2.IMREAD_GRAYSCALE)
    expected = original_alpha.shape[0] * NETWORK_SCALE
    if colors is None or alpha is None or colors.shape != (expected, expected, 3) or alpha.shape != (expected, expected):
        raise ValueError(f"Upscaler did not produce the expected {expected}px images for {color_path.name}")
    # Inference leaves small residuals in otherwise constant black/white regions.
    alpha[alpha < 4] = 0
    alpha[alpha > 251] = 255
    colors = cv2.resize(colors.astype(np.float32), (output_size, output_size), interpolation=cv2.INTER_AREA)
    alpha = cv2.resize(alpha.astype(np.float32), (output_size, output_size), interpolation=cv2.INTER_AREA)
    # Constrain reconstructed fringes to the original silhouette's neighborhood;
    # suppress distant inference noise without blurring the actual edge.
    support = cv2.dilate((original_alpha > 0).astype(np.uint8),
                         cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5)))
    support = cv2.resize(support, (output_size, output_size), interpolation=cv2.INTER_NEAREST)
    alpha[support == 0] = 0
    straight = np.zeros_like(colors)
    np.divide(colors * 255, alpha[:, :, None], out=straight, where=alpha[:, :, None] > 0)
    straight = np.rint(np.clip(straight, 0, 255)).astype(np.uint8)
    alpha = np.rint(alpha).astype(np.uint8)
    straight[alpha == 0] = 0
    return np.dstack((straight, alpha))


def upscale_bgra(images, output_size, upscaler=DEFAULT_UPSCALER):
    """Reconstruct a batch before callers replace any of their processed outputs."""
    if not images:
        raise ValueError("No images to upscale")
    for image in images:
        if image.dtype != np.uint8 or image.ndim != 3 or image.shape[2] != 4 or image.shape[0] != image.shape[1]:
            raise ValueError("Expected square 8-bit BGRA images")
        if not 0 < output_size <= image.shape[0] * NETWORK_SCALE:
            raise ValueError("Output must fit within the model's native 4x reconstruction")
    upscaler = Path(upscaler).resolve()
    models = upscaler.parent / "models"
    if not upscaler.is_file() or not all((models / f"{MODEL_NAME}.{extension}").is_file() for extension in ("bin", "param")):
        raise ValueError(f"Missing local Real-ESRGAN executable or {MODEL_NAME} model: {upscaler}")
    with tempfile.TemporaryDirectory(prefix="rimworld-rgba-") as temporary:
        workspace = Path(temporary)
        prepared, enlarged = workspace / "prepared", workspace / "enlarged"
        prepared.mkdir()
        enlarged.mkdir()
        masks = []
        for index, image in enumerate(images):
            colors, alpha = prepare_bgra(image)
            write_png(prepared / f"{index}-color.png", colors)
            # Give the mask an RGB input so the network reconstructs its contours.
            write_png(prepared / f"{index}-alpha.png", cv2.cvtColor(alpha, cv2.COLOR_GRAY2BGR))
            masks.append(alpha)
        print(f"Reconstructing {len(images)} images locally with {MODEL_NAME}...", flush=True)
        result = subprocess.run([str(upscaler), "-i", str(prepared), "-o", str(enlarged),
                                 "-m", str(models), "-n", MODEL_NAME, "-s", str(NETWORK_SCALE)],
                                capture_output=True, text=True, errors="replace")
        if result.returncode:
            raise ValueError(f"Local Real-ESRGAN failed ({result.returncode}):\n{result.stderr[-4000:]}")
        return [reconstruct_bgra(enlarged / f"{index}-color.png", enlarged / f"{index}-alpha.png", alpha, output_size)
                for index, alpha in enumerate(masks)]
