"""Build Workshop xenotype cards from XML, descriptor config, and cutouts.

The cards help Workshop readers compare xenotypes and understand what the mod
adds at a glance. Emphasize distinctive mechanics while retaining the familiar
vanilla genes that give a complete picture of each xenotype's strengths and costs.

Run: python Scripts/build-xenotype-cards.py to build both sets for every xenotype.
Use --screenshot [XENOTYPE] to build just one xenotype's screenshot cards.
Requires Pillow, ImageMagick, and installed Biotech, Royalty, and Odyssey definitions.
Use --rimworld-data PATH or --config PATH to override the default inputs.

Read game data from XML so balance/content changes cannot leave manually copied
gene lists or totals behind. xenotype-cards.json holds the short descriptions
from CONTENT.md because Workshop positioning is an editorial choice. Shared art
and inheritance handling in gene_art.py keeps both renderers' interpretations consistent.
Definitions represent this mod's standalone configuration, including Royalty,
without loading other installed mods. Totals precede random or lineage additions.
Scaleborn lineages and Titan specialties are shown separately so alternatives
cannot be mistaken for traits every pawn receives; their trigger icons are omitted.

PNG cards and the overview go to XylXenos/Docs/Images/XenotypeCards/ for use
in mod documentation. Editable SVGs, diagnostic JSON snapshots, and the review
gallery go to Art/Generated/Xenotype cards/ so build intermediates stay outside
the distributed mod. The 1280×720 Workshop cards and their SVGs go to
Art/Generated/Workshop screenshots/. Cutouts retain their in-game canvas alignment.
"""

import argparse
import base64
from dataclasses import asdict, dataclass
from functools import lru_cache
import json
import os
from pathlib import Path
import re
import subprocess
from xml.sax.saxutils import escape
import xml.etree.ElementTree as ET

from PIL import Image, ImageFont

from gene_art import (GAME_DATA, GENE_ICON_SCALE, gene_background as resolve_gene_background,
                      inherited_text, parse_color, psycast_biostats, read_defs, read_gene_icons)


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Art/Rimworld art"
MOD = ROOT / "XylXenos/Data"
GENERATED_OUT = ROOT / "Art/Generated/Xenotype cards"
PNG_OUT = ROOT / "XylXenos/Docs/Images/XenotypeCards"
SCREENSHOT_OUT = ROOT / "Art/Generated/Workshop screenshots"
SEMIBOLD_FONT = "C:/Windows/Fonts/seguisb.ttf"
BOLD_FONT = "C:/Windows/Fonts/segoeuib.ttf"
# Work at the Workshop description column's observed display width so legibility
# is judged at the size readers actually see, rather than on enlarged previews.
CARD_WIDTH = 630
CARD_HEIGHT = 340
GENES_LEFT = 200
GENES_RIGHT = CARD_WIDTH - 16
GENES_TOP = 76
GENES_BOTTOM = 326
GENE_GROUP_GAP = 32
GENE_DIVIDER_FRACTION = 0.65
CHOICE_LABEL_Y = 89
CHOICE_PANEL_TOP = 102
# Both cutout sets retain the same camera scale on 384px/768px canvases.
CHARACTER_SCALE = 0.67
CHARACTER_CENTER = 100
CHARACTER_TOP = 68


@dataclass(frozen=True)
class Gene:
    defName: str
    label: str
    path: Path
    color: tuple[float, ...]
    source: str
    category: str
    category_priority: float
    priority: float
    complexity: int
    metabolism: int
    archites: int
    background: Path | None = None

    @property
    def sort_key(self):
        # Match GeneUtility.SortGeneDefs so readers can compare cards with the gene UI.
        return (-self.category_priority, self.priority, self.label.casefold())

    @property
    def featured(self):
        # Keep functional mod additions prominent, including psycasts with their
        # distinctive backgrounds. Zero-cost cosmetics remain supporting context.
        return self.source == "mod" and (self.complexity != 0 or self.metabolism != 0)


@dataclass(frozen=True)
class GeneLayout:
    max_columns: int
    icon_size: int
    row_gap: int


@dataclass(frozen=True)
class Lineage:
    skin: Gene
    traits: tuple[Gene, ...]

    @property
    def genes(self):
        # The color at the left identifies the whole package. The remaining
        # icons retain game order, consistent with the shared gene lists.
        return (self.skin, *self.traits)


# Keep readable icon sizes across cards, but center each card's complete gene
# stack. Fixed group bands leave uneven empty space when their row counts differ.
FEATURED_GENE_LAYOUT = GeneLayout(max_columns=5, icon_size=52, row_gap=12)
SUPPORTING_GENE_LAYOUT = GeneLayout(max_columns=10, icon_size=32, row_gap=10)
# Narrow shared lists only where a separate panel explains mutually exclusive options.
SCALEBORN_GENES_RIGHT = 430
SCALEBORN_GENES_LEFT = 188
SCALEBORN_FEATURED_LAYOUT = GeneLayout(max_columns=4, icon_size=52, row_gap=12)
SCALEBORN_SUPPORTING_LAYOUT = GeneLayout(max_columns=5, icon_size=32, row_gap=10)
TITAN_GENES_RIGHT = 468
TITAN_FEATURED_LAYOUT = GeneLayout(max_columns=4, icon_size=52, row_gap=12)
TITAN_SUPPORTING_LAYOUT = GeneLayout(max_columns=7, icon_size=32, row_gap=10)


