"""Render transparent README gene and xenotype icons. Requires Python 3.10+ and Pillow.

Run from any directory: python path/to/Scripts/render-gene-icons.py
Use --rimworld-data PATH if RimWorld/Data is outside the default Steam location.
Outputs 80px PNGs named by defName in XylXenos/Data/Docs/Images/Genes/{Endo,Xeno},
plus contact-sheet.png with labeled 128px endogene icons in the Genes directory.
Also outputs 48px xenotype PNGs named by defName in XylXenos/Data/Docs/Images/Xenotypes,
with transparent backgrounds.

Based on Art/Codex/render-genes-contact-sheet.py. Renders concrete mod genes
and psycast genes used by xenotypes. Core/Biotech definitions supply parents only.
Uses extracted art in Art/Rimworld art and mod textures, including NoArt where
specified by a definition. Psycasts use the pre-upscaled 256px PNGs in
Art/Rimworld art/Psycasts/Processed. Run process-psycasts.py separately when their
source PSDs change; compositing never invokes ESRGAN.
"""

import argparse
from functools import lru_cache
from pathlib import Path
import os

from PIL import Image, ImageDraw, ImageFont, ImageOps

from gene_art import (ART_DIR, DEF_DIR, GAME_DATA, ROOT, WHITE, inherited_text,
                      read_defs, read_gene_icons, texture_path)


OUTPUT = ROOT / "XylXenos/Data/Docs/Images/Genes"
XENOTYPE_OUTPUT = ROOT / "XylXenos/Data/Docs/Images/Xenotypes"
CONTACT_SHEET_OUTPUT = ROOT / "Art/Generated"
ICON_SIZE = 80
XENOTYPE_ICON_SIZE = 48
SHEET_ICON_SIZE = 128
COLUMNS = 8
CELL_WIDTH = 218
CELL_HEIGHT = 220
MARGIN = 24


@lru_cache(maxsize=None)
def load_background(path, size):
    with Image.open(path) as source:
        return source.convert("RGBA").resize((size, size), Image.Resampling.LANCZOS)


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
    default_background = ART_DIR / "Genes/GeneBackground_Endogene.png"
    sheet = Image.new("RGBA", (COLUMNS * CELL_WIDTH + 2 * MARGIN,
                              sum(row_heights) + 2 * MARGIN), "#202428")
    draw = ImageDraw.Draw(sheet)
    for index, (gene, lines) in enumerate(entries):
        x = MARGIN + index % COLUMNS * CELL_WIDTH
        y = MARGIN + sum(row_heights[:index // COLUMNS])
        background = load_background(gene.background or default_background, SHEET_ICON_SIZE)
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
        default_background = ART_DIR / "Genes" / filename
        directory = OUTPUT / variant
        directory.mkdir(parents=True, exist_ok=True)
        for gene in genes:
            background = load_background(gene.background or default_background, ICON_SIZE)
            render_icon(gene.texture, background, gene.color).save(directory / (gene.name + ".png"))
    print(f"Saved {len(genes)} genes in both Endo and Xeno variants to {OUTPUT}")
    sheet = render_contact_sheet(genes)
    sheet_path = CONTACT_SHEET_OUTPUT / "gene-icon-sheet.png"
    sheet.save(sheet_path)
    print(f"Saved {sheet_path}: {len(genes)} genes, {sheet.width} x {sheet.height}")
    render_xenotype_icons()


if __name__ == "__main__":
    main()
