"""Run with: python -B -m unittest discover -s Art/Codex -p test_pawn_cosmetics.py"""

from dataclasses import replace
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from PIL import Image, ImageChops

from pawn_cosmetics.catalog import CatalogBuilder
from pawn_cosmetics.definitions import DefinitionLibrary, color, size
from pawn_cosmetics.layout import SheetComposer, pack_rows
from pawn_cosmetics.model import ColorSource, Facing, Gender, GraphicKind, Textures
from pawn_cosmetics.rendering import Renderer, directional_path, tint
from pawn_cosmetics.settings import RenderStyle, SheetStyle


class CatalogTests(unittest.TestCase):
    def test_horns_share_art_but_keep_names_colors_and_dependencies(self):
        groups = CatalogBuilder(DefinitionLibrary(game_data=None)).build()
        horns = [group for group in groups if group.gene == "XylLargeHorns"]
        self.assertEqual(len(horns), 1)
        self.assertEqual(horns[0].label, "Large horns")
        self.assertEqual([preview.label for preview in horns[0].previews],
                         ["Cow average normal", "Cow narrow normal", "Dragon average normal", "Dragon narrow normal"])
        cow_graphics, dragon_graphics = horns[0].previews[0].graphics, horns[0].previews[2].graphics
        self.assertEqual(cow_graphics[0].texture(Gender.FEMALE), dragon_graphics[0].texture(Gender.FEMALE))
        self.assertEqual(cow_graphics[0].node.tint, (1, 1, 1, 1))
        self.assertEqual(dragon_graphics[0].node.tint, ColorSource.SKIN)
        self.assertEqual(len(cow_graphics), 1)
        self.assertEqual({graphic.node.key[0] for graphic in dragon_graphics[1:]}, {"XylScaleskin"})

    def test_scaly_tail_keeps_body_comparisons_with_head_and_body_overlays(self):
        groups = CatalogBuilder(DefinitionLibrary(game_data=None)).build()
        tail = next(group for group in groups if group.label == "Lizard tail")
        self.assertEqual([preview.label for preview in tail.previews],
                         ["Smooth Female", "Smooth Thin", "Scaly Female", "Scaly Thin"])
        for preview in tail.previews:
            self.assertEqual(preview.pawn.body.name, preview.label.split()[-1])
            overlays = [graphic for graphic in preview.graphics if graphic.node.kind == GraphicKind.PATTERN]
            self.assertEqual(len(overlays), 2 if preview.label.startswith("Scaly") else 0)

    def test_yellow_eyes_include_each_sex_and_head_shape(self):
        groups = CatalogBuilder(DefinitionLibrary(game_data=None)).build()
        eyes = next(group for group in groups if group.label == "Yellow eyes")
        self.assertEqual({(p.pawn.gender, p.pawn.head.geometry.narrow) for p in eyes.previews},
                         {(gender, narrow) for gender in Gender for narrow in (False, True)})
        female, male = eyes.previews[0], eyes.previews[2]
        self.assertNotEqual(female.graphics[0].texture(female.pawn.gender),
                            male.graphics[0].texture(male.pawn.gender))
        self.assertFalse(eyes.skin_color)

    def test_shared_head_art_stays_female_and_skin_colors_are_last(self):
        groups = CatalogBuilder(DefinitionLibrary(game_data=None)).build()
        ears = next(group for group in groups if group.label == "Cow ears")
        self.assertEqual(len(ears.previews), 2)
        self.assertEqual({p.pawn.gender for p in ears.previews}, {Gender.FEMALE})
        first_color = next(index for index, group in enumerate(groups) if group.skin_color)
        self.assertTrue(all(group.skin_color for group in groups[first_color:]))
        self.assertEqual([group.label for group in groups[:first_color]],
                         sorted((group.label for group in groups[:first_color]), key=str.casefold))

    def test_catalog_covers_all_current_humanlike_art(self):
        library = DefinitionLibrary(game_data=None)
        library.verify_coverage(CatalogBuilder(library).build())


class DefinitionTests(unittest.TestCase):
    def test_draw_overrides_preserve_defaults_in_other_directions(self):
        with TemporaryDirectory() as directory:
            root = Path(directory)
            defs = root / "XylXenos.Data/Defs/GeneDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs><GeneDef><defName>Test</defName>
                <renderNodeProperties><li><parentTagDef>Head</parentTagDef><texPath>test</texPath>
                <baseLayer>12</baseLayer><drawData><defaultData><offset>(1,2,3)</offset></defaultData>
                <dataNorth><layer>4</layer><flip>true</flip></dataNorth></drawData></li>
                </renderNodeProperties></GeneDef></Defs>""")
            node = DefinitionLibrary(root=root, game_data=None).genes["Test"].nodes[0]
            self.assertEqual(node.draws[Facing.NORTH].offset, (1, 2, 3))
            self.assertEqual(node.draws[Facing.NORTH].layer, 4)
            self.assertTrue(node.draws[Facing.NORTH].flip)
            self.assertEqual(node.draws[Facing.SOUTH].layer, 12)
            self.assertFalse(node.draws[Facing.SOUTH].flip)

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
            defs = root / "XylXenos.Data/Defs/HeadTypeDefs"
            defs.mkdir(parents=True)
            (defs / "test.xml").write_text("""<Defs>
                <HeadTypeDef Name="A" ParentName="B" />
                <HeadTypeDef Name="B" ParentName="A" />
                <HeadTypeDef ParentName="A"><defName>Test</defName><gender>Female</gender>
                <graphicPath>Test</graphicPath></HeadTypeDef></Defs>""")
            with self.assertRaisesRegex(ValueError, "Cyclic head inheritance"):
                DefinitionLibrary(root=root, game_data=None)


class RenderingTests(unittest.TestCase):
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
        groups = CatalogBuilder(DefinitionLibrary(game_data=None)).build()
        preview = next(group for group in groups if group.label == "Scaleskin").previews[0]
        renderer = Renderer()
        for facing in (Facing.SOUTH, Facing.NORTH, Facing.EAST):
            with self.subTest(facing=facing):
                bare = renderer.render(replace(preview, graphics=()), facing)
                patterned = renderer.render(preview, facing)
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
        groups = CatalogBuilder(DefinitionLibrary(game_data=None)).build()
        horns = next(group for group in groups if group.label == "Large horns")
        composer = SheetComposer(Renderer(), SheetStyle(background=(110, 120, 130)))
        sheet = composer.compose((horns,), 1600)
        self.assertEqual(sheet.width, 1600)
        self.assertEqual(sheet.getpixel((0, 0)), (110, 120, 130))
        with self.assertRaisesRegex(ValueError, "Large horns"):
            composer.compose((horns,), 50)


if __name__ == "__main__":
    unittest.main()
