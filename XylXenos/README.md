# Xyl's Xenotypes

This directory defines **ten new xenotypes**: bossaps, chyrr, dvergr, nixie, titan, trog, warcat, succuboid, scaleborn, and zeegee. All are marked inheritable except titans.

This reference summarizes the XML data in this mod. Each entry lists every gene referenced by its xenotype definition, including cosmetic entries and conditional genes. The lists describe the definition's gene pool; multiple body shapes, colors, or other mutually exclusive appearance entries need not all be active on one individual. Random additions are described separately. Existing game genes are named but not given a full effects reference; tables explain the new non-cosmetic genes supplied by this mod. Definition IDs are included for exact lookup. Human-readable names for existing genes are descriptive rather than a claim about the game's current localization.

Lore summaries combine xenotype and faction descriptions, with relevant gene, scenario, tip, and animal text. Scenario-specific stories and gaps in the data are identified explicitly. Ideology affinities are configuration preferences, not beliefs shared by every individual. This is a data reference, not an in-game validation report.

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

## Bossaps

Definition: [`XylBossaps`](Defs/GeneDefs/XenotypeDefs_Bossaps.xml). **Inheritable.**

Bossaps are bovine xenohumans engineered as sentient livestock on a glitterworld whose identity is best forgotten. Their fertile, predominantly female population produces abundant milk, and their digestion favors raw plant food. Life in a herd suits both their temperament and their biology: they are mild-mannered, comfortable sharing sleeping quarters, and distressed by isolation. Wild herds also exist on the rim.

Their apparent gentleness can be deceptive. Although unusually accepting of captivity and able to experience pain as pleasure, an injured bossaps may charge into an uncontrollable melee frenzy, using its horns against enemies or even an ally responsible for the injury. Their ideological preferences combine animal personhood, nudism, and pain as virtue with an aversion to ranching, an uneasy legacy for people originally bred to be livestock.

### New functional genes