class GeneLibrary:
    def __init__(self, game):
        definitions = []
        for expansion in ("Core", "Biotech", "Royalty", "Ideology", "Anomaly", "Odyssey"):
            directory = game / expansion / "Defs"
            if directory.is_dir():
                definitions.extend(read_defs(directory, "*"))
        definitions.extend(read_defs(MOD / "Defs", "*"))
        for fallback in ("NoAlphaGenes", "NoBigAndSmall", "NoBigAndSmallFramework"):
            definitions.extend(read_defs(MOD / "Compat" / fallback / "Defs", "GeneCategoryDef"))
        self.parents = {}
        for definition in definitions:
            if definition.get("Name"):
                self.parents.setdefault(definition.tag, {})[definition.get("Name")] = definition
        self.definitions = definitions
        self.genes = {d.findtext("defName"): d for d in definitions if d.tag == "GeneDef"}
        self.categories = {d.findtext("defName"): d for d in definitions if d.tag == "GeneCategoryDef"}
        self.templates = {d.findtext("defName"): d for d in definitions if d.tag in ("GeneTemplateDef", "XylXenos.GeneTemplateDef")}
        self.xenotypes = {d.findtext("defName"): d for d in definitions if d.tag == "XenotypeDef"}
        self.mod_icons = {icon.name: icon for icon in read_gene_icons(game)}
        self.generated_sources = {}
        for tag in ("SkillDef", "ChemicalDef"):
            self.generated_sources[tag] = {d.findtext("defName"): (i, d)
                                           for i, d in enumerate(d for d in definitions if d.tag == tag)}
        abilities = [d for d in definitions if d.tag == "AbilityDef" and d.findtext("defName")
                     and self.value(d, "abilityClass") == "Psycast"]
        abilities.sort(key=lambda d: (int(self.value(d, "level") or 0), self.value(d, "label")))
        self.psycasts = {d.findtext("defName"): (i, d) for i, d in enumerate(abilities)}

        # Honor the mod's category moves without modifying the source XML.
        for operation in ET.parse(MOD / "Patches/Patch_GeneDefs.xml").getroot():
            if operation.get("Class") != "PatchOperationReplace":
                continue
            xpath = " ".join((operation.findtext("xpath") or "").split())
            match = re.fullmatch(r'Defs/GeneDef\[(.+)\]/displayCategory', xpath)
            if not match:
                continue
            selector = match[1]
            parent = re.fullmatch(r'@Name="([^"]+)"', selector)
            targets = ([self.parents["GeneDef"][parent[1]]] if parent else
                       [self.genes[name] for name in re.findall(r'defName="([^"]+)"', selector)])
            for target in targets:
                target.find("displayCategory").text = operation.findtext("value/displayCategory")

    def value(self, definition, field):
        return inherited_text(definition, field, self.parents.get(definition.tag, {}))

    def bonus_gene_maker(self, trigger_name):
        # Follow the trigger's maker reference rather than copying its options
        # into presentation config: edits to the mod must appear on the cards.
        trigger = self.genes[trigger_name]
        maker_name = trigger.findtext("modExtensions/li/comps/li/maker")
        return next(d for d in self.definitions
                    if d.tag == "Xylib.GeneSetMakerDef" and d.findtext("defName") == maker_name)

    def scaleborn_lineages(self):
        maker = self.bonus_gene_maker("XylScalebornLineage")
        lineages = []
        for option in maker.findall("root/options/li/maker"):
            if option.get("Class") != "Xylib.GeneSetMaker_List":
                raise ValueError("Scaleborn lineage cards require explicit gene packages")
            genes = [self.gene(item.text.strip()) for item in option.findall("genes/li")]
            skins = [g for g in genes if g.defName in self.genes
                     and self.value(self.genes[g.defName], "skinColorOverride")]
            if len(genes) != 4 or len(skins) != 1:
                raise ValueError("Each lineage row needs one skin color and three traits")
            skin = skins[0]
            lineages.append(Lineage(skin, tuple(sorted(
                (g for g in genes if g != skin), key=lambda g: g.sort_key))))
        if not lineages:
            raise ValueError("No Scaleborn lineage packages found")
        return lineages

    def titan_specialties(self, xenotype):
        maker = self.bonus_gene_maker("XylSuperSpecialist")
        root = maker.find("root")
        if root.get("Class") != "Xylib.GeneSetMaker_List" or root.findtext("count") != "1":
            raise ValueError("Titan specialty cards require a single choice from an explicit gene list")

        # GeneSetMaker seeds the current xenotype when overrideExistingGenes is
        # false. Generated aptitude genes share an exclusion tag per skill, so
        # existing aptitudes rule out that skill even at a different tier.
        existing_skills = set()
        if maker.findtext("overrideExistingGenes", "false").lower() != "true":
            for item in xenotype.findall("genes/li"):
                template, _, skill = item.text.strip().rpartition("_")
                if template in self.templates and self.value(self.templates[template], "geneTemplateType") == "Skill":
                    existing_skills.add(skill)
        options = []
        for item in root.findall("genes/li"):
            name = item.text.strip()
            template, _, skill = name.rpartition("_")
            if template not in self.templates or self.value(self.templates[template], "geneTemplateType") != "Skill":
                raise ValueError(f"Titan specialty is not a generated aptitude: {name}")
            if skill not in existing_skills:
                options.append(self.gene(name))
        if not options:
            raise ValueError("No Titan specialties remain after excluding existing skills")
        return sorted(options, key=lambda gene: gene.sort_key)

    def gene(self, name):
        definition = self.genes.get(name)
        if definition is not None:
            label = self.value(definition, "label")
            icon = self.value(definition, "iconPath")
            priority = float(self.value(definition, "displayOrderInCategory") or 0)
            # GeneDef defaults to one complexity; omitted XML does not mean zero.
            # Explicit zeroes (including inherited cosmetic costs) stay zero.
            complexity = int(self.value(definition, "biostatCpx") or 1)
            metabolism = int(self.value(definition, "biostatMet") or 0)
            archites = int(self.value(definition, "biostatArc") or 0)
        else:
            template_name, source_name = name.rsplit("_", 1)
            definition = self.templates[template_name]
            template_type = self.value(definition, "geneTemplateType")
            if template_type == "PsychicAbility":
                index, source = self.psycasts[source_name]
                icon = self.value(definition, "iconPath").format(self.value(source, "iconPath"))
                level = int(self.value(source, "level"))
                stats = psycast_biostats(definition, level)
                complexity = int(stats.findtext("biostatCpx", "0"))
                metabolism = int(stats.findtext("biostatMet", "0"))
                archites = int(stats.findtext("biostatArc", "0"))
            elif template_type in ("Skill", "Chemical"):
                index, source = self.generated_sources[template_type + "Def"][source_name]
                icon = self.value(definition, "iconPath").format(source_name)
                # GeneTemplateDef has its own zero default, copied into generated genes.
                complexity = int(self.value(definition, "biostatCpx") or 0)
                metabolism = int(self.value(definition, "biostatMet") or 0)
                archites = int(self.value(definition, "biostatArc") or 0)
                if template_type == "Chemical":
                    override = definition.find(f'chemicalBiostatOverrides/li[chemical="{source_name}"]')
                    if override is not None:
                        complexity = int(override.findtext("biostatCpx", str(complexity)))
                        metabolism = int(override.findtext("biostatMet", str(metabolism)))
            else:
                raise ValueError(f"Unsupported generated gene: {name} ({template_type})")
            source_label = self.value(source, "label") or source_name.lower()
            label = self.value(definition, "label").format(source_label)
            # The game's generator uses the source def's load index, not skill listOrder.
            priority = 1000 * index + int(self.value(definition, "displayOrderOffset") or 0)

        background = None
        if name in self.mod_icons:
            rendered = self.mod_icons[name]
            texture, color, origin = rendered.texture, rendered.color, "mod"
            background = rendered.background
        else:
            texture = ART / "Genes" / (icon.removeprefix("UI/Icons/Genes/") + ".png")
            background = resolve_gene_background(definition, self.parents.get(definition.tag, {}), archites)
            color = parse_color(self.value(definition, "skinColorOverride")
                                or self.value(definition, "hairColorOverride")
                                or self.value(definition, "iconColor"))
            origin = "vanilla"
        if not texture.is_file():
            raise FileNotFoundError(texture)
        category = self.value(definition, "displayCategory") or "Miscellaneous"
        return Gene(name, label, texture, color, origin, category,
                    float(self.value(self.categories[category], "displayPriorityInXenotype") or 0),
                    priority, complexity, metabolism, archites, background)


