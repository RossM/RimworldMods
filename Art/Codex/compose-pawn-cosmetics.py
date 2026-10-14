"""Render pawn cosmetics from XylXenos.Data. Requires Python 3.10+ / Pillow.

Run from any directory: python path/to/Art/Codex/compose-pawn-cosmetics.py
Each labeled triplet is south, north, east; the default output is beside this script.
Use --width PIXELS to set the sheet width (default: 2400). Complete cosmetic
groups wrap into rows in catalog order, filling each row before starting another.
Includes male/female humanlike texture variants, XML skin colors, and
the unused ribs art (explicitly labeled). Replacement heads/skin replace the
reference part; the scaly lizard tail includes its required scaleskin. Wings and
tails include female and thin bodies. Other shared textures stay female-only.
Head attachments and patterns are grouped with both
Female_Average_Normal and Female_Narrow_Normal previews. Distinct male textures
are included for replacement head types and yellow eyes, on a male body.
Yellow eyes includes average/narrow previews for each gender. Bat-wing and lizard-tail
graphics are paired the same way. Skin color genes appear together at the end.

This is an art preview, without game lighting, random brightness, or animation.
XML controls tint, mesh size, offsets, layers, facing visibility, and eye sides.
Missing directions follow Graphic_Multi's texture fallback rules. Pattern nodes
are clipped to the reference silhouette; complex masks preserve untinted teeth.
Adult geometry defaults match RimWorld 1.6. If installed, Core XML supplies body
scales, head offsets, and eye anchors; --rimworld-data can select another installation.
"""

import argparse
from dataclasses import dataclass, replace
from functools import lru_cache
from itertools import product
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "XylXenos.Data"
TEXTURES = DATA / "Textures"
HUMANLIKE = TEXTURES / "Xyl/Things/Pawn/Humanlike"
ART = ROOT / "Art/Rimworld art"
BODY = ART / "Bodies/Naked_Female"
HEAD = ART / "Heads/Female_Average_Normal"
NARROW_HEAD = ART / "Heads/Female_Narrow_Normal"
MALE_BODY = ART / "Bodies/Naked_Male"
MALE_HEAD = ART / "Heads/Male_Average_Normal"
MALE_NARROW_HEAD = ART / "Heads/Male_Narrow_Normal"
THIN_BODY = ART / "Bodies/Naked_Thin"
GAME_DATA = Path("C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Data")
FACINGS = ("south", "north", "east")
SKIN = (0.88, 0.77, 0.65, 1.0)
HAIR = (0.42, 0.30, 0.21, 1.0)
BACKGROUND = (128, 128, 128)
PIXELS_PER_UNIT = 96
WORK_SIZE = 512
CENTER = WORK_SIZE // 2
SHEET_WIDTH = 2400


@dataclass(frozen=True)
class Attachment:
    properties: ET.Element
    texture: Path


@dataclass(frozen=True)
class Cosmetic:
    label: str
    attachments: tuple[Attachment, ...] = ()
    body: Path = BODY
    head: Path = HEAD
    skin: tuple[float, ...] = SKIN


@dataclass(frozen=True)
class CosmeticGroup:
    label: str
    previews: tuple[Cosmetic, ...]


@dataclass(frozen=True)
class HeadGeometry:
    hair_mesh: tuple[float, ...]
    eye_east: tuple[float, ...] | None
    narrow: bool


@dataclass(frozen=True)
class BodyGeometry:
    head_offset: tuple[float, ...]
    graphic_scale: tuple[float, ...]
    eye_anchors: dict[tuple[str, str, bool], tuple[float, ...]]

    @property
    def attachment_scale(self):
        return sum(self.graphic_scale) / 2


@dataclass(frozen=True)
class Geometry:
    bodies: dict[Path, BodyGeometry]
    heads: dict[Path, HeadGeometry]


@dataclass(frozen=True)
class ReferenceArt:
    body: Path
    average_head: Path
    narrow_head: Path


REFERENCES = {"Female": ReferenceArt(BODY, HEAD, NARROW_HEAD),
              "Male": ReferenceArt(MALE_BODY, MALE_HEAD, MALE_NARROW_HEAD)}
