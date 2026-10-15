"""Load XML once and translate RimWorld classes/defaults into the preview model."""

from dataclasses import replace
from pathlib import Path
from types import MappingProxyType
import xml.etree.ElementTree as ET

from .model import (Body, BodyGeometry, ColorSource, Draw, Facing, Gene, Gender,
                    GraphicKind, Head, HeadGeometry, MeshSource, OUTPUT_FACINGS,
                    Part, RenderNode, Side, Textures)
from .settings import (AVERAGE_EYE_OFFSETS, BODY_DEFAULTS, GAME_DATA, HEAD_BASES,
                       HEAD_SHAPES, ROOT)


def vector(text, dimensions):
    values = tuple(float(value.strip()) for value in text.strip("() ").split(","))
    if len(values) != dimensions:
        raise ValueError(f"Expected {dimensions} components: {text}")
    return values


def size(text):
    values = tuple(float(value.strip()) for value in text.strip("() ").split(","))
    if len(values) == 1:
        values *= 2
    if len(values) != 2 or min(values) <= 0:
        raise ValueError(f"Invalid mesh/draw size: {text}")
    return values


def color(text):
    values = tuple(float(value.strip()) for value in text.strip("() ").split(","))
    if any(value > 1 for value in values):
        values = tuple(value / 255 for value in values)
    if len(values) not in (3, 4) or not all(0 <= value <= 1 for value in values):
        raise ValueError(f"Invalid XML color: {text}")
    return values + (1.0,) if len(values) == 3 else values


def read_definitions(directory, tag):
    return [node for path in sorted(directory.rglob("*.xml"))
            for node in ET.parse(path).getroot().findall(tag)]


def _head_geometry(node, parents, visiting=()):
    parent_name = node.get("ParentName")
    if parent_name in visiting:
        raise ValueError(f"Cyclic head inheritance: {visiting + (parent_name,)}")
    base = HEAD_BASES.get(parent_name, HEAD_BASES["AverageBase"])
    if parent_name in parents:
        base = _head_geometry(parents[parent_name], parents, visiting + (parent_name,))
    mesh, eye = node.findtext("hairMeshSize"), node.findtext("eyeOffsetEastWest")
    return HeadGeometry(hair_mesh=size(mesh) if mesh is not None else base.hair_mesh,
                        eye_east=vector(eye, 3) if eye is not None else base.eye_east,
                        narrow=node.findtext("narrow", str(base.narrow)).lower() == "true")


def _draw(node, facing):
    def value(field, fallback):
        return node.findtext(f"drawData/data{facing.xml_name}/{field}",
                             node.findtext(f"drawData/defaultData/{field}", fallback))

    return Draw(offset=vector(value("offset", "(0,0,0)"), 3),
                layer=float(value("layer", node.findtext("baseLayer", "0"))),
                flip=value("flip", "false").lower() == "true",
                rotation=float(value("rotationOffset", "0")))