@lru_cache(maxsize=None)
def image_uri(path):
    return "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def write_text(path, text):
    with path.open("w", encoding="utf-8", newline="\r\n") as output:
        output.write(text + "\n")


def render_png(arguments, path):
    # Direct ImageMagick output can fail when overwriting Windows previews even
    # while Python can write the file. Let ImageMagick render and Python save.
    # Exclude volatile dates so unchanged art stays byte-identical in Git.
    # Preserve color-space metadata rather than stripping all PNG properties.
    rendered = subprocess.run(["magick", *arguments, "-define", "png:exclude-chunks=date,time", "PNG:-"],
                              check=True, stdout=subprocess.PIPE)
    if not path.is_file() or path.read_bytes() != rendered.stdout:
        path.write_bytes(rendered.stdout)


def balanced_rows(genes, max_columns):
    """Avoid sparse trailing rows so changing gene counts retain a balanced composition."""
    if not genes:
        return []
    row_count = (len(genes) + max_columns - 1) // max_columns
    row_size, extra = divmod(len(genes), row_count)
    rows = []
    start = 0
    for row_index in range(row_count):
        end = start + row_size + (1 if row_index < extra else 0)
        rows.append(genes[start:end])
        start = end
    return rows


@lru_cache(maxsize=None)
def image_x_bounds(path, size):
    with Image.open(path) as image:
        width, height = image.size
        bounds = image.convert("RGBA").getchannel("A").getbbox()
    if bounds is None:
        return 0, size
    scale = size / max(width, height)
    inset = (size - width * scale) / 2
    return inset + bounds[0] * scale, inset + bounds[2] * scale