BODY_TYPES = {gender: reference.body for gender, reference in REFERENCES.items()} | {"Thin": THIN_BODY}


def vector(text):
    return tuple(float(value.strip()) for value in text.strip("() ").split(","))


def size(text):
    values = vector(text)
    if len(values) == 1:
        values *= 2
    if len(values) != 2 or min(values) <= 0:
        raise ValueError(f"Invalid mesh/draw size: {text}")
    return values


def color(text):
    values = vector(text)
    if any(value > 1 for value in values):
        values = tuple(value / 255 for value in values)
    if len(values) not in (3, 4) or not all(0 <= value <= 1 for value in values):
        raise ValueError(f"Invalid XML color: {text}")
    return values + (1.0,) if len(values) == 3 else values


def definitions(directory, tag):
    return [node for path in sorted(directory.rglob("*.xml"))
            for node in ET.parse(path).getroot().findall(tag)]


def inherited_text(node, field, parents, default=None):
    value = node.findtext(field)
    if value is not None:
        return value
    parent = parents.get(node.get("ParentName"))
    return inherited_text(parent, field, parents, default) if parent is not None else default


def geometry(game_data):
    body_geometry, head_geometry = {}, {}
    for gender, reference in REFERENCES.items():
        body_geometry[reference.body] = BodyGeometry((0.10 if gender == "Female" else 0.04, 0.34),
                (1, 1) if gender == "Female" else (1, 0.9), {
            ("south", "RightEye", False): (-0.095, 0, 0.18),
            ("south", "LeftEye", False): (0.095, 0, 0.18),
            ("east", "RightEye", False): (0.135, 0, 0.18) if gender == "Female" else (0.15, 0, 0.16),
            ("east", "RightEye", True): (0.10, 0, 0.18),
        })
        head_geometry[reference.average_head] = HeadGeometry((1.5, 1.5),
                (0.13 if gender == "Female" else 0.18, 0, 0.17), False)
        head_geometry[reference.narrow_head] = HeadGeometry((1.3, 1.5), (0.10, 0, 0.17), True)
    body_geometry[THIN_BODY] = BodyGeometry((0.09, 0.34), (0.6, 1), {
        ("south", "RightEye", False): (-0.092, 0, 0.18),
        ("south", "LeftEye", False): (0.092, 0, 0.18),
        ("east", "RightEye", False): (0.10, 0, 0.18),
        ("east", "RightEye", True): (0.10, 0, 0.18),
    })
    # Core parents are also needed to resolve the mod's replacement head types.
    parents = {"AverageBase": ET.fromstring("<HeadTypeDef><hairMeshSize>(1.5,1.5)</hairMeshSize></HeadTypeDef>"),
               "NarrowBase": ET.fromstring("""<HeadTypeDef><hairMeshSize>(1.3,1.5)</hairMeshSize>
                   <eyeOffsetEastWest>(0.10,0,0.17)</eyeOffsetEastWest><narrow>true</narrow></HeadTypeDef>""")}
    mod_heads = definitions(DATA / "Defs/HeadTypeDefs", "HeadTypeDef")
    core_heads = []
    if game_data.is_dir():
        core_bodies = definitions(game_data / "Core/Defs/Misc/BodyTypeDefs", "BodyTypeDef")
        core_heads = definitions(game_data / "Core/Defs/HeadTypeDefs", "HeadTypeDef")
        for body_type, path in BODY_TYPES.items():
            body = next(node for node in core_bodies if node.findtext("defName") == body_type)
            anchors = {(node.findtext("rotation").lower(), node.findtext("tag"),
                        node.findtext("narrowCrown", "false").lower() == "true"): vector(node.findtext("offset"))
                       for node in body.findall("woundAnchors/li")
                       if node.findtext("rotation", "").lower() in FACINGS
                       and node.findtext("tag") in ("RightEye", "LeftEye")}
            body_geometry[path] = BodyGeometry(vector(body.findtext("headOffset")),
                                               size(body.findtext("bodyGraphicScale")), anchors)
    parents.update({node.get("Name"): node for node in core_heads + mod_heads if node.get("Name")})
    for node in core_heads + mod_heads:
        graphic = node.findtext("graphicPath")
        if not graphic:
            continue
        path = TEXTURES / graphic if graphic.startswith("Xyl/") else ART / "Heads" / Path(graphic).name
        eye_offset = inherited_text(node, "eyeOffsetEastWest", parents)
        head_geometry[path] = HeadGeometry(size(inherited_text(node, "hairMeshSize", parents, "(1.5,1.5)")),
                vector(eye_offset) if eye_offset is not None else None,
                inherited_text(node, "narrow", parents, "false").lower() == "true")
    for gene in definitions(DATA / "Defs/GeneDefs", "GeneDef"):
        for body_type, path in BODY_TYPES.items():
            graphic = gene.findtext(f".//bodyTypeGraphicPaths/{body_type}")
            if graphic:
                body_geometry[TEXTURES / graphic] = body_geometry[path]
    return Geometry(body_geometry, head_geometry)


