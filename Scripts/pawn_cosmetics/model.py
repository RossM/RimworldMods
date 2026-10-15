"""Parsed game definitions and fully selected previews; no XML or image operations."""

from dataclasses import dataclass
from enum import Enum, IntEnum
from pathlib import Path
from typing import Mapping


Vec2 = tuple[float, float]
Vec3 = tuple[float, float, float]
Color = tuple[float, float, float, float]


class Gender(Enum):
    FEMALE = "Female"
    MALE = "Male"


class Facing(IntEnum):
    NORTH = 0
    EAST = 1
    SOUTH = 2
    WEST = 3

    @property
    def xml_name(self):
        return self.name.capitalize()


OUTPUT_FACINGS = (Facing.SOUTH, Facing.NORTH, Facing.EAST)


class Part(Enum):
    BODY = "Body"
    HEAD = "Head"


class GraphicKind(Enum):
    PLANE = "plane"
    HEAD_ATTACHMENT = "head attachment"
    EYE = "eye"
    PATTERN = "pattern"


class ColorSource(Enum):
    SKIN = "Skin"
    HAIR = "Hair"


class MeshSource(Enum):
    HAIR = "Hair"


class Side(Enum):
    CENTER = "Center"
    LEFT = "Left"
    RIGHT = "Right"


@dataclass(frozen=True)
class Palette:
    skin: Color
    hair: Color


@dataclass(frozen=True)
class Draw:
    offset: Vec3 = (0, 0, 0)
    layer: float = 0
    flip: bool = False
    rotation: float = 0


@dataclass(frozen=True)
class Textures:
    female: tuple[Path, ...]
    male: tuple[Path, ...]

    def __post_init__(self):
        if not self.female or len(self.female) != len(self.male):
            raise ValueError("A render node needs matching, nonempty female/male texture variants")

    @property
    def sexed(self):
        return self.female != self.male

    def for_gender(self, gender):
        return self.female if gender == Gender.FEMALE else self.male


@dataclass(frozen=True)
class RenderNode:
    key: tuple[str, int]
    debug_label: str
    textures: Textures
    parent: Part
    kind: GraphicKind
    tint: Color | ColorSource
    draws: tuple[Draw, Draw, Draw, Draw]
    visible: frozenset[Facing]
    mesh: Vec2 | MeshSource
    draw_size: Vec2
    scale_mesh_to_body: bool
    scale_offset_to_body: bool
    narrow_offset: float
    side: Side
    eye_anchor: str | None
    requires: tuple[str, ...]
    forbids: tuple[str, ...]

    @property
    def conditional(self):
        return bool(self.requires or self.forbids)

    @property
    def body_attachment(self):
        return self.parent == Part.BODY and self.scale_mesh_to_body


@dataclass(frozen=True)
class Graphic:
    node: RenderNode
    variant: int = 0

    def __post_init__(self):
        if not 0 <= self.variant < len(self.node.textures.female):
            raise ValueError(f"Invalid texture variant for {self.node.key}")

    def texture(self, gender):
        return self.node.textures.for_gender(gender)[self.variant]


@dataclass(frozen=True)
class Gene:
    name: str
    label: str
    nodes: tuple[RenderNode, ...]
    forced_heads: tuple[str, ...]
    skin: Color | None


@dataclass(frozen=True)
class BodyGeometry:
    head_offset: Vec2
    graphic_scale: Vec2
    eye_anchors: Mapping[tuple[Facing, str, bool], Vec3]

    @property
    def attachment_scale(self):
        return sum(self.graphic_scale) / 2


@dataclass(frozen=True)
class HeadGeometry:
    hair_mesh: Vec2
    eye_east: Vec3 | None
    narrow: bool


@dataclass(frozen=True)
class Body:
    name: str
    texture: Path
    geometry: BodyGeometry


@dataclass(frozen=True)
class Head:
    name: str
    gender: Gender
    texture: Path
    geometry: HeadGeometry


@dataclass(frozen=True)
class Pawn:
    body: Body
    head: Head
    palette: Palette

    @property
    def gender(self):
        return self.head.gender


@dataclass(frozen=True)
class Preview:
    label: str
    pawn: Pawn
    graphics: tuple[Graphic, ...] = ()


@dataclass(frozen=True)
class Group:
    gene: str
    label: str
    previews: tuple[Preview, ...]
    skin_color: bool = False

    def __post_init__(self):
        if not self.previews:
            raise ValueError("A cosmetic group needs at least one preview")
