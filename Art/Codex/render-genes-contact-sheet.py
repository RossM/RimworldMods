"""Regenerate genes-contact-sheet.png. Requires Python 3 and Pillow.

Run from any directory: python path/to/Art/Codex/render-genes-contact-sheet.py
Use --rimworld-data PATH if RimWorld/Data is outside the default Steam location.
Each PNG receives one tile, including alternate and unused textures. Labels and
colors come from mod GeneDefs with Core/Biotech parent inheritance. Unreferenced textures use filename labels, and absent
iconColor values use white (no multiplication change). Shared icons list all labels.
"""

import argparse
from dataclasses import dataclass
from pathlib import Path
import os
import re
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
ICON_DIR = ROOT / "XylXenos/Textures/Xyl/UI/Icons/Genes"
DEF_DIR = ROOT / "XylXenos/Defs/GeneDefs"
GAME_DATA = Path("C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Data")
BACKGROUND = ROOT / "Art/Rimworld art/Genes/GeneBackground_Endogene.png"
OUTPUT = Path(__file__).with_name("genes-contact-sheet.png")
WHITE = (1.0, 1.0, 1.0, 1.0)
COLUMNS = 8
ICON_SIZE = 160
CELL_WIDTH = 218
CELL_HEIGHT = 220
MARGIN = 24


@dataclass(frozen=True)
class IconMetadata:
    color: tuple[float, ...] = WHITE
    labels: tuple[str, ...] = ()


def read_gene_metadata(game_data=GAME_DATA):
    genes = [gene for path in sorted(DEF_DIR.rglob("*.xml"))
             for gene in ET.parse(path).getroot().findall("GeneDef")]
    game_genes = []
    for expansion in ("Core", "Biotech"):
        directory = game_data / expansion / "Defs/GeneDefs"
        if not directory.is_dir():
            raise FileNotFoundError(f"Missing {directory}; set --rimworld-data to RimWorld/Data")
        game_genes.extend(gene for path in sorted(directory.rglob("*.xml"))
                          for gene in ET.parse(path).getroot().findall("GeneDef"))
    # Game definitions provide parents only; tiles still come from mod textures.
    # Mod named definitions take precedence over game named definitions.
    parents = {gene.attrib["Name"]: gene for gene in game_genes + genes if "Name" in gene.attrib}

    def inherited_text(gene, field, seen=()):
        value = gene.findtext(field)
        if value is not None:
            return value.strip()
        parent = gene.get("ParentName")
        if parent in seen:
            raise ValueError(f"Cyclic GeneDef inheritance: {seen + (parent,)}")
        if parent:
            if parent not in parents:
                raise ValueError(f"Unresolved GeneDef parent {parent!r} while reading {field}")
            return inherited_text(parents[parent], field, seen + (parent,))
        return None

    metadata = {}
    for gene in genes:
        if gene.get("Abstract", "false").lower() == "true":
            continue
        icon_path = inherited_text(gene, "iconPath")
        if not icon_path:
            continue
        color_text = inherited_text(gene, "iconColor")
        color = WHITE
        if color_text:
            values = tuple(float(value.strip())
                           for value in color_text.strip("() ").split(","))
            if len(values) not in (3, 4) or not all(0 <= value <= 1 for value in values):
                raise ValueError(f"Invalid iconColor: {color_text}")
            color = values + (1.0,) if len(values) == 3 else values
        previous = metadata.get(icon_path)
        if previous is not None and previous.color != color:
            raise ValueError(f"Multiple GeneDef colors for {icon_path}; needs separate tiles")
        labels = previous.labels if previous else ()
        label = inherited_text(gene, "label")
        if label:
            label = label[:1].upper() + label[1:]
            if label not in labels:
                labels += (label,)
        metadata[icon_path] = IconMetadata(color, labels)
    return metadata


def load_font():
    candidates = [Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts/segoeui.ttf",
                  Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), 17)
    return ImageFont.load_default(size=17)


def label_lines(path, metadata, draw, font):
    label = " / ".join(metadata.labels)
    if not label:
        # Unreferenced textures retain filename words and alternate suffixes.
        label = path.stem.removeprefix("Gene_").replace("_", " ")
        label = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", label)
        label = re.sub(r"([A-Z])([A-Z][a-z])", r"\1 \2", label)
        label = label[:1].upper() + label[1:]
    lines = [""]
    for word in label.split():
        candidate = f"{lines[-1]} {word}".strip()
        if draw.textlength(candidate, font=font) > CELL_WIDTH - 16:
            lines.append(word)
        else:
            lines[-1] = candidate
    if any(draw.textlength(line, font=font) > CELL_WIDTH - 16 for line in lines):
        raise ValueError(f"Label exceeds tile: {path.name}")
    return lines


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rimworld-data", type=Path, default=GAME_DATA,
                        help="RimWorld Data directory containing Core and Biotech")
    args = parser.parse_args()
    paths = sorted(ICON_DIR.rglob("*.png"), key=lambda path: path.as_posix().casefold())
    if not paths:
        raise ValueError(f"No icons found in {ICON_DIR}")
    metadata = read_gene_metadata(args.rimworld_data)
    font = load_font()
    measure = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    entries = []
    for path in paths:
        key = path.relative_to(ROOT / "XylXenos/Textures").with_suffix("").as_posix()
        info = metadata.get(key, IconMetadata())
        entries.append((path, info, label_lines(path, info, measure, font)))
    row_heights = [max(CELL_HEIGHT, ICON_SIZE + 20 + 22 * max(len(entry[2]) for entry in entries[start:start + COLUMNS]))
                   for start in range(0, len(entries), COLUMNS)]
    with Image.open(BACKGROUND) as source:
        background = source.convert("RGBA").resize((ICON_SIZE, ICON_SIZE), Image.Resampling.LANCZOS)
    sheet = Image.new("RGBA", (COLUMNS * CELL_WIDTH + 2 * MARGIN,
                              sum(row_heights) + 2 * MARGIN),
                      "#202428")
    draw = ImageDraw.Draw(sheet)
    tinted = 0
    for index, (path, info, lines) in enumerate(entries):
        color = info.color
        with Image.open(path) as source:
            icon = source.convert("RGBA")
        # Multiply straight RGB and alpha independently before source-over composition.
        # The background is never tinted by the gene's iconColor.
        icon = Image.merge("RGBA", tuple(channel.point([round(value * factor) for value in range(256)])
                                         for channel, factor in zip(icon.split(), color)))
        icon = ImageOps.contain(icon, (ICON_SIZE, ICON_SIZE), Image.Resampling.LANCZOS)
        tile = background.copy()
        tile.alpha_composite(icon, ((ICON_SIZE - icon.width) // 2, (ICON_SIZE - icon.height) // 2))
        x = MARGIN + index % COLUMNS * CELL_WIDTH
        y = MARGIN + sum(row_heights[:index // COLUMNS])
        sheet.alpha_composite(tile, (x + (CELL_WIDTH - ICON_SIZE) // 2, y))
        for line_index, line in enumerate(lines):
            draw.text((x + CELL_WIDTH // 2, y + ICON_SIZE + 10 + line_index * 22),
                      line, font=font, fill="#e8e9ea", anchor="mt")
        tinted += color != WHITE
    sheet.convert("RGB").save(OUTPUT)
    print(f"Saved {OUTPUT}: {len(paths)} icons, {tinted} tinted, {sheet.width} x {sheet.height}")


if __name__ == "__main__":
    main()