def attachment_paths(node, gender="Female"):
    single = (node.findtext("texPathFemale") if gender == "Female" else None) or node.findtext("texPath")
    multiple = (node.findall("texPathsFemale/li") if gender == "Female" else []) or node.findall("texPaths/li")
    paths = [single] if single else [item.text for item in multiple]
    if not paths:
        raise ValueError(f"No texture in render node: {ET.tostring(node, encoding='unicode')}")
    return [TEXTURES / path.strip() for path in paths]


def variant_label(name):
    special = {"BatWings": "folded", "BatWingsExtended": "extended",
               "ReptileTail": "smooth", "ReptileTail_Scaleskin": "scaly"}
    if name in special:
        return special[name]
    for prefix in ("FacialStripes", "ForeheadMark_", "Scaleskin_", "Trog_"):
        if name.startswith(prefix):
            suffix = name.removeprefix(prefix).replace("_", " ")
            return suffix if len(suffix) == 1 else suffix.lower()
    return name


def is_body_attachment(node):
    return node.findtext("parentTagDef") == "Body" and (
        node.findtext("workerClass") == "PawnRenderNodeWorker_AttachmentBody"
        or (node.get("Class") == "XylXenos.PawnRenderNodeProperties_GeneDependent"
            and node.findtext("useBodyGraphicScale", "false").lower() == "true"))


