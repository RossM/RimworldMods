# Beaver tail artwork

Gene: `XylTail_Beaver` in `XylXenos/Defs/GeneDefs/GeneDefs_Cosmetic.xml`.

Full-resolution originals from the built-in imagegen tool are kept here. Runtime PNGs are 128 x 128 RGBA textures. The dark charcoal color is baked into the sprite, matching the alphabeaver rather than inheriting pawn hair color.

References inspected: the east/north/south alphabeaver PSDs and naked human body/head PNGs in `Art/Game art source - RimWorld.zip`; the furry/smooth tail sprites and gene icons in `Art/Game art source - Biotech.zip`. Reference extractions remain in ignored `tmp/beaver-tail/reference`.

Rendering follows the installed RimWorld 1.6 `GeneTailBase` and `PawnRenderNodeWorker_AttachmentBody`: mutually exclusive tails, body-size-adjusted placement, layer -2 except layer 90 when facing north. This gene is explicitly cosmetic with zero complexity and metabolism and no stat or capacity modifiers.

Exports (run from repository root with ImageMagick):

```powershell
magick Art/Codex/BeaverTail/BeaverTail_east_source.png -resize 128x128 -strip PNG32:XylXenos/Textures/Xyl/Things/Pawn/Humanlike/BodyAttachments/BeaverTail/BeaverTail_east.png
magick Art/Codex/BeaverTail/BeaverTail_north_source.png -resize 100x100 -background none -gravity center -extent 128x128 -strip PNG32:XylXenos/Textures/Xyl/Things/Pawn/Humanlike/BodyAttachments/BeaverTail/BeaverTail_north.png
Copy-Item XylXenos/Textures/Xyl/Things/Pawn/Humanlike/BodyAttachments/BeaverTail/BeaverTail_north.png XylXenos/Textures/Xyl/Things/Pawn/Humanlike/BodyAttachments/BeaverTail/BeaverTail_south.png
magick Art/Codex/BeaverTail/Gene_TailBeaver_source.png -resize 128x128 -strip PNG32:XylXenos/Textures/Xyl/UI/Icons/Genes/Gene_TailBeaver.png
```

The tail is visible only when facing east, north, or west, using `visibleFacing`. It is hidden entirely when facing south. The south texture remains a copy of north to complete the directional texture set. RimWorld mirrors east for west. The rear sprite is padded to keep its width proportionate to the pawn.

`BeaverTail_preview.png` is an offline compositing check of five adult body types in all four directions, using the runtime textures and definition offsets. It is not an in-game screenshot.

## Imagegen prompts

### East

Use case: stylized-concept. Asset type: single transparent RimWorld pawn tail sprite, EAST-facing pawn, tail only. Reference image 1 is a sheet of original game art: top row alphabeaver east/north/south; middle furry tail; bottom smooth tail. Recreate the dark rounded paddle tail at the far LEFT of the top-left alphabeaver, as an isolated larger clean sprite. Broad blunt oval paddle pointing LEFT, narrowing to a short attachment neck on the RIGHT, very slightly slanted up toward the right. Match original simple near-black 3px-equivalent outline and smooth restrained charcoal gray highlight, darkest underside. No scales, no crosshatch, no fur, no body, no text, no cast shadow. Actual transparent RGBA background. The isolated tail should span about 75% of a square canvas width and 36% height centered, with generous transparent margins, full outline unclipped. Flat 2D game art readable when reduced to 128x128. Only ONE east sprite.

### North initial generation

Use case: stylized-concept. Asset type: single transparent RimWorld pawn tail sprite, NORTH/rear view, tail only. Reference image is a sheet of original game art: top row alphabeaver east/north/south; middle furry tail; bottom smooth tail. Match the tail at the bottom of the TOP MIDDLE alphabeaver. Create an isolated dark smooth broad paddle tail hanging DOWN, slender attachment at TOP center, broadening gradually into a blunt rounded bottom. Symmetric vertical teardrop/paddle, no sharp tip. Near-black clear outline, charcoal gray fill with soft simple highlight on upper/left middle and dark lower edge, matching the alphabeaver's untextured tail. No scales or crosshatch, no fur, no animal or human, no text, no shadow outside tail. Actual transparent RGBA background. Tail about 40% of square canvas width and 70% height, centered, with full outline unclipped and transparent margins. Flat 2D game sprite readable reduced to 128x128. ONE tail only.

