# Gene icon art concepts

## Shared visual direction

Each icon should have one main silhouette and, at most, one supporting detail. Use the same heavy near-black outline, flat off-white or gray body, restrained accent color, and minimal shading as the supplied RimWorld samples. Compose on a transparent 256 x 256 canvas and judge the design primarily at 64 x 64.

Where a gene represents a straightforward change, favor a large arrow as the supporting detail. Direction describes what changes and color describes whether that change is beneficial: green up for a beneficial increase, green down for a beneficial reduction, red up for a harmful increase, and red down for a harmful reduction. An established game-icon family takes precedence over this general rule, such as the blue wound-healing arrows.

Related genes should reuse a composition. Avoid scenes, sequences, clusters of small symbols, ordinary text, and details that only become legible when enlarged. Established pictograms such as sleep `Z` shapes are fine.

## Body and cosmetic genes

### Dwarf (`XylDwarf`)

A coiled white measuring tape beside a broad red down arrow. Keep the markings on the tape large and sparse; the tape establishes body size while the arrow communicates a harmful reduction.

### Forehead mark (`XylForeheadMark`)

A plain white head with the split blue chevron marking centered high on the forehead. Use one representative in-game marking rather than trying to show every variant.

### Ribs (`XylRibs`)

A pale front-facing torso with three broad dark rib lines on each side of a short sternum, matching the actual body texture.

## Random and lineage genes

### Specialist (`XylSpecialist`)

Match the composition of `Gene_StrongArtistic`, replacing the paintbrush with a white six-sided die. Keep the normal green up arrow unchanged; the die acts as the random skill icon.

### Super-specialist (`XylSuperSpecialist`)

Reuse the specialist die, but place it over a larger double-stepped green up arrow. The stronger arrow distinguishes the higher aptitude tier without adding a separate passion symbol.

### Scaleborn lineage (`XylScalebornLineage`)

A single neutral olive dragon egg with a few broad scales and one prominent crack. Keep it lineage-agnostic so the icon remains suitable when more scaleborn lineages are added.

## Biochemistry and health genes

### Lithoid (`XylLithoid`)

A faceted slate-gray heart with a violet crystal at its center. It should look like a living organ made from stone, emphasizing mineral biochemistry without trying to summarize its mixed drug interactions.

### Torpor (`XylTorpor`)

A blue thermometer with two large olive `Z` shapes rising from it. Follow the simple thermometer silhouette and chunky sleep lettering used by the game icons.

### Ultra-fast wound healing (`XylWoundHealing_UltraFast`)

Extend the game's wound-healing family directly: use the pale-blue medical cross built into a blue up arrow from `Gene_WoundHealingRateSuperfast`, enlarged with one additional stepped notch. Do not introduce a wound or body-part illustration.

### Petrification (`XylPetrification`)

A single pawn bust split by a jagged boundary: muted living flesh on one side and cracked gray stone on the other. Use solid stone rather than surface plates so it remains distinct from `Gene_MineralizedSkin`; the transformation itself is clear enough without an arrow.

## Hair-style genes

Use the exact visual family established by `Gene_HairStyleBaldOnly`, `Gene_HairStyleShortOnly`, and `Gene_HairStyleLongOnly`: the same white front-facing head, light-gray facial marks, and simple gray hair silhouette. A small cyan male or pink female symbol at the lower corner is the only addition.

### Bald males (`XylHair_BaldOnly_Male`)

The game-style bald head, including the small shine mark, with a cyan male symbol at the lower corner.

### Short-haired males (`XylHair_ShortOnly_Male`)

The game-style short-haired head with a cyan male symbol at the lower corner.

### Long-haired males (`XylHair_LongOnly_Male`)

The game-style long-haired head with a cyan male symbol at the lower corner.

### Bald females (`XylHair_BaldOnly_Female`)

The game-style bald head, including the small shine mark, with a pink female symbol at the lower corner.

### Short-haired females (`XylHair_ShortOnly_Female`)

The game-style short-haired head with a pink female symbol at the lower corner.

### Long-haired females (`XylHair_LongOnly_Female`)

The game-style long-haired head with a pink female symbol at the lower corner.

## Learning and work genes

### Focused (`XylLearning_Focused`)

A single eye with a bright orange passion flame as its pupil. The eye communicates concentration and the flame identifies passion, without implying a general learning-speed change.

### Lazy (`XylLazy`)

A single dull-gray gear beneath a broad muted-red downward arrow. Keep both shapes large and overlapping so they read as one work-speed symbol.

## Reproduction genes

### Strong genes (`XylStrongGenes`)

A single teal DNA helix with a broad green up arrow behind it. This should form a direct pair with weak genes rather than trying to diagram two parents and a child.

### Weak genes (`XylWeakGenes`)

Reuse the same teal DNA helix with a broad red down arrow behind it. Keep the scale and placement identical to strong genes.

### Parthenogenic (`XylParthenogenic`)

A large pink female symbol with one small white egg held inside its circular center.

### Love euphoria (`XylLoveEuphoria`)

A heart-shaped potion bottle half-filled with violet liquid, with one bubble rising inside the neck. The heart and drug vessel are one combined silhouette.

### Youthful (`XylYouthful`)

A smooth white young face in front of an offset gray copy of the same face bearing two simple wrinkles. Show the old face only as a narrow shadow around one edge, communicating that age remains present while the visible appearance stays young.

## Diet and mood genes

### Voracious (`XylVoracious`)

A large open mouth with a red up arrow behind it, representing a harmful increase in appetite. A simple pointed tooth can hint at cannibalism without adding a separate food object.

### Shameless (`XylShameless`)

Reuse the olive shirt silhouette from `Gene_NakedSpeed`, but replace its superimposed gray weight with a broad green down arrow. The familiar clothing motif plus beneficial reduction communicates reduced shame around being unclothed without depicting a nude body.