def catalog():
    cosmetics = []
    color_cosmetics = []
    variant_groups = []
    genes = definitions(DATA / "Defs/GeneDefs", "GeneDef")
    heads = {node.findtext("defName"): node for node in
             definitions(DATA / "Defs/HeadTypeDefs", "HeadTypeDef") if node.findtext("defName")}
    for gene in genes:
        label = gene.findtext("label", gene.findtext("defName", "")).capitalize()
        destination = color_cosmetics if gene.find("skinColorOverride") is not None else cosmetics
        nodes = gene.findall("renderNodeProperties/li")
        # Gene-dependent render nodes are mutually exclusive variants, not a stack.
        conditional = [node for node in nodes if node.get("Class") == "XylXenos.PawnRenderNodeProperties_GeneDependent"
                       and (node.find("requiredGenes") is not None or node.find("disallowedGenes") is not None)]
        shared = [node for node in nodes if node not in conditional]
        node_sets = [shared + [node] for node in conditional] if conditional else [shared]
        graphic_previews = []
        for node_set in node_sets:
            if not node_set:
                continue
            choices = [[Attachment(node, path) for path in attachment_paths(node)] for node in node_set]
            for attachments in product(*choices):
                variants = [variant_label(attachment.texture.name) for attachment, options in zip(attachments, choices)
                            if len(options) > 1 or conditional]
                suffix = f" ({', '.join(variants)})" if variants else ""
                body, head = BODY, HEAD
                required_genes = {item.text for attachment in attachments
                                  for item in attachment.properties.findall("requiredGenes/li")}
                if "XylScaleskin" in required_genes:
                    scaleskin = next(item for item in genes if item.findtext("defName") == "XylScaleskin")
                    body = TEXTURES / scaleskin.findtext(".//bodyTypeGraphicPaths/Female")
                    head = TEXTURES / heads["Female_Scaleskin_Average_Normal"].findtext("graphicPath")
                graphic_previews.append(Cosmetic(label + suffix, attachments, body=body, head=head))
        separate_gender_textures = any(attachment_paths(node, "Female") != attachment_paths(node, "Male")
                                      for node in nodes)
        if separate_gender_textures:
            for cosmetic in graphic_previews:
                previews = []
                for gender, reference in REFERENCES.items():
                    attachments = []
                    for attachment in cosmetic.attachments:
                        female_paths = attachment_paths(attachment.properties, "Female")
                        gender_paths = attachment_paths(attachment.properties, gender)
                        if len(female_paths) != len(gender_paths):
                            raise ValueError(f"Mismatched male/female texture variants for {label}")
                        attachments.append(Attachment(attachment.properties,
                            gender_paths[female_paths.index(attachment.texture)]))
                    for shape, head in (("Average normal", reference.average_head), ("Narrow normal", reference.narrow_head)):
                        previews.append(replace(cosmetic, label=f"{gender} {shape.lower()}",
                                                attachments=tuple(attachments), body=reference.body, head=head))
                variant_groups.append(CosmeticGroup(cosmetic.label, tuple(previews)))
        # A pair of body graphics shares one card, like the paired head previews.
        elif len(graphic_previews) == 2 and all(len(preview.attachments) == 1
                and preview.attachments[0].properties.findtext("parentTagDef") == "Body"
                for preview in graphic_previews):
            variant_groups.append(CosmeticGroup(label, tuple(
                replace(preview, label=variant_label(preview.attachments[0].texture.name).capitalize())
                for preview in graphic_previews)))
        else:
            destination.extend(graphic_previews)
        forced_heads = [heads[item.text] for item in gene.findall("forcedHeadTypes/li")
                        if item.text in heads and heads[item.text].findtext("gender") in REFERENCES]
        replacements = {}
        for head in forced_heads:
            gender = head.findtext("gender")
            body_path = gene.findtext(f".//bodyTypeGraphicPaths/{gender}")
            head_path = TEXTURES / head.findtext("graphicPath")
            variant = variant_label(head_path.name.removeprefix(gender + "_"))
            replacements.setdefault(f"{label} ({variant})", []).append(Cosmetic(gender,
                body=TEXTURES / body_path if body_path else REFERENCES[gender].body, head=head_path))
        for replacement_label, previews in replacements.items():
            variant_groups.append(CosmeticGroup(replacement_label,
                tuple(sorted(previews, key=lambda preview: preview.label))))
        skin = gene.findtext("skinColorOverride")
        if skin:
            destination.append(Cosmetic(label, skin=color(skin)))

    # This asset is present, but its GeneDef is commented out. Preview it explicitly.
    ribs = ET.fromstring("""<li><nodeClass>XylXenos.PawnRenderNode_BodyPattern</nodeClass>
        <parentTagDef>Body</parentTagDef><baseLayer>3</baseLayer>
        <visibleFacing><li>South</li><li>East</li><li>West</li></visibleFacing></li>""")
    cosmetics.append(Cosmetic("Ribs (unused)", (Attachment(ribs, HUMANLIKE / "BodyAttachments/Ribs/Ribs"),)))
    groups = variant_groups
    for cosmetic in cosmetics:
        affects_head = any(attachment.properties.findtext("parentTagDef") == "Head"
                           for attachment in cosmetic.attachments)
        previews = (cosmetic,)
        if affects_head and cosmetic.head == HEAD:
            previews = (replace(cosmetic, label="Average normal"),
                        replace(cosmetic, label="Narrow normal", head=NARROW_HEAD))
        groups.append(CosmeticGroup(cosmetic.label, previews))
    thin_bodies = {BODY: THIN_BODY}
    for gene in genes:
        female = gene.findtext(".//bodyTypeGraphicPaths/Female")
        thin = gene.findtext(".//bodyTypeGraphicPaths/Thin")
        if female and thin:
            thin_bodies[TEXTURES / female] = TEXTURES / thin
    for index, group in enumerate(groups):
        if all(preview.attachments and all(is_body_attachment(attachment.properties)
               for attachment in preview.attachments) for preview in group.previews):
            previews = []
            for preview in group.previews:
                prefix = preview.label + " " if len(group.previews) > 1 else ""
                previews.extend((replace(preview, label=prefix + "Female"),
                                 replace(preview, label=prefix + "Thin", body=thin_bodies[preview.body])))
            groups[index] = CosmeticGroup(group.label, tuple(previews))
    return (sorted(groups, key=lambda group: group.label.casefold())
            + [CosmeticGroup(cosmetic.label, (cosmetic,))
               for cosmetic in sorted(color_cosmetics, key=lambda cosmetic: cosmetic.label.casefold())])