def lineage_band(lineage, x, y, width, height):
    rgb = tuple(round(channel * 255) for channel in lineage.skin.color[:3])
    border = tuple(round((channel * 0.65 + 0.35) * 255) for channel in lineage.skin.color[:3])
    return (f'<rect x="{x}" y="{y}" width="{width}" height="{height}" rx="7" '
            f'fill="rgb{rgb}" fill-opacity=".18" stroke="rgb{border}" stroke-opacity=".7"/>')


@dataclass(frozen=True)
class ScreenshotRow:
    genes: tuple[Gene, ...]
    center: float
    top: float
    tile_width: int
    tile_height: int
    icon_size: int
    label_size: int


SCREENSHOT_TILE_GAP = 8


def draw_screenshot_rows(rows, default_background, text, picture, gene_icon):
    gap = SCREENSHOT_TILE_GAP
    stat_font = ImageFont.truetype(BOLD_FONT, 20)
    number_x = 53
    for row in rows:
        center, y = row.center, row.top
        tile_width, tile_height = row.tile_width, row.tile_height
        icon_size, label_size = row.icon_size, row.label_size
        font = ImageFont.truetype(BOLD_FONT, label_size)
        row_width = len(row.genes) * (tile_width + gap) - gap
        for column, gene in enumerate(row.genes):
            x = center - row_width / 2 + column * (tile_width + gap)
            stats = [("Complexity.png", str(gene.complexity))]
            if gene.metabolism:
                stats.append(("Metabolism.png", f"{gene.metabolism:+}"))
            if gene.archites:
                stats.append(("ArchiteCapsuleRequired.png", str(gene.archites)))
            icon_center = tile_width / 2 + 15
            foreground_size = icon_size * GENE_ICON_SCALE
            artwork = [(gene.background or default_background, icon_center - icon_size / 2, icon_size),
                       (gene.path, icon_center - foreground_size / 2, foreground_size)]
            bounds = []
            for path, left, size in artwork:
                ink_left, ink_right = image_x_bounds(path, size)
                bounds.append((left + ink_left, left + ink_right))
            symbol_bounds = [image_x_bounds(ART / symbol, 18) for symbol, _ in stats]
            bounds.extend((10 + left, 10 + right) for left, right in symbol_bounds)
            number_bounds = [stat_font.getbbox(value, anchor="rs") for _, value in stats]
            bounds.extend((number_x + left, number_x + right) for left, _, right, _ in number_bounds)
            # Center the visible group over the label. Transparent padding
            # varies across gene and stat art, so the image rectangles alone
            # can leave the group looking off-center.
            group_center = (min(left for left, _ in bounds) + max(right for _, right in bounds)) / 2
            group_x = x + tile_width / 2 - group_center
            gene_icon(gene, group_x + icon_center, y + 4, icon_size,
                      "featured" if gene.featured else "supporting")
            for index, (symbol, value) in enumerate(stats):
                stat_y = y + (4 if tile_height < 90 else 8) + 21 * index
                picture(ART / symbol, group_x + 10, stat_y, 18, 18)
                text(group_x + number_x, stat_y + 16, value, 20, weight=600, anchor="end")

            label = gene.label[:1].upper() + gene.label[1:]
            lines = [""]
            for segment in re.split(r"(?<=-)(?=\S)|(?= )", label):
                candidate = (lines[-1] + segment).lstrip()
                if lines[-1] and font.getlength(candidate) > tile_width - 12:
                    lines.append(segment.lstrip())
                else:
                    lines[-1] = candidate
            if len(lines) > 2 or any(font.getlength(line) > tile_width - 12 for line in lines):
                raise ValueError(f"Gene label exceeds screenshot tile: {label}")
            line_height = 20
            baseline = y + tile_height - 27 + (2 - len(lines)) * line_height / 2
            for index, line in enumerate(lines):
                text(x + tile_width / 2, baseline + line_height * index, line, label_size,
                     weight=600, anchor="middle")


def screenshot_gene_rows(genes, heading_height, *, has_specialties):
    featured = [gene for gene in genes if gene.featured]
    supporting = [gene for gene in genes if not gene.featured]
    gap = SCREENSHOT_TILE_GAP
    if has_specialties:
        # Four columns leave space for the specialties and enough label width
        # for two lines. Tighter rows keep all fixed genes on the same card.
        groups = [
            (balanced_rows(featured, 4), 146, 92, 54, 21),
            (balanced_rows(supporting, 4), 146, 84, 40, 19),
        ]
        grid_center, grid_top, grid_bottom, group_gap = 636, 130, 698, 12
    else:
        groups = [
            (balanced_rows(featured, 5), 176, 110, 60, 21),
            (balanced_rows(supporting, 6), 154, 94, 48, 19),
        ]
        grid_center, grid_top, grid_bottom, group_gap = 761, 134, 686, 20
    grid_top += heading_height
    groups = [group for group in groups if group[0]]
    stack_height = sum(len(rows) * (height + gap) - gap for rows, _, height, _, _ in groups)
    stack_height += max(0, len(groups) - 1) * group_gap
    if stack_height > grid_bottom - grid_top:
        raise ValueError("Gene tiles exceed the screenshot card")
    y = grid_top + (grid_bottom - grid_top - stack_height) / 2
    layout = []
    for rows, tile_width, tile_height, icon_size, label_size in groups:
        for row in rows:
            layout.append(ScreenshotRow(tuple(row), grid_center, y, tile_width,
                                        tile_height, icon_size, label_size))
            y += tile_height + gap
        y += group_gap - gap
    return layout


