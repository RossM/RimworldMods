"""Render female pawn cosmetics from XylXenos.Data. Requires Python 3.10+ / Pillow.

Run from any directory: python path/to/Art/Codex/compose-pawn-cosmetics.py
Each labeled triplet is south, north, east; the default output is beside this script.
Includes all female-compatible humanlike texture variants, XML skin colors, and
the unused ribs art (explicitly labeled). Replacement heads/skin replace the
reference part; the scaly lizard tail includes its required scaleskin. Other
cosmetics use Naked_Female. Head attachments and patterns are grouped with both
Female_Average_Normal and Female_Narrow_Normal previews. Skin color genes appear
together at the end of the sheet.

This is an art preview, without game lighting, random brightness, or animation.
XML controls tint, mesh size, offsets, layers, facing visibility, and eye sides.
Missing directions follow Graphic_Multi's texture fallback rules. Pattern nodes
are clipped to the reference silhouette; complex masks preserve untinted teeth.
Adult female geometry defaults match RimWorld 1.6. If installed, Core XML supplies
head offsets and eye anchors; --rimworld-data can select another installation.
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
GAME_DATA = Path("C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Data")
FACINGS = ("south", "north", "east")
WHITE = (1.0, 1.0, 1.0, 1.0)
SKIN = (0.88, 0.77, 0.65, 1.0)
HAIR = (0.42, 0.30, 0.21, 1.0)
BACKGROUND = (128, 128, 128)
PIXELS_PER_UNIT = 96
WORK_SIZE = 512
CENTER = WORK_SIZE // 2


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
    eye_east: tuple[float, ...]


@dataclass(frozen=True)
class Geometry:
    head_offset: tuple[float, ...]
    eye_south_right: tuple[float, ...]
    eye_south_left: tuple[float, ...]
    average_head: HeadGeometry
    narrow_head: HeadGeometry


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


def geometry(game_data):
    fallback = Geometry((0.10, 0.34), (-0.095, 0, 0.18),
                        (0.095, 0, 0.18), HeadGeometry((1.5, 1.5), (0.13, 0, 0.17)),
                        HeadGeometry((1.3, 1.5), (0.10, 0, 0.17)))
    if not game_data.is_dir():
        return fallback
    bodies = definitions(game_data / "Core/Defs/Misc/BodyTypeDefs", "BodyTypeDef")
    heads = definitions(game_data / "Core/Defs/HeadTypeDefs", "HeadTypeDef")
    body = next(node for node in bodies if node.findtext("defName") == "Female")
    head = next(node for node in heads if node.findtext("graphicPath", "").endswith("/Female_Average_Normal"))
    average_base = next(node for node in heads if node.get("Name") == "AverageBase")
    narrow_base = next(node for node in heads if node.get("Name") == "NarrowBase")
    anchors = {node.findtext("tag"): vector(node.findtext("offset"))
               for node in body.findall("woundAnchors/li")
               if node.findtext("rotation") == "South" and node.findtext("tag") in ("RightEye", "LeftEye")}
    return Geometry(vector(body.findtext("headOffset")), anchors["RightEye"],
                    anchors["LeftEye"],
                    HeadGeometry(size(average_base.findtext("hairMeshSize")), vector(head.findtext("eyeOffsetEastWest"))),
                    HeadGeometry(size(narrow_base.findtext("hairMeshSize")), vector(narrow_base.findtext("eyeOffsetEastWest"))))


def attachment_paths(node):
    single = node.findtext("texPathFemale") or node.findtext("texPath")
    multiple = node.findall("texPathsFemale/li") or node.findall("texPaths/li")
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


def catalog():
    cosmetics = []
    color_cosmetics = []
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
                destination.append(Cosmetic(label + suffix, attachments, body=body, head=head))
        forced_heads = [heads[item.text] for item in gene.findall("forcedHeadTypes/li")
                        if item.text in heads and heads[item.text].findtext("gender") == "Female"]
        body_path = gene.findtext(".//bodyTypeGraphicPaths/Female")
        for head in forced_heads:
            head_path = TEXTURES / head.findtext("graphicPath")
            variant = variant_label(head_path.name.removeprefix("Female_"))
            destination.append(Cosmetic(f"{label} ({variant})", body=TEXTURES / body_path if body_path else BODY,
                                       head=head_path))
        skin = gene.findtext("skinColorOverride")
        if skin:
            destination.append(Cosmetic(label, skin=color(skin)))

    # This asset is present, but its GeneDef is commented out. Preview it explicitly.
    ribs = ET.fromstring("""<li><nodeClass>XylXenos.PawnRenderNode_BodyPattern</nodeClass>
        <parentTagDef>Body</parentTagDef><baseLayer>3</baseLayer>
        <visibleFacing><li>South</li><li>East</li><li>West</li></visibleFacing></li>""")
    cosmetics.append(Cosmetic("Ribs (unused)", (Attachment(ribs, HUMANLIKE / "BodyAttachments/Ribs/Ribs"),)))
    ordered = (sorted(cosmetics, key=lambda cosmetic: cosmetic.label.casefold())
               + sorted(color_cosmetics, key=lambda cosmetic: cosmetic.label.casefold()))
    groups = []
    for cosmetic in ordered:
        affects_head = any(attachment.properties.findtext("parentTagDef") == "Head"
                           for attachment in cosmetic.attachments)
        previews = (cosmetic,)
        if affects_head and cosmetic.head == HEAD:
            previews = (replace(cosmetic, label="Average normal"),
                        replace(cosmetic, label="Narrow normal", head=NARROW_HEAD))
        groups.append(CosmeticGroup(cosmetic.label, previews))
    return groups


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
    narrow = cosmetic.head == NARROW_HEAD
    head_geometry = dimensions.narrow_head if narrow else dimensions.average_head
    head_offset = (dimensions.head_offset[0] if facing == "east" else 0, 0, dimensions.head_offset[1])
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
        origin = head_offset if parent_head else (0, 0, 0)
        if eye:
            anchor = head_geometry.eye_east if facing == "east" else (
                dimensions.eye_south_right if node.findtext("side") == "Right" else dimensions.eye_south_left)
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
                *HUMANLIKE.glob("Heads/Female/*.png"), *HUMANLIKE.glob("Bodies/Scaleskin_Female_*.png")]
    missing = []
    for path in relevant:
        if "Male" in path.parts or path.stem.endswith("m"):
            continue
        base = path.with_name(path.stem.rsplit("_", 1)[0])
        if base not in used:
            missing.append(str(path.relative_to(ROOT)))
    if missing:
        raise ValueError("Female-compatible art omitted from catalog:\n" + "\n".join(missing))


def label_font():
    for filename in ("C:/Windows/Fonts/arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(filename, 14)
        except OSError:
            continue
    return ImageFont.load_default()


def make_sheet(groups, dimensions, columns):
    rendered = [[[render(preview, facing, dimensions) for facing in FACINGS]
                 for preview in group.previews] for group in groups]
    boxes = [[[image.getchannel("A").getbbox() for image in views] for views in previews]
             for previews in rendered]
    top_edge = min(box[1] for previews in boxes for triplet in previews for box in triplet) - 7
    view_gap, version_gap, group_gap, row_gap = 8, 24, 36, 12
    padding, margin = 3, 14
    # Keep rotations close and head versions together, without changing scale.
    triplet_widths = [[sum(box[2] - box[0] + 2 * padding for box in triplet) + 2 * view_gap
                       for triplet in previews] for previews in boxes]
    group_widths = [sum(widths) + (len(widths) - 1) * version_gap for widths in triplet_widths]
    font = label_font()
    measuring_draw = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    card_width = max(max(group_widths),
                     max(round(measuring_draw.textlength(group.label, font=font)) for group in groups))
    rows = (len(groups) + columns - 1) // columns
    # Only the row containing the beaver tail needs its extra vertical space.
    bottom_edges = [max(box[3] for previews in boxes[row * columns:(row + 1) * columns]
                        for triplet in previews for box in triplet) + 7
                    for row in range(rows)]
    label_heights = [46 if any(len(group.previews) > 1 for group in groups[row * columns:(row + 1) * columns])
                     else 27 for row in range(rows)]
    row_heights = [bottom - top_edge + label_height for bottom, label_height in zip(bottom_edges, label_heights)]
    sheet = Image.new("RGB", (2 * margin + columns * card_width + (columns - 1) * group_gap,
                              2 * margin + sum(row_heights) + (rows - 1) * row_gap), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    for index, (group, previews) in enumerate(zip(groups, rendered)):
        left = margin + (index % columns) * (card_width + group_gap)
        row = index // columns
        top = margin + sum(row_heights[:row]) + row * row_gap
        view_height = bottom_edges[row] - top_edge
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
    return sheet


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("pawn-cosmetics-sheet.png"))
    parser.add_argument("--columns", type=int, default=4, help="Cosmetic groups per row (default: 4)")
    parser.add_argument("--rimworld-data", type=Path, default=GAME_DATA)
    args = parser.parse_args()
    if args.columns < 1:
        parser.error("--columns must be positive")
    if args.rimworld_data != GAME_DATA and not args.rimworld_data.is_dir():
        parser.error("--rimworld-data must be an existing RimWorld/Data directory")
    groups = catalog()
    verify_coverage(groups)
    sheet = make_sheet(groups, geometry(args.rimworld_data), min(args.columns, len(groups)))
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