@lru_cache(maxsize=None)
def directional_path(base, facing):
    paths = {direction: Path(f"{base}_{direction}.png") for direction in ("north", "east", "south", "west")}
    paths = {direction: path for direction, path in paths.items() if path.is_file()}
    north = next((paths[direction] for direction in ("north", "south", "east", "west") if direction in paths), None)
    if north is None:
        raise FileNotFoundError(f"No directional art for {base}")
    return paths.get(facing, paths.get("west", north) if facing == "east" else north)


@lru_cache(maxsize=None)
def load_image(path):
    with Image.open(path) as source:
        image = source.convert("RGBA")
    # Cutout shaders discard this near-transparent residue (notably DemonTail).
    image.putalpha(image.getchannel("A").point(lambda alpha: 0 if alpha < 26 else alpha))
    return image


def tint(image, rgba, mask=None):
    channels = image.split()
    output = []
    for channel, factor in zip(channels, rgba):
        multiplied = channel.point(lambda value: round(value * factor))
        output.append(Image.composite(multiplied, channel, mask) if mask is not None else multiplied)
    return Image.merge("RGBA", output)


def draw_value(node, facing, field, default):
    value = node.findtext(f"drawData/data{facing.capitalize()}/{field}")
    if value is None:
        value = node.findtext(f"drawData/defaultData/{field}", default)
    return value


