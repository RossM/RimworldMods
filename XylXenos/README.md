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
- [Scaleborn](#scaleborn)
- [Succuboid](#succuboid)
- [Zeegee](#zeegee)
- [Currently unused genes](#currently-unused-genes)

## Bossaps

Bossaps were created as sentient livestock by the gene-engineers of a glitterworld best forgotten. They gain extra nourishment from raw plants, and their mostly female herds produce abundant milk. Naturally gregarious, they become unhappy in small colonies but tolerate shared barracks. While usually docile, injury can send a bossaps charging with its large horns in an uncontrollable rage - even at the friend whose shot went astray.

The glitterworld that created bossaps had a strict vegan philosophy that it was unethical to keep animals that couldn’t consent to their own treatment. Their solution was to engineer livestock that not only consented to being owned, but did so enthusiastically. Long after the fall of their birthplace, wild Bossaps herds can be found on many rimworlds.

"Bossaps" is a contraction of "Bos sapiens", a scientific name meaning "Intelligent cow".

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

Chyrr are batlike xenohumans who trace their origin to the blind healer Marah. They have a strong aptitude for medicine, and their echolocation and keen hearing let them “see” using sound as well as vision. Their bodies are easily injured, but they can stun living enemies with a cry of ultrasound and psychic energy, then take flight on membranous wings for a short escape. In colder weather, they naturally enter a state of hibernation, leaving them protected from hypothermia and frostbite but otherwise helpless.

According to chyrr legend, a blind woman named Marah came across an injured stranger. She brought the stranger back to her tribe and spent many moons nursing him back to health. As a parting gift, the stranger transformed Marah and her tribe into chyrr.

"Chyrr" is a corruption of "chiropteran", the scientific name for bats, which comes from the roots *chiro* "hand" and *ptera* "wing".

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

Dvergr hail from the vast underground cities beneath the inhospitable surface of the mineral world Svartalfheim. They see in darkness, tolerate great heat, and never need to go outdoors. They are fast workers and skilled miners, builders, and craftspeople. Their stomachs extract extra nourishment from raw fungus, although they hate the taste. They are both culturally and metabolically dependent on regular alcohol consumption, and going without it leads to illness and eventually death.

Dvergr society is organized around the clan and the corporation; the two are one and the same. They are renowned for their skill with explosives, deep understanding of geology, excellent legal departments, and remarkably short safety manuals.

"Dvergr" is the Old Norse word for dwarf.

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

Originally engineered for the water world of Atlantis, nixies have spread across inhabited space. Their scaled skin and webbed hands provide protection and let them move quickly in water, but long periods outside it will leave them miserable with cracked, bleeding skin. Beyond their physical adaptations, they possess otherworldly beauty and innate psychic powers that draw others towards them. They are skilled negotiators but poor miners and builders, trading with “dryskins” for manufactured goods.

The planet Atlantis is entirely covered by a shallow, planet-wide sea. Without access to dry land, nixies were unable to develop advanced technology and industry. Instead, they put their efforts into developing art and culture, creating beautiful coral gardens and ethereal songs that echo for miles underwater. The source of their psychic abilities is a mystery.

"Nixie" comes from the name of mythical water spirits from European folklore that used their beauty and songs to lure unsuspecting people into the water.

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

Titans are mass-produced workers originally designed for reclaiming deathworlds. Specialized carbon-silicate lithoid biochemistry makes them immune to toxins and highly resistant to injury and disease, letting them work in conditions that would be lethal for other xenotypes. Each is engineered with exceptional aptitude and passion for one random skill, but they have great difficulty learning skills that they aren’t passionate about. Their unusual biochemistry also leaves them susceptible to petrification, an incurable genetic disease that gradually turns their tissues to solid stone.

Today, titans can be found on more than just the deathworlds they were engineered for. They are employed in space construction, mining, and agriculture, among other fields. Their employers are mostly well-resourced governments or corporations that are able to afford the expense of providing them with a regular supply of lithoid drugs such as softener and atlasite.

"Titan" comes from the name of godlike beings from Greek mythology. The most well-known titan was Atlas, who held up the world on his shoulders.

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

Trogs emerged from generations of interbreeding among wasters, dirtmoles, and neanderthals on the worst rimworlds known to mankind. They are ugly, slow, and poor learners, but skilled miners and animal handlers. They emit special pheromones that let them move freely among giant insects, and even tame them and employ them as war animals. Like wasters, pollution invigorates them, but they are only partially resistant to its effects. Roughly half inherit additional random genes from who-knows-where.

Trogs can be found anywhere other xenotypes don’t want to live, from polluted wastelands to underground caverns to volcanic lava fields. They form aggressive, territorial clans that often squabble as much among themselves as they do with outsiders. Despite their unwholesome reputation, however, they can be quite welcoming to outcast and shunned people of any xenotype.

"Trog" is a contraction of "troglodyte", meaning a person who lives in a cave.

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

Warcats were created in military gene-vats as experimental supersoldiers - part human, part great cat, all fast-twitch muscle. But something in the splice broke, leaving them dependent on regular raw meat to survive. Their descendants roam the rimworlds in packs, hunting with retractable claws, powerful melee attacks, and catlike reflexes.

Warcat tribes can be a constant threat on the rim. Their need for meat drives them to hunt anything they can, even other xenohumans. Many warcat hunters carry the skulls of those they have eaten as trophies.

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

## Scaleborn

Scaleborn were fashioned after the mythical dragons of ancient Earth. Their tough scales, claws, and sharp horns make them ferocious fighters, and each lineage has unique abilities such as fiery breath or blinding oil. Despite their strength, however, their reptilian metabolism makes them slow runners, and they become even slower in the cold.

On the dino-world Tyrantis V, scaleborn are the dominant predators, pursuing the megafauna in ritual hunts. Successful hunters will share the meat with not only their own tribe but their neighbors, hosting great feasts and displaying the skulls of their prey as trophies.

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

## Succuboid

Succuboids were created as fashionable companions for glitterworld elite, with distinctive pink skin, horns, wings, and tails. They are beautiful and charming, and intimacy gives their partners a lasting, addictive euphoria. Manual labor was never a consideration of their design, and their inherent laziness and increased need for sleep make them poor workers.

While their origin is very similar to that of highmates, succuboids have one important difference: at some point, they took control of their own reproduction, engineering themselves with the ability to reproduce with other xenotypes and invariably produce succuboid daughters. On some worlds they are considered prestigious companions, on others they are valuable merchandise, and on yet others they are treated as dangerous social parasites.

"Succuboid" is a homage to succubi, mythological female demons that tempted men in their sleep.

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

## Zeegee

Zeegees descend from the inhabitants of deep-space habitats and ancient generation ships. They are comfortable indoors and in low gravity, but on planets they are prone to bouts of debilitating nausea. They learn quickly, excel at intellectual work, and have a natural connection to machines that makes them adept at controlling mechanoids and piloting gravships. In emergency situations oxygen and protective proteins stored in their bone marrow can briefly protect them against vacuum, toxins, and temperature extremes. Enhanced vision, adapted for the darkness and distances of deep space, makes them excellent marksmen.

Even the most colossal spaceborn habitat can eventually break down due to years of accidents, neglect, or conflict. The former inhabitants of those lost paradises are now scattered across the galaxy. Many find their homes in spacer guilds and salvager gangs, but even more have settled on planets where they put their unique biological traits to use despite the gravity.

"Zeegee" is a contraction of the initial letters of "zero gravity".

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
