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
FONT = "C:/Windows/Fonts/segoeui.ttf"
BOLD_FONT = "C:/Windows/Fonts/segoeuib.ttf"
# Keep each xenotype a compact, scannable Workshop panel. Landscape proportions
# leave room for the character and at most two rows of the current new-gene lists.
CARD_WIDTH = 1700
CARD_HEIGHT = 1000
GENES_LEFT = 480
GENES_RIGHT = CARD_WIDTH - 44
# Both cutout sets retain the same camera scale on 384px/768px canvases.
CHARACTER_SCALE = 1.7
CHARACTER_CENTER = 244
CHARACTER_TOP = 250


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


@dataclass(frozen=True)
class GeneLayout:
    heading: str
    max_columns: int
    icon_size: int
    font_size: int
    label_offset: int
    label_leading: int
    minimum_row_pitch: int


# New genes explain the mod's distinctive additions and deserve the visual emphasis.
# Familiar vanilla genes remain visible as supporting context at a smaller size.
NEW_GENE_LAYOUT = GeneLayout(
    heading="NEW GENES", max_columns=5, icon_size=112, font_size=23,
    label_offset=143, label_leading=28, minimum_row_pitch=170)
VANILLA_GENE_LAYOUT = GeneLayout(
    heading="VANILLA GENES", max_columns=10, icon_size=64, font_size=18,
    label_offset=87, label_leading=22, minimum_row_pitch=114)


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

    def gene(self, name):
        definition = self.genes.get(name)
        if definition is not None:
            label = self.value(definition, "label")
            icon = self.value(definition, "iconPath")
            priority = float(self.value(definition, "displayOrderInCategory") or 0)
            complexity = int(self.value(definition, "biostatCpx") or 0)
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
    if path.suffix == ".psd":
        # Keep conversions with their source art so exports do not accumulate
        # competing copies in presentation-output directories.
        png = path.with_suffix(".png")
        if not png.is_file() or png.stat().st_mtime_ns < path.stat().st_mtime_ns:
            subprocess.run(["magick", str(path) + "[0]", str(png)], check=True)
        path = png
    return "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def caption(label, font_size, width):
    label = label[:1].upper() + label[1:]
    words = label.split()
    while max(ImageFont.truetype(FONT, font_size).getlength(word) for word in words) > width:
        font_size -= 1
    font = ImageFont.truetype(FONT, font_size)
    lines = [""]
    for word in words:
        candidate = (lines[-1] + " " + word).strip()
        if lines[-1] and font.getlength(candidate) > width:
            lines.append(word)
        else:
            lines[-1] = candidate
    return lines, font_size