### North final shape correction

Edit image 1 (the isolated north beaver tail). Image 2 is the ORIGINAL ALPHABEAVER rear-view shape reference in the top middle of the reference sheet. Make the tail match that alphabeaver much more closely: REMOVE the long thin spoon handle/neck. The attachment should be a short broad rounded top merging directly into a squat broad oval paddle, like a rounded trapezoid with a domed top, broader lower half and completely rounded blunt bottom. Total silhouette about 50% canvas width and 65% canvas height, centered. The top attachment is about HALF the paddle width and there is NO stem longer than 5% of tail length. Keep the simple dark charcoal gray soft highlight, solid black outline of uniform thickness, transparent background. No texture, scales, crosshatch, body, or text. Only change shape as described. It is a RimWorld game sprite.

### Icon initial generation

Use case: stylized-concept. Asset type: one RimWorld cosmetic gene UI icon on actual transparent RGBA background. Reference 1: original RimWorld smooth-tail gene icon, match its simple solid WHITE silhouette language. Reference 2: original alphabeaver and vanilla tail artwork sheet, use alphabeaver paddle tail shape only. Draw a single solid pure-white silhouette of a BEAVER TAIL: short narrower attachment neck at upper right flowing into a broad blunt oval paddle at lower left, angled diagonally about 40 degrees. Rounded paddle tip, no point, no fur, no scales, no pattern, no outline, no gray shading, no body, no extra symbol, no hexagon, no text or backdrop. Bold recognizable continuous shape similar proportions to the original alphabeaver tail, occupying central 74% of square icon with transparent margins. Intended to reduce to 128x128.

### Icon edge correction (before outline)

Edit target image 1: clean the white beaver-tail gene icon. The silhouette outline currently has ugly ragged/chipped edges and stray speckles. Replace it with an impeccably smooth anti-aliased curved silhouette, smooth continuous contour like reference image 2 (original RimWorld gene icon). Preserve beaver-tail subject: broad round oval paddle lower-left, short neck upper-right, diagonal orientation. Shorten neck slightly. Pure solid white interior, fully opaque; actual fully transparent background; clean smooth alpha boundary, no fringes, no isolated stray pixels, no grain, no texture, no gray, no black outline, no shadows. One single shape centered on square canvas with 15% margin on each side. Designed to remain smooth at 128x128. No text. Reference image 2 is style only, do not recreate its curled thin tail.

### Icon black outline (current version)

The white fill is tinted by the gene's `(0.75, 0.75, 0.75)` icon color, almost matching the gray endogene badge. A solid black outline keeps the silhouette visible at small UI sizes. `Gene_TailBeaver_source.png` is the full-size outlined source; the export command above produces the runtime icon. `Gene_TailBeaver_preview.png` compares the old and current icons at 48 and 96 pixels with the vanilla badge and the gene tint; this is an offline composite, not an in-game screenshot.

Built-in imagegen edit prompt:

Use case: precise-object-edit. Edit the supplied RimWorld BEAVER TAIL gene icon. Preserve its exact broad paddle silhouette, diagonal placement with rounded paddle lower-left and narrow short attachment upper-right, and white interior. ADD a continuous SOLID BLACK OUTLINE following the whole silhouette. Outline must be bold, smooth, uniform, and fully opaque, about 4 pixels thick at a final 128x128 resolution (about 40 pixels on this full-size canvas), so the icon remains distinct over a light gray circular gene badge when shown at 48 pixels. Flat UI symbol, pure white fill and pure black outline, smooth antialiased edges. Actual transparent RGBA background outside the outline, with no cast shadow, no backdrop, no frame, no extra objects, no text, no scales, no fur, no texture. Keep the same shape and composition; the black outline is the only visual change. Keep full silhouette within generous transparent margins on a square canvas.