def draw_screenshot_specialty_panel(specialties, parts, text):
    center, top, width, height = 1104, 134, 288, 564
    parts.append(f'<rect x="{center - width / 2}" y="{top}" width="{width}" '
                 f'height="{height}" rx="7" fill="none" stroke="#77717f"/>')
    text(center, 165, "One of", 24, "#c9c4cf", 600, anchor="middle")
    rows = []
    for start in range(0, len(specialties), 2):
        y = 180 + (start // 2) * (94 + SCREENSHOT_TILE_GAP)
        rows.append(ScreenshotRow(tuple(specialties[start:start + 2]), center, y, 132, 94, 48, 19))
    if rows[-1].top + rows[-1].tile_height > top + height:
        raise ValueError("Titan specialty tiles exceed the screenshot card")
    return rows


def draw_screenshot_lineage_panels(lineages, parts, text):
    # Index by skin gene so the display order is independent of XML option order.
    by_skin = {lineage.skin.defName: lineage for lineage in lineages}
    panel_order = [
        ("Skin_DeepRed", "Red", 332, 156),
        ("XylSkin_DarkGreen", "Green", 948, 156),
        ("Skin_SlateGray", "Black", 332, 326),
        ("Skin_SheerWhite", "White", 948, 326),
        ("XylSkin_DarkBlue", "Blue", 640, 496),
    ]
    if set(by_skin) != {skin for skin, _, _, _ in panel_order}:
        raise ValueError("Scaleborn lineage skins do not match the screenshot arrangement")
    rows = []
    for skin, label, center, y in panel_order:
        lineage = by_skin[skin]
        parts.append(lineage_band(lineage, center - 296, y, 592, 148))
        text(center, y + 34, label, 27, weight=700, anchor="middle")
        rows.append(ScreenshotRow(lineage.genes, center, y + 43, 138, 94, 48, 19))
    return rows


def build_card(library, def_name, descriptor, *, screenshot=False):
    xenotype = library.xenotypes[def_name]
    name = library.value(xenotype, "label").capitalize()
    genes = [library.gene(g.text.strip()) for g in xenotype.findall("genes/li")]
    sorted_genes = sorted(genes, key=lambda gene: gene.sort_key)
    featured_genes = [gene for gene in sorted_genes if gene.featured]
    supporting_genes = [gene for gene in sorted_genes if not gene.featured]
    base_genes = featured_genes + supporting_genes
    is_scaleborn = def_name == "XylScaleborn"
    is_titan = def_name == "XylTitan"
    lineages = library.scaleborn_lineages() if is_scaleborn else []
    specialties = library.titan_specialties(xenotype) if is_titan else []
    hidden_trigger = {"XylScaleborn": "XylScalebornLineage", "XylTitan": "XylSuperSpecialist"}.get(def_name)
    # The option panels explain these genes. Hiding their proxies is a
    # presentation choice; keep them in the defined genes used for the totals.
    featured_genes = [gene for gene in featured_genes if gene.defName != hidden_trigger]
    complexity = sum(g.complexity for g in genes)
    metabolism = sum(g.metabolism for g in genes)
    inheritable = (library.value(xenotype, "inheritable") or "false").lower() == "true"
    background_type = "Endogene" if inheritable else "Xenogene"
    gene_background = ART / "Genes" / f"GeneBackground_{background_type}.png"
    portrait = ROOT / "Art/Cutouts/Processed" / (name + "_South.png")
    if not portrait.is_file():
        portrait = ROOT / "Art/Cutouts" / (name + "_South.png")
    parts = []

    def text(x, y, value, size=22, fill="#ebe7df", weight=400, anchor="start"):
        alignment = f' text-anchor="{anchor}"' if anchor != "start" else ""
        parts.append(f'<text x="{x}" y="{y}" fill="{fill}" font-family="Segoe UI, sans-serif" font-size="{size}" font-weight="{weight}"{alignment}>{escape(str(value))}</text>')

    def picture(path, x, y, width, height, attributes=""):
        parts.append(f'<image x="{x}" y="{y}" width="{width}" height="{height}" xlink:href="{image_uri(path)}" {attributes}/>')

    card_width, card_height = (1280, 720) if screenshot else (CARD_WIDTH, CARD_HEIGHT)
    header_scale = 2 if screenshot else 1
    header_baseline = 40 * header_scale
    picture(MOD / "Textures" / (xenotype.findtext("iconPath") + ".png"),
            16 * header_scale, 14 * header_scale, 36 * header_scale, 36 * header_scale)
    title_size = 24 * header_scale
    descriptor_size = 18 * header_scale
    title_font = ImageFont.truetype(BOLD_FONT, title_size)
    descriptor_font = ImageFont.truetype(SEMIBOLD_FONT, descriptor_size)
    descriptor_x = 64 * header_scale + title_font.getlength(name.upper()) + 20 * header_scale
    if descriptor_x + descriptor_font.getlength(descriptor) > 480 * header_scale:
        raise ValueError(f"{name} title and descriptor overlap the biostats")
    # Center ascenders through the baseline beside the capitals. Descenders hang
    # below that span; including them would push words with g/p/y upward.
    title_top = title_font.getbbox(name.upper(), anchor="ls")[1]
    descriptor_top = descriptor_font.getbbox(descriptor, anchor="ls")[1]
    descriptor_y = header_baseline + (title_top - descriptor_top) / 2
    text(64 * header_scale, header_baseline, name.upper(), title_size, weight=700)
    text(round(descriptor_x, 2), descriptor_y, descriptor, descriptor_size, "#c9c4cf", weight=600)
    for x, value, symbol, color in [
            (494, complexity, "Complexity.png", "#8bc9db"),
            (558, f"{metabolism:+}" if metabolism else "0", "Metabolism.png", "#d39ab3")]:
        picture(ART / symbol, x * header_scale, 24 * header_scale, 18 * header_scale, 18 * header_scale)
        text((x + 24) * header_scale, 41 * header_scale, value, 22 * header_scale, color, 700)
    header_right = card_width - 16 * header_scale
    parts.append(f'<path d="M{16 * header_scale} {60 * header_scale}H{header_right}" stroke="#51515a" stroke-width="1"/>')
    header_end = len(parts)
    character_center = CHARACTER_CENTER
    if screenshot:
        character_center = 166
        parts.append(f'<ellipse cx="{character_center}" cy="413" rx="141" ry="220" fill="url(#portrait-glow)"/>')
    else:
        parts.append('<ellipse cx="100" cy="200" rx="90" ry="115" fill="url(#portrait-glow)"/>')
    with Image.open(portrait) as im:
        left, top, right, bottom = im.convert("RGBA").getchannel("A").getbbox()
        width, height = im.size
    # Physical size is part of the xenotype's identity. Fitting each silhouette
    # to the same box would erase it. The shared canvases already encode in-game
    # alignment; processed art has twice the resolution, not twice the physical size.
    character_scale = (CHARACTER_SCALE * header_scale) * 384 / width
    character_x = character_center - width * character_scale / 2
    character_y = CHARACTER_TOP * header_scale
    picture(portrait, character_x, character_y, width * character_scale, height * character_scale)

    positions = []
    filters = []

    def gene_icon(gene, cx, y, size, group):
        tint = f"tint{len(filters)}"
        r, g, b, a = gene.color
        filters.append(f'<filter id="{tint}" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values="{r} 0 0 0 0  0 {g} 0 0 0  0 0 {b} 0 0  0 0 0 {a} 0"/></filter>')
        picture(gene.background or gene_background, cx - size / 2, y, size, size)
        foreground_size = size * GENE_ICON_SCALE
        picture(gene.path, cx - foreground_size / 2, y + (size - foreground_size) / 2,
                foreground_size, foreground_size, f'filter="url(#{tint})"')
        positions.append(dict(defName=gene.defName, group=group, x=cx, y=y, size=size))

    def card_svg(description, title):
        return "\n".join([
            f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{card_width}" height="{card_height}" viewBox="0 0 {card_width} {card_height}">',
            f'<title>{escape(title)}</title>',
            f'<desc>{escape(description)}</desc>',
            '<defs><radialGradient id="portrait-glow"><stop stop-color="#a99bbb" stop-opacity=".16"/><stop offset="1" stop-color="#a99bbb" stop-opacity="0"/></radialGradient>',
            '<linearGradient id="surface" x2="0" y2="1"><stop stop-color="#2b2e33"/><stop offset="1" stop-color="#202226"/></linearGradient>',
            *filters, '</defs>',
            f'<rect x="0.5" y="0.5" width="{card_width-1}" height="{card_height-1}" rx="10" fill="url(#surface)" stroke="#4a4c52" stroke-width="1"/>',
            *parts, '</svg>'])

    biostat_description = f"Complexity {complexity}; metabolism {metabolism}."
    title = f"{name} — {descriptor}"
    if screenshot:
        SCREENSHOT_OUT.mkdir(parents=True, exist_ok=True)

        def save_screenshot(stem, page_title, description):
            svg_path = SCREENSHOT_OUT / f"{stem}.svg"
            png_path = SCREENSHOT_OUT / f"{stem}.png"
            write_text(svg_path, card_svg(description, page_title))
            render_png(["-background", "none", str(svg_path)], png_path)
            print(f"{stem}: {len(positions)} labeled genes; {card_width}x{card_height}")
            return png_path

        rows = screenshot_gene_rows(featured_genes + supporting_genes,
                                    heading_height=48 if lineages else 0,
                                    has_specialties=bool(specialties))
        if lineages:
            first_row = rows[0]
            text(first_row.center, first_row.top - 28,
                 "+ random lineage", 34, "#c9c4cf", 600, anchor="middle")
        if specialties:
            rows.extend(draw_screenshot_specialty_panel(specialties, parts, text))
        draw_screenshot_rows(rows, gene_background, text, picture, gene_icon)
        png_path = save_screenshot(name, title, biostat_description + " Each gene tile shows its label and biostats.")

        if lineages:
            del parts[header_end:]
            filters.clear()
            positions.clear()
            rows = draw_screenshot_lineage_panels(lineages, parts, text)
            draw_screenshot_rows(rows, gene_background, text, picture, gene_icon)
            save_screenshot(f"{name} lineages", f"{name} lineages",
                            "Each scaleborn receives one random lineage.")
        return png_path

    if is_scaleborn:
        genes_right, genes_left, featured_style, supporting_style = SCALEBORN_GENES_RIGHT, SCALEBORN_GENES_LEFT, SCALEBORN_FEATURED_LAYOUT, SCALEBORN_SUPPORTING_LAYOUT
    elif is_titan:
        genes_right, genes_left, featured_style, supporting_style = TITAN_GENES_RIGHT, GENES_LEFT, TITAN_FEATURED_LAYOUT, TITAN_SUPPORTING_LAYOUT
    else:
        genes_right, genes_left, featured_style, supporting_style = GENES_RIGHT, GENES_LEFT, FEATURED_GENE_LAYOUT, SUPPORTING_GENE_LAYOUT
    groups = [
        ("featured", featured_genes, featured_style),
        ("supporting", supporting_genes, supporting_style)]
    gene_groups = [(group_name, balanced_rows(group, style.max_columns), style)
                   for group_name, group, style in groups if group]
    stack_height = sum(len(rows) * style.icon_size + (len(rows) - 1) * style.row_gap
                       for _, rows, style in gene_groups)
    stack_height += max(0, len(gene_groups) - 1) * GENE_GROUP_GAP
    if stack_height > GENES_BOTTOM - GENES_TOP:
        raise ValueError(f"{name} gene stack exceeds the card")
    y = GENES_TOP + (GENES_BOTTOM - GENES_TOP - stack_height) / 2
    group_center = (genes_left + genes_right) / 2
    divider_width = (genes_right - genes_left) * GENE_DIVIDER_FRACTION
    for group_index, (group_name, rows, style) in enumerate(gene_groups):
        if group_index:
            # Equal clearance on either side makes the line a group separator;
            # inset ends keep it from reading as a full-width section boundary.
            divider_y = y + GENE_GROUP_GAP / 2
            parts.append(f'<path d="M{group_center-divider_width/2:.2f} {divider_y:.2f}H{group_center+divider_width/2:.2f}" stroke="#51515a" stroke-width="1"/>')
            y += GENE_GROUP_GAP
        spacing = (genes_right - genes_left) / style.max_columns
        for row_index, row in enumerate(rows):
            if row_index:
                y += style.row_gap
            for col, gene in enumerate(row):
                cx = group_center + (col - (len(row) - 1) / 2) * spacing
                gene_icon(gene, cx, y, style.icon_size, group_name)
            y += style.icon_size

    # Separate colored boxes express mutually exclusive packages. The first icon
    # identifies the skin color without needing another text label.
    if lineages:
        text(534, CHOICE_LABEL_Y, "One lineage of", 16, "#c9c4cf", 600, anchor="middle")
    lineage_top, lineage_bottom = CHOICE_PANEL_TOP, GENES_BOTTOM
    row_height, icon_size = 40, 32
    if lineages and len(lineages) * row_height > lineage_bottom - lineage_top:
        raise ValueError("Scaleborn lineage rows exceed the card")
    row_gap = ((lineage_bottom - lineage_top - len(lineages) * row_height)
               / max(1, len(lineages) - 1))
    for row_index, lineage in enumerate(lineages):
        y = lineage_top + row_index * (row_height + row_gap)
        parts.append(lineage_band(lineage, 454, y, 160, row_height))
        for col, gene in enumerate(lineage.genes):
            gene_icon(gene, 474 + col * 40, y + 4, icon_size, f"lineage:{lineage.skin.defName}")

    if specialties:
        icon_size, gap, columns = 32, 10, 2
        panel_center = 551
        rows = (len(specialties) + columns - 1) // columns
        grid_height = rows * icon_size + (rows - 1) * gap
        panel_height = GENES_BOTTOM - CHOICE_PANEL_TOP
        if grid_height > panel_height:
            raise ValueError("Titan specialty options exceed the card")
        # Match padding and gaps on both axes. An unfilled box lets the card's
        # background gradient continue through the options without a color seam.
        padding = (panel_height - grid_height) / 2
        panel_width = columns * icon_size + (columns - 1) * gap + 2 * padding
        panel_left = panel_center - panel_width / 2
        text(panel_center, CHOICE_LABEL_Y, "One of", 16, "#c9c4cf", 600, anchor="middle")
        parts.append(f'<rect x="{panel_left}" y="{CHOICE_PANEL_TOP}" width="{panel_width}" height="{panel_height}" rx="7" fill="none" stroke="#77717f"/>')
        for index, gene in enumerate(specialties):
            row, column = divmod(index, columns)
            gene_icon(gene, panel_left + padding + icon_size / 2 + column * (icon_size + gap),
                      CHOICE_PANEL_TOP + padding + row * (icon_size + gap), icon_size, "specialty")

    lineage_description = (" Shared genes on the left; five alternative lineage packages on the right, "
                           "with skin colors first inside each box." if lineages else "")
    specialty_description = (" Shared genes on the left; one random specialty from the boxed "
                             "aptitudes on the right, excluding skills with existing aptitudes." if specialties else "")
    description = (biostat_description + " New functional genes above vanilla, cosmetic, and psycast genes. "
                   "Each group is sorted by category priority descending, gene priority ascending, "
                   f"and label alphabetically.{lineage_description}{specialty_description}")
    stem = name
    svg_path = GENERATED_OUT / (stem + ".svg")
    png_path = PNG_OUT / (stem + ".png")
    write_text(svg_path, card_svg(description, title))
    def gene_data(gene):
        data = asdict(gene)
        data["path"] = str(gene.path.relative_to(ROOT))
        if gene.background is None:
            del data["background"]
        else:
            data["background"] = str(gene.background.relative_to(ROOT))
        return data

    manifest = dict(defName=def_name, xenotype=name, descriptor=descriptor, complexity=complexity, metabolism=metabolism,
                    width=CARD_WIDTH, height=CARD_HEIGHT, character=str(portrait.relative_to(ROOT)),
                    character_layout=dict(x=character_x, y=character_y, scale=character_scale,
                                          source_width=width, source_height=height,
                                          scale_per_original_pixel=CHARACTER_SCALE,
                                          visible_bounds=[left, top, right, bottom]),
                    totals_scope="Defined xenotype genes before random or lineage additions; Royalty included.",
                    genes=[gene_data(gene) for gene in base_genes],
                    layout=positions)
    if lineages:
        manifest["lineages"] = [dict(skin=lineage.skin.defName, color=lineage.skin.color,
                                    genes=[gene_data(gene) for gene in lineage.genes]) for lineage in lineages]
    if specialties:
        manifest["specialties"] = [gene_data(gene) for gene in specialties]
    write_text(GENERATED_OUT / (stem + ".json"), json.dumps(manifest, indent=2))
    render_png(["-background", "none", str(svg_path)], png_path)
    with Image.open(png_path) as saved:
        saved.verify()
    print(f"{name}: {len(featured_genes)} featured + {len(supporting_genes)} supporting; complexity {complexity}; metabolism {metabolism}; {CARD_WIDTH}x{CARD_HEIGHT}")
    return png_path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rimworld-data", type=Path, default=GAME_DATA)
    parser.add_argument("--config", type=Path, default=Path(__file__).with_name("xenotype-cards.json"))
    parser.add_argument("--screenshot", nargs="?", const="Dvergr", metavar="XENOTYPE",
                        help="Build screenshot cards for one xenotype (defaults to Dvergr)")
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding="utf-8"))
    descriptions = list(config["descriptions"].items())
    library = GeneLibrary(args.rimworld_data)
    if {name for name, _ in descriptions} != {name for name in library.xenotypes if name and name.startswith("Xyl")}:
        raise ValueError("Description config must match the complete mod xenotype roster")
    if args.screenshot:
        def_name = "Xyl" + args.screenshot
        if def_name not in config["descriptions"]:
            available = ", ".join(name.removeprefix("Xyl") for name, _ in descriptions)
            parser.error(f"unknown xenotype {args.screenshot!r}; choose from {available}")
        build_card(library, def_name, config["descriptions"][def_name], screenshot=True)
        return
    GENERATED_OUT.mkdir(parents=True, exist_ok=True)
    PNG_OUT.mkdir(parents=True, exist_ok=True)
    paths = [build_card(library, name, descriptor) for name, descriptor in descriptions]
    render_png(["montage", *map(str, paths), "-thumbnail", f"{CARD_WIDTH}x{CARD_HEIGHT}", "-tile", "3x",
                "-geometry", "+12+12", "-gravity", "north", "-background", "#17191c"],
               PNG_OUT / "all-xenotypes.png")
    cards = []
    for path, (def_name, descriptor) in zip(paths, descriptions):
        name = library.value(library.xenotypes[def_name], "label").capitalize()
        png_url = Path(os.path.relpath(path, GENERATED_OUT)).as_posix()
        cards.append(f'<figure><a href="{png_url}"><img src="{png_url}" alt="{name}: {descriptor}" loading="lazy"></a><figcaption>{name} · <a href="{path.stem}.svg">SVG</a></figcaption></figure>')
    # A fixed-width review gallery exposes the actual Workshop reading scale;
    # responsive thumbnail columns would conceal the very legibility being judged.
    card_markup = "\n".join(cards)
    write_text(GENERATED_OUT / "index.html", f'''<!doctype html>
<html lang="en">
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Xyl’s Xenotypes — xenotype cards</title>
<style>
body {{ margin:24px; background:#17191c; color:#ebe7df; font:16px Segoe UI,sans-serif }}
main {{ display:grid; grid-template-columns:{CARD_WIDTH}px; gap:24px }}
figure {{ margin:0 }}
img {{ display:block; width:{CARD_WIDTH}px; height:{CARD_HEIGHT}px }}
a {{ color:#b8afc8 }}
figcaption {{ padding:8px }}
p {{ max-width:{CARD_WIDTH}px }}
</style>
<h1>Xyl’s Xenotypes</h1>
<p>Cards shown at their native {CARD_WIDTH}-pixel Workshop display width.
Descriptions from CONTENT.md. Totals reflect the defined xenotype gene lists,
before random or lineage additions. Royalty genes included. Select a card for the PNG.</p>
<main>
{card_markup}
</main>
</html>''')
    for def_name, descriptor in descriptions:
        build_card(library, def_name, descriptor, screenshot=True)


if __name__ == "__main__":
    main()
