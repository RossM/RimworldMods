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
# Work at the Workshop description column's observed display width so legibility
# is judged at the size readers actually see, rather than on enlarged previews.
CARD_WIDTH = 630
CARD_HEIGHT = 340
GENES_LEFT = 200
GENES_RIGHT = CARD_WIDTH - 16
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
    top: int
    bottom: int


# Reserve consistent bands so cards can be compared without groups moving around
# with every gene count. Unlabeled 52px/32px icons remain readable at page scale.
FEATURED_GENE_LAYOUT = GeneLayout(max_columns=5, icon_size=52, row_gap=12, top=78, bottom=194)
SUPPORTING_GENE_LAYOUT = GeneLayout(max_columns=10, icon_size=32, row_gap=10, top=214, bottom=330)


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
    return "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def write_text(path, text):
    with path.open("w", encoding="utf-8", newline="\r\n") as output:
        output.write(text + "\n")


def render_png(arguments, path):
    # Direct ImageMagick output can fail when overwriting Windows previews even
    # while Python can write the file. Let ImageMagick render and Python save.
    rendered = subprocess.run(["magick", *arguments, "PNG:-"],
                              check=True, stdout=subprocess.PIPE)
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
    ordered = featured_genes + supporting_genes
    complexity = sum(g.complexity for g in genes)
    metabolism = sum(g.metabolism for g in genes)
    portrait = ROOT / "Art/Cutouts/Processed" / (name + "_South.png")
    if not portrait.is_file():
        portrait = ROOT / "Art/Cutouts" / (name + "_South.png")
    parts = []

    def text(x, y, value, size=22, fill="#ebe7df", weight=400):
        parts.append(f'<text x="{x}" y="{y}" fill="{fill}" font-family="Segoe UI, sans-serif" font-size="{size}" font-weight="{weight}">{escape(str(value))}</text>')

    def picture(path, x, y, width, height, attributes=""):
        parts.append(f'<image x="{x}" y="{y}" width="{width}" height="{height}" xlink:href="{image_uri(path)}" {attributes}/>')

    picture(MOD / "Textures" / (xenotype.findtext("iconPath") + ".png"), 16, 14, 36, 36)
    title_size = 24
    descriptor_size = 14
    descriptor_x = 64 + ImageFont.truetype(BOLD_FONT, title_size).getlength(name.upper()) + 14
    if descriptor_x + ImageFont.truetype(FONT, descriptor_size).getlength(descriptor) > 480:
        raise ValueError(f"{name} title and descriptor overlap the biostats")
    text(64, 40, name.upper(), title_size, weight=700)
    text(round(descriptor_x, 2), 40, descriptor, descriptor_size, "#c9c4cf")
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
    gene_index = 0
    gene_groups = [("featured", featured_genes, FEATURED_GENE_LAYOUT),
                   ("supporting", supporting_genes, SUPPORTING_GENE_LAYOUT)]
    if featured_genes and supporting_genes:
        parts.append(f'<path d="M{GENES_LEFT} 204H{GENES_RIGHT}" stroke="#51515a" stroke-width="1"/>')
    for group_name, group, style in gene_groups:
        if not group:
            continue
        spacing = (GENES_RIGHT - GENES_LEFT) / style.max_columns
        group_center = (GENES_LEFT + GENES_RIGHT) / 2
        rows = balanced_rows(group, style.max_columns)
        group_height = len(rows) * style.icon_size + (len(rows) - 1) * style.row_gap
        if group_height > style.bottom - style.top:
            raise ValueError(f"{name} {group_name} genes exceed their layout band")
        y = style.top + (style.bottom - style.top - group_height) / 2
        for row in rows:
            for col, gene in enumerate(row):
                cx = group_center + (col - (len(row) - 1) / 2) * spacing
                icon_left = cx - style.icon_size / 2
                picture(ART / "Genes/GeneBackground_Endogene.png", icon_left, y, style.icon_size, style.icon_size)
                picture(gene.path, icon_left, y, style.icon_size, style.icon_size, f'filter="url(#tint{gene_index})"')
                positions.append(dict(defName=gene.defName, group=group_name,
                                      x=cx, y=y, size=style.icon_size))
                gene_index += 1
            y += style.icon_size + style.row_gap
    filters = []
    for i, gene in enumerate(ordered):
        r, g, b, a = gene.color
        filters.append(f'<filter id="tint{i}" color-interpolation-filters="sRGB"><feColorMatrix type="matrix" values="{r} 0 0 0 0  0 {g} 0 0 0  0 0 {b} 0 0  0 0 0 {a} 0"/></filter>')
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="{CARD_WIDTH}" height="{CARD_HEIGHT}" viewBox="0 0 {CARD_WIDTH} {CARD_HEIGHT}">',
           f'<title>{escape(name)} — {escape(descriptor)}</title>',
           f'<desc>Complexity {complexity}; metabolism {metabolism}. New functional genes above vanilla, cosmetic, and psycast genes. Each group is sorted by category priority descending, gene priority ascending, and label alphabetically.</desc>',
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
                    genes=[dict(asdict(gene), path=str(gene.path.relative_to(ROOT))) for gene in ordered],
                    layout=positions)
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
    with zipfile.ZipFile(OUT / "xenotype-card-mockups.zip", "w", zipfile.ZIP_DEFLATED) as bundle:
        for path in paths:
            for artifact in (path, path.with_suffix(".svg"), path.with_name(path.stem + "-data.json")):
                bundle.write(artifact, artifact.name)
        for filename in ("index.html", "all-xenotypes.png"):
            bundle.write(OUT / filename, filename)


if __name__ == "__main__":
    main()
