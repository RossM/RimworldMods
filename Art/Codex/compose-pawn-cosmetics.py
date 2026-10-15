"""Render a labeled pawn cosmetic sheet from XylXenos.Data (Python 3.10+ / Pillow).

Run from any directory: python path/to/Art/Codex/compose-pawn-cosmetics.py
Each triplet shows south, north, east. Complete cosmetic groups wrap at --width.
The default output is pawn-cosmetics-sheet.png beside this script.

XML supplies textures, colors, offsets, layers, and prerequisite overlays.
Head cosmetics compare average/narrow heads; sex-specific art includes both sexes.
Wings/tails compare female/thin bodies. Skin colors appear together at the end.
This is an art preview without game lighting, random brightness, or animation.
Core XML supplies adult geometry when available; otherwise bundled defaults apply.
See pawn_cosmetics/README.md for organization and extension points.
"""

import argparse
from pathlib import Path

from PIL import Image

from pawn_cosmetics.catalog import CatalogBuilder
from pawn_cosmetics.definitions import DefinitionLibrary
from pawn_cosmetics.layout import SheetComposer
from pawn_cosmetics.model import OUTPUT_FACINGS
from pawn_cosmetics.rendering import Renderer
from pawn_cosmetics.settings import DEFAULT_WIDTH, GAME_DATA


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("pawn-cosmetics-sheet.png"))
    parser.add_argument("--width", type=int, default=DEFAULT_WIDTH,
                        help="Fixed sheet width in pixels (default: %(default)s)")
    parser.add_argument("--rimworld-data", type=Path, default=GAME_DATA)
    args = parser.parse_args()
    if args.width < 1:
        parser.error("--width must be positive")
    if args.rimworld_data != GAME_DATA and not args.rimworld_data.is_dir():
        parser.error("--rimworld-data must be an existing RimWorld/Data directory")
    try:
        library = DefinitionLibrary(game_data=args.rimworld_data)
        groups = CatalogBuilder(library).build()
        library.verify_coverage(groups)
        sheet = SheetComposer(Renderer()).compose(groups, args.width)
    except ValueError as error:
        parser.error(str(error))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.output)
    with Image.open(args.output) as saved:
        saved.verify()
    preview_count = sum(len(group.previews) for group in groups)
    directions = ", ".join(facing.name.lower() for facing in OUTPUT_FACINGS)
    print(f"Saved {args.output.resolve()}: {sheet.width} x {sheet.height}; "
          f"{len(groups)} cosmetic groups, {preview_count} head/body previews, "
          f"{preview_count * len(OUTPUT_FACINGS)} views ({directions})")


if __name__ == "__main__":
    main()
