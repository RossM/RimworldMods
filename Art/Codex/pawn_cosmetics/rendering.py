"""Composite selected pawn graphics. This layer has no catalog or XML rules."""

from functools import lru_cache
from pathlib import Path

from PIL import Image, ImageChops, ImageOps

from .model import ColorSource, Facing, GraphicKind, MeshSource, Part, Side
from .settings import RenderStyle


@lru_cache(maxsize=None)
def directional_path(base, facing):
    paths = {direction: Path(f"{base}_{direction.name.lower()}.png") for direction in Facing}
    paths = {direction: path for direction, path in paths.items() if path.is_file()}
    north = next((paths[direction] for direction in
                  (Facing.NORTH, Facing.SOUTH, Facing.EAST, Facing.WEST) if direction in paths), None)
    if north is None:
        raise FileNotFoundError(f"No directional art for {base}")
    return paths.get(facing, paths.get(Facing.WEST, north) if facing == Facing.EAST else north)


def tint(image, rgba, mask=None):
    channels = []
    for channel, factor in zip(image.split(), rgba):
        multiplied = channel.point(lambda value: round(value * factor))
        channels.append(Image.composite(multiplied, channel, mask) if mask is not None else multiplied)
    return Image.merge("RGBA", channels)


class Renderer:
    def __init__(self, style=RenderStyle()):
        self.style = style
        self._images = {}

    def image(self, path):
        if path in self._images:
            return self._images[path]
        with Image.open(path) as source:
            image = source.convert("RGBA")
        # Cutout shaders discard near-transparent residue, notably on demon tails.
        image.putalpha(image.getchannel("A").point(lambda value: 0 if value < self.style.alpha_cutoff else value))
        self._images[path] = image
        return image

    def layer(self, image, mesh, offset):
        ppu, size = self.style.pixels_per_unit, self.style.canvas_size
        image = image.resize(tuple(round(value * ppu) for value in mesh), Image.Resampling.LANCZOS)
        position = (round(size // 2 + offset[0] * ppu - image.width / 2),
                    round(size // 2 - offset[2] * ppu - image.height / 2))
        if min(position) < 0 or position[0] + image.width > size or position[1] + image.height > size:
            raise ValueError("Layer exceeds working canvas; increase RenderStyle.canvas_size")
        canvas = Image.new("RGBA", (size, size))
        canvas.alpha_composite(image, position)
        return canvas

    def _graphic(self, graphic, pawn, facing, head_offset):
        node, body, head = graphic.node, pawn.body.geometry, pawn.head.geometry
        draw = node.draws[facing]
        offset = draw.offset
        if node.scale_offset_to_body:
            offset = tuple(value * body.attachment_scale for value in offset)
        if node.kind == GraphicKind.EYE:
            anchor = body.eye_anchors.get((facing, node.eye_anchor, head.narrow if facing == Facing.EAST else False))
            if anchor is not None:
                if facing == Facing.EAST and head.eye_east is not None:
                    anchor = head.eye_east
                offset = tuple(a + b for a, b in zip(offset, anchor))
        origin = head_offset if node.parent == Part.HEAD else (0, 0, 0)
        offset = tuple(a + b for a, b in zip(offset, origin))
        if head.narrow and facing == Facing.EAST:
            offset = (offset[0] - node.narrow_offset, offset[1], offset[2] - node.narrow_offset)
        mesh = head.hair_mesh if node.mesh == MeshSource.HAIR else node.mesh
        mesh = tuple(a * b for a, b in zip(mesh, node.draw_size))
        if node.scale_mesh_to_body:
            mesh = tuple(value * body.attachment_scale for value in mesh)

        path = directional_path(graphic.texture(pawn.gender), facing)
        rgba = {ColorSource.SKIN: pawn.palette.skin, ColorSource.HAIR: pawn.palette.hair}.get(node.tint, node.tint)
        mask_path = path.with_name(path.stem + "m.png")
        mask = self.image(mask_path) if mask_path.is_file() else None
        image = tint(self.image(path), rgba, mask.getchannel("R") if mask is not None else None)
        if mask is not None:
            image = tint(image, pawn.palette.hair, mask.getchannel("G"))
        if draw.flip:
            image = ImageOps.mirror(image)
        return self.layer(image, mesh, offset)

    def render(self, preview, facing):
        pawn = preview.pawn
        offset = pawn.body.geometry.head_offset
        head_offset = (offset[0] if facing == Facing.EAST else 0, 0, offset[1])
        body = self.layer(tint(self.image(directional_path(pawn.body.texture, facing)), pawn.palette.skin),
                          (1.5, 1.5), (0, 0, 0))
        head = self.layer(tint(self.image(directional_path(pawn.head.texture, facing)), pawn.palette.skin),
                          (1.5, 1.5), head_offset)
        silhouettes = {Part.BODY: body, Part.HEAD: head}
        layers = [(0.0, body), (50.0, head)]
        for graphic in preview.graphics:
            node = graphic.node
            if facing not in node.visible or (facing == Facing.EAST and node.side == Side.LEFT):
                continue
            overlay = self._graphic(graphic, pawn, facing, head_offset)
            if node.kind == GraphicKind.PATTERN:
                silhouette = silhouettes[node.parent]
                overlay.putalpha(ImageChops.multiply(overlay.getchannel("A"), silhouette.getchannel("A")))
            layers.append((node.draws[facing].layer, overlay))
        composite = Image.new("RGBA", body.size)
        for _, image in sorted(layers, key=lambda item: item[0]):
            composite = Image.alpha_composite(composite, image)
        return composite
