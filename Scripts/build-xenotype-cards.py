"""Build Workshop PNG/SVG mockups from XML, descriptor config, and cutouts.

The cards help Workshop readers compare xenotypes and understand what the mod
adds at a glance. Emphasize distinctive mechanics while retaining the familiar
vanilla genes that give a complete picture of each xenotype's strengths and costs.

Run: python Scripts/build-xenotype-cards.py
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

PNG/SVG cards, diagnostic JSON snapshots, a review gallery, and a ZIP bundle go to
Art/Workshop/Mockups/. Cutouts retain their original in-game canvas alignment.
"""

import argparse
import base64
from dataclasses import asdict, dataclass
from functools import lru_cache
import json
from pathlib import Path
import re
import subprocess
from xml.sax.saxutils import escape
import xml.etree.ElementTree as ET
import zipfile

from PIL import Image, ImageFont

from gene_art import GAME_DATA, inherited_text, parse_color, read_defs, read_gene_icons


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Art/Rimworld art"
MOD = ROOT / "XylXenos/Data"
OUT = ROOT / "Art/Workshop/Mockups"
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

    @property
    def sort_key(self):
        # Match GeneUtility.SortGeneDefs so readers can compare cards with the gene UI.
        return (-self.category_priority, self.priority, self.label.casefold())

    @property
    def featured(self):
        # Emphasis introduces unfamiliar mechanics, rather than every mod asset.
        # Psycasts already have familiar icons; zero-cost cosmetics need no equal
        # billing with mechanics. Keep functional tails, horns, and skin defenses.
        return (self.source == "mod" and self.category != "XylPsycasts"
                and (self.complexity != 0 or self.metabolism != 0))


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
SCALEBORN_GENES_RIGHT = 410
SCALEBORN_FEATURED_LAYOUT = GeneLayout(max_columns=3, icon_size=52, row_gap=12)
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
                stats = next(s for s in definition.findall("biostats/li")
                             if int(s.findtext("levels").split("~")[0]) <= level
                             <= int(s.findtext("levels").split("~")[-1]))
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

        if name in self.mod_icons:
            rendered = self.mod_icons[name]
            texture, color, origin = rendered.texture, rendered.color, "mod"
        else:
            texture = ART / "Genes" / (icon.removeprefix("UI/Icons/Genes/") + ".png")
            color = parse_color(self.value(definition, "skinColorOverride")
                                or self.value(definition, "hairColorOverride")
                                or self.value(definition, "iconColor"))
            origin = "vanilla"
        if not texture.is_file():
            raise FileNotFoundError(texture)
        category = self.value(definition, "displayCategory") or "Miscellaneous"
        return Gene(name, label, texture, color, origin, category,
                    float(self.value(self.categories[category], "displayPriorityInXenotype") or 0),
                    priority, complexity, metabolism, archites)


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