| Gene | Effect |
| --- | --- |
| herd instinct ([`XylHerdInstinct`](Defs/GeneDefs/GeneDefs_Mood.xml)) | Needs a sufficiently large colony to avoid a mood penalty; does not mind sleeping in barracks. |
| docile ([`XylDocile`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Never initiates social fights; is easier to enslave and less likely to rebel or start prison breaks. Receives a +20 mood thought while imprisoned or enslaved. |
| large horns ([`XylLargeHorns`](Defs/GeneDefs/GeneDefs_InnateWeapons.xml)) | Carriers of this gene have large horns that function as a weapon. |
| seeing red ([`XylSeeingRed`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Combat damage can trigger an uncontrollable melee frenzy, increasing movement and melee damage while greatly reducing pain. The tips warn that an enraged carrier can attack allies who hurt them. |
| pain reversal ([`XylPainReversal`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Grants the Masochist trait: pain gives pleasure and a mood benefit, rather than ordinary pain-related unhappiness. |
| hyperlactation ([`XylHyperlactation`](Defs/GeneDefs/GeneDefs_Hyperlactation.xml)) | Female carriers lactate without pregnancy or breastfeeding and can be milked, including while imprisoned. Active from age 13; inactive in males. |
| usually female ([`XylGender_UsuallyFemale`](Defs/GeneDefs/GeneDefs_Reproduction.xml)) | Sets a 75% female chance when present as a germline gene; has no effect as a xenogene. |
| herbivore stomach ([`XylHerbivoreStomach`](Defs/GeneDefs/GeneDefs_Diet.xml)) | Raw vegetable nutrition x1.8; raw meat x0.5 and cooked meat x0.8. Removes the raw-food thought and food-poisoning chance for raw vegetables. |

**New appearance genes:** cow ears ([`XylEars_Cow`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)).

### Existing game genes

- Cold tolerant (`MinTemp_SmallDecrease`).
- Heat tolerant (`MaxTemp_SmallIncrease`).
- Robust (`Robust`).
- Fertile (`Fertile`).
- Long hair (`Hair_LongOnly`).
- Standard body (`Body_Standard`).
- Fat body (`Body_Fat`).
- Hulk body (`Body_Hulk`).
- Nearsighted (`Nearsighted`).
- Poor cooking aptitude (`AptitudePoor_Cooking`).
- Strong plants aptitude (`AptitudeStrong_Plants`).

## Chyrr

Definition: [`XylChyrr`](Defs/GeneDefs/XenotypeDefs_Chyrr.xml). **Inheritable.**

Chyrr are nocturnal, winged mystics adapted to the heat of jungles and deserts. Sunlight troubles them, while cold slows their metabolism into torpor and eventually hibernation. Their delicate bodies and weak immunity make them vulnerable, but short flights and a stunning ultrasonic cry give them ways to escape or disable threats without relying on physical strength.

Sound is central to their experience of the world. Acute hearing and echolocation let even those born blind perceive their surroundings, fitting their affinity for blindsight. Their mystical character also encompasses low-level psychic sensing and a hazy awareness of events just before they happen. They have a natural aptitude for medicine, though little is recorded about their origins or social institutions.

**Definition note:** precognition appears in the lore, but the separate `XylPrecognition` gene is not included in the chyrr gene list.

### New functional genes

| Gene | Effect |
| --- | --- |
| sonic wave ([`XylSonicWave`](Defs/GeneDefs/GeneDefs_Abilities.xml)) | Releases an ultrasonic and psychic wave that stuns nearby organic creatures around the target. The ability description excludes mechanoids and drones. |
| bat wings ([`XylBatWings`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Enables short flights, blocked by some heavy torso apparel; also reduces Manipulation by 0.1. |
| torpor ([`XylTorpor`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Cold causes progressively impaired consciousness and eventual hibernation, reducing food use and slowing hypothermia and starvation. Warming reverses the condition. |
| nocturnal ([`XylNocturnal`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Grants the Night Owl trait, favoring nighttime activity over being awake during the day. |
| keen ears ([`XylKeenEars`](Defs/GeneDefs/GeneDefs_Senses.xml)) | Multiplies Hearing by 1.2. |
| echolocation ([`XylEcholocation`](Defs/GeneDefs/GeneDefs_Senses.xml)) | Uses Hearing in place of Sight when higher, except for reading speed. Gives a 15% chance of congenital blindness; the tips note that blind or blindfolded carriers can shoot without smoke penalties. |

**New appearance genes:** small pointed ears ([`XylEars_SmallPointed`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)).

### Existing game genes

- Weak immunity (`Immunity_Weak`).
- Cold sensitive (`MinTemp_SmallIncrease`).
- Heat tolerant (`MaxTemp_SmallIncrease`).
- Mild UV sensitivity (`UVSensitivity_Mild`).
- Delicate (`Delicate`).
- Standard body (`Body_Standard`).
- Thin body (`Body_Thin`).
- No beard (`Beard_NoBeardOnly`).
- Dark Vision (`DarkVision`).
- Poor mining aptitude (`AptitudePoor_Mining`).
- Strong medicine aptitude (`AptitudeStrong_Medicine`).

## Dvergr

Definition: [`XylDvergr`](Defs/GeneDefs/XenotypeDefs_Dvergr.xml). **Inheritable.**

Dvergr civilization grew beneath the mineral-rich surface of Svartalfheim, where magma upwellings power vast underground cities. Short, heat-tolerant, and at home in darkness, dvergr are well suited to the mining, construction, and craftwork around which their lives revolve. Fungus provides both food and brewing material: their digestion makes it nourishing without making it appetizing, while alcohol is a biological necessity. These adaptations let expeditions establish new settlements around little more than promising rock, fungus, and beer.

Their devotion to work does not make them easy company. Stoic and industrious, they prefer practical labor to conversation, yet melancholy and aggression can interrupt long stretches of effort with tantrums or mental breaks. Those crises sometimes give way to extraordinary creative inspiration, making the ambition to build something worth remembering more than an empty ideal. Their settlements form loose unions whose unruly, demanding members can become powerful allies once won over.

### New functional genes

| Gene | Effect |
| --- | --- |
| stoic ([`XylStoic`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Grants the positive Nerves trait degree, reducing the mental-break threshold and improving resilience to stress. |
| melancholy ([`XylMelancholy`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Carriers of this gene always have the tortured artist trait, which gives them a permanent mood penalty but also grants a chance to gain creative inspiration after a mental break. |
| dwarf ([`XylDwarf`](Defs/GeneDefs/GeneDefs_Body.xml)) | Smaller body (body-size factor 0.9), proportionately larger head, and -0.1 movement speed. |
| fungus eater ([`XylFungusEater`](Defs/GeneDefs/GeneDefs_Diet.xml)) | Raw fungus nutrition x1.8, without removing dislike of its taste. Unlocks fungus wort brewing and, with Ideology, fungal gravel. |
| industrious ([`XylIndustrious`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Carriers of this gene are exceptionally fast workers. |

**New appearance genes:** bald males ([`XylHair_BaldOnly_Male`](Defs/GeneDefs/GeneDefs_Hair.xml)); small pointed ears ([`XylEars_SmallPointed`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)).

### Existing game genes

- Psychically dull (`PsychicAbility_Dull`).
- Very heat tolerant (`MaxTemp_LargeIncrease`).
- Aggressive (`Aggression_Aggressive`).
- Short hair (`Hair_ShortOnly`).
- Bushy beard (`Beard_BushyOnly`).
- Light gray skin (`Skin_LightGray`).
- Slate gray skin (`Skin_SlateGray`).
- Ink-black skin (`Skin_InkBlack`).
- Strong Stomach (`StrongStomach`).
- Dark Vision (`DarkVision`).
- Cave Dweller (`CaveDweller`).
- Unstoppable (`Unstoppable`).
- Strong construction aptitude (`AptitudeStrong_Construction`).
- Strong mining aptitude (`AptitudeStrong_Mining`).
- Strong crafting aptitude (`AptitudeStrong_Crafting`).
- Poor medicine aptitude (`AptitudePoor_Medicine`).
- Poor social aptitude (`AptitudePoor_Social`).
- Alcohol dependency (`ChemicalDependency_Alcohol`).

## Nixie

Definition: [`XylNixie`](Defs/GeneDefs/XenotypeDefs_Nixie.xml). **Inheritable.**

Nixies were engineered for Atlantis, an ocean world, and have since spread across many planets. On the rim they have lived for thousands of years in river clans and coastal camps, where water sustains both their way of life and their bodies. Their blue, scaled skin must stay moist and dries rapidly in heat; webbed extremities and swift movement through water make rivers and shorelines natural homes. They also travel inland in small bands, and use enlarged, land-capable engineered seals called selkies as pack animals.

Beauty, graceful movement, eloquence, and an instinctive psychic pull give nixies a presence that can seem half-divine. Peaceful tribes trade openly with the 'dryskins', while their affinity for nudism and high life accompanies a physiology unusually sensitive to drugs.

### New functional genes

| Gene | Effect |
| --- | --- |
| Beckon ([`XylPsycast_Beckon`](Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml)) | Grants the Beckon psycast (draws a target toward the caster) without requiring a psylink. Royalty-dependent; the tips specify at least 25 psyfocus for innate nixie psycasts. |
| Focus ([`XylPsycast_Focus`](Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml)) | Grants the Focus psycast (improves the target's focus and performance) without requiring a psylink. Royalty-dependent; the tips specify at least 25 psyfocus for innate nixie psycasts. |
| scaleskin ([`XylScaleskin`](Defs/GeneDefs/GeneDefs_Body.xml)) | Protective scales add 40 percentage points of sharp armor and 15 of blunt armor. |
| drug sensitive ([`XylDrugSensitive`](Defs/GeneDefs/GeneDefs_Drugs.xml)) | Drug effects last longer and tolerance rises faster; adds 0.4 to the drug-effect multiplier and doubles the global addiction-chance factor. |
| aquatic ([`XylAquatic`](Defs/GeneDefs/GeneDefs_Needs.xml)) | Adds a wetness need: standing in water, rain, or showers restores moisture. Dryness causes mood penalties up to -20; soaking wet gives +3. Unlocks showers and lowers minimum comfortable temperature by 5 degrees C. Tips warn of faster drying above 30 degrees C. |

**New appearance genes:** fin ears ([`XylEars_Fin`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)).

### Existing game genes

- Enhanced psychic sensitivity (`PsychicAbility_Enhanced`).
- Naked Speed (`NakedSpeed`).
- Webbed Phalanges (`WebbedPhalanges`).
- Pessimist (`Mood_Pessimist`).
- Very cold tolerant (`MinTemp_LargeDecrease`).
- Heat sensitive (`MaxTemp_SmallDecrease`).
- Weak melee damage (`MeleeDamage_Weak`).
- Pretty (`Beauty_Pretty`).
- Bald (`Hair_BaldOnly`).
- Snow-white hair (`Hair_SnowWhite`).
- Grayless hair (`Hair_Grayless`).
- Blue skin (`Skin_Blue`).
- Poor mining aptitude (`AptitudePoor_Mining`).
- Poor construction aptitude (`AptitudePoor_Construction`).
- Strong social aptitude (`AptitudeStrong_Social`).

## Titan

Definition: [`XylTitan`](Defs/GeneDefs/XenotypeDefs_Titan.xml). **Non-inheritable.**

Titans are sterile, vat-grown giants created to build structures that will outlast their own lives. Their massive bodies, stony armor, strength, and resistance to toxins suit arduous labor, while an inability to feel joy removes the ordinary desire for recreation. They are taciturn and narrowly focused: each has an exceptional aptitude in one field, but little interest in learning subjects outside their passions. Their reputation is one of relentless construction in deserts, tundra, and poisonous wastelands, and they have an ideological affinity for shipborn life.

Their unusual durability comes with unusual medical needs. Carbon-silicates are incorporated into their biochemistry, leaving most ordinary drugs ineffective, and their tissues can progressively turn to stone through incurable petrification. Specialized drugs can prevent the disease before it begins or gradually strengthen resistance to hostile environments, but depend on continued dosing. Their slow healing and movement remain practical limitations even behind the image of an inexhaustible worker.

### New functional genes

| Gene | Effect |
| --- | --- |
| rock toss ([`XylRockToss`](Defs/GeneDefs/GeneDefs_Abilities.xml)) | The carrier can pick up a rock chunk in combat and toss it. It will land near a targeted location, damaging everything in a radius around where it lands. |
| lithoid ([`XylLithoid`](Defs/GeneDefs/GeneDefs_Drugs.xml)) | Carriers of this gene have a unique biochemistry that incorporates carbon-silicates. They are completely unaffected by most drugs that work on baseliners, and instead must use specialized drugs designed for lithoids. |
| rockskin ([`XylMineralizedSkin`](Defs/GeneDefs/GeneDefs_Body.xml)) | Rock-like plates add 70 percentage points of sharp armor, 40 blunt, and 50 heat, at -0.4 movement speed. Also reduces romance chance with people lacking this gene (factor 0.2). |
| petrification ([`XylPetrification`](Defs/GeneDefs/GeneDefs_Petrification.xml)) | Can develop incurable petrification, gradually replacing tissue with stone. Medical care slows progression and surgery removes affected tissue. Softener prevents onset when taken every five days, but does not treat an existing disease, even if dormant. |
| focused ([`XylLearning_Focused`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Carriers of this gene care deeply about subjects they are interested in. They learn faster for skills they have a burning passion in, but much slower at skills they have no passion in. |
| giant ([`XylGiant`](Defs/GeneDefs/GeneDefs_Body.xml)) | Body size x1.5 and health scale x1.3. The larger body carries more and resists drugs and toxins better, but is easier to hit with ranged weapons. |
| joyless ([`XylJoyless`](Defs/GeneDefs/GeneDefs_Needs.xml)) | Carriers of this gene are genetically incapable of feeling joy. They have no need for recreation, and get no mood bonuses or penalties from it. |
| super-specialist ([`XylSuperSpecialist`](Defs/GeneDefs/GeneDefs_BonusGenes.xml)) | Replaces itself with one random remarkable aptitude gene: +8 aptitude and one passion level in Construction, Mining, Cooking, Plants, Animals, Crafting, Artistic, Medicine, Social, or Intellectual. Shooting and Melee are excluded from this generator. |

**New appearance genes:** bald males ([`XylHair_BaldOnly_Male`](Defs/GeneDefs/GeneDefs_Hair.xml)).

### Existing game genes

- Strong melee damage (`MeleeDamage_Strong`).
- Super immunity (`Immunity_SuperStrong`).
- Slow wound healing (`WoundHealing_Slow`).
- Superclotting (`Superclotting`).
- Psychically deaf (`PsychicAbility_Deaf`).
- Slow runner (`MoveSpeed_Slow`).
- Total toxic resistance (`ToxResist_Total`).
- Sterile (`Sterile`).
- Ugly (`Beauty_Ugly`).
- No beard (`Beard_NoBeardOnly`).
- Hulk body (`Body_Hulk`).
- Poor social aptitude (`AptitudePoor_Social`).
- Poor intellectual aptitude (`AptitudePoor_Intellectual`).

**Lithoid drug context:** [Softener, atlasite, and crystal](Defs/Drugs/Drugs_Titan.xml) are tailored to lithoids. Softener prevents new petrification; atlasite requires a dose every three days to build and maintain protective saturation, which rapidly declines after a missed dose; crystal produces euphoria, improves performance, and dulls pain, but is highly addictive.

## Trog

Definition: [`XylTrog`](Defs/GeneDefs/XenotypeDefs_Trog.xml). **Inheritable.**

Trogs are the descendants of repeated interbreeding among neanderthals, wasters, dirtmoles, and numerous other xenohuman lineages on some of humanity's least habitable rimworlds. That ancestry remains unsettled: their unstable genomes frequently express unexpected additional genes. Malformed, foul-smelling, and slow, they nevertheless endure toxic sludge and underground decay with formidable strength, disease resistance, and tolerance of pain. Darkness offers refuge from their sensitivity to sunlight, while pollution can invigorate them.

Their relationship with giant insects makes these hostile environments more livable and their settlements more dangerous. Pheromones protect them from wild insects, which they can tame or turn into a defensive advantage against intruders. Warlike trog tribes are insular and refuse dealings with outsiders, relying on tenacity and insect allies despite primitive technology. Their preference for darkness and flesh purity also fits bodies that painfully reject artificial implants, making technological replacement of flesh especially unappealing.

### New functional genes

| Gene | Effect |
| --- | --- |
| toxic burst ([`XylToxicBurst`](Defs/GeneDefs/GeneDefs_Abilities.xml)) | Carriers have the ability to release a cloud of tox gas around them from a special gland located near their anus. The gas affects the user normally. |
| bio-rejection ([`XylBioRejection`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Artificial implants or body parts cause ongoing pain until removed. The tips specify that a mechlink triggers rejection but a psylink does not. |
| insect pheromones ([`XylInsectPheromones`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Prevents hostility from wild insects. The tips also state that colony animals benefit from this protection. |
| genetic atavism ([`XylGeneticAtavism`](Defs/GeneDefs/GeneDefs_BonusGenes.xml)) | 50% chance to generate extra random xenogenes. The generator excludes archite genes and constrains the resulting metabolic total to -2 through +2; individual trogs can therefore differ. |

**New appearance genes:** warped head ([`XylHead_Trog`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)); dark green skin ([`XylSkin_DarkGreen`](Defs/GeneDefs/GeneDefs_SkinColors.xml)); olive skin ([`XylSkin_Olive`](Defs/GeneDefs/GeneDefs_SkinColors.xml)).

### Existing game genes

- Strong immunity (`Immunity_Strong`).
- Slow runner (`MoveSpeed_Slow`).
- Partial toxic-environment resistance (`ToxicEnvironmentResistance_Partial`).
- Mild UV sensitivity (`UVSensitivity_Mild`).
- Aggressive (`Aggression_Aggressive`).
- Strong melee damage (`MeleeDamage_Strong`).
- Reduced pain (`Pain_Reduced`).
- Very ugly (`Beauty_VeryUgly`).
- Bald (`Hair_BaldOnly`).
- Human headbone (`Headbone_Human`).
- Mini-horns (`Headbone_MiniHorns`).
- Green skin (`Skin_Green`).
- Slow study (`Learning_Slow`).
- Dark Vision (`DarkVision`).
- Pollution Rush (`PollutionRush`).
- Mild cell instability (`Instability_Mild`).
- Strong mining aptitude (`AptitudeStrong_Mining`).
- Strong animals aptitude (`AptitudeStrong_Animals`).
- Terrible intellectual aptitude (`AptitudeTerrible_Intellectual`).
- Psychite addiction resistance (`AddictionResistant_Psychite`).

## Warcat

Definition: [`XylWarcat`](Defs/GeneDefs/XenotypeDefs_Warcat.xml). **Inheritable.**

Warcats began as military hybrids of humans and great cats, built around fast reflexes, powerful melee attacks, and retractable claws. A flaw in their engineering left them unable to make essential nutrients that must now come from raw meat. This dependence shapes their place on the rim: they roam in close-knit prides and hunting bands, taking whatever prey they can overpower, including other xenohumans. Their arrival on the local world is forgotten, but thousands of years of hunting have made them an established part of its ecosystem.

Strong internal bonds coexist with aggression, volatile moods, and a capacity for feral rage. Hunting bands form loose confederations that are difficult to deal with but can still be reasoned with. Cannibalism and raiding fit their predatory way of life, whereas animal personhood conflicts with it. Pride membership is not assured: exiles may be forced to seek new territory and establish a home with scant supplies, and the recorded example of such a group includes slaves. Their prowess makes them dangerous settlers; their accumulating need for raw meat makes hunger a constant constraint.

### New functional genes

| Gene | Effect |
| --- | --- |
| feral rage ([`XylFeralRage`](Defs/GeneDefs/GeneDefs_Abilities.xml)) | Carriers have the ability to enter a state of feral rage, giving increased movement speed (+50%) and faster melee attacks (+50%). After the rage ends, there is a temporary backlash which causes pain (+10%) and slows movement (-20%). |
| moody ([`XylMoody`](Defs/GeneDefs/GeneDefs_Mood.xml)) | Carriers of this gene have a volatile emotional state. They randomly get good and bad moods. |
| fast reflexes ([`XylFastReflexes`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Grants the Nimble trait, improving melee dodging and trap avoidance. |
| retractable claws ([`XylRetractableClaws`](Defs/GeneDefs/GeneDefs_InnateWeapons.xml)) | Carriers of this gene have retractable claws that function as weapons. |
| carnivore stomach ([`XylCarnivoreStomach`](Defs/GeneDefs/GeneDefs_Diet.xml)) | Raw meat nutrition x1.8; raw vegetables x0.5 and cooked vegetables x0.8. Removes the raw-food thought and food-poisoning chance for raw meat; does not itself grant the Cannibal trait. |
| meat dependence ([`XylMeatDependence`](Defs/GeneDefs/GeneDefs_Diet.xml)) | From age 13, requires raw meat for essential nutrients. Deficiency causes declining mood and health, pain, weakness, mental instability, coma, and eventually death. The deficit accumulates: recovery requires making up missed raw meat. |

**New appearance genes:** yellow eyes ([`XylEyes_Yellow`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)); facial stripes ([`XylFacialStripes`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)).

### Existing game genes

- Hyper-aggressive (`Aggression_HyperAggressive`).
- Strong melee damage (`MeleeDamage_Strong`).
- Sleepy (`Sleepy`).
- High libido (`Libido_High`).
- Long hair (`Hair_LongOnly`).
- No beard (`Beard_NoBeardOnly`).
- Cat ears (`Ears_Cat`).
- Standard body (`Body_Standard`).
- Thin body (`Body_Thin`).
- Dark Vision (`DarkVision`).
- Remarkable melee aptitude (`AptitudeRemarkable_Melee`).
- Poor plants aptitude (`AptitudePoor_Plants`).
- Poor crafting aptitude (`AptitudePoor_Crafting`).

## Succuboid

Definition: [`XylSuccuboid`](Defs/GeneDefs/XenotypeDefs_Minor.xml). **Inheritable.**

Succuboids were engineered as fashionable companions for glitterworld elites. Their beauty, social aptitude, and psychic influence over attraction accompany a distinctive appearance: pink skin, wings, small horns, a smooth tail, and looks that remain youthful despite ordinary aging. Their delicate bodies, substantial need for sleep, and slow work suit a life of leisure, while high libido and euphoric secretions make intimacy with them compelling and potentially addictive.

At some point, succuboids took control of their own reproduction and engineered the ability to bear succuboid daughters with partners of other xenotypes. Their exclusively female lineage can now preserve itself across generations, although reproduction still depends on males from outside that lineage. This combination of reproductive autonomy and intimate dependence on outsiders shapes their ambiguous place in society: some people prize them as prestigious companions, others treat them as valuable commodities, and still others fear them as social parasites. Their origins as designed companions continue to influence how they are treated even after they gained control over their biological future.

### New functional genes

| Gene | Effect |
| --- | --- |
| Word of Love ([`XylPsycast_WordOfLove`](Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml)) | Grants the Word of Love psycast (influences romantic attraction) without requiring a psylink, via the mod's psycast-gene template. |
| bat wings ([`XylBatWings`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Enables short flights, blocked by some heavy torso apparel; also reduces Manipulation by 0.1. |
| always female ([`XylGender_AlwaysFemale`](Defs/GeneDefs/GeneDefs_Reproduction.xml)) | Makes germline carriers female; has no effect as a xenogene. |
| strong genes ([`XylStrongGenes`](Defs/GeneDefs/GeneDefs_Reproduction.xml)) | With a parent of a different xenotype, offspring copy the carrier's endogenes exactly unless the other parent also has strong genes. |
| love euphoria ([`XylLoveEuphoria`](Defs/GeneDefs/GeneDefs_Reproduction.xml)) | Lovin' gives the partner a euphoric mood boost (+14) and a chance of inspiration, but can create an addiction. Withdrawal worsens mood, work speed, and tiredness. Active from age 16. |
| youthful ([`XylYouthful`](Defs/GeneDefs/GeneDefs_Reproduction.xml)) | Caps the age used in lovin' frequency, lovin' age-factor, and relationship-compatibility calculations at 18. Does not stop biological aging, prevent age-related illness, or extend lifespan. See the [implementation](../Source_XylRaces/Patches/PatchLovin.cs). |
| shameless ([`XylShameless`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Carriers of this gene are never ashamed by nudity, and are much happier when not wearing clothes. |
| lazy ([`XylLazy`](Defs/GeneDefs/GeneDefs_Traits.xml)) | Carriers of this gene are slow workers. |

**New appearance genes:** pink skin ([`XylSkin_Pink`](Defs/GeneDefs/GeneDefs_SkinColors.xml)).

### Existing game genes

- Enhanced psychic sensitivity (`PsychicAbility_Enhanced`).
- Naked Speed (`NakedSpeed`).
- Weak melee damage (`MeleeDamage_Weak`).
- Very Sleepy (`VerySleepy`).
- Delicate (`Delicate`).
- High libido (`Libido_High`).
- Beautiful (`Beauty_Beautiful`).
- Long hair (`Hair_LongOnly`).
- Mini-horns (`Headbone_MiniHorns`).
- Standard body (`Body_Standard`).
- Pink hair (`Hair_Pink`).
- Light purple hair (`Hair_LightPurple`).
- Smooth tail (`Tail_Smooth`).
- Grayless hair (`Hair_Grayless`).
- Strong social aptitude (`AptitudeStrong_Social`).

## Scaleborn

Definition: [`XylScaleborn`](Defs/GeneDefs/XenotypeDefs_Minor.xml). **Inheritable.**

Scaleborn began with a fashion for genetic modifications that recreated the dragons of ancient Earth mythology. The resulting horns, retractable claws, powerful melee attacks, and armored scales have since acquired a practical role on Tyrantis V, a dinosaur world where scaleborn survive as top predators. They hunt its great megafauna for both food and materials, supported by a digestive system that extracts abundant nutrition from raw meat.

Their draconic inheritance takes five forms: red scaleborn breathe fire and resist burning; dark green scaleborn spray acid and resist toxins; white scaleborn spray foam and heal quickly; blue scaleborn release electromagnetic blasts and resist being staggered; black scaleborn have robust bodies and spray blinding oil that leaves flammable puddles. Despite their formidable natural weapons, they move slowly, sleep often, and enter torpor when cold. Aggression and volatile moods accompany these physical adaptations, while psychic dullness reduces their sensitivity to psychic influence. What began as an imitation of mythical creatures has become a way of life among the giant animals of Tyrantis V.

### New functional genes

| Gene | Effect |
| --- | --- |
| moody ([`XylMoody`](Defs/GeneDefs/GeneDefs_Mood.xml)) | Carriers of this gene have a volatile emotional state. They randomly get good and bad moods. |
| torpor ([`XylTorpor`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Cold causes progressively impaired consciousness and eventual hibernation, reducing food use and slowing hypothermia and starvation. Warming reverses the condition. |
| large horns ([`XylLargeHorns`](Defs/GeneDefs/GeneDefs_InnateWeapons.xml)) | Carriers of this gene have large horns that function as a weapon. |
| retractable claws ([`XylRetractableClaws`](Defs/GeneDefs/GeneDefs_InnateWeapons.xml)) | Carriers of this gene have retractable claws that function as weapons. |
| carnivore stomach ([`XylCarnivoreStomach`](Defs/GeneDefs/GeneDefs_Diet.xml)) | Raw meat nutrition x1.8; raw vegetables x0.5 and cooked vegetables x0.8. Removes the raw-food thought and food-poisoning chance for raw meat; does not itself grant the Cannibal trait. |
| scaleskin ([`XylScaleskin`](Defs/GeneDefs/GeneDefs_Body.xml)) | Protective scales add 40 percentage points of sharp armor and 15 of blunt armor. |
| scaleborn lineage ([`XylScalebornLineage`](Defs/GeneDefs/GeneDefs_BonusGenes.xml)) | Replaces itself with one of five complete lineage packages: fire, acid/toxin, foam/healing, EMP, or oil. See the variant list below. |

### Existing game genes

- Psychically dull (`PsychicAbility_Dull`).
- Slow runner (`MoveSpeed_Slow`).
- Cold tolerant (`MinTemp_SmallDecrease`).
- Heat tolerant (`MaxTemp_SmallIncrease`).
- Aggressive (`Aggression_Aggressive`).
- Strong melee damage (`MeleeDamage_Strong`).
- Sleepy (`Sleepy`).
- Bald (`Hair_BaldOnly`).
- No beard (`Beard_NoBeardOnly`).
- Facial Ridges (`FacialRidges`).
- Poor cooking aptitude (`AptitudePoor_Cooking`).

### Additional lineage genes

The lineage generator selects one complete package and removes the `XylScalebornLineage` placeholder:

- **Red:** fire spew (`FireSpew`), fire resistance (`FireResistant`), deep red skin (`Skin_DeepRed`).
- **Green:** acid spray (`AcidSpray`), partial toxic resistance (`ToxResist_Partial`), new cosmetic dark green skin (`XylSkin_DarkGreen`).
- **White:** foam spray (`FoamSpray`), fast wound healing (`WoundHealing_Fast`), sheer white skin (`Skin_SheerWhite`).
- **Blue:** EMP blast (`XylEMPBlast`), unstoppable (`Unstoppable`), new cosmetic dark blue skin (`XylSkin_DarkBlue`).
- **Black:** oil spray (`XylOilSpray`), robust (`Robust`), slate gray skin (`Skin_SlateGray`).

The black lineage uses the existing slate gray skin gene (`Skin_SlateGray`).

These new functional genes occur only in their respective lineage packages:

| Gene | Effect |
| --- | --- |
| EMP blast ([`XylEMPBlast`](Defs/GeneDefs/GeneDefs_Abilities.xml)) | Releases an electromagnetic pulse around the carrier, disabling nearby electronic devices. The blast has a radius of 3.5 cells and a five-day cooldown. |
| oil spray ([`XylOilSpray`](Defs/GeneDefs/GeneDefs_Abilities.xml)) | Sprays sticky oil from neck glands through the mouth, temporarily blinding targets and leaving flammable puddles. Has a range of 8.9 cells and a five-day cooldown. |

## Zeegee

Definition: [`XylZeegee`](Defs/GeneDefs/XenotypeDefs_Minor.xml). **Inheritable.**

Zeegees descend from the inhabitants of deep-space habitats and ancient generation ships. Their aptitude for advanced technology and rapid learning suit the machinery-dependent environments of their ancestry, while dark vision and a preference for enclosed surroundings fit life inside a habitat. Silver skin and a distinctive forehead mark set them apart, and their keen distance vision supports an aptitude for shooting. They are less suited to close combat or handling animals.

Planetary life is physically difficult for them. Their delicate bodies and weak immunity leave them vulnerable, and adapting to surface gravity brings bouts of nausea and vomiting that impair everyday activity. Yet they retain a specialized means of surviving environmental emergencies: oxygen and proteins stored in their bone marrow can temporarily protect them against vacuum, toxins, and extreme temperatures. These reserves offer a chance to survive a crisis, followed by a period of recovery, rather than permanent freedom from the hazards outside a habitat. No distinct zeegee society or named homeworld is described.

### New functional genes

| Gene | Effect |
| --- | --- |
| planet sickness ([`XylPlanetSickness`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Periodic nausea and vomiting while on a planet's surface, with a configured mean interval of 30 days. An episode lasts about a day and multiplies Consciousness by 0.6, Moving by 0.8, Manipulation by 0.9, and Eating by 0.5. |
| emergency reserves ([`XylEmergencyReserves`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Early vacuum exposure, hypothermia, heatstroke, or toxic buildup triggers stored oxygen and proteins, temporarily protecting against all four hazards. The active phase lasts about one day, followed by roughly four days of recovery with increased hunger before the reserves are available again. |
| telescopic vision ([`XylTelescopicVision`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Multiplies the shooting-accuracy factor at long range by 2 and at medium range by 1.5. |
| tech affinity ([`XylTechAffinity`](Defs/GeneDefs/GeneDefs_Miscellaneous.xml)) | Adds 3 mech bandwidth and, with Odyssey, 0.3 piloting ability. |

**New appearance genes:** forehead mark ([`XylForeheadMark`](Defs/GeneDefs/GeneDefs_Cosmetic.xml)); dark silver skin ([`XylSkin_DarkSilver`](Defs/GeneDefs/GeneDefs_SkinColors.xml)); light silver skin ([`XylSkin_LightSilver`](Defs/GeneDefs/GeneDefs_SkinColors.xml)).

### Existing game genes

- Weak immunity (`Immunity_Weak`).
- Weak melee damage (`MeleeDamage_Weak`).
- Delicate (`Delicate`).
- Short hair (`Hair_ShortOnly`).
- Space movement speed (`MoveSpeed_Space`; requires Odyssey).
- Fast learning (`Learning_Fast`).
- Dark vision (`DarkVision`).
- Cave dweller (`CaveDweller`).
- Strong shooting aptitude (`AptitudeStrong_Shooting`).
- Poor melee aptitude (`AptitudePoor_Melee`).
- Poor animals aptitude (`AptitudePoor_Animals`).
- Strong intellectual aptitude (`AptitudeStrong_Intellectual`).

## Source guide

- [Xenotype and gene definitions](Defs/GeneDefs/): gene pools, effects, innate weapons, abilities, health conditions, and random gene generators.
- [Faction descriptions](Defs/Factions/): bossaps herd, dvergr union, nixie tribe, trog tribe, and warcat tribe.
- [Gameplay tips](Defs/TipSetDefs/): supplementary details about biology and gene use.
- [Scenarios](Defs/Scenarios/Scenarios.xml) and [Royalty scenarios](Compat/Royalty/Defs/Scenarios_Royalty.xml): dvergr migration, warcat exile, and the nixie cult survivor.
- [Odyssey animals](Compat/Odyssey/Defs/Races_Animal_Odyssey.xml): nixie selkies.
- [Titan drugs](Defs/Drugs/Drugs_Titan.xml): lithoid biochemistry and petrification management.
- [Psycast gene template](Defs/GeneDefs/GeneTemplateDefs_Psycasts.xml): creates genes from game psycasts; these grant their abilities without a psylink. Nixie Beckon and Focus entries explicitly require Royalty; the succuboid Word of Love reference lacks that explicit guard.

Other new genes exist in the mod, but are not part of these ten xenotypes' fixed gene lists. They are outside this xenotype-by-xenotype reference unless relevant to a documented random variant or flavor-text discrepancy.
