# Xyl's Xenotypes

Eleven xenotypes for RimWorld, from fungus-fed underground builders to vat-grown stone giants, aquatic traders, and descendants of generation-ship crews. Each brings its own needs, strengths, and complications to colony life.

**Requires RimWorld 1.7 and Biotech.** Royalty adds innate psycasts; Ideology and Odyssey provide additional features where noted.

- [Bossaps](#--bossaps)
- [Chyrr](#--chyrr)
- [Dvergr](#--dvergr)
- [Nixie](#--nixie)
- [Titan](#--titan)
- [Trog](#--trog)
- [Warcat](#--warcat)
- [Zeegee](#--zeegee)
- [Omegabeaver](#--omegabeaver)
- [Scaleborn](#--scaleborn)
- [Succuboid](#--succuboid)
- [Currently unused genes](#currently-unused-genes)

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylBossaps.png" alt="Bossaps" width="48" height="48"> <br> Bossaps

Bossaps were created as sentient livestock by the gene-engineers of a glitterworld best forgotten. They gain extra nourishment from raw plants, and their mostly female herds produce abundant milk. Naturally gregarious, they become unhappy in small colonies but tolerate shared barracks. While usually docile, injury can send a bossaps charging horns-first in an uncontrollable rage - even at the friend whose shot went astray.

The glitterworld that created bossaps had a strict vegan philosophy that it was unethical to keep animals that couldn’t consent to their own treatment. Their solution was to engineer livestock that not only consented to being owned, but did so enthusiastically. Long after the fall of their birthplace, wild bossaps herds can be found on many rimworlds.

### Trivia

"Bossaps" is a contraction of "Bos sapiens", a fictional scientific name that means "Wise cow".

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHerbivoreStomach.png" alt="Herbivore stomach" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 1 | [Herbivore stomach](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Diet.xml) | Carriers of this gene have specialized stomachs that can extract more nutrition from raw plant-based foods, but are poor at digesting meat. They never get negative thoughts from eating plant-based foods. <br><sub>**Raw vegetable nutrition multiplier** x180% &middot; **Raw meat nutrition multiplier** x50% &middot; **Cooked meat nutrition multiplier** x80% &middot; **Raw vegetable food poisoning chance** x0%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHerdInstinct.png" alt="Herd instinct" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Herd instinct](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Mood.xml) | Carriers of this gene need the comfort of being in a large group. They get a mood penalty if there are too few members of the colony. They also don't mind sleeping in a barracks. <br><sub>**Suppresses trait:** Recluse &middot; **Herd instinct alone:** -12 mood</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylDocile.png" alt="Docile" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Docile](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene are generally docile. They are easy to enslave and rarely rebel. They also never start social fights. <br><sub>**Global will loss factor** x400% &middot; **Slave suppression fall rate** x0.25 &middot; **Will never start or join a slave rebellion while mood is at least 50.** &middot; **Will never do social fights.** &middot; **Will never prison break.**</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSeeingRed.png" alt="Seeing red" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 4 | [Seeing red](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene have a chance of going into a blind range when they are damaged in combat, charging the nearest enemy and fighting in melee until all enemies are dead. During this state they are faster, stronger, and nearly immune to pain. This ability can't be controlled. <br><sub>**Rage chance** 15%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylLargeHorns.png" alt="Large horns" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Large horns](../XylXenos.Data/Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Carriers of this gene have large horns that function as a weapon. <br><sub>**Melee damage per second** 6.29 &middot; **Melee armor penetration** 33%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylPainReversal.png" alt="Pain reversal" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Pain reversal](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene have scrambled senses that make them feel pain as pleasurable. <br><sub>**Forced traits:** Masochist</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHyperlactation.png" alt="Hyperlactation" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Hyperlactation](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hyperlactation.xml) | Female carriers of this gene produce large amounts of breast milk, and lactate even when not pregnant or breastfeeding. This gene is inactive on males. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylGender_UsuallyFemale.png" alt="Usually female" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Usually female](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | Carriers of this gene are mostly female.<br><br>This gene has no effect unless it is a germline gene. |

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEars_Cow.png" alt="Cow ears" width="80" height="80"> | [Cow ears](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |

**Other genes and appearance**

- **Traits:** cold tolerant, heat tolerant, robust, fertile, nearsighted.
- **Skills:** poor cooking, strong plants.
- **Appearance:** long-haired, standard body, fat body, hulk body.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylChyrr.png" alt="Chyrr" width="48" height="48"> <br> Chyrr

Chyrr are batlike xenohumans who trace their origin to a legendary blind healer. They have a strong aptitude for medicine, and their echolocation and keen hearing let them “see” using sound as well as vision. Their bodies are easily injured, but they can stun living enemies with a cry of ultrasound and psychic energy, then take flight on membranous wings for a short escape. In colder weather, they naturally enter a state of hibernation, leaving them protected from hypothermia and frostbite but otherwise helpless.

According to chyrr myth, a blind woman named Marah came across an injured stranger. She brought the stranger back to her tribe and spent many moons nursing him back to health. As a parting gift, the stranger transformed Marah and her tribe into chyrr.

### Trivia

"Chyrr" is a corruption of "chiropteran", the scientific name for bats, which comes from the roots *chiro* "hand" and *ptera* "wing".

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSonicWave.png" alt="Sonic wave" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Sonic wave](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Abilities.xml) | Carriers of this gene are able to release a wave of high-intensity ultrasonic noise and psychic energy at a target. The noise will stun any non-mechanoid creature nearby. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylBatWings.png" alt="Bat wings" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Bat wings](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene have bat-like wings, allowing them to fly for short periods. Wearing some heavy torso-covering items will prevent flight. <br><sub>**Max flight time** 8s &middot; **Flight cooldown** 45s &middot; **Manipulation** -10%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTorpor.png" alt="Torpor" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Torpor](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | The body of a carrier of this gene automatically goes into hibernation in cold temperatures. This will impair their capabilities and eventually cause unconsciousness. It also reduces food consumption and slows the progression of hypothermia and starvation. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylNocturnal.png" alt="Nocturnal" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Nocturnal](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene are naturally active at night. <br><sub>**Forced traits:** Night owl</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEcholocation.png" alt="Echolocation" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Echolocation](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Senses.xml) | Carriers of this gene can use high-frequency sound waves to "see" with their ears instead of their eyes. <br><sub>**Sight:** Use hearing if higher &middot; **New recipes:** Make blindfold</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylKeenEars.png" alt="Keen ears" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Keen ears](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Senses.xml) | Carriers of this gene have improved hearing. <br><sub>**Hearing** x120%</sub>|

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEars_SmallPointed.png" alt="Small pointed ears" width="80" height="80"> | [Small pointed ears](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |

**Other genes and appearance**

- **Traits:** weak immunity, cold weakness, heat tolerant, mild UV sensitivity, delicate, dark vision.
- **Skills:** poor mining, strong medical.
- **Appearance:** standard body, thin body, beardless.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylDvergr.png" alt="Dvergr" width="48" height="48"> <br> Dvergr

Dvergr hail from the vast underground cities beneath the inhospitable surface of the mineral world Svartalfheim. They see in darkness, tolerate great heat, and never need to go outdoors. They are fast workers and skilled miners, builders, and craftspeople. Their stomachs extract extra nourishment from raw fungus, although they hate the taste. They are both culturally and metabolically dependent on regular alcohol consumption, and going without it leads to illness and eventually death.

Dvergr society is organized around the clan and the corporation; the two are one and the same. They are renowned for their skill with explosives, deep understanding of geology, excellent legal departments, and remarkably short safety manuals.

### Faction: Rough dvergr union

A union of industrial mining settlements led by dvergr clan corporations. Their people trace their origins to the underground cities of Svartalfheim, and build their lives around mining, manufacturing, and the interests of the clan. Their mountain halls are filled with workshops, breweries, and machinery.

They defend their claims with firearms and explosives, applying their knowledge of excavation as readily to an enemy's walls as to a mineral deposit. Competition over resources can turn violent, and their enthusiasm for blasting often exceeds their concern for safety.

This particular union is unfriendly to outsiders, but can be persuaded to trade and form alliances.

### Trivia

"Dvergr" is an Old Norse word for dwarf, and their homeland in Old Norse mythology was either called "Svartálfheim" or "Niðavellir" depending on the source.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylFungusEater.png" alt="Fungus eater" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Fungus eater](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Diet.xml) | Carriers of this gene have special digestive enzymes that extract more nutrition from raw fungus - but they don't do anything about the taste. <br><sub>**Raw fungus nutrition multiplier** 180% &middot; **New buildables:** Fungal gravel &middot; **New recipes:** Make wort from fungus</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylStoic.png" alt="Stoic" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Stoic](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene are mentally tough and won't break down under stresses that would crack most people. <br><sub>**Forced traits:** Steadfast</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylMelancholy.png" alt="Melancholy" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 3 | [Melancholy](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene always have the tortured artist trait, which gives them a permanent mood penalty but also grants a chance to gain creative inspiration after a mental break. <br><sub>**Forced traits:** Tortured artist</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylDwarf.png" alt="Dwarf" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Dwarf](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Body.xml) | Carriers of this gene have smaller bodies with proportionately larger heads. Their short legs somewhat reduce their movement speed. <br><sub>**Move speed** -0.10 &middot; **Body size** x90%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylIndustrious.png" alt="Industrious" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Industrious](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene are exceptionally fast workers. <br><sub>**Forced traits:** Hard worker</sub> |

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEars_SmallPointed.png" alt="Small pointed ears" width="80" height="80"> | [Small pointed ears](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHair_BaldOnly_Male.png" alt="Bald males" width="80" height="80"> | [Bald males](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hair.xml) |

**Other genes and appearance**

- **Traits:** psychically dull, heat super-tolerant, aggressive, strong stomach, dark vision, indoor dweller, unstoppable, alcohol dependency.
- **Skills:** strong construction, strong mining, strong crafting, poor medical, poor social.
- **Appearance:** short-haired, only bushy beards, light gray skin, slate gray skin, ink black skin.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylNixie.png" alt="Nixie" width="48" height="48"> <br> Nixie

Originally engineered for the water world of Atlantis, nixies have spread across inhabited space. Their scaled skin provides protection and webbed hands let them move quickly in water, but long periods outside it will leave them miserable with cracked, bleeding skin. Beyond their physical adaptations, they possess otherworldly beauty and innate psychic powers that draw others towards them. They are skilled negotiators but poor miners and builders, trading with “dryskins” for manufactured goods.

The planet Atlantis is entirely covered by a shallow, planet-wide sea. Without access to dry land, nixies were unable to develop advanced technology and industry. Instead, they put their efforts into developing art and culture, creating beautiful coral gardens and ethereal songs that echo for miles underwater. The source of their psychic abilities is a mystery.

### Faction: Gentle nixie tribe

A tribe of aquatic nixies descended from people engineered for the ocean world Atlantis. They settle along rivers and coasts, cultivating coral gardens and producing sea silk. Their culture values beauty, music, and a life spent in the water, with little use for clothing.

They use simple tools and weapons, relying on trade for machinery and manufactured goods they cannot easily make themselves. In combaat, tough scales and innate psychic abilities help compensate for their lack of advanced equipment.

This particular tribe pursues a gentle way of life. They are open to trade and alliances, even with dryskins.

### Trivia

"Nixie" comes from the name of mythical water spirits from Germanic folklore that used their beauty and music to lure unsuspecting people into the water.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylPsycast_Beckon.png" alt="Beckon" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Beckon](../XylXenos.Data/Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml) | The carrier automatically learns the beckon psycast, and can use it without a psylink.<br><br>Psychically command the target to approach the caster. <br><sub>**Enables psyfocus**</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylPsycast_Focus.png" alt="Focus" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Focus](../XylXenos.Data/Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml) | The carrier automatically learns the focus psycast, and can use it without a psylink.<br><br>Psychically focus the target's mind, boosting their sight, hearing and moving capacities. <br><sub>**Enables psyfocus**</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylDrugSensitive.png" alt="Drug sensitive" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Drug sensitive](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Drugs.xml) | Carriers of this gene are easily affected by drugs. Effects from a single dose last longer, they gain tolerance faster, and they are more likely to become addicted. <br><sub>**Addiction chance** x200% &middot; **Drug sensitivity** +40%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylScaleskin.png" alt="Scaleskin" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Scaleskin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Body.xml) | Carriers of this gene grow tough but flexible scales all over their body that can deflect or absorb attacks. <br><sub>**Armor - Sharp** +40.0% &middot; **Armor - Blunt** +15.0%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylAquatic.png" alt="Aquatic" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 3 | [Aquatic](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Needs.xml) | Carriers of this gene have a need to periodically soak in water, and will become unhappy if they stay dry for too long. <br><sub>**Min comfortable temperature** -5.0C &middot; **New buildables:** Shower &middot; **Adds need:** Wetness</sub>|

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEars_Fin.png" alt="Fin ears" width="80" height="80"> | [Fin ears](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |

**Other genes and appearance**

- **Traits:** psy-sensitive, naked speed, webbed phalanges, unhappy (Royalty), cold super-tolerant, heat weakness, weak melee damage, attractive.
- **Skills:** poor mining, poor construction, strong social.
- **Appearance:** no hair, snow-white hair, grayless hair, blue skin.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylTitan.png" alt="Titan" width="48" height="48"> <br> Titan

Titans are mass-produced workers originally designed for reclaiming deathworlds. Specialized carbon-silicate lithoid biochemistry makes them immune to toxins and highly resistant to injury and disease, letting them work in conditions that would be lethal for other xenotypes. Each is engineered with exceptional aptitude and passion for one random skill, but they have great difficulty learning skills that they aren’t passionate about. Their unusual biochemistry also leaves them susceptible to petrification, an incurable genetic disease that gradually turns their tissues to solid stone.

Today, titans can be found on more than just the deathworlds they were engineered for. They are employed in space construction, mining, and agriculture, among other fields. Their employers are mostly well-resourced governments or corporations that are able to afford the expense of providing them with a regular supply of lithoid drugs such as softener and atlasite.

### Trivia

"Titan" comes from the name of godlike beings from Greek mythology. One well-known titan was Atlas, who held up the sky on his shoulders.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylRockToss.png" alt="Rock toss" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Rock toss](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Abilities.xml) | The carrier can pick up a rock chunk in combat and toss it. It will land near a targeted location, damaging everything in a radius around where it lands. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylPetrification.png" alt="Petrification" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 4 | [Petrification](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Petrification.xml) | Carriers of this gene have a chance to develop a disease named petrification. Petrification slowly replaces tissues throughout the body with stone. The disease is incurable, but progress can be slowed with medical treatment, and the affected tissues can be removed surgically. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylJoyless.png" alt="Joyless" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 1 | [Joyless](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Needs.xml) | Carriers of this gene are genetically incapable of feeling joy. They have no need for recreation, and get no mood bonuses or penalties from it. <br><sub>**Disables need:** Recreation</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylLithoid.png" alt="Lithoid" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 1 | [Lithoid](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Drugs.xml) | Carriers of this gene have a unique biochemistry that incorporates carbon-silicates. They are completely unaffected by most drugs that work on baseliners, and instead must use specialized drugs designed for lithoids. <br><sub>**Suppressed traits:** Chemical need, chemical fascination</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylGiant.png" alt="Giant" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Giant](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Body.xml) | Carriers of this gene are exceptionally large. They can take more damage, carry more, and are more resistant to drugs and toxins, but are also easier to hit with ranged weapons. <br><sub>**Body size** x150% &middot; **Hit points** x130%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylRockskin.png" alt="Rockskin" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -4 | [Rockskin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Body.xml) | Carriers of this gene have extremely tough skin studded with rock-like plates that can deflect or absorb attacks. The inflexible skin reduces movement speed. <br><sub>**Armor - Sharp** +70.0% &middot; **Armor - Blunt** +40.0% &middot; **Armor - Heat** +50.0% &middot; **Move speed** -0.40 &middot; **Non-rockskin romance chance** x20%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylLearning_Focused.png" alt="Focused" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 1 | [Focused](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene care deeply about subjects they are interested in. They learn faster for skills they have a burning passion in, but much slower at skills they have no passion in. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylSuperSpecialist.png" alt="Super-specialist" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Super-specialist](../XylXenos.Data/Defs/GeneDefs/GeneDefs_BonusGenes.xml) | The carrier's aptitude in one random skill is increased by 8. Aptitude acts like an offset on skill level. Additionally, one level of passion is added to that skill. |

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Xeno/XylHair_BaldOnly_Male.png" alt="Bald males" width="80" height="80"> | [Bald males](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hair.xml) |

**Other genes and appearance**

- **Traits:** strong melee damage, super immunity, slow wound healing, superclotting, psychically deaf, slow runner, tox immunity, sterile, unattractive.
- **Skills:** poor social, poor intellectual.
- **Appearance:** beardless, hulk body.

**Lithoid drugs:** [Softener](../XylXenos.Data/Defs/Drugs/Drugs_Titan.xml) prevents petrification. Atlasite builds protective resistance with regular doses. Crystal improves performance and dulls pain, but is highly addictive.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylTrog.png" alt="Trog" width="48" height="48"> <br> Trog

Trogs emerged from generations of interbreeding among wasters, dirtmoles, and neanderthals on the worst rimworlds known to mankind. They are ugly, slow, and poor learners, but skilled miners and animal handlers. They emit special pheromones that let them move freely among giant insects, and even tame them and employ them as war animals. Like wasters, pollution invigorates them, but they are only partially resistant to its effects. Roughly half inherit additional random genes from who-knows-where.

Trogs can be found anywhere other xenotypes don’t want to live, from polluted wastelands to underground caverns to volcanic lava fields. They form aggressive, territorial clans that often squabble as much among themselves as they do with outsiders. Despite their unwholesome reputation, they can be quite welcoming to outcast and shunned people of any xenotype.

### Faction: Savage trog tribe

A collection of territorial clans led by trogs, descendants of interbreeding wasters, dirtmoles, and neanderthals. They inhabit polluted wastes, underground caverns, and volcanic badlands, making homes in places most other peoples avoid. Their settlements shelter mutants, fugitives, and outcasts of many xenotypes.

Their miners use simple tools, and their warriors carry bows and crude melee weapons. Their ability to tame giant insects makes up for much of their technological weakness: megaspiders and other war insects accompany their raiders into battle.

This particular tribe refuses peaceful relations with outsiders. You will not be able to trade or ally with them.

### Trivia

"Trog" is an abbreviation of "troglodyte", meaning a person who lives in a cave.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylToxicBurst.png" alt="Toxic burst" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Toxic burst](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Abilities.xml) | Carriers have the ability to release a cloud of tox gas around them from a special gland located near their anus. The gas affects the user normally. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylDrugResistant.png" alt="Drug resistant" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Drug resistant](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Drugs.xml) | Carriers of this gene are less affected by drugs. Effects from a single dose wear off faster, they gain tolerance slower, and they are less likely to become addicted. <br><sub>**Addiction chance** x70% &middot; **Drug sensitivity** -50%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylBioRejection.png" alt="Bio-rejection" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Bio-rejection](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene have a severe allergic reaction to any sort of artificial implant or body part. They will suffer continual pain until the implant or part is removed. <br><sub>**Suppressed traits:** Body modder</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylInsectPheromones.png" alt="Insect pheromones" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Insect pheromones](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene produce special pheromones which prevent wild insects from attacking them. This protection is briefly disabled if their faction provokes the insects. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylGeneticAtavism.png" alt="Genetic atavism" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Genetic atavism](../XylXenos.Data/Defs/GeneDefs/GeneDefs_BonusGenes.xml) | Carriers of this gene have an unstable genome that may have random extra genes. |

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHead_Warped.png" alt="Warped head" width="80" height="80"> | [Warped head](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_DarkGreen.png" alt="Dark green skin" width="80" height="80"> | [Dark green skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_Olive.png" alt="Olive skin" width="80" height="80"> | [Olive skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |

**Other genes and appearance**

- **Traits:** strong immunity, slow runner, partial antitoxic lungs, mild UV sensitivity, aggressive, strong melee damage, reduced pain, very unattractive, slow study, dark vision, pollution stimulus, mild cell instability.
- **Skills:** strong mining, strong animals, awful intellectual.
- **Appearance:** no hair, human headbone, mini-horns, green skin.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylWarcat.png" alt="Warcat" width="48" height="48"> <br> Warcat

Warcats were created in military gene-vats as experimental supersoldiers - part human, part great cat, all fast-twitch muscle. But something in the splice broke, leaving them dependent on a diet of raw meat to survive. Their descendants roam the rimworlds in packs, hunting with retractable claws, powerful melee attacks, and catlike reflexes.

Warcat tribes are a constant threat on the rim. Their need for meat drives them to hunt anything they can, even other xenohumans. Many warcat hunters carry the skulls of those they have eaten as trophies.

### Faction: Cannibal warcat tribe

A loose confederation of warcat hunting bands, descended from soldiers created in military gene-vats. Dependent on raw meat, they roam in search of prey and hunt other xenohumans as readily as animals. Successful hunters keep the skulls of eaten enemies as trophies.

They fight with bows and simple melee weapons, but remain dangerous at close quarters even when unarmed. Razor-sharp claws, powerful muscles, and catlike reflexes are the surviving legacy of their military origins.

This particular tribe is hostile to outsiders, but can be persuaded to trade and form alliances.

### Trivia

Real cats are unable to produce adequate amounts of certain essential nutrients such as taurine, and must get them from meat as part of their diet.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylCarnivoreStomach.png" alt="Carnivore stomach" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 1 | [Carnivore stomach](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Diet.xml) | Carriers of this gene have specialized stomachs that can extract more nutrition from raw meat, but are poor at digesting plant-based foods. They never get negative thoughts from eating meat. <br><sub>**Raw meat nutrition multiplier** x180% &middot; **Raw vegetable nutrition multiplier** x50% &middot; **Cooked vegetable nutrition multiplier** x80% &middot; **Raw meat food poisoning chance** x0%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylMeatDependence.png" alt="Meat dependence" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 3 | [Meat dependence](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Diet.xml) | Carriers of this gene are unable to synthesize certain essential nutrients and must obtain them by eating raw meat. Without it, their health and mood will steadily decline, leading to pain, muscle weakness, mental instability, coma, and eventually death. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylFeralRage.png" alt="Feral rage" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Feral rage](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Abilities.xml) | Carriers have the ability to enter a state of feral rage, giving increased movement speed and faster melee attacks. After the rage ends, there is a temporary backlash which causes pain and slows movement.. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylMoody.png" alt="Moody" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 1 | [Moody](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Mood.xml) | Carriers of this gene have a volatile emotional state. They randomly get good and bad moods. <br><sub>**Good mood** +12 mood &middot; **Bad mood** -12 mood</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylRetractableClaws.png" alt="Retractable claws" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Retractable claws](../XylXenos.Data/Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Carriers of this gene have retractable claws that function as weapons. <br><sub>**Melee damage per second** 7.08 &middot; **Melee armor penetration** 13%</sub>|
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylFastReflexes.png" alt="Fast reflexes" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Fast reflexes](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene have excellent reflexes that allow them to dodge melee attacks and evade traps. <br><sub>**Forced traits:** Nimble</sub> |

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEyes_Yellow.png" alt="Yellow eyes" width="80" height="80"> | [Yellow eyes](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylFacialStripes.png" alt="Facial stripes" width="80" height="80"> | [Facial stripes](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |

**Other genes and appearance**

- **Traits:** hyper-aggressive, strong melee damage, sleepy, high libido, dark vision.
- **Skills:** great melee, poor plants, poor crafting.
- **Appearance:** long-haired, beardless, cat ears, standard body, thin body.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylZeegee.png" alt="Zeegee" width="48" height="48"> <br> Zeegee

Zeegees descend from the inhabitants of deep-space habitats and ancient generation ships. They are comfortable indoors and in low gravity, but on planets they are prone to bouts of debilitating nausea. They learn quickly, excel at intellectual work, and have a natural connection to machines that makes them adept at controlling mechanoids and piloting gravships. In emergency situations oxygen and protective proteins stored in their bone marrow can briefly protect them against vacuum, toxins, and temperature extremes. Enhanced vision, adapted for the low light and vast distances of space, makes them excellent marksmen.

Even the most colossal spaceborn habitat can eventually break down due to centuries of accidents, neglect, or conflict. The former inhabitants of those lost paradises are now scattered across the galaxy. Many find their homes in spacer guilds and salvager gangs, but even more have settled on planets where they put their unique biological traits to use despite the gravity.

### Trivia

"Zeegee" is a phonetic spelling of the initial letters of "zero gravity".

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylPlanetSickness.png" alt="Planet sickness" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 3 | [Planet sickness](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene are prone to bouts of nausea and vomiting when on a planet's surface. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEmergencyReserves.png" alt="Emergency reserves" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Emergency reserves](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene store extra oxygen and special proteins in their bone marrow. When the carrier is exposed to extreme heat, cold, toxins, or vacuum, these reserves temporarily flood the body, protecting tissues from damage. <br><sub>**Duration:** 1 day &middot; **Recharge time:** 4 days</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTelescopicVision.png" alt="Telescopic vision" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Telescopic vision](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene can see exceptionally well at a distance. Their shooting accuracy at long ranges is increased. <br><sub>**Accuracy factor (long)** x200% &middot; **Accuracy factor (medium)** x150%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTechAffinity.png" alt="Tech affinity" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Tech affinity](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene are naturally adept with advanced technology. <br><sub>**Mech bandwidth** +3 &middot; **Piloting ability** +30%</sub> |

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylForeheadMark.png" alt="Forehead mark" width="80" height="80"> | [Forehead mark](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Cosmetic.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_DarkSilver.png" alt="Dark silver skin" width="80" height="80"> | [Dark silver skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_LightSilver.png" alt="Light silver skin" width="80" height="80"> | [Light silver skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |

**Other genes and appearance**

- **Traits:** weak immunity, weak melee damage, delicate, low gravity adapted (Odyssey), quick study, dark vision, indoor dweller.
- **Skills:** strong shooting, poor melee, poor animals, strong intellectual.
- **Appearance:** short-haired.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylOmegabeaver.png" alt="Omegabeaver" width="48" height="48"> <br> Omegabeaver

Omegabeavers evolved from alphabeavers left behind on a planet abandoned by humans due to rampant pollution. These small, industrious builders excel at construction and logging, using strong, chisel-shaped teeth to cut trees quickly. Their webbed fingers let them move effortlessly in water. Naturally kind and optimistic, they are poor fighters despite their powerful bites, and learn new skills slowly.

On their home planet, omegabeavers build enormous reservoirs to store clean water and channels to divert polluted water, maintaining oases of green among the cracked and rotting landscape.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTail_Beaver.png" alt="Beaver tail" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Beaver tail](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Tails.xml) | Carriers of this gene grow a broad, flat tail resembling that of an alphabeaver that helps with temperature regulation. <br><sub>**Max comfortable temperature** +5.0C &middot; **Min comfortable temperature** -5.0C</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylDwarf.png" alt="Dwarf" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Dwarf](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Body.xml) | Carriers of this gene have smaller bodies with proportionately larger heads. Their short legs somewhat reduce their movement speed. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylIndustrious.png" alt="Industrious" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Industrious](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene are exceptionally fast workers. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylNose_Beaver.png" alt="Beaver teeth" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Beaver teeth](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene have strong, chisel-shaped teeth and powerful jaw muscles. They cut trees twice as fast, and their bites deal twice as much damage. <br><sub>**Tree cutting speed** x200% &middot; **Bite damage** x200%</sub> |

**Other genes and appearance**

- **Traits:** psychically deaf, webbed phalanges, happy, slow wound healing, weak melee damage, kind instinct, fertile, furskin, slow study, nearsighted.
- **Skills:** great construction, strong cooking, strong plants, poor animals, poor intellectual.
- **Appearance:** no hair, beardless.

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylScaleborn.png" alt="Scaleborn" width="48" height="48"> <br> Scaleborn

Scaleborn were fashioned after the mythical dragons of ancient Earth. Their tough scales, claws, and sharp horns make them ferocious fighters, and each lineage has unique abilities such as fiery breath or blinding oil. Despite their strength, however, their reptilian metabolism makes them slow runners, and they become even slower in the cold.

On the dino-world Tyrantis V, scaleborn are the dominant predators, pursuing the megafauna in ritual hunts. Successful hunters will share the meat with not only their own tribe but their neighbors, hosting great feasts and displaying the bones of their prey as trophies.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylCarnivoreStomach.png" alt="Carnivore stomach" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 1 | [Carnivore stomach](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Diet.xml) | Carriers of this gene have specialized stomachs that can extract more nutrition from raw meat, but are poor at digesting plant-based foods. They never get negative thoughts from eating meat. <br><sub>**Raw meat nutrition multiplier** x180% &middot; **Raw vegetable nutrition multiplier** x50% &middot; **Cooked vegetable nutrition multiplier** x80% &middot; **Raw meat food poisoning chance** x0%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylGreedy.png" alt="Greedy" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Greedy](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene have an instinctive desire for material possessions. They get a mood penalty if they don't have an impressive bedroom. <br><sub>**Forced traits:** Greedy</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTorpor.png" alt="Torpor" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Torpor](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | The body of a carrier of this gene automatically goes into hibernation in cold temperatures. This will impair their capabilities and eventually cause unconsciousness. It also reduces food consumption and slows the progression of hypothermia and starvation. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylLargeHorns.png" alt="Large horns" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Large horns](../XylXenos.Data/Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Carriers of this gene have large horns that function as a weapon. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylRetractableClaws.png" alt="Retractable claws" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Retractable claws](../XylXenos.Data/Defs/GeneDefs/GeneDefs_InnateWeapons.xml) | Carriers of this gene have retractable claws that function as weapons. <br><sub>**Melee damage per second** 7.08 &middot; **Melee armor penetration** 13%</sub>|
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylLineageInheritance.png" alt="Lineage inheritance" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Lineage inheritance](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | When a carrier of this gene has a baby with a partner who also has this gene, the baby will inherit all of one parent's endogenes and none of the other's. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTail_Lizard.png" alt="Lizard tail" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Lizard tail](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Tails.xml) | Carriers of this gene grow a long, heavy tail that improves balance and mobility. <br><sub>**Move speed** +0.10</sub>  |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylScaleskin.png" alt="Scaleskin" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Scaleskin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Body.xml) | Carriers of this gene grow tough but flexible scales all over their body that can deflect or absorb attacks. <br><sub>**Armor - Sharp** +40.0% &middot; **Armor - Blunt** +15.0%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylScalebornLineage.png" alt="Scaleborn lineage" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 5 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Scaleborn lineage](../XylXenos.Data/Defs/GeneDefs/GeneDefs_BonusGenes.xml) | Replaces itself with one of five randomly selected gene packages representing five different lineages. |

**Other genes and appearance**

- **Traits:** psychically dull, slow runner, cold tolerant, heat tolerant, aggressive, strong melee damage, sleepy.
- **Skills:** awful cooking.
- **Appearance:** no hair, beardless, facial ridges.

### Lineages

Each scaleborn receives one additional gene package from the list below. Scaleborn children always inherit the complete gene package of one of their parents.

| Lineage | Genes |
| --- | --- |
| Red | Fire spew, fire resistance, poor cooking (replaces awful cooking), deep red skin |
| Green | Acid spray, partial toxic resistance, good shooting, dark green skin |
| White | Foam spray, fast wound healing, good medical, sheer white skin |
| Blue | EMP burst, unstoppable, good intellectual, dark blue skin |
| Black | Oil spray, robust, good melee, slate gray skin |

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEMPBurst.png" alt="EMP burst" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [EMP burst](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Abilities.xml) | Carriers have a specialized muscle-derived organ that can release an electric shock in an area around them, disabling nearby electronic devices. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylOilSpray.png" alt="Oil spray" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Oil spray](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Abilities.xml) | Carriers have glands in their neck that can be used to spray a sticky oil that blinds opponents and leaves flammable puddles. |

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_DarkGreen.png" alt="Dark green skin" width="80" height="80"> | [Dark green skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_DarkBlue.png" alt="Dark blue skin" width="80" height="80"> | [Dark blue skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |

## <img src="../XylXenos.Data/Docs/Images/Xenotypes/XylSuccuboid.png" alt="Succuboid" width="48" height="48"> <br> Succuboid

Succuboids were created as fashionable companions for glitterworld elite, with distinctive pink skin, horns, wings, and tails. They are beautiful and charming, and intimacy gives their partners a lasting, addictive euphoria. Manual labor was never a consideration of their design, and their inherent laziness and increased need for sleep make them poor workers.

While their origin is very similar to that of highmates, succuboids have one important difference: at some point, they took control of their own reproduction, engineering themselves with the ability to reproduce with other xenotypes and invariably produce succuboid daughters. On some worlds they are considered prestigious status symbols, on others they are valuable merchandise, and on yet others they are treated as dangerous social parasites.

### Trivia

"Succuboid" is a homage to succubi, mythological female demons that tempted men in their sleep.

The succuboid mode of reproduction resembles [gynogenesis](https://en.wikipedia.org/wiki/Gynogenesis), a real world trait found in some fish and amphibians where eggs develop into a clone of the mother but only after being activated by sperm from a male of a closely related species.

### Genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylPsycast_WordOfLove.png" alt="Word of Love" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Word of Love](../XylXenos.Data/Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml) | The carrier automatically learns the word of love psycast, and can use it without a psylink.<br><br>Speak about someone's romantic virtues while using psychic suggestion to implant romantic desire in the listener. For days afterward, the listener will feel psychically-induced romantic attraction towards the other person. This greatly increases opinion and makes them much more likely to attempt romantic advances and marriage proposals if they get the chance. This power can be used to connect two other people, induce love for the caster, or force oneself to love another. <br><sub>**Enables psyfocus**</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylBatWings.png" alt="Bat wings" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -3 | [Bat wings](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene have bat-like wings, allowing them to fly for short periods. Wearing some heavy torso-covering items will prevent flight. <br><sub>**Max flight time** 8s &middot; **Flight cooldown** 45s &middot; **Manipulation** -10%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylShameless.png" alt="Shameless" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Shameless](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene are never ashamed by nudity, and are much happier when not wearing clothes. <br><sub>**Forced traits:** Nudist</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylGender_AlwaysFemale.png" alt="Always female" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Always female](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | Carriers of this gene are always female.<br><br>This gene has no effect unless it is a germline gene. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylStrongGenes.png" alt="Strong genes" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Strong genes](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | When a carrier of this gene has a baby with a partner without this gene, the baby will inherit all of the carrier's endogenes and none of the partner's. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylLoveEuphoria.png" alt="Love euphoria" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Love euphoria](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | Carriers of this gene secrete chemicals that cause a euphoric, drug-like high in their partner after lovin'. The chemicals grant a mood bonus but rewire the partner's brain causing an immediate and long-lasting addiction. <br><sub>**Partner mood:** +14 &middot; **High duration per dose:** 1.0 days &middot; **Addiction chance:** 100%<sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylFlirty.png" alt="Flirty" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Flirty](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene are more likely to attempt romantic interactions. <br><sub>**Forced traits:** Flirt</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTail_Pointed.png" alt="Pointed tail" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Pointed tail](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Tails.xml) | Carriers of this gene grow a narrow, pointed tail that increases the character's impact in social interactions. <br><sub>**Social impact** +40.0%</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylLazy.png" alt="Lazy" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> 2 | [Lazy](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene are slow workers. <br><sub>**Forced traits:** Lazy</sub>|

**Appearance genes**

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_Pink.png" alt="Pink skin" width="80" height="80"> | [Pink skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |

**Other genes and appearance**

- **Traits:** psy-sensitive, naked speed, weak melee damage, very sleepy, delicate, high libido, very attractive.
- **Skills:** strong social.
- **Appearance:** long-haired, mini-horns, standard body, pink hair, purple hair, grayless hair.

## Currently unused genes

These genes are available for custom xenotypes but are not part of the mod's xenotypes or their lineage packages.

### Functional genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Biostats | Gene | Effect |
| --- | --- | --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylEvenTemper.png" alt="Even temper" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Even temper](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene have very stable neurochemistry, with less variation between individuals. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylGender_AlwaysMale.png" alt="Always male" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Always male](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | Carriers of this gene are always male.<br><br>This gene has no effect unless it is a germline gene. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylGender_UsuallyMale.png" alt="Usually male" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Usually male](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | Carriers of this gene are mostly male.<br><br>This gene has no effect unless it is a germline gene. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylNoEmpathy.png" alt="No empathy" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [No empathy](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Traits.xml) | Carriers of this gene lack empathy. They aren't affected by the suffering of others. <br><sub>**Forced traits:** Psychopath</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylParthenogenetic.png" alt="Parthenogenetic" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Parthenogenetic](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | Female carriers of this gene can become spontaneously pregnant without a father. The chance of becoming pregnant this way isn't affected by genetic fertility factors. <br><sub>**Average interval:** 120 days</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylPrecognition.png" alt="Precognition" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -2 | [Precognition](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene can see a short distance into the future, giving the ability to dodge both melee and ranged attacks. The effect scales with psychic sensitivity. <br><sub>**Melee dodge chance** +15 &middot; **Ranged dodge chance** +15</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSpecialist.png" alt="Specialist" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 2 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Specialist](../XylXenos.Data/Defs/GeneDefs/GeneDefs_BonusGenes.xml) | The carrier's aptitude in one random skill is increased by 4. Aptitude acts like an offset on skill level. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylTail_Squirrel.png" alt="Squirrel tail" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 <br> <img src="../XylXenos.Data/Docs/Images/Metabolism.png" alt="Metabolism" width="24" height="24" align="absmiddle"> -1 | [Squirrel tail](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Tails.xml) | Carriers of this gene grow a long, fluffy tail that keeps them warm and increases the opinion of other characters towards them. <br><sub>**Min comfortable temperature** -5.0C &middot; **Opinion** +8</sub> |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylWeakGenes.png" alt="Weak genes" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 1 | [Weak genes](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Reproduction.xml) | When a carrier of this gene has a baby with a partner without this gene, the baby will inherit all of the partner's endogenes and none of the carrier's. |
| <img src="../XylXenos.Data/Docs/Images/Genes/Arch/XylWoundHealing_UltraFast.png" alt="Ultra-fast wound healing" width="80" height="80"> | <img src="../XylXenos.Data/Docs/Images/Complexity.png" alt="Complexity" width="24" height="24" align="absmiddle"> 4 <br> <img src="../XylXenos.Data/Docs/Images/ArchiteCapsuleRequired.png" alt="Archite capsules" width="24" height="24" align="absmiddle"> 2 | [Ultra-fast wound healing](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Miscellaneous.xml) | Carriers of this gene heal wounds unnaturally quickly, often recovering from even the most serious injuries in mere hours. It won't fix permanent scars or blood loss. |

### Appearance genes

| &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Icon&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Gene |
| --- | --- |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHair_BaldOnly_Female.png" alt="Bald females" width="80" height="80"> | [Bald females](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hair.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHair_ShortOnly_Female.png" alt="Short-haired females" width="80" height="80"> | [Short-haired females](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hair.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHair_LongOnly_Female.png" alt="Long-haired females" width="80" height="80"> | [Long-haired females](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hair.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHair_ShortOnly_Male.png" alt="Short-haired males" width="80" height="80"> | [Short-haired males](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hair.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylHair_LongOnly_Male.png" alt="Long-haired males" width="80" height="80"> | [Long-haired males](../XylXenos.Data/Defs/GeneDefs/GeneDefs_Hair.xml) |
| <img src="../XylXenos.Data/Docs/Images/Genes/Endo/XylSkin_DarkPurple.png" alt="Dark purple skin" width="80" height="80"> | [Dark purple skin](../XylXenos.Data/Defs/GeneDefs/GeneDefs_SkinColors.xml) |

Additional psycast genes are generated from the available abilities. The xenotypes above use Beckon, Focus, and Word of Love.
