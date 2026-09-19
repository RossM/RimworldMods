# Xenotype icon concepts

Reference: [current icon contact sheet](CurrentXenotypeIcons.png).

## Primary constraint: readable and distinct at 32 px

32 x 32 px is the design target, not a secondary preview. The 128 px `_small` files are source assets; their resolution does not establish in-game legibility. Judge concepts at native 32 px alongside the seven existing icons before refining larger artwork.

Match the existing front-facing white heads, black contours, and simple facial cutouts. Give each new icon one unmistakable silhouette and one dominant internal cue. Exaggerate identifying anatomy where needed. Omit incidental traits rather than shrinking them to fit.

As starting targets at the final 32 px size, allow roughly 2 px of outside margin, use approximately 2 px contours, and keep essential gaps and facial marks at least 2 px wide through their readable sections. Tapered tips may be thinner, but identity must not depend on them. Adjust these targets after rasterization to match the existing set's visual weight. Do not rely on fine gray texture, tiny pupils, hair strands, nostrils, or delicate expressions.

These are proposed designs based on the current xenotype definitions, not replacement assets. All four currently use vanilla placeholder icons.

## Zeegee: tall oval + large eyes + forehead mark

Use a tall, narrow, smooth head with a high forehead and tapered chin. Two oversized upright black oval eyes and one solid black vertical forehead mark are the only internal features. Keep clear white space between all three. Omit the hairline, mouth, and projecting ears; close-cropped hair does not need a separate shape at this scale.

The slender silhouette and enlarged eyes suggest a delicate, space-adapted human. The forehead mark becomes a strong, high-contrast identifier rather than a gray decorative accent. Keep the eyes upright rather than Nixie's slanted eyes; the tall, earless outline supplies the stronger distinction.

**Must read at 32 px:** narrow head, large paired eyes, separate forehead mark.

## Omegabeaver: round ears + broad head + tooth block

Use a squat, broad head with two prominent round ear bumps. Add small black eyes and one broad black muzzle/mouth shape containing two large white rectangular incisors. Let the tooth block interrupt the lower face outline. Separate the teeth with a sturdy black gap; omit a separately drawn nose if it crowds the muzzle.

Round ears distinguish the silhouette from Warcat's pointed ears. The projecting tooth block separates the face from Dvergr's bearded head. Keep the cheeks smooth: no fur notches, whiskers, cheek lines, or tiny muzzle details. The rounded proportions convey friendliness without requiring a smile.

**Must read at 32 px:** round ears and unmistakable paired buck teeth.

## Scaleborn: tall horns + angular jaw

Use a broad, angular head with a short squared jaw and two thick horns rising steeply above the temples. Give the horns a clear outward bend and preserve open gaps between the horns and crown. Use two broad, slanted black eye cutouts; let their upper edges imply the heavy brow. The angular cheek contour supplies the facial ridges without extra internal lines.

Make the horns taller and more upright than Bossaps' wide cattle horns, and keep the face strongly symmetrical to distinguish it from Trog. Remove the gray scale plates: at 32 px they spend contrast on texture and risk resembling Titan's mineral patches. Avoid breath effects so the emblem covers every lineage.

**Must read at 32 px:** tall paired horns, angular symmetrical face, heavy eyes.

## Succuboid: short horns + long hair frame

Use a tapered white face enclosed by one broad, smooth hair shape that widens toward the lower sides. Two short, thick triangular horns break the crown. Separate the face from the white hair with a continuous black opening wide enough to survive reduction. Two simple tilted black eyes are the only facial features.

Omit the wings entirely from this concept: hair, horns, eyes, and wings compete for the same 32 px. The short horns and smooth, flared hair frame provide a compact identity without Chyrr's wing silhouette or Warcat's ears and braids. Keep the hair ends blunt and unsegmented so they cannot read as braids. Omit tail, hearts, jewelry, and fine expression lines.

**Must read at 32 px:** two short horns and a broad, continuous long-hair frame around an open face.

## Acceptance checks for future artwork

- Inspect actual 32 x 32 px raster exports at 100% display scale, using the same downsampling as the shipped assets. Enlarged previews are supplementary.
- Compare all eleven icons in one unlabeled row on a representative dark game background. Each new icon should be distinguishable immediately, especially from its closest existing neighbors.
- Check a silhouette-only version. Each outline should be distinct before internal markings help identification.
- Confirm that eyes, teeth, forehead mark, and essential gaps remain separate after reduction. If they merge, enlarge or remove shapes rather than adding detail.
- Review all four new icons together: Zeegee tall and narrow; omegabeaver short and round; scaleborn angular with tall horns; succuboid broad hair frame with short horns.

## Source references

- Existing art: `XylXenos/Textures/Xyl/UI/Icons/Xenotypes/` (seven designs, each with a 128 px `_small` version referenced by its definition).
- Zeegee traits: `XylXenos/Defs/GeneDefs/XenotypeDefs_Zeegee.xml`.
- Omegabeaver, scaleborn, and succuboid traits: `XylXenos/Defs/GeneDefs/XenotypeDefs_Minor.xml`.