def layer(image, mesh, offset):
    image = image.resize(tuple(round(value * PIXELS_PER_UNIT) for value in mesh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (WORK_SIZE, WORK_SIZE))
    position = (round(CENTER + offset[0] * PIXELS_PER_UNIT - image.width / 2),
                round(CENTER - offset[2] * PIXELS_PER_UNIT - image.height / 2))
    if min(position) < 0 or position[0] + image.width > WORK_SIZE or position[1] + image.height > WORK_SIZE:
        raise ValueError("Layer exceeds working canvas; increase WORK_SIZE")
    canvas.alpha_composite(image, position)
    return canvas


def render(cosmetic, facing, dimensions):
    body_geometry = dimensions.bodies[cosmetic.body]
    head_geometry = dimensions.heads[cosmetic.head]
    narrow = head_geometry.narrow
    head_offset = (body_geometry.head_offset[0] if facing == "east" else 0, 0, body_geometry.head_offset[1])
    body = layer(tint(load_image(directional_path(cosmetic.body, facing)), cosmetic.skin), (1.5, 1.5), (0, 0, 0))
    head = layer(tint(load_image(directional_path(cosmetic.head, facing)), cosmetic.skin), (1.5, 1.5), head_offset)
    layers = [(0.0, body), (50.0, head)]
    for attachment in cosmetic.attachments:
        node = attachment.properties
        eye = node.get("Class") == "PawnRenderNodeProperties_Eye"
        visible = [item.text.lower() for item in node.findall("visibleFacing/li")]
        if (visible and facing not in visible) or (eye and facing == "north"):
            continue
        if facing == "east" and node.findtext("side") == "Left":
            continue
        parent_head = node.findtext("parentTagDef") == "Head"
        offset = vector(draw_value(node, facing, "offset", "(0,0,0)"))
        if node.findtext("drawData/scaleOffsetByBodySize", "false").lower() == "true":
            offset = tuple(value * body_geometry.attachment_scale for value in offset)
        origin = head_offset if parent_head else (0, 0, 0)
        if eye:
            anchor = body_geometry.eye_anchors.get((facing, node.findtext("anchorTag"), narrow if facing == "east" else False))
            if anchor is not None:
                if facing == "east" and head_geometry.eye_east is not None:
                    anchor = head_geometry.eye_east
                offset = tuple(a + b for a, b in zip(offset, anchor))
        offset = tuple(a + b for a, b in zip(offset, origin))
        # Only these workers apply narrowCrownHorizontalOffset in the game.
        adjusts_narrow = (node.get("Class") == "XylXenos.PawnRenderNodeProperties_GeneDependent"
                          or node.findtext("workerClass") == "PawnRenderNodeWorker_Head")
        if narrow and facing == "east" and adjusts_narrow:
            adjustment = float(node.findtext("narrowCrownHorizontalOffset", "0"))
            offset = (offset[0] - adjustment, offset[1], offset[2] - adjustment)
        node_class = node.findtext("nodeClass", "")
        if eye or "PawnRenderNode_AttachmentHead" in node_class:
            mesh = head_geometry.hair_mesh
        else:
            mesh = size(node.findtext("overrideMeshSize", "(1.5,1.5)"))
        draw_size = size(node.findtext("drawSize", "1"))
        mesh = tuple(a * b for a, b in zip(mesh, draw_size))
        if is_body_attachment(node):
            mesh = tuple(value * body_geometry.attachment_scale for value in mesh)
        path = directional_path(attachment.texture, facing)
        image = load_image(path)
        tint_color = {"Skin": cosmetic.skin, "Hair": HAIR}.get(node.findtext("colorType"),
                         color(node.findtext("color", "(1,1,1)")))
        mask_path = path.with_name(path.stem + "m.png")
        mask = load_image(mask_path).getchannel("R") if mask_path.is_file() else None
        image = tint(image, tint_color, mask)
        if draw_value(node, facing, "flip", "false").lower() == "true":
            image = ImageOps.mirror(image)
        overlay = layer(image, mesh, offset)
        if node.findtext("nodeClass", "").endswith("Pattern"):
            silhouette = head if parent_head else body
            overlay.putalpha(ImageChops.multiply(overlay.getchannel("A"), silhouette.getchannel("A")))
        depth = float(draw_value(node, facing, "layer", node.findtext("baseLayer", "0")))
        layers.append((depth, overlay))
    composite = Image.new("RGBA", body.size)
    for _, image in sorted(layers, key=lambda item: item[0]):
        composite = Image.alpha_composite(composite, image)
    return composite


def verify_coverage(groups):
    cosmetics = [preview for group in groups for preview in group.previews]
    used = {path for cosmetic in cosmetics for path in (cosmetic.body, cosmetic.head)}
    used.update(attachment.texture for cosmetic in cosmetics for attachment in cosmetic.attachments)
    relevant = [*HUMANLIKE.glob("HeadAttachments/**/*.png"), *HUMANLIKE.glob("BodyAttachments/**/*.png"),
                *HUMANLIKE.glob("Heads/**/*.png"),
                *HUMANLIKE.glob("Bodies/Scaleskin_Female_*.png"), *HUMANLIKE.glob("Bodies/Scaleskin_Male_*.png"),
                *HUMANLIKE.glob("Bodies/Scaleskin_Thin_*.png")]
    missing = []
    for path in relevant:
        if path.stem.endswith("m"):
            continue
        base = path.with_name(path.stem.rsplit("_", 1)[0])
        if base not in used:
            missing.append(str(path.relative_to(ROOT)))
    if missing:
        raise ValueError("Humanlike male/female art omitted from catalog:\n" + "\n".join(missing))


def label_font():
    for filename in ("C:/Windows/Fonts/arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(filename, 14)
        except OSError:
            continue
    return ImageFont.load_default()


def pack_rows(widths, available_width, gap):
    rows = []
    start, used_width = 0, 0
    for index, width in enumerate(widths):
        if width > available_width:
            raise ValueError(f"Group width {width} exceeds available row width {available_width}")
        required_width = width + (gap if index > start else 0)
        if used_width + required_width > available_width:
            rows.append(range(start, index))
            start, used_width = index, width
        else:
            used_width += required_width
    if widths:
        rows.append(range(start, len(widths)))
    return rows


def make_sheet(groups, dimensions, sheet_width):
    rendered = [[[render(preview, facing, dimensions) for facing in FACINGS]
                 for preview in group.previews] for group in groups]
    boxes = [[[image.getchannel("A").getbbox() for image in views] for views in previews]
             for previews in rendered]
    top_edge = min(box[1] for previews in boxes for triplet in previews for box in triplet) - 7
    view_gap, version_gap, group_gap, row_gap = 8, 24, 60, 24
    padding, margin = 3, 14
    # Keep rotations close and cosmetic versions together, without changing scale.
    triplet_widths = [[sum(box[2] - box[0] + 2 * padding for box in triplet) + 2 * view_gap
                       for triplet in previews] for previews in boxes]
    group_widths = [sum(widths) + (len(widths) - 1) * version_gap for widths in triplet_widths]
    font = label_font()
    measuring_draw = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    card_widths = [max(width, round(measuring_draw.textlength(group.label, font=font)))
                   for group, width in zip(groups, group_widths)]
    available_width = sheet_width - 2 * margin
    widest_index = max(range(len(groups)), key=lambda index: card_widths[index])
    minimum_width = card_widths[widest_index] + 2 * margin
    if sheet_width < minimum_width:
        raise ValueError(f"--width must be at least {minimum_width} pixels to fit {groups[widest_index].label!r}")
    rows = pack_rows(card_widths, available_width, group_gap)
    row_widths = [sum(card_widths[index] for index in row) + (len(row) - 1) * group_gap for row in rows]
    # Only the row containing the beaver tail needs its extra vertical space.
    bottom_edges = [max(box[3] for index in row for triplet in boxes[index] for box in triplet) + 7
                    for row in rows]
    label_heights = [46 if any(len(groups[index].previews) > 1 for index in row) else 27 for row in rows]
    row_heights = [bottom - top_edge + label_height for bottom, label_height in zip(bottom_edges, label_heights)]
    sheet = Image.new("RGB", (sheet_width,
                              2 * margin + sum(row_heights) + (len(rows) - 1) * row_gap), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    top = margin
    for row, indices in enumerate(rows):
        left = margin + (available_width - row_widths[row]) // 2
        view_height = bottom_edges[row] - top_edge
        for index in indices:
            group, previews = groups[index], rendered[index]
            card_width = card_widths[index]
            x = left + (card_width - group_widths[index]) // 2
            for preview, views, triplet, width in zip(group.previews, previews, boxes[index], triplet_widths[index]):
                version_left = x
                for image, box in zip(views, triplet):
                    crop = image.crop((box[0] - padding, top_edge, box[2] + padding, bottom_edges[row]))
                    sheet.paste(crop, (x, top), crop)
                    x += crop.width + view_gap
                if len(group.previews) > 1:
                    draw.text((version_left + width / 2, top + view_height + 5), preview.label,
                              fill=(40, 40, 40), font=font, anchor="mt")
                x = version_left + width + version_gap
            if draw.textlength(group.label, font=font) > card_width:
                raise ValueError(f"Label too wide: {group.label}")
            caption_offset = 24 if label_heights[row] == 46 else 5
            draw.text((left + card_width / 2, top + view_height + caption_offset), group.label,
                      fill=(24, 24, 24), font=font, anchor="mt")
            left += card_width + group_gap
        top += row_heights[row] + row_gap
    return sheet


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("pawn-cosmetics-sheet.png"))
    parser.add_argument("--width", type=int, default=SHEET_WIDTH, help="Fixed sheet width in pixels (default: %(default)s)")
    parser.add_argument("--rimworld-data", type=Path, default=GAME_DATA)
    args = parser.parse_args()
    if args.width < 1:
        parser.error("--width must be positive")
    if args.rimworld_data != GAME_DATA and not args.rimworld_data.is_dir():
        parser.error("--rimworld-data must be an existing RimWorld/Data directory")
    groups = catalog()
    verify_coverage(groups)
    try:
        sheet = make_sheet(groups, geometry(args.rimworld_data), args.width)
    except ValueError as error:
        parser.error(str(error))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.output)
    # Reopen the saved artifact to catch truncated/invalid output immediately.
    with Image.open(args.output) as saved:
        saved.verify()
    preview_count = sum(len(group.previews) for group in groups)
    print(f"Saved {args.output.resolve()}: {sheet.width} x {sheet.height}; "
          f"{len(groups)} cosmetic groups, {preview_count} head/body previews, "
          f"{preview_count * 3} views (south, north, east)")


if __name__ == "__main__":
    main()
