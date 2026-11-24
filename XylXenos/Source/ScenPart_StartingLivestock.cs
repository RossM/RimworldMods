namespace XylXenos;

[UsedFromXml]
public class ScenPart_StartingLivestock : ScenPart
{
    private PawnKindDef? animalKind;

    public List<string>? tradeTags = ["AnimalFarm"];

    public int countMale = 1;
    public int countFemale = 1;

    [Unsaved] private string? countMaleBuf;
    [Unsaved] private string? countFemaleBuf;

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Defs.Look(ref animalKind, nameof(animalKind));
        Scribe_Collections.Look(ref tradeTags, nameof(tradeTags));
        Scribe_Values.Look(ref countMale, nameof(countMale));
        Scribe_Values.Look(ref countFemale, nameof(countFemale));
    }

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 3f + 1f);
        Rect curRect = scenPartRect.TopPartPixels(RowHeight);
        Widgets.TextFieldNumericLabeled(curRect, "Male".Translate().CapitalizeFirst(), ref countMale, ref countMaleBuf, 1f);
        curRect.y += RowHeight;
        Widgets.TextFieldNumericLabeled(curRect, "Female".Translate().CapitalizeFirst(), ref countFemale, ref countFemaleBuf, 1f);
        curRect.y += RowHeight;
        if (!Widgets.ButtonText(curRect, CurrentAnimalLabel().CapitalizeFirst()))
            return;
        List<FloatMenuOption> list =
        [
            new("XylRandomLivestock".Translate().CapitalizeFirst(), () => { animalKind = null; }),
            .. PossibleAnimals(checkForTamer: false)
                .Select(localKind => new FloatMenuOption(localKind.LabelCap, () => { animalKind = localKind; })),
        ];
        // The scenario editor draws this interface from the window stack.
        Find.WindowStack!.Add(new FloatMenu(list));
    }

    private static IEnumerable<PawnKindDef> PossibleAnimals(bool checkForTamer = true)
    {
        return DefDatabase<PawnKindDef>.AllDefs.Where(kindDef => kindDef.RaceProps.Animal && (!checkForTamer || CanKeepPetTame(kindDef)));
    }

    private static bool CanKeepPetTame(PawnKindDef def)
    {
        var initData = Find.GameInitData;
        DebugAssert.NotNull(initData);
        var bestTamer = initData!.startingAndOptionalPawns.Take(initData.startingPawnCount).MaxBy(pawn =>
        {
            DebugAssert.NotNull(pawn.skills);
            return pawn.skills!.GetSkill(SkillDefOf.Animals).Level;
        });
        DebugAssert.NotNull(bestTamer);
        var skills = bestTamer!.skills;
        DebugAssert.NotNull(skills);
        int level = skills!.GetSkill(SkillDefOf.Animals).Level;
        float statValueAbstract = def.race.GetStatValueAbstract(StatDefOf.MinimumHandlingSkill);
        return level >= statValueAbstract;
    }

    private string CurrentAnimalLabel()
    {
        return animalKind?.label ?? "XylRandomLivestock".Translate();
    }

    private List<PawnKindDef> RandomPets()
    {
        List<PawnKindDef> result = [.. PossibleAnimals().Where(Validate)];
        if (result is not { Count: > 0 })
            result = [.. PossibleAnimals(checkForTamer: false).Where(Validate)];
        return result;

        bool Validate(PawnKindDef td)
        {
            if (tradeTags is not { Count: > 0 })
                return true;
            return td.race.tradeTags is { Count: > 0 } && td.race.tradeTags.Intersect(tradeTags).Any();
        }
    }

    public override string Summary(Scenario scen)
    {
        return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);
    }

    public override IEnumerable<string> GetSummaryListEntries(string tag)
    {
        if (tag == "PlayerStartsWith")
            yield return
                $"{CurrentAnimalLabel().CapitalizeFirst()} x{countMale + countFemale} ({countMale} {"Male".Translate()}, {countFemale} {"Female".Translate()})";
    }

    public override void Randomize()
    {
        animalKind = Rand.Value < 0.5f ? null : PossibleAnimals(checkForTamer: false).RandomElement();
        countMale = countFemale = 1;
    }

    private static float PetWeight(PawnKindDef animal)
    {
        var initData = Find.GameInitData;
        DebugAssert.NotNull(initData);
        if (initData!.playerFaction.ideos?.PrimaryIdeo is not { } ideo)
            return 1f;
        return Enumerable.Any(ideo.VeneratedAnimals, veneratedAnimal => veneratedAnimal == animal.race) ? 8f : 1f;
    }

    public override IEnumerable<Thing> PlayerStartingThings()
    {
        // RandomPets builds its list from loaded PawnKindDefs, so each candidate passed to PetWeight exists.
        PawnKindDef? kindDef = animalKind ?? RandomPets().RandomElementByWeightWithFallback(pawnKindDef => PetWeight(pawnKindDef!));
        DebugAssert.NotNull(kindDef);

        for (int i = 0; i < countMale; i++)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(kindDef, Faction.OfPlayer, fixedGender: Gender.Male);
            Pawn animal = PawnGenerator.GeneratePawn(request);
            if (animal.Name is null or { Numerical: true })
                animal.Name = PawnBioAndNameGenerator.GeneratePawnName(animal);
            yield return animal;
        }

        for (int i = 0; i < countFemale; i++)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(kindDef, Faction.OfPlayer, fixedGender: Gender.Female);
            Pawn animal = PawnGenerator.GeneratePawn(request);
            if (animal.Name is null or { Numerical: true })
                animal.Name = PawnBioAndNameGenerator.GeneratePawnName(animal);
            yield return animal;
        }
    }
}
