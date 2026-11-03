"""Keep gene icons and Workshop cards consistent in how they interpret source art.

Both renderers use these inheritance, pigment-color, and texture-resolution rules
so a new gene or art correction appears consistently in both kinds of output.
"""

from dataclasses import dataclass
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
DEF_DIR = ROOT / "XylXenos/Data/Defs/GeneDefs"
# Vanilla textures are extracted source art; this library does not read Assets.zip.
ART_DIR = ROOT / "Art/Rimworld art"
GAME_DATA = Path("C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Data")
WHITE = (1.0, 1.0, 1.0, 1.0)


@dataclass(frozen=True)
class GeneIcon:
    name: str
    label: str
    texture: Path
    color: tuple[float, ...]
    background: Path | None = None


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
        path = ROOT / "XylXenos/Data/Textures" / (icon_path + ".png")
    elif icon_path.startswith("UI/Icons/Genes/"):
        path = ART_DIR / "Genes" / (icon_path.removeprefix("UI/Icons/Genes/") + ".png")
    elif icon_path.startswith("UI/Abilities/"):
        # Preprocessing the silhouette avoids jagged edges when gene icons are
        # enlarged. Compositors read finished art and never invoke the GPU model.
        path = ART_DIR / "Psycasts/Processed" / (Path(icon_path).name + ".png")
        if not path.is_file():
            raise FileNotFoundError(f"Missing processed psycast art: {path}; run python Scripts/process-psycasts.py")
    else:
        raise ValueError(f"Unsupported icon path: {icon_path}")
    if not path.is_file():
        raise FileNotFoundError(f"Missing source art: {path}")
    return path


def gene_background(definition, parents):
    path = inherited_text(
        definition, "modExtensions/li[@Class='Xylib.DefModExtension_GeneWithComps']/backgroundPath",
        parents)
    return texture_path(path) if path else None


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
        icons[name] = GeneIcon(name, label, texture_path(icon_path), color,
                               gene_background(gene, parents))

    # Mirror GeneDefGenerator's template naming and ability icon substitution for
    # the psycasts referenced by the mod's xenotypes, rather than unused abilities.
    templates = read_defs(DEF_DIR, "XylXenos.GeneTemplateDef")
    template_parents = {template.attrib["Name"]: template for template in templates
                        if "Name" in template.attrib}
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
        background = gene_background(template, template_parents)
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
            icons[name] = GeneIcon(name, label, texture_path(icon_path), WHITE, background)
        if missing := wanted - icons.keys():
            raise ValueError(f"Missing psycast definitions: {sorted(missing)}")
    return sorted(icons.values(), key=lambda icon: icon.name)
