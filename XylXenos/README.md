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

Bossaps are bovine xenohumans with a strong need for the company of a herd. They are natural farmers who thrive on raw plant foods. Most bossaps are female and produce abundant milk. Their docile temperaments make communal life easy, but isolation leaves them unhappy. In combat, that calm can vanish without warning. Pain excites them, and a wounded bossaps may charge the nearest enemy in a blind fury, ignoring orders until the fighting is over.

Bossaps were engineered as sentient livestock by the gene-artisans of a glitterworld best left forgotten. Free herds still share meals, childcare, and sleeping space, but now decide for themselves how to live. Outsiders sometimes mistake their mild manners for obedience. Many former owners made the same mistake.

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

Chyrr are winged, nocturnal xenohumans adapted to warm climates. They find their way through darkness by sound and can take flight for short periods, making them difficult to corner. Though too fragile to withstand much punishment, they can stun living enemies with a piercing cry. Their keen senses also serve them well in medicine. They fare poorly outside the warmth and darkness they are accustomed to: sunlight slows them, and cold can send them into torpor.

Chyrr mystics are native to steaming jungles and sun-blasted deserts, where their communities shelter in high roosts during the day. Their traditions describe perception as a mingling of sound and low-level psychic sensing, with a hazy thread of precognition that lets the listener act just before something happens. Outsiders disagree over how much of this is psychic insight and how much is simply careful listening. To the chyrr, listening is a sacred discipline either way.

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

Dvergr are short, industrious xenohumans at home in the heat and darkness of deep tunnels. Their aptitude for working stone and metal makes them accomplished miners, builders, and craftspeople. Their digestion makes raw fungus nourishing without improving its taste, and they must regularly drink alcohol to survive. Dvergr endure hardship with little outward complaint, but their stoicism conceals a persistent melancholy. When they do break down, they sometimes emerge inspired to create something extraordinary.

Dvergr come from Svartalfheim, a craggy, mineral-rich world whose vast underground cities are powered by magma upwellings. Workshops and breweries form the centers of communal life, though many dvergr would rather discuss a stubborn seam of rock than make small talk, even over a drink. Smiles are rare, and idle hands rarer. Their most treasured works often come from quiet craftspeople who labor for years before revealing something no one else could have made.

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

Made for ocean worlds, nixies are graceful aquatic xenohumans who swim easily through cold water. They are beautiful and persuasive, with psychic gifts that let them beckon others closer or sharpen a companion's focus. Without regular soaking, their natural melancholy deepens. Though their scales offer some protection, they are weak in close combat and poor at heavy construction. Their sensitive bodies tolerate heat poorly and react strongly to drugs.

Nixies were engineered for the ocean world Atlantis and have since spread to many planets. On the rim, they live in scattered river clans and coastal camps. Their beauty, fluid movements, and graceful speech have earned them a place in local folklore as beings half-wild and half-divine. Stories credit them with an innate psychic pull that draws strangers to the water whether the nixies intend it or not. A clan visiting to trade may find itself greeted with offerings, suspicion, or both.

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

Titans are stony giants engineered for specialized labor. Each has an exceptional talent for a particular kind of work, and none feels the need for recreation. Their immense strength and armored skin make them formidable in a fight, but they are slow to move and slow to recover from injury. Their unusual biochemistry renders most ordinary drugs useless. It also leaves them vulnerable to petrification, an incurable disease that gradually replaces living tissue with stone.

Titans are grown in vats to build things that will outlast them. They have raised structures in poisonous wastelands, burning deserts, and frozen tundras, often for employers who care little about what becomes of them. They say little about their work and seem to care even less about its purpose. In abandoned industrial settlements, some still tend the machines and walls they helped build, long after the people who commissioned them have gone.

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

Trogs are misshapen xenohumans adapted to dark, polluted places. They are capable miners and animal handlers, and their scent allows them to live among wild insects without being attacked. Pollution invigorates them, but their resistance has limits: even the toxic clouds they release in self-defense can poison them. They have the strength for hard physical work, though they move slowly and struggle to learn. Their bodies painfully reject artificial parts, making it difficult to replace an injured limb.

Trogs descend from generations of interbreeding among neanderthals, wasters, and dirtmoles on some of the worst rimworlds humanity ever forgot. They make their homes amid toxic sludge and underground rot, and smell much like their surroundings. About half develop unexpected extra genes, adding to the bewildering variety of trog bodies and faces. Outsiders rarely understand how these communities survive, and are often dismayed to discover how stubbornly they endure.

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

Warcats are feline xenohumans bred for close combat. Their reflexes and killing instincts make them deadly with a blade or their own claws. They can push themselves into a feral rage, but are left aching and sluggish afterward. Their predatory metabolism requires regular meals of raw meat; without it, they sicken and eventually die. The same aggression that makes them fearsome fighters also makes them volatile companions, quick to turn a disagreement into violence.

Warcats were forged in military gene-vats by splicing humans with great cats, producing graceful predators packed with fast-twitch muscle. Something in the splice failed, leaving them dependent on raw meat. Their descendants now roam the rimworlds in tight-knit hunting clans, sharing the spoils of anything they can bring down, including other xenohumans. To many warcat hunters, a stranger carrying a weapon is simply more dangerous game.

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

Scaleborn are heavily scaled carnivores built to overpower large prey. Their natural armor and vicious close-range attacks make them dangerous even without equipment, while different lineages possess weapons such as fiery breath or blinding oil. They gain more nourishment from raw meat than other humans do, but digest plants poorly. For all their strength, scaleborn are sluggish and temperamental. They are ill-suited to prolonged pursuit, and cold can send them into torpor.

Scaleborn began with a fashion craze for recreating the mythical dragons of ancient Earth through genetic modification. Their descendants now live as apex predators on the dinosaur world Tyrantis V, hunting its megafauna for food and materials. Hunting clans trace their ancestry through scale color and inherited weapons. The remains of a great kill can feed a clan, furnish its dwellings, and provide trophies that keep the story of the hunt alive for generations.

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

Designed as beautiful companions, succuboids are winged xenohumans whose charm makes them adept at winning others over. Their intimate partners experience a lasting euphoria that can become addictive. Succuboids are exclusively female and bear daughters of their own kind, even with partners of another xenotype. They sleep long hours and work slowly, and their delicate bodies are poorly suited to combat or hard labor.

Succuboids were originally engineered as fashionable companions for glitterworld elites. At some point they took control of their own reproduction, modifying themselves so they could bear succuboid daughters regardless of their partner's xenotype. Today, some societies regard them as prestigious status symbols or valuable commodities; others see them as dangerous social parasites. The same family might be welcomed at one world's courts and forbidden to settle on the next.

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

Zeegees are adapted to life in spacecraft and enclosed habitats. They learn quickly and have a natural understanding of advanced technology, while their keen distance vision makes them effective marksmen. Their bodies store reserves that briefly protect them from hazards such as vacuum and extreme temperatures. These reserves do little for the ordinary hardships of planetary life: zeegees are prone to nausea on the surface, vulnerable to disease, and easily injured.

Zeegees descend from the inhabitants of deep-space habitats and ancient generation ships. After generations of life in space, they find planetary gravity an uncomfortable burden. In their oldest communities, a person's home is identified by vessel and deck rather than world and nation. Some families have crossed interstellar distances without a single member ever standing beneath an open sky. They see little reason to start.

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
