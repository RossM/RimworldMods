"""Render transparent README gene and xenotype icons. Requires Python 3.10+ and Pillow.

Run from any directory: python path/to/Scripts/render-gene-icons.py
Use --rimworld-data PATH if RimWorld/Data is outside the default Steam location.
Outputs 80px PNGs named by defName in XylXenos.Data/Docs/Images/Genes/{Endo,Xeno},
plus contact-sheet.png with labeled 128px endogene icons in the Genes directory.
Also outputs 48px xenotype PNGs named by defName in XylXenos.Data/Docs/Images/Xenotypes,
with transparent backgrounds.

Based on Art/Codex/render-genes-contact-sheet.py. Renders concrete mod genes
and psycast genes used by xenotypes. Core/Biotech definitions supply parents only.
Uses extracted art in Art/Rimworld art and mod textures, including NoArt where
specified by a definition. Psycast PSDs come from Game art source - Royalty.zip.
"""

import argparse
from dataclasses import dataclass
from pathlib import Path
import os
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
DEF_DIR = ROOT / "XylXenos.Data/Defs/GeneDefs"
ART_DIR = ROOT / "Art/Rimworld art"
OUTPUT = ROOT / "XylXenos.Data/Docs/Images/Genes"
XENOTYPE_OUTPUT = ROOT / "XylXenos.Data/Docs/Images/Xenotypes"
GAME_DATA = Path("C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Data")
WHITE = (1.0, 1.0, 1.0, 1.0)
ICON_SIZE = 80
XENOTYPE_ICON_SIZE = 48
SHEET_ICON_SIZE = 128
COLUMNS = 8
CELL_WIDTH = 218
CELL_HEIGHT = 220
MARGIN = 24


@dataclass(frozen=True)
class GeneIcon:
    name: str
    label: str
    texture: Path
    color: tuple[float, ...]


def read_defs(directory, tag):
    if not directory.is_dir():
        raise FileNotFoundError(f"Missing {directory}; set --rimworld-data to RimWorld/Data")
    return [definition for path in sorted(directory.rglob("*.xml"))
            for definition in ET.parse(path).getroot().findall(tag)]


def inherited_text(definition, field, parents, seen=()):
    value = definition.findtext(field)
    if value is not None:
        return value.strip()
    parent = definition.get("ParentName")
    if parent in seen:
        raise ValueError(f"Cyclic inheritance: {seen + (parent,)}")
    if parent:
        if parent not in parents:
            raise ValueError(f"Unresolved parent {parent!r} while reading {field}")
        return inherited_text(parents[parent], field, parents, seen + (parent,))
    return None


def parse_color(text):
    if not text:
        return WHITE
    values = tuple(float(value.strip()) for value in text.strip("() ").split(","))
    # RimWorld accepts normalized colors and byte-valued colors in XML.
    if any(value > 1 for value in values):
        values = tuple(value / 255 for value in values)
    if len(values) not in (3, 4) or not all(0 <= value <= 1 for value in values):
        raise ValueError(f"Invalid color: {text}")
    return values + (1.0,) if len(values) == 3 else values


def texture_path(icon_path):
    if icon_path.startswith("Xyl/"):
        path = ROOT / "XylXenos.Data/Textures" / (icon_path + ".png")
    elif icon_path.startswith("UI/Icons/Genes/"):
        path = ART_DIR / "Genes" / (Path(icon_path).name + ".png")
    elif icon_path.startswith("UI/Abilities/"):
        path = ART_DIR / "Psycasts" / (Path(icon_path).name + ".psd")
    else:
        raise ValueError(f"Unsupported icon path: {icon_path}")
    if not path.is_file():
        raise FileNotFoundError(f"Missing source art: {path}")
    return path


