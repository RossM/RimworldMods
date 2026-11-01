"""Run with: python -B -m unittest discover -s Scripts -p test_pawn_cosmetics.py"""

from dataclasses import replace
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest
from unittest.mock import Mock

from PIL import Image, ImageChops

from pawn_cosmetics.catalog import CatalogBuilder
from pawn_cosmetics.definitions import DefinitionLibrary, color, size
from pawn_cosmetics.layout import SheetComposer, pack_rows
from pawn_cosmetics.model import ColorSource, Facing, Gender, GraphicKind, Group, Preview, Textures
from pawn_cosmetics.rendering import Renderer, directional_path, tint
from pawn_cosmetics.settings import RenderStyle, SheetStyle


class CatalogTests(unittest.TestCase):
    def test_grouped_variants_keep_names_colors_and_dependencies_with_shared_or_distinct_art(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            for dragon_texture in ("horns", "dragon_horns"):
                with self.subTest(dragon_texture=dragon_texture):
                    (defs / "test.xml").write_text(f"""<Defs>
                        <GeneDef><defName>XylLargeHorns</defName><label>large horns</label>
                        <renderNodeProperties>
                          <li><debugLabel>LargeHorns</debugLabel><parentTagDef>Head</parentTagDef>
                            <texPath>horns</texPath><disallowedGenes><li>Scales</li></disallowedGenes></li>
                          <li><debugLabel>LargeHorns_Scaleskin</debugLabel><parentTagDef>Head</parentTagDef>
                            <texPath>{dragon_texture}</texPath><colorType>Skin</colorType>
                            <requiredGenes><li>Scales</li></requiredGenes></li>
                        </renderNodeProperties></GeneDef>
                        <GeneDef><defName>Scales</defName><renderNodeProperties>
                          <li><parentTagDef>Head</parentTagDef><texPath>scales</texPath></li>
                        </renderNodeProperties></GeneDef></Defs>""")
                    groups = CatalogBuilder(DefinitionLibrary(root=root, game_data=None)).build()
                    horns = [group for group in groups if group.gene == "XylLargeHorns"]
                    self.assertEqual(len(horns), 1)
                    self.assertEqual(horns[0].label, "Large horns")
                    self.assertEqual([preview.label for preview in horns[0].previews],
                                     ["Cow average normal", "Cow narrow normal",
                                      "Dragon average normal", "Dragon narrow normal"])
                    cow, dragon = horns[0].previews[0].graphics, horns[0].previews[2].graphics
                    self.assertEqual(cow[0].texture(Gender.FEMALE).name, "horns")
                    self.assertEqual(dragon[0].texture(Gender.FEMALE).name, dragon_texture)
                    self.assertEqual(cow[0].node.tint, (1, 1, 1, 1))
                    self.assertEqual(dragon[0].node.tint, ColorSource.SKIN)
                    self.assertEqual(len(cow), 1)
                    self.assertEqual({graphic.node.key[0] for graphic in dragon[1:]}, {"Scales"})

    def test_prerequisite_head_overlays_preserve_primary_body_comparisons(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs>
                <GeneDef><defName>Tail</defName><label>tail</label><renderNodeProperties>
                  <li Class="XylXenos.PawnRenderNodeProperties_GeneDependent">
                    <debugLabel>LizardTail</debugLabel><parentTagDef>Body</parentTagDef>
                    <useBodyGraphicScale>true</useBodyGraphicScale><texPath>tail</texPath>
                    <disallowedGenes><li>Scales</li></disallowedGenes></li>
                  <li Class="XylXenos.PawnRenderNodeProperties_GeneDependent">
                    <debugLabel>LizardTail_Scaleskin</debugLabel><parentTagDef>Body</parentTagDef>
                    <useBodyGraphicScale>true</useBodyGraphicScale><texPath>tail</texPath>
                    <requiredGenes><li>Scales</li></requiredGenes></li>
                </renderNodeProperties></GeneDef>
                <GeneDef><defName>Scales</defName><renderNodeProperties>
                  <li><nodeClass>HeadPattern</nodeClass><parentTagDef>Head</parentTagDef><texPath>scales</texPath></li>
                  <li><nodeClass>BodyPattern</nodeClass><parentTagDef>Body</parentTagDef><texPath>scales</texPath></li>
                </renderNodeProperties></GeneDef></Defs>""")
            groups = CatalogBuilder(DefinitionLibrary(root=root, game_data=None)).build()
            tail = next(group for group in groups if group.gene == "Tail")
            self.assertEqual([preview.label for preview in tail.previews],
                             ["Smooth Female", "Smooth Thin", "Scaly Female", "Scaly Thin"])
            for preview in tail.previews:
                self.assertEqual(preview.pawn.body.name, preview.label.split()[-1])
                overlays = [graphic for graphic in preview.graphics if graphic.node.kind == GraphicKind.PATTERN]
                self.assertEqual(len(overlays), 2 if preview.label.startswith("Scaly") else 0)

    def test_sex_specific_eyes_include_each_sex_and_head_shape(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs><GeneDef><defName>Eyes</defName>
                <renderNodeProperties><li Class="PawnRenderNodeProperties_Eye">
                <parentTagDef>Head</parentTagDef><texPath>male</texPath><texPathFemale>female</texPathFemale>
                </li></renderNodeProperties></GeneDef></Defs>""")
            eyes, = CatalogBuilder(DefinitionLibrary(root=root, game_data=None)).build()
            self.assertEqual({(p.pawn.gender, p.pawn.head.geometry.narrow) for p in eyes.previews},
                             {(gender, narrow) for gender in Gender for narrow in (False, True)})
            for preview in eyes.previews:
                self.assertEqual(preview.graphics[0].texture(preview.pawn.gender).name,
                                 preview.pawn.gender.value.lower())
            self.assertFalse(eyes.skin_color)

    def test_shared_head_art_stays_female_and_skin_colors_are_last(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs>
                <GeneDef><defName>Ears</defName><label>zebra ears</label><renderNodeProperties>
                  <li><parentTagDef>Head</parentTagDef><texPath>ears</texPath></li>
                </renderNodeProperties></GeneDef>
                <GeneDef><defName>Nose</defName><label>big nose</label><renderNodeProperties>
                  <li><parentTagDef>Head</parentTagDef><texPath>nose</texPath></li>
                </renderNodeProperties></GeneDef>
                <GeneDef><defName>Skin</defName><label>amber</label><skinColorOverride>(1,0.5,0)</skinColorOverride></GeneDef>
                </Defs>""")
            groups = CatalogBuilder(DefinitionLibrary(root=root, game_data=None)).build()
            ears = next(group for group in groups if group.gene == "Ears")
            self.assertEqual(len(ears.previews), 2)
            self.assertEqual({p.pawn.gender for p in ears.previews}, {Gender.FEMALE})
            self.assertEqual([group.label for group in groups], ["Big nose", "Zebra ears", "Amber"])
            self.assertEqual([group.skin_color for group in groups], [False, False, True])

    def test_catalog_covers_all_current_humanlike_art(self):
        library = DefinitionLibrary(game_data=None)
        library.verify_coverage(CatalogBuilder(library).build())


class DefinitionTests(unittest.TestCase):
    def test_narrow_head_offsets_follow_the_worker(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            for worker, properties, expected in (
                    ("XylXenos.PawnRenderNodeWorker_ForeheadMark", "", (0.07, 0, 0)),
                    ("PawnRenderNodeWorker_Head", "", (0.07, 0, 0.07)),
                    ("", 'Class="XylXenos.PawnRenderNodeProperties_GeneDependent"', (0.07, 0, 0.07)),
                    ("PawnRenderNodeWorker_FlipWhenCrawling", "", (0, 0, 0))):
                with self.subTest(worker=worker, properties=properties):
                    (defs / "test.xml").write_text(f"""<Defs><GeneDef><defName>Test</defName>
                        <renderNodeProperties><li {properties}><parentTagDef>Head</parentTagDef>
                        <texPath>test</texPath><workerClass>{worker}</workerClass>
                        <narrowCrownHorizontalOffset>0.07</narrowCrownHorizontalOffset>
                        </li></renderNodeProperties></GeneDef></Defs>""")
                    node = DefinitionLibrary(root=root, game_data=None).genes["Test"].nodes[0]
                    self.assertEqual(node.narrow_offset, expected)

    def test_draw_overrides_preserve_defaults_in_other_directions(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs><GeneDef><defName>Test</defName>
                <renderNodeProperties><li><parentTagDef>Head</parentTagDef><texPath>test</texPath>
                <baseLayer>12</baseLayer><drawData><defaultData><offset>(1,2,3)</offset>
                <rotationOffset>35</rotationOffset></defaultData>
                <dataNorth><layer>4</layer><flip>true</flip><rotationOffset>180</rotationOffset></dataNorth>
                <dataSouth><rotationOffset>0</rotationOffset></dataSouth></drawData></li>
                </renderNodeProperties></GeneDef></Defs>""")
            node = DefinitionLibrary(root=root, game_data=None).genes["Test"].nodes[0]
            self.assertEqual(node.draws[Facing.NORTH].offset, (1, 2, 3))
            self.assertEqual(node.draws[Facing.NORTH].layer, 4)
            self.assertTrue(node.draws[Facing.NORTH].flip)
            self.assertEqual(node.draws[Facing.SOUTH].layer, 12)
            self.assertFalse(node.draws[Facing.SOUTH].flip)
            self.assertEqual(node.draws[Facing.NORTH].rotation, 180)
            self.assertEqual(node.draws[Facing.EAST].rotation, 35)
            self.assertEqual(node.draws[Facing.SOUTH].rotation, 0)

    def test_color_and_size_accept_xml_conventions_and_reject_invalid_values(self):
        self.assertEqual(color("(255,128,0)"), (1, 128 / 255, 0, 1))
        self.assertEqual(color("(0,0,0,0.5)"), (0, 0, 0, 0.5))
        self.assertEqual(size("1.5"), (1.5, 1.5))
        for value in ("(0,1)", "(1,2,3)"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                size(value)
        with self.assertRaises(ValueError):
            color("(-1,0,0)")

    def test_mismatched_sex_specific_variants_are_rejected(self):
        with self.assertRaises(ValueError):
            Textures((Path("female_a"), Path("female_b")), (Path("male_a"),))

    def test_head_inheritance_cycles_are_reported(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/HeadTypeDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs>
                <HeadTypeDef Name="A" ParentName="B" />
                <HeadTypeDef Name="B" ParentName="A" />
                <HeadTypeDef ParentName="A"><defName>Test</defName><gender>Female</gender>
                <graphicPath>Test</graphicPath></HeadTypeDef></Defs>""")
            with self.assertRaisesRegex(ValueError, "Cyclic head inheritance"):
                DefinitionLibrary(root=root, game_data=None)


class RenderingTests(unittest.TestCase):
    def test_narrow_head_corrections_shift_only_the_expected_axes_in_side_views(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs>
                <GeneDef><defName>Mark</defName><renderNodeProperties><li>
                  <workerClass>XylXenos.PawnRenderNodeWorker_ForeheadMark</workerClass>
                  <nodeClass>PawnRenderNode_AttachmentHead</nodeClass><parentTagDef>Head</parentTagDef>
                  <texPaths><li>mark_a</li><li>mark_b</li></texPaths><drawSize>0.25</drawSize>
                  <narrowCrownHorizontalOffset>0.07</narrowCrownHorizontalOffset>
                  <drawData><dataEast><offset>(0.25,0,0)</offset></dataEast></drawData>
                </li></renderNodeProperties></GeneDef>
                <GeneDef><defName>Attachment</defName><renderNodeProperties>
                  <li Class="XylXenos.PawnRenderNodeProperties_GeneDependent">
                    <parentTagDef>Head</parentTagDef><texPath>mark_a</texPath>
                    <narrowCrownHorizontalOffset>0.03</narrowCrownHorizontalOffset>
                  </li></renderNodeProperties></GeneDef></Defs>""")
            library = DefinitionLibrary(root=root, game_data=None)
            library.textures.mkdir(parents=True)
            for name in ("mark_a", "mark_b"):
                Image.new("RGBA", (4, 2), "white").save(library.textures / f"{name}_south.png")
            renderer = Renderer(RenderStyle(pixels_per_unit=100))
            for group in CatalogBuilder(library).build():
                horizontal, vertical = (-7, 0) if group.gene == "Mark" else (-3, 3)
                for preview in group.previews:
                    graphic = preview.graphics[0]
                    uncorrected = replace(graphic, node=replace(graphic.node, narrow_offset=(0, 0, 0)))
                    for facing in (Facing.SOUTH, Facing.NORTH, Facing.EAST):
                        with self.subTest(group=group.label, preview=preview.label, facing=facing):
                            before = renderer._graphic(uncorrected, preview.pawn, facing, (0, 0, 0))
                            after = renderer._graphic(graphic, preview.pawn, facing, (0, 0, 0))
                            self.assertIsNotNone(before.getchannel("A").getbbox())
                            if preview.pawn.head.geometry.narrow and facing == Facing.EAST:
                                expected = ImageChops.offset(before, horizontal, vertical)
                            else:
                                expected = before
                            self.assertEqual(after.tobytes(), expected.tobytes())

    def test_rotation_turns_a_scaled_mesh_clockwise_around_its_offset(self):
        renderer = Renderer(RenderStyle(pixels_per_unit=1, canvas_size=12))
        source = Image.new("RGBA", (4, 2))
        source.putpixel((0, 0), (255, 0, 0, 255))
        source.putpixel((3, 0), (0, 0, 255, 255))
        quarter_turn = renderer.layer(source, (4, 2), (2, 0, 1), rotation=90)
        self.assertEqual(quarter_turn.getpixel((8, 3)), (255, 0, 0, 255))
        self.assertEqual(quarter_turn.getpixel((8, 6)), (0, 0, 255, 255))
        half_turn = renderer.layer(source, (4, 2), (2, 0, 1), rotation=180)
        self.assertEqual(half_turn.getpixel((9, 5)), (255, 0, 0, 255))
        self.assertEqual(half_turn.getpixel((6, 5)), (0, 0, 255, 255))
        self.assertEqual(source.getpixel((0, 0)), (255, 0, 0, 255))

    def test_directional_rotation_changes_only_south_on_both_body_types(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs><GeneDef><defName>Tail</defName>
                <renderNodeProperties><li><workerClass>PawnRenderNodeWorker_AttachmentBody</workerClass>
                <parentTagDef>Body</parentTagDef><texPath>tail</texPath><overrideMeshSize>(1,1)</overrideMeshSize>
                <drawData><dataSouth><rotationOffset>180</rotationOffset></dataSouth></drawData>
                </li></renderNodeProperties></GeneDef></Defs>""")
            library = DefinitionLibrary(root=root, game_data=None)
            library.textures.mkdir(parents=True)
            image = Image.new("RGBA", (4, 2))
            image.putpixel((0, 0), (255, 0, 0, 255))
            image.save(library.textures / "tail_south.png")
            tail, = CatalogBuilder(library).build()
            self.assertEqual({preview.pawn.body.name for preview in tail.previews}, {"Female", "Thin"})
            renderer = Renderer()
            for preview in tail.previews:
                graphic = preview.graphics[0]
                unrotated = replace(graphic.node, draws=tuple(replace(draw, rotation=0) for draw in graphic.node.draws))
                original = replace(graphic, node=unrotated)
                for facing in (Facing.SOUTH, Facing.NORTH, Facing.EAST):
                    with self.subTest(body=preview.pawn.body.name, facing=facing):
                        before = renderer._graphic(original, preview.pawn, facing, (0, 0, 0)).tobytes()
                        after = renderer._graphic(graphic, preview.pawn, facing, (0, 0, 0)).tobytes()
                        if facing == Facing.SOUTH:
                            self.assertNotEqual(before, after)
                        else:
                            self.assertEqual(before, after)

    def test_mask_tint_preserves_unmasked_pixels_and_partial_alpha(self):
        source = Image.new("RGBA", (2, 1), (200, 100, 50, 128))
        mask = Image.new("L", (2, 1))
        mask.putdata([255, 0])
        result = tint(source, (0.5, 1, 0, 0.5), mask)
        self.assertEqual(result.getpixel((0, 0)), (100, 100, 0, 64))
        self.assertEqual(result.getpixel((1, 0)), (200, 100, 50, 128))
        self.assertEqual(source.getpixel((0, 0)), (200, 100, 50, 128))

    def test_directional_fallback_uses_west_for_east_and_available_art_for_north(self):
        with TemporaryDirectory() as directory:
            base = Path(directory) / "test"
            Image.new("RGBA", (1, 1)).save(f"{base}_west.png")
            self.assertEqual(directional_path(base, Facing.EAST), Path(f"{base}_west.png"))
            self.assertEqual(directional_path(base, Facing.NORTH), Path(f"{base}_west.png"))
            self.assertEqual(directional_path(base, Facing.SOUTH), Path(f"{base}_west.png"))

    def test_patterns_change_skin_without_painting_outside_the_pawn(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos/Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs><GeneDef><defName>Pattern</defName>
                <renderNodeProperties>
                  <li><nodeClass>HeadPattern</nodeClass><parentTagDef>Head</parentTagDef>
                    <texPath>pattern</texPath><baseLayer>80</baseLayer></li>
                  <li><nodeClass>BodyPattern</nodeClass><parentTagDef>Body</parentTagDef>
                    <texPath>pattern</texPath><baseLayer>40</baseLayer></li>
                </renderNodeProperties></GeneDef></Defs>""")
            library = DefinitionLibrary(root=root, game_data=None)
            group, = CatalogBuilder(library).build()
            preview = group.previews[0]
            silhouette = Image.new("RGBA", (8, 8))
            silhouette.paste("white", (2, 2, 6, 6))
            for base in (preview.pawn.head.texture, preview.pawn.body.texture):
                base.parent.mkdir(parents=True, exist_ok=True)
                for facing in ("south", "north", "east"):
                    silhouette.save(f"{base}_{facing}.png")
            library.textures.mkdir(parents=True)
            Image.new("RGBA", (8, 8), "blue").save(library.textures / "pattern_south.png")
            renderer = Renderer()
            for graphic in preview.graphics:
                for facing in (Facing.SOUTH, Facing.NORTH, Facing.EAST):
                    with self.subTest(part=graphic.node.parent, facing=facing):
                        bare = renderer.render(replace(preview, graphics=()), facing)
                        patterned = renderer.render(replace(preview, graphics=(graphic,)), facing)
                        outside = bare.getchannel("A").point(lambda value: 255 if value == 0 else 0)
                        self.assertIsNone(ImageChops.multiply(patterned.getchannel("A"), outside).getbbox())
                        self.assertNotEqual(bare.tobytes(), patterned.tobytes())

    def test_render_scale_and_alpha_cutoff_are_configurable(self):
        renderer = Renderer(RenderStyle(pixels_per_unit=10, canvas_size=40, alpha_cutoff=20))
        self.assertEqual(renderer.layer(Image.new("RGBA", (1, 1), "white"), (1, 2), (0, 0, 0))
                         .getchannel("A").getbbox(), (15, 10, 25, 30))
        with TemporaryDirectory() as directory:
            path = Path(directory) / "test.png"
            source = Image.new("RGBA", (3, 1), "white")
            source.putalpha(Image.frombytes("L", (3, 1), bytes([19, 20, 128])))
            source.save(path)
            self.assertEqual(list(renderer.image(path).getchannel("A").getdata()), [0, 20, 128])


class LayoutTests(unittest.TestCase):
    def test_packing_fills_rows_in_order_and_keeps_exact_fits(self):
        self.assertEqual([list(row) for row in pack_rows([40, 50, 60, 30], 100, 10)],
                         [[0, 1], [2, 3]])
        self.assertEqual(pack_rows([], 100, 10), [])
        with self.assertRaises(ValueError):
            pack_rows([101], 100, 10)

    def test_sheet_honors_width_background_and_reports_oversize_groups(self):
        with TemporaryDirectory() as directory:
            pawn = CatalogBuilder(DefinitionLibrary(root=directory, game_data=None)).pawn()
            group = Group("Test", "Example", (Preview("Sample", pawn),))
            renderer = Mock(spec=Renderer)
            renderer.render.return_value = Image.new("RGBA", (100, 100), "white")
            composer = SheetComposer(renderer, SheetStyle(background=(110, 120, 130)))
            sheet = composer.compose((group,), 1600)
            self.assertEqual(sheet.width, 1600)
            self.assertEqual(sheet.getpixel((0, 0)), (110, 120, 130))
            with self.assertRaisesRegex(ValueError, "Example"):
                composer.compose((group,), 50)


if __name__ == "__main__":
    unittest.main()
