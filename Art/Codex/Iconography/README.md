# Gene icon components

72 transparent PNG components for composing RimWorld gene icons in a paint program.

- Every component is on a 1024 x 1024 RGBA canvas, with its visible bounds centered and its longest dimension fitted to 896 px (64 px minimum margin).
- Single-color components are pure white. Tint them after importing. Components with internal shading or multiple colors retain those colors, including grayscale shading.
- Black outlines are omitted. Add an outside stroke after resizing each component to its final compositing size.
- Components are framed independently. Reposition and scale parts when combining them; these are reusable pieces, not registered layers for rebuilding the original icons automatically.
- `Catalog.jpg` is a labeled preview. Its gray panels are not present in the component PNGs.

## Folders

1. `01_Modifiers`: arrows, prohibition cross, sound waves, motion and anger marks, aggression burst, medical cross.
2. `02_Anatomy`: heads, bodies, eyes, ears, stomachs, hearts, bone, hand, wings and tails.
3. `03_Expressions`: standalone facial features and multicolor mood disks.
4. `04_Symbols`: sex symbols, DNA, gears, chess piece, insect, moon, cloud, sleep, radiation and UV sun.
5. `05_Objects`: flask, bubbles, brush, food, rock, mountains, anvil, cat and thermometers.
6. `06_Patterns`: mineral patches, scale mesh and facial stripes.

## Sources and fidelity

The library was selected by inspecting all 49 gene icons in `XylXenos/Textures/Xyl/UI/Icons/Genes` and all 45 `Gene_*.png` icons in `Art/Rimworld art`. Corresponding layered GIMP artwork in `Art` supplied most exports. Hidden reference, outline and unused experiment layers were excluded, except for the clean standalone medical cross recovered from the emergency-reserves draft.

`sources.json` identifies the source file and GIMP layer names/indices for each component, its cropped source resolution, color treatment and any reconstruction. Most shapes were exported directly from existing layers. The vanilla-only PNGs are 128 x 128; their exports are enlarged for consistent canvas size and do not gain new detail. At small icon sizes this is usually inconsequential, but prefer the native-layer components when enlarging a composition.

Color selection and black-matte removal recover alpha for flattened vanilla art. Tiny disconnected selection remnants were removed. The thermometer's hidden right half is restored by mirroring the unobscured left half. The hot thermometer's small shaft occlusion is filled with its original fluid color. No generative redraws were needed.

`broken-bone` intentionally keeps the fracture gap. `scale-mesh` is the full diamond-shaped source pattern, intended to be clipped to a head or body mask. `head-horned` and `head-bull` retain their integrated horns; the library does not invent separate hidden horn roots.

## Rebuilding

`build_library.py` recreates the PNGs, catalog and source index from repository artwork. Run it with Python containing Pillow and NumPy, with ImageMagick's `magick` executable on PATH. It uses a temporary cache for XCF layer exports and does not modify source artwork. Rebuilding overwrites the generated library files, so keep hand-edited variants elsewhere or under new filenames.