def read_gene_icons(game_data):
    genes = read_defs(DEF_DIR, "GeneDef")
    game_genes = [gene for expansion in ("Core", "Biotech")
                  for gene in read_defs(game_data / expansion / "Defs/GeneDefs", "GeneDef")]
    parents = {gene.attrib["Name"]: gene for gene in game_genes + genes if "Name" in gene.attrib}
    icons = {}
    for gene in genes:
        if gene.get("Abstract", "false").lower() == "true":
            continue
        skin = inherited_text(gene, "skinColorOverride", parents)
        hair = inherited_text(gene, "hairColorOverride", parents)
        name = gene.findtext("defName")
        icon_path = inherited_text(gene, "iconPath", parents)
        if not name or not icon_path:
            raise ValueError(f"Gene missing defName or iconPath: {name}")
        # Color genes use their actual pigment rather than the default iconColor.
        color = parse_color(skin or hair or inherited_text(gene, "iconColor", parents))
        if name in icons:
            raise ValueError(f"Duplicate gene: {name}")
        label = inherited_text(gene, "label", parents) or name
        icons[name] = GeneIcon(name, label, texture_path(icon_path), color)

    # Mirror GeneDefGenerator's template naming and ability icon substitution for
    # the psycasts referenced by the mod's xenotypes, rather than unused abilities.
    templates = read_defs(DEF_DIR, "XylXenos.GeneTemplateDef")
    xenotypes = read_defs(DEF_DIR, "XenotypeDef")
    referenced = {item.text.strip() for xenotype in xenotypes
                  for item in xenotype.findall("genes/li") if item.text}
    for template in templates:
        if template.findtext("geneTemplateType") != "PsychicAbility":
            continue
        prefix = template.findtext("defName") + "_"
        wanted = {name for name in referenced if name.startswith(prefix)}
        if not wanted:
            continue
        abilities = read_defs(game_data / "Royalty/Defs/AbilityDefs", "AbilityDef")
        ability_parents = {ability.attrib["Name"]: ability for ability in abilities
                           if "Name" in ability.attrib}
        for ability in abilities:
            name = prefix + (ability.findtext("defName") or "")
            if name not in wanted:
                continue
            path = inherited_text(ability, "iconPath", ability_parents)
            icon_path = template.findtext("iconPath").format(path)
            label = template.findtext("label").format(
                inherited_text(ability, "label", ability_parents) or name)
            icons[name] = GeneIcon(name, label, texture_path(icon_path), WHITE)
        if missing := wanted - icons.keys():
            raise ValueError(f"Missing psycast definitions: {sorted(missing)}")
    return sorted(icons.values(), key=lambda icon: icon.name)


