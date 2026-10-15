"""Reference art, preview choices, display names, and visual defaults live here."""

from dataclasses import dataclass
from pathlib import Path
from types import MappingProxyType

from .model import BodyGeometry, Facing, Gender, HeadGeometry, Palette


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = ROOT / "Art/Generated/pawn-cosmetics-sheet.png"
GAME_DATA = Path("C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Data")
HEAD_SHAPES = ("Average_Normal", "Narrow_Normal")
BODY_ATTACHMENT_TYPES = ("Female", "Thin")
DEFAULT_WIDTH = 1920
DEFAULT_PALETTE = Palette(skin=(0.88, 0.77, 0.65, 1.0), hair=(0.42, 0.30, 0.21, 1.0))

# Keep these variants in one gene group, with captions keyed by XML debug label.
GROUPED_VARIANTS = {
    "XylLargeHorns": {"LargeHorns": "Cow", "LargeHorns_Scaleskin": "Dragon"},
}
VARIANT_CAPTIONS = {"BatWings": "Folded", "BatWingsExtended": "Extended",
                    "LizardTail": "Smooth", "LizardTail_Scaleskin": "Scaly"}


def variant_caption(name):
    if name in VARIANT_CAPTIONS:
        return VARIANT_CAPTIONS[name]
    for prefix in ("FacialStripes", "ForeheadMark_", "Trog_"):
        if name.startswith(prefix):
            suffix = name.removeprefix(prefix).replace("_", " ")
            return suffix if len(suffix) == 1 else suffix.lower()
    return name


def _body_defaults(offset, scale, eye_x, eye_east):
    return BodyGeometry(offset, scale, MappingProxyType({
        (Facing.SOUTH, "RightEye", False): (-eye_x, 0, 0.18),
        (Facing.SOUTH, "LeftEye", False): (eye_x, 0, 0.18),
        (Facing.EAST, "RightEye", False): eye_east,
        (Facing.EAST, "RightEye", True): (0.10, 0, 0.18),
    }))


# Fallbacks keep the script usable without an installed game. Core XML overrides them.
BODY_DEFAULTS = {
    "Female": _body_defaults((0.10, 0.34), (1, 1), 0.095, (0.135, 0, 0.18)),
    "Male": _body_defaults((0.04, 0.34), (1, 0.9), 0.095, (0.15, 0, 0.16)),
    "Thin": _body_defaults((0.09, 0.34), (0.6, 1), 0.092, (0.10, 0, 0.18)),
}
HEAD_BASES = {"AverageBase": HeadGeometry((1.5, 1.5), None, False),
              "NarrowBase": HeadGeometry((1.3, 1.5), (0.10, 0, 0.17), True)}
AVERAGE_EYE_OFFSETS = {Gender.FEMALE: (0.13, 0, 0.17), Gender.MALE: (0.18, 0, 0.17)}


@dataclass(frozen=True)
class RenderStyle:
    pixels_per_unit: int = 96
    canvas_size: int = 512
    alpha_cutoff: int = 26


@dataclass(frozen=True)
class SheetStyle:
    background: tuple[int, int, int] = (128, 128, 128)
    view_gap: int = 8
    version_gap: int = 24
    group_gap: int = 60
    row_gap: int = 24
    margin: int = 14
    horizontal_padding: int = 3
    vertical_padding: int = 7
    font_size: int = 14
    label_top: int = 5
    label_leading: int = 5
    label_bottom: int = 3
    fonts: tuple[str, ...] = ("C:/Windows/Fonts/arial.ttf", "DejaVuSans.ttf")

    @property
    def line_height(self):
        return self.font_size + self.label_leading

    def caption_height(self, lines):
        return self.label_top + lines * self.line_height + self.label_bottom
