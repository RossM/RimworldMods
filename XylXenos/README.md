# Xyl's Xenotypes

Ten xenotypes for RimWorld, from fungus-fed underground builders to vat-grown stone giants, aquatic traders, and descendants of generation-ship crews. Each brings its own needs, strengths, and complications to colony life.

**Requires RimWorld 1.6, Biotech, and Harmony.** Royalty adds innate psycasts; Ideology and Odyssey provide additional features where noted.

All xenotypes except titans are inheritable. The tables describe the mod's new functional genes; the lists below them include existing genes and appearance options.

- [Bossaps](#bossaps)
- [Chyrr](#chyrr)
- [Dvergr](#dvergr)
- [Nixie](#nixie)
- [Titan](#titan)
- [Trog](#trog)
- [Warcat](#warcat)
- [Succuboid](#succuboid)
- [Scaleborn](#scaleborn)
- [Zeegee](#zeegee)
- [Currently unused genes](#currently-unused-genes)

## Bossaps

Bossaps were engineered as sentient livestock by glitterworld gene-artisans. Fertile, mostly female, and able to live on raw plants, they produce abundant milk and find comfort in a herd. Their docility makes them easy to exploit, but injury can send them charging into a horn-first frenzy—even against an ally who hurt them. Wild herds roam the rim, far from the people who designed them.

### Genes

| Gene | Effect |
| --- | --- |
| [Herd instinct](Defs/GeneDefs/GeneDefs_Mood.xml) | Small colonies cause unhappiness; shared barracks do not. |
| [Docile](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Never starts social fights. Easier to enslave, less likely to escape or rebel, and happier in captivity. |
| [Large horns](Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Adds horns that serve as melee weapons. |
| [Seeing red](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Injury can trigger an uncontrollable melee frenzy with greater speed, strength, and pain resistance. Allies who cause injury can become targets. |
| [Pain reversal](Defs/GeneDefs/GeneDefs_Traits.xml) | Grants Masochist: pain improves mood. |
| [Hyperlactation](Defs/GeneDefs/GeneDefs_Hyperlactation.xml) | Females produce milk without pregnancy and can be milked. |
| [Usually female](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Germline carriers have a 75% chance of being female. |
| [Herbivore stomach](Defs/GeneDefs/GeneDefs_Diet.xml) | Gets more nutrition from raw plants and less from meat. Can eat raw plants without the usual raw-food mood penalty or food poisoning. |

**Other genes and appearance**

- **Traits:** cold tolerant, heat tolerant, robust, fertile, nearsighted.
- **Skills:** poor cooking aptitude, strong plants aptitude.
- **Appearance:** [cow ears](Defs/GeneDefs/GeneDefs_Cosmetic.xml); long hair, standard body, fat body, hulk body.

## Chyrr

Chyrr are nocturnal flying mystics from hot jungles and deserts. They navigate by sound, with hearing sharp enough to compensate for blindness, and defend their fragile bodies with a stunning cry. Their affinity for blindsight suits a life in which eyes are optional. Sunlight troubles them; cold sends them into hibernation. They make gifted medics, provided they can stay warm and work at night.

### Genes

| Gene | Effect |
| --- | --- |
| [Sonic wave](Defs/GeneDefs/GeneDefs_Abilities.xml) | Stuns organic creatures around a target with an ultrasonic and psychic cry. |
| [Bat wings](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Allows short flights at the cost of some manipulation. Heavy torso apparel can prevent flight. |
| [Torpor](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Cold causes hibernation, reducing food needs and slowing starvation and hypothermia. Warmth reverses it. |
| [Nocturnal](Defs/GeneDefs/GeneDefs_Traits.xml) | Grants Night Owl: prefers being awake at night. |
| [Keen ears](Defs/GeneDefs/GeneDefs_Senses.xml) | Improves hearing by 20%. |
| [Echolocation](Defs/GeneDefs/GeneDefs_Senses.xml) | Uses hearing instead of sight when better, except for reading. Some carriers are born blind. |

**Other genes and appearance**

- **Traits:** weak immunity, cold sensitive, heat tolerant, mild UV sensitivity, delicate, dark vision.
- **Skills:** poor mining aptitude, strong medicine aptitude.
- **Appearance:** [small pointed ears](Defs/GeneDefs/GeneDefs_Cosmetic.xml); standard body, thin body, no beard.

## Dvergr

Dvergr come from Svartalfheim, a mineral-rich world of underground cities powered by magma. Short, heat-tolerant, and industrious, they build their lives around mining and craftwork. Fungus feeds them and supplies their breweries, though even their specialized digestion cannot improve its taste. Alcohol is a necessity.

Their settlements form loose unions: difficult to please, but powerful allies once won over. Dvergr prefer work to conversation, and their stoicism conceals a persistent melancholy. Long stretches of labor sometimes end in a breakdown—and, occasionally, a work of extraordinary craftsmanship.

### Genes

| Gene | Effect |
| --- | --- |
| [Stoic](Defs/GeneDefs/GeneDefs_Traits.xml) | Lowers the mental-break threshold. |
| [Melancholy](Defs/GeneDefs/GeneDefs_Traits.xml) | Grants Tortured Artist: persistent unhappiness, with a chance of creative inspiration after a breakdown. |
| [Dwarf](Defs/GeneDefs/GeneDefs_Body.xml) | A smaller body with slightly slower movement. |
| [Fungus eater](Defs/GeneDefs/GeneDefs_Diet.xml) | Gets extra nutrition from raw fungus. Enables fungus brewing and, with Ideology, fungal gravel. |
| [Industrious](Defs/GeneDefs/GeneDefs_Traits.xml) | Works faster. |

**Other genes and appearance**

- **Traits:** psychically dull, very heat tolerant, aggressive, strong stomach, dark vision, cave dweller, unstoppable, alcohol dependency.
- **Skills:** strong construction aptitude, strong mining aptitude, strong crafting aptitude, poor medicine aptitude, poor social aptitude.
- **Appearance:** [bald males](Defs/GeneDefs/GeneDefs_Hair.xml); [small pointed ears](Defs/GeneDefs/GeneDefs_Cosmetic.xml); short hair, bushy beard, light gray skin, slate gray skin, ink-black skin.

## Nixie

Engineered for the ocean world Atlantis, nixies have spread across many planets. On the rim they live in river clans and coastal camps, trading with the 'dryskins' and sometimes venturing inland with engineered seal pack animals called selkies. Their scaled skin needs regular moisture, making water both a home and a necessity.

Beauty, eloquence, and an instinctive psychic attraction give them an almost divine presence. They favor nudity and pleasure, but their sensitivity to drugs makes indulgence risky. In a colony, they excel at social work and move easily through water; dry heat is a constant discomfort.

### Genes

| Gene | Effect |
| --- | --- |
| [Beckon](Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml) | Draws a target toward the caster. Requires Royalty and psyfocus, but no psylink. |
| [Focus](Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml) | Improves a target's performance. Requires Royalty and psyfocus, but no psylink. |
| [Scaleskin](Defs/GeneDefs/GeneDefs_Body.xml) | Scales provide natural protection against sharp and blunt attacks. |
| [Drug sensitive](Defs/GeneDefs/GeneDefs_Drugs.xml) | Drug effects last longer, tolerance builds faster, and addiction is more likely. |
| [Aquatic](Defs/GeneDefs/GeneDefs_Needs.xml) | Needs water, rain, or showers to keep skin moist. Dryness causes unhappiness; hot weather dries skin faster. Unlocks showers. |

**Other genes and appearance**

- **Traits:** enhanced psychic sensitivity, naked speed, webbed phalanges, pessimist, very cold tolerant, heat sensitive, weak melee damage, pretty.
- **Skills:** poor mining aptitude, poor construction aptitude, strong social aptitude.
- **Appearance:** [fin ears](Defs/GeneDefs/GeneDefs_Cosmetic.xml); bald, snow-white hair, grayless hair, blue skin.

## Titan

Titans are sterile, vat-grown giants built to leave enduring structures behind them. Massive, armored, and indifferent to recreation, they labor in places most people would struggle to survive. Each has an exceptional talent in one field and little interest in subjects outside their passions.

Their carbon-silicate bodies require specialized drugs and heal slowly. They can also develop petrification, an incurable disease that gradually turns tissue to stone. Their endurance depends on care tailored to their unusual biology.

### Genes

| Gene | Effect |
| --- | --- |
| [Rock toss](Defs/GeneDefs/GeneDefs_Abilities.xml) | Throws a rock chunk, damaging everything near its landing point. |
| [Lithoid](Defs/GeneDefs/GeneDefs_Drugs.xml) | Most ordinary drugs have no effect; uses specialized lithoid drugs instead. |
| [Rockskin](Defs/GeneDefs/GeneDefs_Body.xml) | Heavy natural armor against sharp, blunt, and heat damage, at the cost of movement speed. |
| [Petrification](Defs/GeneDefs/GeneDefs_Petrification.xml) | Risks a disease that turns tissue to stone. Treatment slows it and surgery removes affected tissue; softener prevents onset but cannot cure it. |
| [Focused](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Learns faster in burning passions and much slower in skills without passion. |
| [Giant](Defs/GeneDefs/GeneDefs_Body.xml) | Larger and tougher, with greater carrying capacity and resistance to drugs and toxins. Easier to hit at range. |
| [Joyless](Defs/GeneDefs/GeneDefs_Needs.xml) | Has no recreation need or recreation-related mood effects. |
| [Super-specialist](Defs/GeneDefs/GeneDefs_BonusGenes.xml) | Gains +8 aptitude and one passion level in a random non-combat skill. |

**Other genes and appearance**

- **Traits:** strong melee damage, super immunity, slow wound healing, superclotting, psychically deaf, slow runner, total toxic resistance, sterile, ugly.
- **Skills:** poor social aptitude, poor intellectual aptitude.
- **Appearance:** [bald males](Defs/GeneDefs/GeneDefs_Hair.xml); no beard, hulk body.

**Lithoid drugs:** [Softener](Defs/Drugs/Drugs_Titan.xml) prevents petrification. Atlasite builds protective resistance with regular doses; crystal improves performance and dulls pain, but is highly addictive.

## Trog

Trogs descend from neanderthals, wasters, dirtmoles, and other xenohumans interbreeding on some of the harshest rimworlds. Their mixed ancestry still produces unexpected genes, while strength, disease resistance, and tolerance of pain sustain them in polluted underground warrens. Their bodies reject artificial implants, limiting their options for augmentation.

Their tribes are insular and warlike, defending their territory with brute force and tamed giant insects. Pheromones let them move among wild hives without being attacked, making the surrounding insect colonies another obstacle for intruders. Outsiders can expect little welcome and few opportunities to negotiate.

### Genes

| Gene | Effect |
| --- | --- |
| [Toxic burst](Defs/GeneDefs/GeneDefs_Abilities.xml) | Releases tox gas around the carrier, who is also exposed to it. |
| [Bio-rejection](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Artificial body parts and implants cause persistent pain. Mechlinks trigger it; psylinks do not. |
| [Insect pheromones](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Wild insects leave carriers alone. Colony animals also benefit from the protection. |
| [Genetic atavism](Defs/GeneDefs/GeneDefs_BonusGenes.xml) | A 50% chance of extra random non-archite xenogenes. |

**Other genes and appearance**

- **Traits:** strong immunity, slow runner, partial toxic-environment resistance, mild UV sensitivity, aggressive, strong melee damage, reduced pain, very ugly, slow study, dark vision, pollution rush, mild cell instability, psychite addiction resistance.
- **Skills:** strong mining aptitude, strong animals aptitude, terrible intellectual aptitude.
- **Appearance:** [warped head](Defs/GeneDefs/GeneDefs_Cosmetic.xml); [dark green skin](Defs/GeneDefs/GeneDefs_SkinColors.xml); [olive skin](Defs/GeneDefs/GeneDefs_SkinColors.xml); bald, human headbone, mini-horns, green skin.

## Warcat

Warcats are military hybrids of humans and great cats, built for speed and close combat. A flaw in their engineering left them dependent on raw meat. Their hunting bands roam in tight-knit prides, taking any prey they can overpower—including other xenohumans. After thousands of years on the rim, they are part of the ecosystem.

Proud, volatile, and fiercely aggressive, warcats form loose confederations that can still be negotiated with. Exiles must find new hunting grounds and a place to settle. Their claws and reflexes make them dangerous opponents, but even the strongest hunter must keep feeding.

### Genes

| Gene | Effect |
| --- | --- |
| [Feral rage](Defs/GeneDefs/GeneDefs_Abilities.xml) | Temporarily increases movement and melee attack speed, followed by pain and slower movement. |
| [Moody](Defs/GeneDefs/GeneDefs_Mood.xml) | Random bouts of good and bad mood. |
| [Fast reflexes](Defs/GeneDefs/GeneDefs_Traits.xml) | Grants Nimble: better melee dodging and trap avoidance. |
| [Retractable claws](Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Adds retractable claws as melee weapons. |
| [Carnivore stomach](Defs/GeneDefs/GeneDefs_Diet.xml) | Gets more nutrition from raw meat and less from plants. Can eat raw meat without the usual raw-food mood penalty or food poisoning. |
| [Meat dependence](Defs/GeneDefs/GeneDefs_Diet.xml) | Requires raw meat to survive. Deficiency accumulates and can become fatal; recovery requires making up the missed meat. |

**Other genes and appearance**

- **Traits:** hyper-aggressive, strong melee damage, sleepy, high libido, dark vision.
- **Skills:** remarkable melee aptitude, poor plants aptitude, poor crafting aptitude.
- **Appearance:** [yellow eyes](Defs/GeneDefs/GeneDefs_Cosmetic.xml); [facial stripes](Defs/GeneDefs/GeneDefs_Cosmetic.xml); long hair, no beard, cat ears, standard body, thin body.

## Succuboid

Succuboids were designed as fashionable companions for glitterworld elites: beautiful, winged, socially gifted, and able to make intimacy chemically addictive. They prefer leisure and nudity, and their youthful appearance lasts into old age.

They later took control of their reproduction, ensuring that their daughters would inherit the succuboid lineage regardless of their partner's xenotype. They still need males from other populations to reproduce. Their independence has not ended the fascination surrounding them: outsiders variously regard them as prestigious companions, valuable commodities, or dangerous social parasites.

### Genes

| Gene | Effect |
| --- | --- |
| [Word of Love](Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml) | Influences romantic attraction. Requires Royalty, but no psylink. |
| [Bat wings](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Allows short flights at the cost of some manipulation. Heavy torso apparel can prevent flight. |
| [Always female](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Germline carriers are always female. |
| [Strong genes](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Children inherit the carrier's endogenes when the other parent has a different xenotype, unless both parents have strong genes. |
| [Love euphoria](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Intimacy gives partners euphoria and possible inspiration, but can cause addiction and withdrawal. |
| [Youthful](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Uses an age no higher than 18 for lovin' frequency and relationship compatibility. Biological aging continues normally. |
| [Shameless](Defs/GeneDefs/GeneDefs_Traits.xml) | Grants Nudist: happier without clothes. |
| [Lazy](Defs/GeneDefs/GeneDefs_Traits.xml) | Works slower. |

**Other genes and appearance**

- **Traits:** enhanced psychic sensitivity, naked speed, weak melee damage, very sleepy, delicate, high libido, beautiful.
- **Skills:** strong social aptitude.
- **Appearance:** [pink skin](Defs/GeneDefs/GeneDefs_SkinColors.xml); long hair, mini-horns, standard body, pink hair, light purple hair, smooth tail, grayless hair.

## Scaleborn

Scaleborn began as a fashion for recreating the dragons of ancient Earth. On the dinosaur world Tyrantis V, those modifications became tools of survival: horns, claws, armored scales, and specialized breath weapons help them hunt megafauna for food and materials.

Five colored lineages carry different weapons and defenses. All are carnivorous, aggressive, and prone to unpredictable moods. Their strength comes with a slow pace and a need for sleep; cold can send them into torpor.

### Genes

| Gene | Effect |
| --- | --- |
| [Moody](Defs/GeneDefs/GeneDefs_Mood.xml) | Random bouts of good and bad mood. |
| [Torpor](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Cold causes hibernation, reducing food needs and slowing starvation and hypothermia. Warmth reverses it. |
| [Large horns](Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Adds horns that serve as melee weapons. |
| [Retractable claws](Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Adds retractable claws as melee weapons. |
| [Carnivore stomach](Defs/GeneDefs/GeneDefs_Diet.xml) | Gets more nutrition from raw meat and less from plants. Can eat raw meat without the usual raw-food mood penalty or food poisoning. |
| [Scaleskin](Defs/GeneDefs/GeneDefs_Body.xml) | Scales provide natural protection against sharp and blunt attacks. |
| [Scaleborn lineage](Defs/GeneDefs/GeneDefs_BonusGenes.xml) | Adds one of the five lineage packages below. |

**Other genes and appearance**

- **Traits:** psychically dull, slow runner, cold tolerant, heat tolerant, aggressive, strong melee damage, sleepy.
- **Skills:** poor cooking aptitude.
- **Appearance:** bald, no beard, facial ridges.

### Lineages

Each scaleborn receives one additional gene package:

| Lineage | Genes |
| --- | --- |
| Red | Fire spew, fire resistance, deep red skin |
| Green | Acid spray, partial toxic resistance, dark green skin |
| White | Foam spray, fast wound healing, sheer white skin |
| Blue | EMP blast, unstoppable, dark blue skin |
| Black | Oil spray, robust, slate gray skin |

| Gene | Effect |
| --- | --- |
| [EMP blast](Defs/GeneDefs/GeneDefs_Abilities.xml) | Disables nearby electronics with an electromagnetic pulse. |
| [Oil spray](Defs/GeneDefs/GeneDefs_Abilities.xml) | Sprays oil that temporarily blinds targets and leaves flammable puddles. |

## Zeegee

Zeegees descend from the inhabitants of deep-space habitats and ancient generation ships. Silver-skinned and marked on the forehead, they are quick learners with a talent for advanced technology. Keen distance vision makes them good shots, while dark vision and comfort indoors suit life aboard a ship.

Planetary gravity is harder on them. Their delicate bodies suffer bouts of nausea on the surface, and weak immunity adds to the difficulty of settling a new world. Stored oxygen and protective proteins give them a temporary defense against vacuum and other environmental emergencies, buying time to reach safety.

### Genes

| Gene | Effect |
| --- | --- |
| [Planet sickness](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Periodic nausea and vomiting on planetary surfaces. |
| [Emergency reserves](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Temporary protection against vacuum, toxins, heatstroke, and hypothermia, followed by a recovery period with increased hunger. |
| [Telescopic vision](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Improves shooting accuracy at medium and long range. |
| [Tech affinity](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Adds 3 mech bandwidth and improves piloting with Odyssey. |

**Other genes and appearance**

- **Traits:** weak immunity, weak melee damage, delicate, space movement speed (Odyssey), fast learning, dark vision, cave dweller.
- **Skills:** strong shooting aptitude, poor melee aptitude, poor animals aptitude, strong intellectual aptitude.
- **Appearance:** [forehead mark](Defs/GeneDefs/GeneDefs_Cosmetic.xml); [dark silver skin](Defs/GeneDefs/GeneDefs_SkinColors.xml); [light silver skin](Defs/GeneDefs/GeneDefs_SkinColors.xml); short hair.

## Currently unused genes

These genes are available for custom xenotypes but are not part of the mod's xenotypes or their lineage packages.

### Functional genes

| Gene | Effect |
| --- | --- |
| [Drug resistant](Defs/GeneDefs/GeneDefs_Drugs.xml) | Drug effects wear off faster, tolerance builds slower, and addiction is less likely. |
| [Even temper](Defs/GeneDefs/GeneDefs_Traits.xml) | Suppresses mood and nerves extremes, neuroticism, and several other volatile personality traits. |
| [Always male](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Germline carriers are always male. |
| [Usually male](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Germline carriers have a 75% chance of being male. |
| [Parthenogenic](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Females can become pregnant without a father. |
| [Precognition](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Improves melee and ranged dodging in proportion to psychic sensitivity. |
| [Specialist](Defs/GeneDefs/GeneDefs_BonusGenes.xml) | Grants +4 aptitude in a random non-combat skill. |
| [Voracious](Defs/GeneDefs/GeneDefs_Traits.xml) | Grants Cannibal and Gourmand. |
| [Weak genes](Defs/GeneDefs/GeneDefs_Reproduction.xml) | Children inherit the other parent's endogenes when that parent has a different xenotype, unless both parents have weak genes. |
| [Ultra-fast wound healing](Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | An archite gene that rapidly heals wounds, but not permanent scars or blood loss. |

### Appearance genes

- [Bald females](Defs/GeneDefs/GeneDefs_Hair.xml).
- [Short-haired females](Defs/GeneDefs/GeneDefs_Hair.xml).
- [Long-haired females](Defs/GeneDefs/GeneDefs_Hair.xml).
- [Short-haired males](Defs/GeneDefs/GeneDefs_Hair.xml).
- [Long-haired males](Defs/GeneDefs/GeneDefs_Hair.xml).
- [Visible ribs](Defs/GeneDefs/GeneDefs_Cosmetic.xml).
- [Dark purple skin](Defs/GeneDefs/GeneDefs_SkinColors.xml).

Additional psycast genes are generated from the available abilities. The xenotypes above use Beckon, Focus, and Word of Love.

## Mod data

Browse the [xenotype and gene definitions](Defs/GeneDefs/), [factions](Defs/Factions/), and [scenarios](Defs/Scenarios/) for the full data.