def render_icon(texture, background, color=WHITE):
    with Image.open(texture) as source:
        icon = source.convert("RGBA")
    # Match the contact sheet: multiply straight RGB and alpha independently,
    # then source-over composite onto an untinted gene background symbol.
    icon = Image.merge("RGBA", tuple(
        channel.point([round(value * factor) for value in range(256)])
        for channel, factor in zip(icon.split(), color)))
    icon = ImageOps.contain(icon, background.size, Image.Resampling.LANCZOS)
    tile = background.copy()
    tile.alpha_composite(icon, ((tile.width - icon.width) // 2, (tile.height - icon.height) // 2))
    return tile


def render_xenotype_icons():
    xenotypes = read_defs(DEF_DIR, "XenotypeDef")
    parents = {xenotype.attrib["Name"]: xenotype for xenotype in xenotypes
               if "Name" in xenotype.attrib}
    background = Image.new("RGBA", (XENOTYPE_ICON_SIZE, XENOTYPE_ICON_SIZE))
    XENOTYPE_OUTPUT.mkdir(parents=True, exist_ok=True)
    rendered = set()
    for xenotype in xenotypes:
        if xenotype.get("Abstract", "false").lower() == "true":
            continue
        name = xenotype.findtext("defName")
        icon_path = inherited_text(xenotype, "iconPath", parents)
        if not name or not icon_path:
            raise ValueError(f"Xenotype missing defName or iconPath: {name}")
        if name in rendered:
            raise ValueError(f"Duplicate xenotype: {name}")
        render_icon(texture_path(icon_path), background).save(
            XENOTYPE_OUTPUT / (name + ".png"))
        rendered.add(name)
    print(f"Saved {len(rendered)} xenotype icons to {XENOTYPE_OUTPUT}")


def load_font():
    candidates = [Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts/segoeui.ttf",
                  Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), 17)
    return ImageFont.load_default(size=17)


def label_lines(gene, draw, font):
    label = gene.label[:1].upper() + gene.label[1:]
    lines = [""]
    for word in label.split():
        candidate = f"{lines[-1]} {word}".strip()
        if draw.textlength(candidate, font=font) > CELL_WIDTH - 16:
            lines.append(word)
        else:
            lines[-1] = candidate
    if any(draw.textlength(line, font=font) > CELL_WIDTH - 16 for line in lines):
        raise ValueError(f"Label exceeds tile: {gene.name}")
    return lines


def render_contact_sheet(genes):
    if not genes:
        raise ValueError("No genes to render in contact sheet")
    font = load_font()
    measure = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    entries = [(gene, label_lines(gene, measure, font))
               for gene in sorted(genes, key=lambda gene: (gene.label.casefold(), gene.name))]
    # Preserve the original contact sheet's cell spacing, margins, and typography.
    row_heights = [max(CELL_HEIGHT, SHEET_ICON_SIZE + 20 + 22 * max(len(lines) for _, lines
                       in entries[start:start + COLUMNS]))
                   for start in range(0, len(entries), COLUMNS)]
    with Image.open(ART_DIR / "Genes/GeneBackground_Endogene.png") as source:
        background = source.convert("RGBA").resize(
            (SHEET_ICON_SIZE, SHEET_ICON_SIZE), Image.Resampling.LANCZOS)
    sheet = Image.new("RGBA", (COLUMNS * CELL_WIDTH + 2 * MARGIN,
                              sum(row_heights) + 2 * MARGIN), "#202428")
    draw = ImageDraw.Draw(sheet)
    for index, (gene, lines) in enumerate(entries):
        x = MARGIN + index % COLUMNS * CELL_WIDTH
        y = MARGIN + sum(row_heights[:index // COLUMNS])
        sheet.alpha_composite(render_icon(gene.texture, background, gene.color),
                              (x + (CELL_WIDTH - SHEET_ICON_SIZE) // 2, y))
        for line_index, line in enumerate(lines):
            draw.text((x + CELL_WIDTH // 2, y + SHEET_ICON_SIZE + 10 + line_index * 22),
                      line, font=font, fill="#e8e9ea", anchor="mt")
    return sheet.convert("RGB")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rimworld-data", type=Path, default=GAME_DATA,
                        help="RimWorld Data directory containing Core, Biotech, and Royalty")
    args = parser.parse_args()
    genes = read_gene_icons(args.rimworld_data)
    for variant, filename in (("Endo", "GeneBackground_Endogene.png"),
                              ("Xeno", "GeneBackground_Xenogene.png"),
                              ("Arch", "GeneBackground_ArchiteGene.png")):
        with Image.open(ART_DIR / "Genes" / filename) as source:
            background = source.convert("RGBA").resize(
                (ICON_SIZE, ICON_SIZE), Image.Resampling.LANCZOS)
        directory = OUTPUT / variant
        directory.mkdir(parents=True, exist_ok=True)
        for gene in genes:
            render_icon(gene.texture, background, gene.color).save(directory / (gene.name + ".png"))
    print(f"Saved {len(genes)} genes in both Endo and Xeno variants to {OUTPUT}")
    sheet = render_contact_sheet(genes)
    sheet_path = OUTPUT / "contact-sheet.png"
    sheet.save(sheet_path)
    print(f"Saved {sheet_path}: {len(genes)} genes, {sheet.width} x {sheet.height}")
    render_xenotype_icons()


if __name__ == "__main__":
    main()