class DefinitionLibrary:
    def __init__(self, root=ROOT, game_data=GAME_DATA):
        self.root = Path(root)
        self.textures = self.root / "XylXenos.Data/Textures"
        self.art = self.root / "Art/Rimworld art"
        defs = self.root / "XylXenos.Data/Defs"
        gene_xml = read_definitions(defs / "GeneDefs", "GeneDef")
        mod_heads = read_definitions(defs / "HeadTypeDefs", "HeadTypeDef")
        core_heads, core_bodies = [], []
        if game_data is not None and Path(game_data).is_dir():
            core = Path(game_data) / "Core/Defs"
            core_heads = read_definitions(core / "HeadTypeDefs", "HeadTypeDef")
            core_bodies = read_definitions(core / "Misc/BodyTypeDefs", "BodyTypeDef")

        self.bodies = self._bodies(core_bodies)
        self.heads = self._heads(core_heads + mod_heads)
        self.genes = self._genes(gene_xml)

    def _bodies(self, core_bodies):
        bodies = {name: Body(name, self.art / "Bodies" / f"Naked_{name}", geometry)
                  for name, geometry in BODY_DEFAULTS.items()}
        for node in core_bodies:
            name = node.findtext("defName")
            if name not in bodies:
                continue
            anchors = {(Facing[node.findtext("rotation").upper()], node.findtext("tag"),
                        node.findtext("narrowCrown", "false").lower() == "true"): vector(node.findtext("offset"), 3)
                       for node in node.findall("woundAnchors/li")
                       if node.findtext("rotation", "").upper() in {f.name for f in OUTPUT_FACINGS}
                       and node.findtext("tag") in ("LeftEye", "RightEye")}
            geometry = BodyGeometry(vector(node.findtext("headOffset"), 2),
                                    size(node.findtext("bodyGraphicScale")), MappingProxyType(anchors))
            texture = self.art / "Bodies" / Path(node.findtext("bodyNakedGraphicPath")).name
            bodies[name] = Body(name, texture, geometry)
        return bodies

    def _heads(self, head_xml):
        heads = {}
        for gender in Gender:
            for shape in HEAD_SHAPES:
                base = HEAD_BASES[shape.split("_")[0] + "Base"]
                geometry = replace(base, eye_east=AVERAGE_EYE_OFFSETS[gender]) if not base.narrow else base
                name = f"{gender.value}_{shape.replace('_', '')}"
                heads[name] = Head(name, gender, self.art / "Heads" / f"{gender.value}_{shape}", geometry)
        parents = {node.get("Name"): node for node in head_xml if node.get("Name")}
        for node in head_xml:
            name, graphic = node.findtext("defName"), node.findtext("graphicPath")
            if not name or not graphic or node.findtext("gender") not in {gender.value for gender in Gender}:
                continue
            path = self.textures / graphic if graphic.startswith("Xyl/") else self.art / "Heads" / Path(graphic).name
            heads[name] = Head(name, Gender(node.findtext("gender")), path, _head_geometry(node, parents))
        return heads

    def _genes(self, gene_xml):
        genes = {}
        for node in gene_xml:
            name = node.findtext("defName")
            if not name:
                continue
            nodes = tuple(self._node(item, (name, index))
                          for index, item in enumerate(node.findall("renderNodeProperties/li")))
            skin = node.findtext("skinColorOverride")
            genes[name] = Gene(name=name, label=node.findtext("label", name).capitalize(), nodes=nodes,
                               forced_heads=tuple(item.text.strip() for item in node.findall("forcedHeadTypes/li")),
                               skin=color(skin) if skin else None)
        return genes

    def reference_head(self, gender, shape):
        return self.heads[f"{gender.value}_{shape.replace('_', '')}"]

    def _paths(self, node, gender):
        single = (node.findtext("texPathFemale") if gender == Gender.FEMALE else None) or node.findtext("texPath")
        multiple = (node.findall("texPathsFemale/li") if gender == Gender.FEMALE else []) or node.findall("texPaths/li")
        paths = [single] if single else [item.text for item in multiple]
        return tuple(self.textures / path.strip() for path in paths)

    def _node(self, node, key):
        properties_class = node.get("Class", "")
        node_class = node.findtext("nodeClass", "")
        worker_class = node.findtext("workerClass", "")
        eye = properties_class == "PawnRenderNodeProperties_Eye"
        if eye:
            kind = GraphicKind.EYE
        elif node_class.endswith("Pattern"):
            kind = GraphicKind.PATTERN
        elif "PawnRenderNode_AttachmentHead" in node_class:
            kind = GraphicKind.HEAD_ATTACHMENT
        else:
            kind = GraphicKind.PLANE
        parent = Part(node.findtext("parentTagDef"))
        tint_type = node.findtext("colorType")
        tint = (ColorSource(tint_type) if tint_type in {source.value for source in ColorSource}
                else color(node.findtext("color", "(1,1,1)")))
        visible = frozenset(Facing[item.text.strip().upper()] for item in node.findall("visibleFacing/li"))
        if not visible:
            visible = frozenset((Facing.SOUTH, Facing.EAST, Facing.WEST) if eye else Facing)
        if eye:
            visible -= {Facing.NORTH}
        dependent = properties_class == "XylXenos.PawnRenderNodeProperties_GeneDependent"
        body_attachment = parent == Part.BODY and (worker_class == "PawnRenderNodeWorker_AttachmentBody"
                          or (dependent and node.findtext("useBodyGraphicScale", "false").lower() == "true"))
        adjusts_narrow = dependent or worker_class == "PawnRenderNodeWorker_Head"
        narrow_offset = float(node.findtext("narrowCrownHorizontalOffset", "0")) if adjusts_narrow else 0
        mesh = (MeshSource.HAIR if kind in (GraphicKind.EYE, GraphicKind.HEAD_ATTACHMENT)
                else size(node.findtext("overrideMeshSize", "(1.5,1.5)")))
        return RenderNode(
            key=key,
            debug_label=node.findtext("debugLabel", key[0]),
            textures=Textures(self._paths(node, Gender.FEMALE), self._paths(node, Gender.MALE)),
            parent=parent, kind=kind, tint=tint,
            draws=tuple(_draw(node, facing) for facing in Facing), visible=visible,
            mesh=mesh, draw_size=size(node.findtext("drawSize", "1")),
            scale_mesh_to_body=body_attachment,
            scale_offset_to_body=node.findtext("drawData/scaleOffsetByBodySize", "false").lower() == "true",
            narrow_offset=narrow_offset, side=Side(node.findtext("side", "Center")),
            eye_anchor=node.findtext("anchorTag"),
            requires=tuple(item.text.strip() for item in node.findall("requiredGenes/li")),
            forbids=tuple(item.text.strip() for item in node.findall("disallowedGenes/li")))

    def verify_coverage(self, groups):
        used = set()
        for group in groups:
            for preview in group.previews:
                used.update((preview.pawn.body.texture, preview.pawn.head.texture))
                used.update(graphic.texture(preview.pawn.gender) for graphic in preview.graphics)
        humanlike = self.textures / "Xyl/Things/Pawn/Humanlike"
        missing = []
        for folder in ("HeadAttachments", "BodyAttachments", "Heads"):
            for path in (humanlike / folder).rglob("*.png"):
                if not path.stem.endswith("m") and path.with_name(path.stem.rsplit("_", 1)[0]) not in used:
                    missing.append(str(path.relative_to(self.root)))
        if missing:
            raise ValueError("Humanlike art omitted from catalog:\n" + "\n".join(sorted(missing)))