def build_card(library, def_name, descriptor):
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
    portrait = ROOT / "Art/Cutouts/Processed" / (name + "_South.png")
    if not portrait.is_file():
        portrait = ROOT / "Art/Cutouts" / (name + "_South.png")
    parts = []

    def text(x, y, value, size=22, fill="#ebe7df", weight=400, anchor="start"):
        alignment = f' text-anchor="{anchor}"' if anchor != "start" else ""
        parts.append(f'<text x="{x}" y="{y}" fill="{fill}" font-family="Segoe UI, sans-serif" font-size="{size}" font-weight="{weight}"{alignment}>{escape(str(value))}</text>')

    def picture(path, x, y, width, height, attributes=""):
        parts.append(f'<image x="{x}" y="{y}" width="{width}" height="{height}" xlink:href="{image_uri(path)}" {attributes}/>')

    picture(MOD / "Textures" / (xenotype.findtext("iconPath") + ".png"), 16, 14, 36, 36)
    title_size = 24
    descriptor_size = 18
    title_font = ImageFont.truetype(BOLD_FONT, title_size)
    descriptor_font = ImageFont.truetype(SEMIBOLD_FONT, descriptor_size)
    descriptor_x = 64 + title_font.getlength(name.upper()) + 20
    if descriptor_x + descriptor_font.getlength(descriptor) > 480:
        raise ValueError(f"{name} title and descriptor overlap the biostats")
    # Center ascenders through the baseline beside the capitals. Descenders hang
    # below that span; including them would push words with g/p/y upward.
    title_top = title_font.getbbox(name.upper(), anchor="ls")[1]
    descriptor_top = descriptor_font.getbbox(descriptor, anchor="ls")[1]
    descriptor_y = 40 + (title_top - descriptor_top) / 2
    text(64, 40, name.upper(), title_size, weight=700)
    text(round(descriptor_x, 2), descriptor_y, descriptor, descriptor_size, "#c9c4cf", weight=600)
    for x, value, symbol, color in [
            (494, complexity, "Complexity.png", "#8bc9db"),
            (558, f"{metabolism:+}" if metabolism else "0", "Metabolism.png", "#d39ab3")]:
        picture(ART / symbol, x, 24, 18, 18)
        text(x + 24, 41, value, 22, color, 700)
    parts.append(f'<path d="M16 60H{GENES_RIGHT}" stroke="#51515a" stroke-width="1"/>')
    parts.append('<ellipse cx="100" cy="200" rx="90" ry="115" fill="url(#portrait-glow)"/>')
    with Image.open(portrait) as im:
        left, top, right, bottom = im.convert("RGBA").getchannel("A").getbbox()
        width, height = im.size
    # Physical size is part of the xenotype's identity. Fitting each silhouette
    # to the same box would erase it. The shared canvases already encode in-game
    # alignment; processed art has twice the resolution, not twice the physical size.
    character_scale = CHARACTER_SCALE * 384 / width
    character_x = CHARACTER_CENTER - width * character_scale / 2
    character_y = CHARACTER_TOP
    picture(portrait, character_x, character_y, width * character_scale, height * character_scale)

    positions = []
    filters = []

    def gene_icon(gene, cx, y, size, group):
        tint = f"tint{len(filters)}"
        r, g, b, a = gene.color
        filters.append(f'<filter id="{tint}" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values="{r} 0 0 0 0  0 {g} 0 0 0  0 0 {b} 0 0  0 0 0 {a} 0"/></filter>')
        picture(ART / "Genes/GeneBackground_Endogene.png", cx - size / 2, y, size, size)
        picture(gene.path, cx - size / 2, y, size, size, f'filter="url(#{tint})"')
        positions.append(dict(defName=gene.defName, group=group, x=cx, y=y, size=size))

    if is_scaleborn:
        genes_right, featured_style, supporting_style = SCALEBORN_GENES_RIGHT, SCALEBORN_FEATURED_LAYOUT, SCALEBORN_SUPPORTING_LAYOUT
    elif is_titan:
        genes_right, featured_style, supporting_style = TITAN_GENES_RIGHT, TITAN_FEATURED_LAYOUT, TITAN_SUPPORTING_LAYOUT
    else:
        genes_right, featured_style, supporting_style = GENES_RIGHT, FEATURED_GENE_LAYOUT, SUPPORTING_GENE_LAYOUT
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
    group_center = (GENES_LEFT + genes_right) / 2
    divider_width = (genes_right - GENES_LEFT) * GENE_DIVIDER_FRACTION
    for group_index, (group_name, rows, style) in enumerate(gene_groups):
        if group_index:
            # Equal clearance on either side makes the line a group separator;
            # inset ends keep it from reading as a full-width section boundary.
            divider_y = y + GENE_GROUP_GAP / 2
            parts.append(f'<path d="M{group_center-divider_width/2:.2f} {divider_y:.2f}H{group_center+divider_width/2:.2f}" stroke="#51515a" stroke-width="1"/>')
            y += GENE_GROUP_GAP
        spacing = (genes_right - GENES_LEFT) / style.max_columns
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
        text(522, CHOICE_LABEL_Y, "One lineage of", 16, "#c9c4cf", 600, anchor="middle")
    lineage_top, lineage_bottom = CHOICE_PANEL_TOP, GENES_BOTTOM
    row_height, icon_size = 40, 32
    if lineages and len(lineages) * row_height > lineage_bottom - lineage_top:
        raise ValueError("Scaleborn lineage rows exceed the card")
    row_gap = ((lineage_bottom - lineage_top - len(lineages) * row_height)
               / max(1, len(lineages) - 1))
    for row_index, lineage in enumerate(lineages):
        y = lineage_top + row_index * (row_height + row_gap)
        rgb = tuple(round(channel * 255) for channel in lineage.skin.color[:3])
        border = tuple(round((channel * 0.65 + 0.35) * 255) for channel in lineage.skin.color[:3])
        parts.append(f'<rect x="430" y="{y}" width="184" height="{row_height}" rx="7" fill="rgb{rgb}" fill-opacity=".18" stroke="rgb{border}" stroke-opacity=".7"/>')
        for col, gene in enumerate(lineage.genes):
            gene_icon(gene, 450 + col * 48, y + 4, icon_size, f"lineage:{lineage.skin.defName}")

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
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{CARD_WIDTH}" height="{CARD_HEIGHT}" viewBox="0 0 {CARD_WIDTH} {CARD_HEIGHT}">',
           f'<title>{escape(name)} — {escape(descriptor)}</title>',
           f'<desc>Complexity {complexity}; metabolism {metabolism}. New functional genes above vanilla, cosmetic, and psycast genes. Each group is sorted by category priority descending, gene priority ascending, and label alphabetically.{lineage_description}{specialty_description}</desc>',
           '<defs><radialGradient id="portrait-glow"><stop stop-color="#a99bbb" stop-opacity=".16"/><stop offset="1" stop-color="#a99bbb" stop-opacity="0"/></radialGradient>',
           '<linearGradient id="surface" x2="0" y2="1"><stop stop-color="#2b2e33"/><stop offset="1" stop-color="#202226"/></linearGradient>',
           *filters, '</defs>',
           f'<rect x="0.5" y="0.5" width="{CARD_WIDTH-1}" height="{CARD_HEIGHT-1}" rx="10" fill="url(#surface)" stroke="#4a4c52" stroke-width="1"/>',
           *parts, '</svg>']
    stem = name.lower() + "-card"
    svg_path = OUT / (stem + ".svg")
    png_path = OUT / (stem + ".png")
    write_text(svg_path, "\n".join(svg))
    manifest = dict(defName=def_name, xenotype=name, descriptor=descriptor, complexity=complexity, metabolism=metabolism,
                    width=CARD_WIDTH, height=CARD_HEIGHT, character=str(portrait.relative_to(ROOT)),
                    character_layout=dict(x=character_x, y=character_y, scale=character_scale,
                                          source_width=width, source_height=height,
                                          scale_per_original_pixel=CHARACTER_SCALE,
                                          visible_bounds=[left, top, right, bottom]),
                    totals_scope="Defined xenotype genes before random or lineage additions; Royalty included.",
                    genes=[dict(asdict(gene), path=str(gene.path.relative_to(ROOT))) for gene in base_genes],
                    layout=positions)
    if lineages:
        manifest["lineages"] = [dict(skin=lineage.skin.defName, color=lineage.skin.color,
                                    genes=[dict(asdict(gene), path=str(gene.path.relative_to(ROOT)))
                                           for gene in lineage.genes]) for lineage in lineages]
    if specialties:
        manifest["specialties"] = [dict(asdict(gene), path=str(gene.path.relative_to(ROOT)))
                                   for gene in specialties]
    write_text(OUT / (stem + "-data.json"), json.dumps(manifest, indent=2))
    render_png(["-background", "none", str(svg_path)], png_path)
    with Image.open(png_path) as saved:
        saved.verify()
    print(f"{name}: {len(featured_genes)} featured + {len(supporting_genes)} supporting; complexity {complexity}; metabolism {metabolism}; {CARD_WIDTH}x{CARD_HEIGHT}")
    return png_path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rimworld-data", type=Path, default=GAME_DATA)
    parser.add_argument("--config", type=Path, default=Path(__file__).with_name("xenotype-cards.json"))
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding="utf-8"))
    descriptions = list(config["descriptions"].items())
    library = GeneLibrary(args.rimworld_data)
    if {name for name, _ in descriptions} != {name for name in library.xenotypes if name and name.startswith("Xyl")}:
        raise ValueError("Description config must match the complete mod xenotype roster")
    OUT.mkdir(parents=True, exist_ok=True)
    paths = [build_card(library, name, descriptor) for name, descriptor in descriptions]
    render_png(["montage", *map(str, paths), "-thumbnail", f"{CARD_WIDTH}x{CARD_HEIGHT}", "-tile", "3x",
                "-geometry", "+12+12", "-gravity", "north", "-background", "#17191c"],
               OUT / "all-xenotypes.png")
    cards = [f'<figure><a href="{path.name}"><img src="{path.name}" alt="{name}: {descriptor}" loading="lazy"></a><figcaption>{name} · <a href="{path.stem}.svg">SVG</a></figcaption></figure>'
             for path, (def_name, descriptor) in zip(paths, descriptions)
             for name in [library.value(library.xenotypes[def_name], "label").capitalize()]]
    # A fixed-width review gallery exposes the actual Workshop reading scale;
    # responsive thumbnail columns would conceal the very legibility being judged.
    card_markup = "\n".join(cards)
    write_text(OUT / "index.html", f'''<!doctype html>
<html lang="en">
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Xyl’s Xenotypes — card mockups</title>
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
    artifacts = []
    for path in paths:
        artifacts.extend((path, path.with_suffix(".svg"), path.with_name(path.stem + "-data.json")))
    artifacts.extend(OUT / name for name in ("index.html", "all-xenotypes.png"))
    with zipfile.ZipFile(OUT / "xenotype-card-mockups.zip", "w") as bundle:
        for artifact in artifacts:
            # Filesystem modification times reflect the build, not the content.
            # A fixed ZIP epoch keeps the bundle stable when its files are stable.
            entry = zipfile.ZipInfo(artifact.name, date_time=(1980, 1, 1, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            bundle.writestr(entry, artifact.read_bytes())


if __name__ == "__main__":
    main()
