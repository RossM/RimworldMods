"""Choose primary variants, reference pawns, and prerequisites before rendering."""

from dataclasses import replace
from itertools import product

from .model import Gender, Graphic, Group, Part, Pawn, Preview
from .settings import BODY_ATTACHMENT_TYPES, DEFAULT_PALETTE, GROUPED_VARIANTS, HEAD_SHAPES, variant_caption


class CatalogBuilder:
    def __init__(self, library):
        self.library = library

    def pawn(self, gender=Gender.FEMALE, shape=HEAD_SHAPES[0], body=None):
        return Pawn(self.library.bodies[body or gender.value],
                    self.library.reference_head(gender, shape), DEFAULT_PALETTE)

    @staticmethod
    def variants(gene):
        if not gene.nodes:
            return ()
        shared = tuple(node for node in gene.nodes if not node.conditional)
        conditional = tuple(node for node in gene.nodes if node.conditional)
        node_sets = [shared + (node,) for node in conditional] if conditional else [shared]
        return tuple(tuple(Graphic(node, index) for node, index in zip(nodes, indices))
                     for nodes in node_sets
                     for indices in product(*(range(len(node.textures.female)) for node in nodes)))

    @staticmethod
    def variant_caption(graphics):
        captions = []
        for graphic in graphics:
            node = graphic.node
            override = GROUPED_VARIANTS.get(node.key[0], {}).get(node.debug_label)
            if override is not None:
                captions.append(override)
            elif node.conditional or len(node.textures.female) > 1:
                name = node.debug_label if node.conditional else graphic.texture(Gender.FEMALE).name
                captions.append(variant_caption(name))
        return ", ".join(captions)

    @staticmethod
    def variant_label(gene, graphics):
        caption = CatalogBuilder.variant_caption(graphics)
        return f"{gene.label} ({caption})" if caption else gene.label

    def _complete_graphics(self, gene, primary):
        graphics = list(primary)
        included = {gene.name}
        # Only primary graphics determine comparison bodies/heads. Prerequisite
        # overlays must not change a tail's body comparison into a head comparison.
        for graphic in graphics:
            for name in graphic.node.requires:
                if name in included:
                    continue
                prerequisite = self.library.genes.get(name)
                if prerequisite is None:
                    raise ValueError(f"Unknown render-node prerequisite: {name}")
                choices = self.variants(prerequisite)
                if len(choices) > 1:
                    raise ValueError(f"Prerequisite {name} needs an explicit graphic variant")
                included.add(name)
                if choices:
                    graphics.extend(choices[0])
        for graphic in graphics:
            if included.intersection(graphic.node.forbids):
                raise ValueError(f"Conflicting prerequisite genes for {graphic.node.key}")
        return tuple(graphics)

    @staticmethod
    def body_variant(graphics):
        return bool(graphics) and all(graphic.node.body_attachment for graphic in graphics)

    def _previews(self, gene, graphics, *, prefix=""):
        complete = self._complete_graphics(gene, graphics)
        if self.body_variant(graphics):
            return tuple(Preview(f"{prefix} {body}".strip(), self.pawn(body=body), complete)
                         for body in BODY_ATTACHMENT_TYPES)
        affects_head = any(graphic.node.parent == Part.HEAD for graphic in graphics)
        sexed = any(graphic.node.textures.sexed for graphic in graphics)
        genders = tuple(Gender) if sexed else (Gender.FEMALE,)
        shapes = HEAD_SHAPES if affects_head else HEAD_SHAPES[:1]
        previews = []
        for gender in genders:
            for shape in shapes:
                if affects_head:
                    caption = shape.replace("_", " ").capitalize()
                    label = f"{gender.value} {caption.lower()}" if sexed else caption
                else:
                    label = gender.value if sexed else gene.label
                if prefix:
                    label = f"{prefix} {label.lower()}"
                previews.append(Preview(label, self.pawn(gender, shape), complete))
        return tuple(previews)

    def build(self):
        groups = []
        for gene in self.library.genes.values():
            variants = self.variants(gene)
            paired_body = len(variants) == 2 and all(self.body_variant(graphics) for graphics in variants)
            if paired_body or gene.name in GROUPED_VARIANTS:
                previews = tuple(preview for graphics in variants
                                 for preview in self._previews(gene, graphics, prefix=self.variant_caption(graphics)))
                groups.append(Group(gene.name, gene.label, previews))
            else:
                for graphics in variants:
                    groups.append(Group(gene.name, self.variant_label(gene, graphics), self._previews(gene, graphics)))

            replacements = {}
            for name in gene.forced_heads:
                head = self.library.heads[name]
                caption = variant_caption(head.texture.name.removeprefix(head.gender.value + "_"))
                pawn = replace(self.pawn(head.gender), head=head)
                replacements.setdefault(caption, []).append(Preview(head.gender.value, pawn))
            for caption, previews in replacements.items():
                groups.append(Group(gene.name, f"{gene.label} ({caption})",
                                    tuple(sorted(previews, key=lambda preview: preview.label))))
            if gene.skin is not None:
                pawn = replace(self.pawn(), palette=replace(DEFAULT_PALETTE, skin=gene.skin))
                groups.append(Group(gene.name, gene.label, (Preview(gene.label, pawn),), skin_color=True))

        labels = [group.label for group in groups]
        if len(labels) != len(set(labels)):
            raise ValueError("Cosmetic variants need distinct display labels")
        return tuple(sorted(groups, key=lambda group: (group.skin_color, group.label.casefold())))