def write_text(path, text):
    with path.open("w", encoding="utf-8", newline="\r\n") as output:
        output.write(text + "\n")


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
    new_genes = [gene for gene in sorted_genes if gene.source == "mod"]
    vanilla_genes = [gene for gene in sorted_genes if gene.source == "vanilla"]
    ordered = new_genes + vanilla_genes
    complexity = sum(g.complexity for g in genes)
    metabolism = sum(g.metabolism for g in genes)
    portrait = ROOT / "Art/Cutouts/Processed" / (name + "_South.png")
    if not portrait.is_file():
        portrait = ROOT / "Art/Cutouts" / (name + "_South.png")
    parts = []

    def text(x, y, value, size=22, fill="#ebe7df", weight=400, anchor="start", tracking=0):
        parts.append(f'<text x="{x}" y="{y}" fill="{fill}" font-family="Segoe UI, sans-serif" font-size="{size}" font-weight="{weight}" text-anchor="{anchor}" letter-spacing="{tracking}">{escape(str(value))}</text>')

    def picture(path, x, y, width, height, attributes=""):
        parts.append(f'<image x="{x}" y="{y}" width="{width}" height="{height}" xlink:href="{image_uri(path)}" {attributes}/>')

    text(52, 45, "XYL’S XENOTYPES", 17, "#b8afc8", 600, tracking=3)
    picture(MOD / "Textures" / (xenotype.findtext("iconPath") + ".png"), 52, 84, 108, 108)
    title_size = 72
    while ImageFont.truetype(BOLD_FONT, title_size).getlength(name.upper()) + 2 * len(name) > 1040:
        title_size -= 1
    text(184, 150, name.upper(), title_size, weight=600, tracking=2)
    text(187, 192, descriptor, 26, "#c9c4cf")
    for x, label, value, symbol, color in [
            (1290, "COMPLEXITY", complexity, "Complexity.png", "#8bc9db"),
            (1484, "METABOLISM", f"{metabolism:+}" if metabolism else "0", "Metabolism.png", "#d39ab3")]:
        text(x, 104, label, 17, "#bcb8c3", 600, tracking=1)
        picture(ART / symbol, x - 3, 121, 45, 45)
        text(x + 52, 164, value, 48, color, 600)
    parts.append(f'<path d="M44 214H{GENES_RIGHT}" stroke="#51515a" stroke-width="1"/>')
    parts.append('<ellipse cx="244" cy="600" rx="213" ry="310" fill="url(#portrait-glow)"/>')
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
    y = 292
    gene_index = 0
    gene_groups = [(new_genes, NEW_GENE_LAYOUT), (vanilla_genes, VANILLA_GENE_LAYOUT)]
    for group_index, (group, style) in enumerate(gene_groups):
        if not group:
            continue
        spacing = (GENES_RIGHT - GENES_LEFT) / style.max_columns
        group_center = (GENES_LEFT + GENES_RIGHT) / 2
        rows = balanced_rows(group, style.max_columns)
        if group_index == 0:
            text(GENES_LEFT, y - 34, style.heading, 20, "#b8afc8", 600, tracking=2)
        else:
            parts.append(f'<path d="M{GENES_LEFT} {y-67}H{GENES_RIGHT}" stroke="#51515a" stroke-width="1"/>')
            text(GENES_LEFT, y - 28, style.heading, 18, "#bcb8c3", 600, tracking=2)
        for row_index, row in enumerate(rows):
            row_bottom = y + style.icon_size
            for col, gene in enumerate(row):
                cx = group_center + (col - (len(row) - 1) / 2) * spacing
                icon_left = cx - style.icon_size / 2
                picture(ART / "Genes/GeneBackground_Endogene.png", icon_left, y, style.icon_size, style.icon_size)
                picture(gene.path, icon_left, y, style.icon_size, style.icon_size, f'filter="url(#tint{gene_index})"')
                lines, actual_size = caption(gene.label, style.font_size, spacing - 16)
                for line, label in enumerate(lines):
                    text(cx, y + style.label_offset + line * style.label_leading,
                         label, actual_size, "#e0dce4", anchor="middle")
                label_bottom = y + style.label_offset + (len(lines)-1) * style.label_leading + 6
                row_bottom = max(row_bottom, label_bottom)
                positions.append(dict(defName=gene.defName, x=cx, y=y, size=style.icon_size,
                                      label_lines=len(lines), label_bottom=label_bottom))
                gene_index += 1
            if row_index + 1 < len(rows):
                y += max(style.minimum_row_pitch, row_bottom-y+22)
        y = row_bottom + (81 if group_index == 0 else 24)
    if y > CARD_HEIGHT:
        raise ValueError(f"{name} gene layout exceeds the {CARD_WIDTH}x{CARD_HEIGHT} card: {y}")
    filters = []
    for i, gene in enumerate(ordered):
        r, g, b, a = gene.color
        filters.append(f'<filter id="tint{i}" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values="{r} 0 0 0 0  0 {g} 0 0 0  0 0 {b} 0 0  0 0 0 {a} 0"/></filter>')
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{CARD_WIDTH}" height="{CARD_HEIGHT}" viewBox="0 0 {CARD_WIDTH} {CARD_HEIGHT}">',
           f'<title>{escape(name)} — {escape(descriptor)}</title>',
           f'<desc>Complexity {complexity}; metabolism {metabolism}. Mod genes above vanilla genes, sorted by category priority descending, gene priority ascending, and label alphabetically.</desc>',
           '<defs><radialGradient id="portrait-glow"><stop stop-color="#a99bbb" stop-opacity=".16"/><stop offset="1" stop-color="#a99bbb" stop-opacity="0"/></radialGradient>',
           '<linearGradient id="surface" x2="0" y2="1"><stop stop-color="#2b2e33"/><stop offset="1" stop-color="#202226"/></linearGradient>',
           *filters, '</defs>',
           f'<rect x="1" y="1" width="{CARD_WIDTH-2}" height="{CARD_HEIGHT-2}" rx="22" fill="url(#surface)" stroke="#4a4c52" stroke-width="2"/>',
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
                    genes=[dict(asdict(gene), path=str(gene.path.relative_to(ROOT))) for gene in ordered],
                    layout=positions)
    write_text(OUT / (stem + "-data.json"), json.dumps(manifest, indent=2))
    subprocess.run(["magick", "-background", "none", str(svg_path), str(png_path)], check=True)
    with Image.open(png_path) as saved:
        saved.verify()
    print(f"{name}: {len(new_genes)} new + {len(vanilla_genes)} vanilla; complexity {complexity}; metabolism {metabolism}; {CARD_WIDTH}x{CARD_HEIGHT}")
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
    subprocess.run(["magick", "montage", *map(str, paths), "-thumbnail", "567x334", "-tile", "3x",
                    "-geometry", "+12+12", "-gravity", "north", "-background", "#17191c",
                    str(OUT / "all-xenotypes.png")], check=True)
    cards = [f'<figure><a href="{path.name}"><img src="{path.name}" alt="{name}: {descriptor}" loading="lazy"></a><figcaption>{name} · <a href="{path.stem}.svg">SVG</a></figcaption></figure>'
             for path, (def_name, descriptor) in zip(paths, descriptions)
             for name in [library.value(library.xenotypes[def_name], "label").capitalize()]]
    write_text(OUT / "index.html", '<!doctype html><html lang="en"><meta charset="utf-8"><title>Xyl’s Xenotypes — card mockups</title><style>body{margin:24px;background:#17191c;color:#ebe7df;font:16px Segoe UI,sans-serif}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(350px,1fr));gap:24px}figure{margin:0}img{width:100%;height:auto}a{color:#b8afc8}figcaption{padding:8px}p{max-width:900px}</style><h1>Xyl’s Xenotypes</h1><p>Descriptions from CONTENT.md. Totals reflect the defined xenotype gene lists, before random or lineage additions. Royalty genes included. Select a card for the full-size PNG.</p><main>' + "\n".join(cards) + '</main></html>')
    with zipfile.ZipFile(OUT / "xenotype-card-mockups.zip", "w", zipfile.ZIP_DEFLATED) as bundle:
        for path in paths:
            for artifact in (path, path.with_suffix(".svg"), path.with_name(path.stem + "-data.json")):
                bundle.write(artifact, artifact.name)
        for filename in ("index.html", "all-xenotypes.png"):
            bundle.write(OUT / filename, filename)


if __name__ == "__main__":
    main()
