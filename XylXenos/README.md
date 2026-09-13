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

Engineered as sentient livestock, bossaps are sturdy plant-eaters whose mostly female herds produce abundant milk. They are gentle and happiest in a large herd, but pain can turn that docility into a bull's blind fury. An enraged bossaps is beyond command, charging with lethal horns at whoever caused the hurt, even a companion whose shot went astray.

The gene-artisans who made bossaps served a glitterworld best left forgotten. Their creations have since spread far beyond its pastures, though many are still bought and sold. Free bossaps favor large communal households, sharing meals, childcare, and sleeping space. They have little use for the privacy other xenohumans prize, and often find a crowded household more comforting than a room of their own.

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

Chyrr are nocturnal, flying mystics native to steaming jungles and sun-blasted deserts. They find their way through darkness by sound, defending their fragile bodies with a keening cry that leaves living foes reeling while they take wing. Even a cool night can draw them into hibernation. Their bodies slow to conserve energy and endure the cold, but they remain helpless until warmth rouses them.

Chyrr communities gather in high roosts, where calls carry news between households after sunset. They put great trust in what can be heard and little in appearances; a familiar voice can be more reassuring than a friendly face. Their mystics teach that patient listening reveals faint psychic impressions and a hazy thread of precognition. The same tradition guides their healers, who listen for illness in a patient's breathing as carefully as a seer listens for omens.

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

Dvergr hail from craggy, mineral-rich worlds where most things worth doing happen underground. Short and stoic, they would rather break rock than make small talk, and their industry serves them well wherever there are halls to build or workshops to fill. They can live on raw fungus despite hating the taste, but cannot live without a steady supply of alcohol. Beneath their stubborn resilience lies a melancholy that sometimes breaks through, leaving them shaken but newly inspired to create.

Their homeworld, Svartalfheim, is honeycombed with vast underground cities powered by magma upwellings. Workshops and breweries are the centers of community life. Among dvergr, fine words count for less than work that holds up, and a craftsperson's reputation may last as long as the things they made. Smiles are rare, idle hands rarer.

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

Made for ocean worlds, nixies are beautiful in form, fluid in movement, and graceful in speech. An innate psychic pull makes their company difficult to resist, even when they make no effort to charm. Protected by tough scales, they fight best in water, slipping away from pursuers and wearing them down from a distance. They need water for comfort as well as safety, and long periods without soaking leave them miserable.

Originally engineered for the ocean world Atlantis, nixies now live on many planets. On the rim they gather in scattered river clans and coastal camps. Long association with particular waterways has given many clans a place in local folklore, half-wild and half-divine. Reverence rarely amounts to understanding: a visiting clan may find offerings left at its landing place, even by people who refuse to trade with it.

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

Titans are stony giants mass-produced to reclaim deathworlds. Their strength and armored bodies let them labor in poisonous conditions that would kill ordinary workers. Their mineralized flesh can develop petrification, an incurable disease that slowly turns living tissue to stone. Each titan has an extraordinary aptitude for a particular task and needs no recreation, but learns painfully slowly at anything that does not engage their passions.

Reclamation companies grow titan work crews in vats, each batch assigned to a different stage of making a hostile world habitable. Their work has raised settlements in burning deserts, frozen tundras, and poisonous wastelands, but little thought is given to their lives once a project is complete. In abandoned industrial settlements, some still maintain walls and machines for employers who vanished generations ago. Storms howl, companies collapse, and the titans keep working.

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

Trogs are the lurching result of generations of interbreeding between neanderthals, wasters, and dirtmoles on the worst rimworlds humanity ever forgot. Their ancestry still produces surprises: about half develop unexpected extra genes. Slow, ugly, and foul-smelling, they are nevertheless capable settlers with a knack for mining and handling animals. Wild insects leave them alone, and pollution invigorates them, though even the gas they release to repel enemies can poison them too.

Trog tribes raise families amid toxic sludge and underground rot, making their homes in derelict mines and along the margins of insect hives. What outsiders see as a wasteland offers them shelter, ore, and insects to tame. A tribe's fortunes depend on making use of whatever survives there, including the strange talents of its own members. These insular communities refuse peaceful dealings with outsiders, and neighboring settlements know them chiefly as raiders.

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

Warcats were forged in military gene-vats: part human, part great cat, all fast-twitch muscle. They fight savagely at close quarters, but something in the splice tied their survival to a hunger cooked meals cannot satisfy. Without regular raw meat from hunts or livestock, they can lose control of themselves, then sicken and die. Even well fed, their killing instincts lie close to the surface, and a disagreement can quickly turn violent.

Warcats now roam the rimworlds in tight-knit hunting clans, pursuing anything they can bring down, including other xenohumans. On some worlds they have been part of the ecosystem for thousands of years. A clan lives by its hunters' success, and sharing a kill is as much an obligation as a reward. Hunting companions often remain close for life, but such loyalty carries no promise of mercy toward outsiders.

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

Scaleborn were made in the image of the mythical dragons of ancient Earth. Armored in scales and armed with vicious claws, they hunt large prey with the aid of inherited weapons such as fiery breath or blinding oil. Raw meat sustains them far better than plants. Slow to stir and quick to anger, they spend much of their time resting between hunts; cold can send them into hibernation.

What began as a fashion craze for genetic modification has become a way of life on Tyrantis V. There, scaleborn live as the top predators of a dino world, hunting its megafauna for food and materials. Clans trace their ancestry through inherited colors and weapons, taking pride in lineages whose original designers are long forgotten. A great kill can feed a settlement and furnish its dwellings, with trophies preserving the hunters' names long after the meat is gone.

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

Engineered as luxuries for glitterworld elites, succuboids are beautiful, winged companions whose charms can become addictive. They win people over easily and leave their lovers in a lasting euphoria. All are female, bearing daughters of their own kind even with partners of other xenotypes. Their makers expected others to provide for them: long hours asleep and slow work while awake leave a lone succuboid barely able to feed herself.

Succuboids eventually took control of their own reproduction, altering themselves so their daughters would inherit their full xenotype. The change allowed them to establish families far beyond the estates they were made to adorn. Today they are viewed variously as prestigious status symbols, valuable commodities, or dangerous social parasites. Their lovers can find it difficult to distinguish affection from addiction, and succuboids have no easier way to tell.

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

Zeegees descend from the inhabitants of deep-space habitats and ancient generation ships. They learn quickly and have an intuitive grasp of the machines that keep a ship alive. Their bodies can weather a brief loss of pressure, buying time to reach safety when a hull is breached. Planetary life is less forgiving: their fragile bodies are easily injured, and gravity leaves them nauseous and unsteady.

Some zeegee families have crossed interstellar distances without a single member setting foot on a planet. They name their homes by vessel and deck, and reckon the years from a ship's departure rather than the founding of a distant nation. Open sky can feel dangerously exposed to people used to a world bounded by bulkheads. Even those born planetside may inherit the conviction that their real home is somewhere among the stars.

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
