using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevTools.Testing;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Yobai.Tests;

[TestFixture(TestType.Playing)]
public sealed class PartnerSleepTests
{
    private Pawn first = null!;
    private Pawn second = null!;
    private Pawn third = null!;
    private Pawn outsider = null!;
    private Building_Bed firstBed = null!;
    private Building_Bed secondBed = null!;
    private Building_Bed thirdBed = null!;
    private Building_Bed visitBed = null!;
    private readonly List<Thing> spawned = [];

    [SetUp]
    public void SetUp()
    {
        Map map = Find.CurrentMap;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
        DevTools.DevLog.Write("Starting " + Test.Current.TestCase.FullName);
        DevTools.DevLog.Flush();
        foreach (IntVec3 cell in CellRect.CenteredOn(new IntVec3(40, 0, 40), 6))
        {
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            if (cell.x == 34 || cell.x == 46 || cell.z == 34 || cell.z == 46)
            {
                ThingDef wallDef = cell == new IntVec3(40, 0, 34)
                    ? DefDatabase<ThingDef>.GetNamed("Door") : ThingDefOf.Wall;
                Thing wall = ThingMaker.MakeThing(wallDef, ThingDefOf.WoodLog);
                wall.SetFaction(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
                spawned.Add(wall);
            }
        }
        foreach (IntVec3 cell in CellRect.CenteredOn(new IntVec3(40, 0, 40), 5))
            map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);

        Pawn[] pawns = new Pawn[4];
        for (int index = 0; index < pawns.Length; index++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
                Faction.OfPlayer, fixedBiologicalAge: 25, fixedChronologicalAge: 25,
                canGeneratePawnRelations: false));
            if (ModsConfig.IdeologyActive)
                pawn.ideo.SetIdeo(null);
            pawn.health.RemoveAllHediffs();
            pawn.needs.rest.CurLevel = 0.2f;
            GenSpawn.Spawn(pawn, new IntVec3(38 + index, 0, 43), map);
            // Allow the test to start sleep jobs without pawns choosing their own rest or lovin' jobs.
            pawn.mindState.canSleepTick = int.MaxValue;
            pawn.mindState.canLovinTick = int.MaxValue;
            pawns[index] = pawn;
            spawned.Add(pawn);
        }
        // First -- spouse -- Second -- spouse -- Third; Outsider has no romantic ties.
        (first, second, third, outsider) = (pawns[0], pawns[1], pawns[2], pawns[3]);
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, second);
        second.relations.AddDirectRelation(PawnRelationDefOf.Spouse, third);

        Building_Bed[] beds = new Building_Bed[3];
        for (int index = 0; index < beds.Length; index++)
        {
            Building_Bed bed = (Building_Bed)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DoubleBed"), ThingDefOf.WoodLog);
            bed.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(bed, new IntVec3(36 + index * 4, 0, 37), map);
            bed.CompAssignableToPawn.TryAssignPawn(pawns[index]);
            beds[index] = bed;
            spawned.Add(bed);
        }
        (firstBed, secondBed, thirdBed) = (beds[0], beds[1], beds[2]);
        visitBed = (Building_Bed)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DoubleBed"), ThingDefOf.WoodLog);
        visitBed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(visitBed, new IntVec3(52, 0, 40), map);
        visitBed.CompAssignableToPawn.TryAssignPawn(outsider);
        spawned.Add(visitBed);
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
    }

    [TearDown]
    public void TearDown()
    {
        if (Test.Current.Status == Status.Failed)
        {
            foreach (Pawn pawn in spawned.OfType<Pawn>())
                DevTools.DevLog.Write($"{pawn.LabelShort}: job={pawn.CurJob}, bed={pawn.CurrentBed()}, owned={pawn.ownership?.OwnedBed}, eligible={BedSharingHelpers.CanShareBedsNow(pawn)}, hediffs={string.Join(", ", pawn.health.hediffSet.hediffs.Select(h => h.def.defName))}");
            foreach (Building_Bed bed in spawned.OfType<Building_Bed>().Where(b => b.Spawned))
                DevTools.DevLog.Write($"{bed}: owners={string.Join(", ", bed.OwnersForReading.Select(p => p.LabelShort))}, occupants={string.Join(", ", bed.CurOccupants.Select(p => p.LabelShort))}, room={bed.GetRoom().Role}");
        }
        // Remove the roof first so it cannot collapse during the next test.
        foreach (IntVec3 cell in CellRect.CenteredOn(new IntVec3(40, 0, 40), 6))
            Find.CurrentMap.roofGrid.SetRoof(cell, null);
        foreach (Thing thing in spawned.AsEnumerable().Reverse())
        {
            if (!thing.Destroyed)
                thing.Destroy();
        }
        spawned.Clear();
        DevTools.DevLog.Flush();
    }

    private Job? GetSleepJob(Pawn pawn)
    {
        return new JobGiver_GetRest().TryIssueJobPackage(pawn, default).Job;
    }

    private void StartPolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(pawn, bed));
        pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
    }

    private static Ideo CreateIdeoWithLovinPrecept(string preceptName)
    {
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(FactionDefOf.PlayerColony));
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")).ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed(preceptName)));
        return ideo;
    }

    [Test]
    public void PolyculeIsTransitiveButBedSharingPartnersAreNot()
    {
        Expect.AreEqual(3, PolyculeHelpers.GetPolycule(first).Count);
        Expect.IsTrue(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, second));
        Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, third));
        Expect.IsFalse(PolyculeHelpers.GetPolycule(first).Contains(outsider));
    }

    [Test]
    public void PolyculeRoomIsBedroom()
    {
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        Expect.IsTrue(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
    }

    [Test]
    public void UnrelatedAdultMakesRoomBarracks()
    {
        thirdBed.CompAssignableToPawn.TryUnassignPawn(third);
        thirdBed.CompAssignableToPawn.TryAssignPawn(outsider);
        Expect.AreEqual(RoomRoleDefOf.Barracks, firstBed.GetRoom().Role);
        Expect.IsTrue(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, secondBed));
    }

    [Test]
    public void BreakupChangesVanillaBedroomClassification()
    {
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        firstBed.NotifyRoomAssignedPawnsChanged();
        Expect.AreEqual(RoomRoleDefOf.Barracks, firstBed.GetRoom().Role);
    }

    [Test]
    public void ConnectingPartnerMayBeOutsideTheBedroom()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        secondBed.Destroy();
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        Expect.IsTrue(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
    }

    [Test]
    public IEnumerator EmptyBedsAllHaveAChance()
    {
        HashSet<Building_Bed> selected = [];
        for (int attempt = 0; attempt < 100; attempt++)
        {
            yield return new WaitTicks(1);
            using RandBlock random = new(12345 + attempt);
            Job? job = GetSleepJob(first);
            if (job != null)
            {
                selected.Add((Building_Bed)job.targetA.Thing);
                first.ClearReservationsForJob(job);
                JobMaker.ReturnToPool(job);
            }
        }
        Expect.AreEqual(3, selected.Count);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public void WalkingSleepJobKeepsItsChosenDestination()
    {
        StartPolyculeBedroomSleepJob(first, secondBed);
        Job selected = first.CurJob;
        IntVec3 reserved = RestUtility.GetBedSleepingSlotPosFor(first, secondBed);
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Job? next = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
            Expect.AreEqual(selected.targetA.Thing, next.targetA.Thing);
            Expect.AreEqual(reserved, RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        }
    }

    [Test]
    public void FutureSleepJobCanReserveAnIndirectPartnersBed()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job pending = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(pending.TryMakePreToilReservations(first, false));
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
        first.ClearReservationsForJob(pending);
        JobMaker.ReturnToPool(pending);
    }

    [Test]
    public void BorrowingDoesNotMakeBedAvailableToOrdinaryVanillaSearches()
    {
        thirdBed.CompAssignableToPawn.TryAssignPawn(outsider);
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, first, true));
    }

    [Test]
    public IEnumerator PartnerAndEmptyDoubleBedsArePreferredToIndirectPartner()
    {
        StartPolyculeBedroomSleepJob(third, thirdBed);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == thirdBed, 600);
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed, 600);
        HashSet<Building_Bed> selected = [];
        for (int attempt = 0; attempt < 100; attempt++)
        {
            yield return new WaitTicks(1);
            using RandBlock random = new(24680 + attempt);
            Job? chosen = GetSleepJob(first);
            Expect.IsNotNull(chosen);
            try
            {
                Building_Bed bed = (Building_Bed)chosen!.targetA.Thing;
                Expect.IsTrue(bed == firstBed || bed == secondBed);
                selected.Add(bed);
            }
            finally
            {
                first.ClearReservationsForJob(chosen!);
                JobMaker.ReturnToPool(chosen!);
            }
        }
        Expect.AreEqual(2, selected.Count);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator OccupiedPartnerBedsAndEmptyDoubleBedsAllHaveAChance()
    {
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, third);
        StartPolyculeBedroomSleepJob(second, secondBed);
        StartPolyculeBedroomSleepJob(third, thirdBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed
            && third.CurrentBed() == thirdBed, 600);
        HashSet<Building_Bed> selected = [];
        for (int attempt = 0; attempt < 100; attempt++)
        {
            yield return new WaitTicks(1);
            using RandBlock random = new(13579 + attempt);
            Job? sleep = GetSleepJob(first);
            Expect.IsNotNull(sleep);
            try
            {
                Building_Bed bed = (Building_Bed)sleep!.targetA.Thing;
                Expect.IsTrue(bed == firstBed || bed == secondBed || bed == thirdBed);
                selected.Add(bed);
            }
            finally
            {
                first.ClearReservationsForJob(sleep!);
                JobMaker.ReturnToPool(sleep!);
            }
        }
        Expect.AreEqual(3, selected.Count);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator RejectedPartnerBedFallsBackWithoutLeavingBookings()
    {
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed, 600);
        CompForbiddable comp = new()
        {
            parent = secondBed,
            props = new CompProperties { compClass = typeof(CompForbiddable) }
        };
        secondBed.AllComps.Add(comp);
        secondBed.compForbiddable = comp;
        secondBed.SetForbidden(true, false);
        Job? sleep = GetSleepJob(first);
        Expect.IsNotNull(sleep);
        try
        {
            Building_Bed bed = (Building_Bed)sleep!.targetA.Thing;
            Expect.IsTrue(bed == firstBed || bed == thirdBed);
            Expect.IsFalse(secondBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
            Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        }
        finally
        {
            first.ClearReservationsForJob(sleep!);
            JobMaker.ReturnToPool(sleep!);
        }
    }

    [Test]
    public IEnumerator IndirectPartnerBedIsAvoided()
    {
        StartPolyculeBedroomSleepJob(third, thirdBed);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == thirdBed, 600);
        for (int attempt = 0; attempt < 20; attempt++)
        {
            yield return new WaitTicks(1);
            Job? chosen = GetSleepJob(first);
            Expect.ReferencesAreNotEqual(thirdBed, chosen?.targetA.Thing);
            if (chosen != null)
            {
                first.ClearReservationsForJob(chosen);
                JobMaker.ReturnToPool(chosen);
            }
        }
    }

    [Test]
    public IEnumerator BorrowerUsesFreeSlotWithoutChangingOwners()
    {
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed, 600);
        StartPolyculeBedroomSleepJob(first, secondBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed, 600);
        Expect.ReferencesAreEqual(secondBed, first.CurrentBed());
        Expect.AreNotEqual(first.Position, second.Position);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        Expect.AreEqual(1, secondBed.OwnersForReading.Count);
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, first, false));
    }

    [Test]
    public IEnumerator VanillaLovinFindsTheBorrowedBedPartner()
    {
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed, 600);
        StartPolyculeBedroomSleepJob(first, secondBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed, 600);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        Expect.ReferencesAreEqual(second, LovePartnerRelationUtility.GetPartnerInMyBed(first));
        Job? lovin = new JobGiver_DoLovin().TryIssueJobPackage(first, default).Job;
        Expect.IsNotNull(lovin);
        Expect.AreEqual(JobDefOf.Lovin, lovin?.def);
        Expect.ReferencesAreEqual(secondBed, lovin?.targetB.Thing);
    }

    [Test]
    public IEnumerator LovinInPartnerBedPreservesAssignments()
    {
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed, 600);
        StartPolyculeBedroomSleepJob(first, secondBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed, 600);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Lovin, second, secondBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurJobDef == JobDefOf.Lovin, 600);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        Expect.AreEqual(1, secondBed.OwnersForReading.Count);
        Expect.AreNotEqual(first.Position, second.Position);
        yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
            && second.CurJobDef == JobDefOf.LayDown
            && first.needs.mood.thoughts.memories.Memories
            .Any(memory => memory.def == ThoughtDefOf.GotSomeLovin), 6000);
        Expect.AreEqual(JobDefOf.LayDown, first.CurJobDef);
        Expect.AreEqual(JobDefOf.LayDown, second.CurJobDef);
        Expect.IsTrue(first.needs.mood.thoughts.memories.Memories
            .Any(memory => memory.def == ThoughtDefOf.GotSomeLovin));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator LovinInBorrowedBedPreservesOwnersAndSleepingSlots()
    {
        StartPolyculeBedroomSleepJob(second, thirdBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == thirdBed, 600);
        StartPolyculeBedroomSleepJob(first, thirdBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        IntVec3 firstSlot = first.Position;
        IntVec3 secondSlot = second.Position;
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Lovin, second, thirdBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurJobDef == JobDefOf.Lovin, 600);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
        Expect.AreEqual(1, thirdBed.OwnersForReading.Count);
        Expect.AreEqual(firstSlot, first.Position);
        Expect.AreEqual(secondSlot, second.Position);
        yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
            && second.CurJobDef == JobDefOf.LayDown
            && first.needs.mood.thoughts.memories.Memories
            .Any(memory => memory.def == ThoughtDefOf.GotSomeLovin), 6000);
        Expect.AreEqual(JobDefOf.LayDown, first.CurJobDef);
        Expect.AreEqual(JobDefOf.LayDown, second.CurJobDef);
        Expect.IsTrue(first.needs.mood.thoughts.memories.Memories
            .Any(memory => memory.def == ThoughtDefOf.GotSomeLovin));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator NightVisitorCanFinishLovinAndResumeSleep()
    {
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.ownership.ClaimBedIfNonMedical(visitBed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake(), 1200);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        first.jobs.StartJob(NightVisitHelpers.TryMakeVisitJob(first, visitBed)!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == visitBed, 1200);
        IntVec3 firstSlot = first.Position;
        IntVec3 secondSlot = second.Position;
        Expect.IsFalse(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, visitBed));
        Expect.AreEqual(JobDefOf.Lovin, first.CurJobDef);
        yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
            && second.CurJobDef == JobDefOf.LayDown
            && first.needs.mood.thoughts.memories.Memories
                .Any(memory => memory.def == ThoughtDefOf.GotSomeLovin), 6000);
        Expect.AreEqual(JobDefOf.LayDown, first.CurJobDef);
        Expect.AreEqual(JobDefOf.LayDown, second.CurJobDef);
        Expect.IsTrue(first.needs.mood.thoughts.memories.Memories
            .Any(memory => memory.def == ThoughtDefOf.GotSomeLovin));
        Expect.AreEqual(firstSlot, first.Position);
        Expect.AreEqual(secondSlot, second.Position);
        Expect.ReferencesAreEqual(visitBed, first.CurrentBed());
        Expect.ReferencesAreEqual(visitBed, second.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(visitBed, second.ownership.OwnedBed);
        Expect.IsNull(outsider.ownership.OwnedBed);
        Expect.AreEqual(1, visitBed.OwnersForReading.Count);
    }

    [Test]
    public IEnumerator RestJobGiverChoosesAndStartsPolyculeBedroomSleep()
    {
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed, 600);
        Job? selected = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
        Expect.IsNotNull(selected);
        Expect.AreEqual(JobDefOf.LayDown, selected?.def);
        Building_Bed chosenBed = (Building_Bed)selected!.targetA.Thing;
        Expect.IsTrue(chosenBed == firstBed || chosenBed == secondBed || chosenBed == thirdBed);
        first.jobs.StartJob(selected, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == chosenBed, 600);
        Expect.ReferencesAreEqual(chosenBed, first.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public void OtherBedSearchesDoNotBookSharedSleepingPlaces()
    {
        Expect.ReferencesAreEqual(firstBed, RestUtility.FindBedFor(first));
        foreach (Building_Bed bed in new[] { firstBed, secondBed, thirdBed })
        {
            Expect.IsFalse(first.HasReserved(bed));
            Expect.IsFalse(bed.GetComp<CompBedSharing>().HasTemporaryAssignments);
        }
    }

    [Test]
    public void RestJobGiverUsesGroundWhenNoBedroomBedIsAvailable()
    {
        foreach (Building_Bed bed in new[] { firstBed, secondBed, thirdBed })
        {
            CompForbiddable comp = new()
            {
                parent = bed,
                props = new CompProperties { compClass = typeof(CompForbiddable) }
            };
            bed.AllComps.Add(comp);
            bed.compForbiddable = comp;
            bed.SetForbidden(true);
            Expect.IsTrue(bed.IsForbidden(first));
        }
        Expect.IsNull(RestUtility.FindBedFor(first));
        Job? sleep = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
        Expect.IsNotNull(sleep);
        try
        {
            Expect.AreEqual(JobDefOf.LayDown, sleep!.def);
            Expect.IsFalse(sleep.targetA.HasThing);
            Expect.IsTrue(sleep.targetA.Cell.IsValid);
            foreach (Building_Bed bed in new[] { firstBed, secondBed, thirdBed })
            {
                Expect.IsFalse(bed.GetComp<CompBedSharing>().HasTemporaryAssignments);
            }
            Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        }
        finally
        {
            if (sleep != null)
            {
                first.ClearReservationsForJob(sleep);
                JobMaker.ReturnToPool(sleep);
            }
        }
    }

    [Test]
    public IEnumerator BedtimeDoesNotVisitAnotherBedroom()
    {
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.ownership.ClaimBedIfNonMedical(visitBed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake(), 1200);
        if (first.CurJob != null)
            first.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
        Expect.IsNull(first.CurrentBed());
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        int visits = 0;
        for (int attempt = 0; attempt < 100; attempt++)
        {
            yield return new WaitTicks(1);
            using RandBlock random = new(45678 + attempt);
            Job? job = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
            if (job?.targetA.Thing == visitBed)
                visits++;
            first.ClearReservationsForJob(job!);
            JobMaker.ReturnToPool(job!);
        }
        Expect.AreEqual(0, visits);
    }

    [Test]
    public IEnumerator SleepingPartnerCanWakeAndVisitAnotherBedroom()
    {
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.ownership.ClaimBedIfNonMedical(visitBed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake()
            && first.CurrentBed() == firstBed && !first.Awake(), 1200);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        Expect.IsTrue(NightVisitHelpers.CanLeaveBed(first));
        Job? visit = NightVisitHelpers.TryMakeVisitJob(first, visitBed);
        Expect.IsNotNull(visit);
        Expect.AreEqual(JobDefOf.Lovin, visit?.def);
        first.jobs.StartJob(visit!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == visitBed, 1200);
        Expect.ReferencesAreEqual(visitBed, first.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(visitBed, second.ownership.OwnedBed);
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator SleepingPawnCanVisitPartnerWhenCurrentBedmateIsNotRomantic(bool bedmateArrived)
    {
        // A non-romantic companion must not block departure for a night visit. In the incoming
        // case, start its actual travel after the host falls asleep: a reservation is not a travelling pawn.
        outsider.ownership.ClaimBedIfNonMedical(firstBed);
        second.ownership.ClaimBedIfNonMedical(visitBed);
        firstBed.GetComp<CompBedSharing>().nightVisitsEnabled = false;
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        Job bedmateSleep = JobMaker.MakeJob(JobDefOf.LayDown, firstBed);
        bool alwaysDoLovin = DebugSettings.alwaysDoLovin;
        try
        {
            if (bedmateArrived)
                outsider.jobs.StartJob(bedmateSleep, JobCondition.InterruptForced);
            yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
                && second.CurrentBed() == visitBed && !second.Awake()
                && (!bedmateArrived || outsider.CurrentBed() == firstBed && !outsider.Awake()), 1200);
            if (!bedmateArrived)
            {
                outsider.jobs.StartJob(bedmateSleep, JobCondition.InterruptForced);
                Expect.IsNull(outsider.CurrentBed());
            }
            Expect.IsFalse(LovePartnerRelationUtility.LovePartnerRelationExists(first, outsider));
            Expect.IsTrue(BedUtility.WillingToShareBed(first, outsider));
            Expect.IsTrue(firstBed.GetComp<CompBedSharing>().GetBedmates().Contains(outsider));
            Expect.IsTrue(NightVisitHelpers.CanLeaveBed(first));
            DebugSettings.alwaysDoLovin = true;
            first.mindState.canLovinTick = 0;
            second.mindState.canLovinTick = 0;
            yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.Lovin, 5500);
            Expect.AreEqual(JobDefOf.Lovin, first.CurJobDef);
            Expect.ReferencesAreEqual(second, first.CurJob.targetA.Thing);
            Expect.ReferencesAreEqual(visitBed, first.CurJob.targetB.Thing);
            yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
                && second.CurJobDef == JobDefOf.LayDown
                && first.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == ThoughtDefOf.GotSomeLovin), 6000);
            Expect.ReferencesAreEqual(visitBed, first.CurrentBed());
            Expect.ReferencesAreEqual(visitBed, second.CurrentBed());
            Expect.IsTrue(second.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == ThoughtDefOf.GotSomeLovin));
            // The incoming companion can finish its trip while the visitor is away.
            Expect.ReferencesAreEqual(firstBed, outsider.CurrentBed());
            Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
            Expect.ReferencesAreEqual(firstBed, outsider.ownership.OwnedBed);
            Expect.ReferencesAreEqual(visitBed, second.ownership.OwnedBed);
        }
        finally
        {
            DebugSettings.alwaysDoLovin = alwaysDoLovin;
        }
    }

    [Test]
    public IEnumerator ThinkTreeVisitStartsLovinAndResumesSleep()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        firstBed.GetComp<CompBedSharing>().nightVisitsEnabled = false;
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && second.CurrentBed() == visitBed && !second.Awake(), 1200);
        Expect.AreEqual(1, first.thinker.MainThinkNodeRoot.ThisAndChildrenRecursive.OfType<JobGiver_NightVisit>().Count());
        bool alwaysDoLovin = DebugSettings.alwaysDoLovin;
        try
        {
            DebugSettings.alwaysDoLovin = true;
            first.mindState.canLovinTick = 0;
            second.mindState.canLovinTick = 0;
            yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.Lovin, 5500);
            Expect.AreEqual(JobDefOf.Lovin, first.CurJobDef);
            Expect.ReferencesAreEqual(second, first.CurJob.targetA.Thing);
            Expect.ReferencesAreEqual(visitBed, first.CurJob.targetB.Thing);
            yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
                && second.CurJobDef == JobDefOf.LayDown
                && first.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == ThoughtDefOf.GotSomeLovin), 6000);
            Expect.ReferencesAreEqual(visitBed, first.CurrentBed());
            Expect.ReferencesAreEqual(visitBed, second.CurrentBed());
            Expect.IsTrue(second.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == ThoughtDefOf.GotSomeLovin));
            Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
            Expect.ReferencesAreEqual(visitBed, second.ownership.OwnedBed);
        }
        finally
        {
            DebugSettings.alwaysDoLovin = alwaysDoLovin;
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator NightVisitRequiresBothLovinCooldowns(bool visitorOnCooldown)
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && second.CurrentBed() == visitBed && !second.Awake(), 1200);
        first.mindState.canLovinTick = visitorOnCooldown ? int.MaxValue : 0;
        second.mindState.canLovinTick = visitorOnCooldown ? 0 : int.MaxValue;
        Expect.IsTrue(NightVisitHelpers.FindVisits(first, visitBed).Any());
        Expect.IsFalse(NightVisitHelpers.AreReadyForLovin(first, second));
        Expect.IsNull(new JobGiver_NightVisit().TryIssueJobPackage(first, default).Job);
    }

    [Test]
    public IEnumerator NightVisitsTryAtMostOncePerHour()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && second.CurrentBed() == visitBed && !second.Awake(), 1200);
        JobGiver_NightVisit node = new();
        Expect.IsNull(node.TryIssueJobPackage(first, default).Job);
        int attemptedAt = first.mindState.thinkData[node.UniqueSaveKey];
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        Expect.IsTrue(NightVisitHelpers.AreReadyForLovin(first, second));
        Expect.IsNull(node.TryIssueJobPackage(first, default).Job);
        Expect.AreEqual(attemptedAt, first.mindState.thinkData[node.UniqueSaveKey]);
    }

    [Test]
    public IEnumerator StarvationPreventsLovinVisit()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && second.CurrentBed() == visitBed && !second.Awake(), 1200);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        second.needs.food.CurLevel = 0f;
        Expect.IsFalse(NightVisitHelpers.AreReadyForLovin(first, second));
        Expect.LessThan(LovePartnerRelationUtility.GetLovinMtbHours(first, second), 0f);
        Expect.IsNull(new JobGiver_NightVisit().TryIssueJobPackage(first, default).Job);
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator NightVisitAbortsIfPartnerBecomesUnavailableDuringTravel(bool partnerLeaves)
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake(), 1200);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        Job? visit = NightVisitHelpers.TryMakeVisitJob(first, visitBed);
        Expect.IsNotNull(visit);
        first.jobs.StartJob(visit!, JobCondition.InterruptForced);
        Expect.IsNull(first.CurrentBed());
        int visitId = visit!.loadID;
        if (partnerLeaves)
            second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 1000), JobCondition.InterruptForced);
        else
            second.mindState.canLovinTick = int.MaxValue;
        yield return new WaitUntilTimeout(() => first.CurJob?.loadID != visitId, 600);
        Expect.AreNotEqual(JobDefOf.Lovin, first.CurJobDef);
        Expect.IsFalse(visitBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        Expect.IsFalse(first.jobs.jobQueue.Any(queued => queued.job.def == JobDefOf.LayDown
            && queued.job.targetA.Thing == visitBed));
        Expect.IsFalse(first.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == ThoughtDefOf.GotSomeLovin));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator BreakupCancelsVisitEvenWhenBedSharingRemainsAllowed()
    {
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, third);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, secondBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed && !second.Awake(), 1200);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        Job? visit = NightVisitHelpers.TryMakeVisitJob(first, secondBed);
        Expect.IsNotNull(visit);
        first.jobs.StartJob(visit!, JobCondition.InterruptForced);
        int visitId = visit!.loadID;
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        Expect.IsTrue(PolyculeHelpers.GetPolycule(first).Contains(second));
        Expect.IsTrue(BedSharingHelpers.CanSleepInSharedBed(first, secondBed));
        Expect.IsFalse(LovePartnerRelationUtility.LovePartnerRelationExists(first, second));
        yield return new WaitUntilTimeout(() => first.CurJob?.loadID != visitId, 600);
        Expect.AreNotEqual(JobDefOf.Lovin, first.CurJobDef);
        Expect.IsFalse(first.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == ThoughtDefOf.GotSomeLovin));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator RejectedVisitReleasesPartnerReservation()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake(), 1200);
        Job blocker = JobMaker.MakeJob(JobDefOf.Wait, 100);
        visitBed.Map.physicalInteractionReservationManager.Reserve(outsider, blocker, visitBed);
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, visitBed));
        Expect.IsFalse(visitBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        visitBed.Map.physicalInteractionReservationManager.Release(outsider, blocker, visitBed);
        JobMaker.ReturnToPool(blocker);
    }

    [Test]
    public IEnumerator DisabledVisitsDoNotMoveSleepingPartner()
    {
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.ownership.ClaimBedIfNonMedical(visitBed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake()
            && first.CurrentBed() == firstBed && !first.Awake(), 1200);
        visitBed.GetComp<CompBedSharing>().nightVisitsEnabled = false;
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, visitBed));
        Expect.IsNull(new JobGiver_NightVisit().TryIssueJobPackage(first, default).Job);
        Expect.ReferencesAreEqual(firstBed, first.CurrentBed());
    }

    [Test]
    public IEnumerator VisitorMayRemainWhenHostWakes()
    {
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.ownership.ClaimBedIfNonMedical(visitBed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake(), 1200);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        first.jobs.StartJob(NightVisitHelpers.TryMakeVisitJob(first, visitBed)!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == visitBed, 1200);
        second.jobs.curDriver.asleep = false;
        Expect.IsTrue(RestUtility.CanUseBedNow(visitBed, first, false));
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator ThreeSlotBedVisitChoosesReadyPartner(bool firstHostReady)
    {
        Building_Bed bed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        bed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(bed, new IntVec3(52, 0, 45), Find.CurrentMap);
        spawned.Add(bed);
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, third);
        second.ownership.ClaimBedIfNonMedical(bed);
        third.ownership.ClaimBedIfNonMedical(bed);
        firstBed.GetComp<CompBedSharing>().nightVisitsEnabled = false;
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        third.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && second.CurrentBed() == bed && !second.Awake() && third.CurrentBed() == bed && !third.Awake(), 1200);
        Expect.AreEqual(3, bed.SleepingSlotsCount);
        Expect.AreEqual(2, bed.CurOccupants.Count());
        Expect.IsTrue(bed.GetComp<CompBedSharing>().CompGetGizmosExtra().Any());
        Expect.AreEqual(2, NightVisitHelpers.FindVisits(first, bed).Count());

        Pawn readyHost = firstHostReady ? second : third;
        Pawn otherHost = firstHostReady ? third : second;
        bool alwaysDoLovin = DebugSettings.alwaysDoLovin;
        try
        {
            DebugSettings.alwaysDoLovin = true;
            first.mindState.canLovinTick = 0;
            readyHost.mindState.canLovinTick = 0;
            Job? visit = new JobGiver_NightVisit().TryIssueJobPackage(first, default).Job;
            Expect.IsNotNull(visit);
            Expect.ReferencesAreEqual(readyHost, visit!.targetA.Thing);
            Expect.ReferencesAreEqual(bed, visit.targetB.Thing);
            Expect.AreEqual(bed.GetSleepingSlotPos(2), RestUtility.GetBedSleepingSlotPosFor(first, bed));
            first.jobs.StartJob(visit, JobCondition.InterruptForced);
            yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
                && first.CurrentBed() == bed
                && first.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == ThoughtDefOf.GotSomeLovin), 6000);
            Expect.AreEqual(3, bed.CurOccupants.Count());
            Expect.ReferencesAreEqual(first, bed.GetCurOccupant(2));
            Expect.IsTrue(otherHost.CurrentBed() == bed && !otherHost.Awake());
            Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
            Expect.ReferencesAreEqual(bed, second.ownership.OwnedBed);
            Expect.ReferencesAreEqual(bed, third.ownership.OwnedBed);
        }
        finally
        {
            DebugSettings.alwaysDoLovin = alwaysDoLovin;
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator ThreeSlotBedVisitAllowsWillingNonPartner(bool bedmateArriving)
    {
        // A night visit requires one eligible romantic host and compatibility with each bedmate.
        // A willing non-partner may occupy or travel to its slot while the visitor takes the free slot.
        Building_Bed bed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        bed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(bed, new IntVec3(52, 0, 45), Find.CurrentMap);
        spawned.Add(bed);
        second.relations.AddDirectRelation(PawnRelationDefOf.Spouse, outsider);
        second.ownership.ClaimBedIfNonMedical(bed);
        outsider.ownership.ClaimBedIfNonMedical(bed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == bed && !second.Awake(), 1200);
        Job bedmateSleep = JobMaker.MakeJob(JobDefOf.LayDown, bed);
        outsider.jobs.StartJob(bedmateSleep, JobCondition.InterruptForced);
        if (!bedmateArriving)
        {
            yield return new WaitUntilTimeout(() => outsider.CurrentBed() == bed && !outsider.Awake(), 1200);
        }
        try
        {
            Expect.IsTrue(BedSharingHelpers.IsBedEligibleForNightVisits(bed));
            Expect.IsTrue(bed.GetComp<CompBedSharing>().GetBedmates().Contains(outsider));
            Expect.IsTrue(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, second));
            Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, outsider));
            Expect.IsTrue(BedUtility.WillingToShareBed(first, outsider));
            NightVisit[] visits = NightVisitHelpers.FindVisits(first, bed).ToArray();
            Expect.AreEqual(1, visits.Length);
            Expect.ReferencesAreEqual(second, visits[0].Partner);
            Job? visit = NightVisitHelpers.TryMakeVisitJob(first, bed);
            try
            {
                Expect.IsNotNull(visit);
                Expect.AreEqual(JobDefOf.Lovin, visit!.def);
                Expect.ReferencesAreEqual(second, visit.targetA.Thing);
                Expect.ReferencesAreEqual(bed, visit.targetB.Thing);
                Expect.AreEqual(bed.GetSleepingSlotPos(2), RestUtility.GetBedSleepingSlotPosFor(first, bed));
                Expect.IsTrue(visit.TryMakePreToilReservations(first, false));
                Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
                Expect.ReferencesAreEqual(bed, outsider.ownership.OwnedBed);
            }
            finally
            {
                if (visit != null)
                {
                    first.ClearReservationsForJob(visit);
                    JobMaker.ReturnToPool(visit);
                }
            }
        }
        finally
        {
            if (bedmateArriving)
            {
                outsider.ClearReservationsForJob(bedmateSleep);
                outsider.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
            }
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    [LoadIfIdeologyActive]
    public IEnumerator ThreeSlotBedVisitRejectsUnwillingBedmate(bool bedmateArriving)
    {
        // Night visits require compatibility with every bedmate as well as a romantic host.
        // An unwilling non-partner blocks the visit both as an occupant and as a traveller.
        outsider.ideo.SetIdeo(CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict"));
        Expect.IsFalse(BedUtility.WillingToShareBed(first, outsider));
        Building_Bed bed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        bed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(bed, new IntVec3(52, 0, 45), Find.CurrentMap);
        spawned.Add(bed);
        second.relations.AddDirectRelation(PawnRelationDefOf.Spouse, outsider);
        second.ownership.ClaimBedIfNonMedical(bed);
        outsider.ownership.ClaimBedIfNonMedical(bed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == bed && !second.Awake(), 1200);
        Job bedmateSleep = JobMaker.MakeJob(JobDefOf.LayDown, bed);
        outsider.jobs.StartJob(bedmateSleep, JobCondition.InterruptForced);
        if (!bedmateArriving)
        {
            yield return new WaitUntilTimeout(() => outsider.CurrentBed() == bed && !outsider.Awake(), 1200);
        }
        try
        {
            Expect.IsTrue(BedSharingHelpers.IsBedEligibleForNightVisits(bed));
            Expect.IsTrue(bed.GetComp<CompBedSharing>().GetBedmates().Contains(outsider));
            Expect.IsTrue(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, second));
            Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, outsider));
            Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(first, bed));
            Expect.IsFalse(NightVisitHelpers.FindVisits(first, bed).Any());
            Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, bed));
        }
        finally
        {
            if (bedmateArriving)
            {
                outsider.ClearReservationsForJob(bedmateSleep);
                outsider.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
            }
        }
    }

    [Test]
    public IEnumerator ThreeSlotBedKeepsLastPlaceForIncomingVisitor()
    {
        Building_Bed bed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        bed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(bed, new IntVec3(52, 0, 45), Find.CurrentMap);
        spawned.Add(bed);
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, third);
        foreach (Pawn partner in new[] { first, second, third })
            outsider.relations.AddDirectRelation(PawnRelationDefOf.Spouse, partner);
        second.ownership.ClaimBedIfNonMedical(bed);
        third.ownership.ClaimBedIfNonMedical(bed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        third.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, bed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == bed && !second.Awake()
            && third.CurrentBed() == bed && !third.Awake(), 1200);
        Expect.IsTrue(NightVisitHelpers.FindVisits(outsider, bed).Any());
        Job? incoming = NightVisitHelpers.TryMakeVisitJob(first, bed);
        Expect.IsNotNull(incoming);
        try
        {
            Expect.IsNull(first.CurrentBed());
            Expect.AreEqual(bed.GetSleepingSlotPos(2), RestUtility.GetBedSleepingSlotPosFor(first, bed));
            Expect.IsTrue(bed.AnyUnoccupiedSleepingSlot);
            Expect.IsTrue(bed.AnyUnownedSleepingSlot);
            Expect.IsFalse(NightVisitHelpers.FindVisits(outsider, bed).Any());
            Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(outsider, bed));
        }
        finally
        {
            first.ClearReservationsForJob(incoming!);
            JobMaker.ReturnToPool(incoming!);
        }
        yield return new WaitTicks(1);
        Expect.IsTrue(NightVisitHelpers.FindVisits(outsider, bed).Any());
    }

    [Test]
    public IEnumerator AbsentOwnersSlotCannotReceiveNightVisits()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake()
            && first.CurrentBed() == firstBed && !first.Awake(), 1200);
        Expect.IsFalse(visitBed.AnyUnownedSleepingSlot);
        Expect.IsTrue(visitBed.AnyUnoccupiedSleepingSlot);
        Expect.ReferencesAreEqual(visitBed, outsider.ownership.OwnedBed);
        Expect.IsFalse(NightVisitHelpers.FindVisits(first, visitBed).Any());
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, visitBed));
        Expect.IsNull(new JobGiver_NightVisit().TryIssueJobPackage(first, default).Job);
        Expect.IsTrue(visitBed.GetComp<CompBedSharing>().CompGetGizmosExtra().Any());
        for (int attempt = 0; attempt < 100; attempt++)
        {
            yield return new WaitTicks(1);
            using RandBlock random = new(45678 + attempt);
            Job? bedtime = new JobGiver_GetRest().TryIssueJobPackage(third, default).Job;
            Expect.ReferencesAreNotEqual(visitBed, bedtime?.targetA.Thing);
            third.ClearReservationsForJob(bedtime!);
            JobMaker.ReturnToPool(bedtime!);
        }
        Expect.ReferencesAreEqual(firstBed, first.CurrentBed());
    }

    [Test]
    public IEnumerator OccupiedUnassignedSlotDoesNotPermitVisitToAssignedSlot()
    {
        StartPolyculeBedroomSleepJob(first, thirdBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        StartPolyculeBedroomSleepJob(second, thirdBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == thirdBed && !second.Awake(), 1200);
        first.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
        first.ownership.ClaimBedIfNonMedical(visitBed);
        Expect.IsTrue(thirdBed.AnyUnownedSleepingSlot);
        Expect.IsNull(thirdBed.GetCurOccupant(0));
        Expect.ReferencesAreEqual(second, thirdBed.GetCurOccupant(1));
        Expect.IsFalse(NightVisitHelpers.FindVisits(first, thirdBed).Any());
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, thirdBed));
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator FullyAssignedPolyculeBedCanStillBeSharedAtBedtime()
    {
        third.ownership.ClaimBedIfNonMedical(secondBed);
        thirdBed.Destroy();
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed && !second.Awake(), 1200);
        Expect.IsFalse(secondBed.AnyUnownedSleepingSlot);
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, secondBed));
        Job? chosen = GetSleepJob(first);
        Expect.IsNotNull(chosen);
        Expect.IsTrue(chosen!.targetA.Thing == firstBed || chosen.targetA.Thing == secondBed);
        if (chosen != null)
        {
            first.ClearReservationsForJob(chosen);
            JobMaker.ReturnToPool(chosen);
        }
        yield return new WaitTicks(1);
        StartPolyculeBedroomSleepJob(first, secondBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed, 600);
        Expect.ReferencesAreEqual(secondBed, first.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, third.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator PartnerCanVisitHostInAnIndirectPartnersBed()
    {
        StartPolyculeBedroomSleepJob(second, thirdBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == thirdBed && !second.Awake(), 1200);
        first.ownership.ClaimBedIfNonMedical(visitBed);
        Expect.IsFalse(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        Job? visit = NightVisitHelpers.TryMakeVisitJob(first, thirdBed);
        Expect.ReferencesAreEqual(thirdBed, visit?.targetB.Thing);
        first.jobs.StartJob(visit!,
            JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 1200);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        second.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
        Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, first, false));
        Expect.ReferencesAreEqual(visitBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator TwoArrivalsUseDifferentSleepingSlots()
    {
        StartPolyculeBedroomSleepJob(first, secondBed);
        StartPolyculeBedroomSleepJob(second, secondBed);
        Expect.AreNotEqual(RestUtility.GetBedSleepingSlotPosFor(first, secondBed),
            RestUtility.GetBedSleepingSlotPosFor(second, secondBed));
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed
            && second.CurrentBed() == secondBed, 600);
        Expect.ReferencesAreEqual(secondBed, first.CurrentBed());
        Expect.ReferencesAreEqual(secondBed, second.CurrentBed());
    }

    [Test]
    public void MedicalBedsAreExcluded()
    {
        secondBed.Medical = true;
        Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(first, secondBed));
        Expect.IsFalse(secondBed.GetComp<CompBedSharing>().CompGetGizmosExtra().Any());
    }

    [Test]
    public void ForbiddenBedIsExcluded()
    {
        // Add forbidding support to test the behavior of beds from mods that allow it.
        CompForbiddable comp = new()
        {
            parent = secondBed,
            props = new CompProperties { compClass = typeof(CompForbiddable) }
        };
        secondBed.AllComps.Add(comp);
        secondBed.compForbiddable = comp;
        secondBed.SetForbidden(true, false);
        Expect.IsTrue(secondBed.IsForbidden(first));
        Expect.IsFalse(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, secondBed));
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, secondBed));
    }

    [Test]
    public void ToggleAppearsOnlyForRelevantAssignedBeds()
    {
        Expect.AreEqual(1, firstBed.GetComp<CompBedSharing>().CompGetGizmosExtra().Count());
        firstBed.CompAssignableToPawn.TryUnassignPawn(first);
        Expect.IsFalse(firstBed.GetComp<CompBedSharing>().CompGetGizmosExtra().Any());
    }

    [Test]
    public void ToggleSurvivesScribeRoundTrip()
    {
        CompBedSharing comp = firstBed.GetComp<CompBedSharing>();
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Yobai-toggle-test.xml");
        comp.nightVisitsEnabled = false;
        Scribe.saver.InitSaving(path, "YobaiTest");
        comp.PostExposeData();
        Scribe.saver.FinalizeSaving();
        comp.nightVisitsEnabled = true;
        Scribe.loader.InitLoading(path);
        comp.PostExposeData();
        Scribe.loader.FinalizeLoading();
        Expect.IsFalse(comp.nightVisitsEnabled);
        File.Delete(path);
    }

    [Test]
    public IEnumerator TemporaryAssignmentAllowsBedAccessBeforeJobStarts()
    {
        // Creation-tick grace permits vanilla validation before the selected job starts. The
        // next tick requires actual travel or occupancy; native reservation data cannot extend it.
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, first, true));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job pending = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        try
        {
            Expect.IsFalse(first.HasReserved(thirdBed));
            Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, first, true));
            Expect.IsTrue(RestUtility.IsValidBedFor(thirdBed, first, first, true));
            Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out int bookedSlot));
            Expect.AreEqual(thirdBed.GetSleepingSlotPos(bookedSlot), RestUtility.GetBedSleepingSlotPosFor(first, thirdBed));
            Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, outsider, true));
            Expect.IsTrue(pending.TryMakePreToilReservations(first, false));
            yield return new WaitTicks(1);
            Expect.ReferencesAreNotEqual(pending, first.CurJob);
            Expect.IsTrue(first.HasReserved(thirdBed));
            Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
            Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, first, true));
            Expect.IsFalse(RestUtility.IsValidBedFor(thirdBed, first, first, true));
        }
        finally
        {
            first.ClearReservationsForJob(pending);
            JobMaker.ReturnToPool(pending);
        }
    }

    [Test]
    public IEnumerator CancelledTripReleasesTemporaryAssignment()
    {
        StartPolyculeBedroomSleepJob(first, thirdBed);
        Expect.IsTrue(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        IntVec3 slot = RestUtility.GetBedSleepingSlotPosFor(first, thirdBed);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 100), JobCondition.InterruptForced);
        Expect.IsTrue(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        yield return new WaitTicks(1);
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, thirdBed));;
        Job replacement = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(replacement.TryMakePreToilReservations(second, false));
        Expect.AreEqual(slot, RestUtility.GetBedSleepingSlotPosFor(second, thirdBed));
        second.ClearReservationsForJob(replacement);
        JobMaker.ReturnToPool(replacement);
    }

    [Test]
    public IEnumerator ReservedSleepingCellDoesNotPreventBedSharing()
    {
        IntVec3 sleepingCell = thirdBed.GetSleepingSlotPos(0);
        Job blocker = JobMaker.MakeJob(JobDefOf.Wait, 100);
        Expect.IsTrue(outsider.Reserve(sleepingCell, blocker));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job sleep = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsNotNull(sleep);
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out int slot));
        Expect.AreEqual(0, slot);
        Expect.IsTrue(sleep.TryMakePreToilReservations(first, false));
        first.jobs.StartJob(sleep, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        Expect.AreEqual(sleepingCell, first.Position);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        outsider.ClearReservationsForJob(blocker);
        JobMaker.ReturnToPool(blocker);
    }

    [Test]
    public IEnumerator VanillaSleepUsesTemporaryAssignmentWithoutClaimingBed()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job booking = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(booking.TryMakePreToilReservations(first, false));
        Expect.AreEqual(JobDefOf.LayDown, booking.def);
        Expect.AreEqual(typeof(JobDriver_LayDown), booking.GetCachedDriver(first).GetType());
        Job vanillaSleep = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(vanillaSleep.TryMakePreToilReservations(first, false));
        IntVec3 bookedPlace = RestUtility.GetBedSleepingSlotPosFor(first, thirdBed);
        first.ClearReservationsForJob(booking);
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        JobMaker.ReturnToPool(booking);
        first.jobs.StartJob(vanillaSleep, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
        Expect.AreEqual(1, thirdBed.OwnersForReading.Count);
    }

    [Test]
    public IEnumerator NightVisitBookingUsesUnassignedSlotInsidePolyculeBedroom()
    {
        // The host borrows slot zero; the owner is elsewhere and slot one is unassigned.
        StartPolyculeBedroomSleepJob(second, thirdBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == thirdBed && !second.Awake(), 1200);
        Expect.ReferencesAreEqual(second, thirdBed.GetCurOccupant(0));
        Job? visit = NightVisitHelpers.TryMakeVisitJob(first, thirdBed);
        Expect.ReferencesAreEqual(thirdBed, visit?.targetB.Thing);
        Expect.AreEqual(thirdBed.GetSleepingSlotPos(1), RestUtility.GetBedSleepingSlotPosFor(first, thirdBed));
        first.ClearReservationsForJob(visit!);
        JobMaker.ReturnToPool(visit!);
    }

    [Test]
    public IEnumerator OccupiedBookingIsUnavailableToEveryCaller()
    {
        // Physical occupancy blocks every caller independently of the active assignment. Keep
        // the first pawn actually travelling so its temporary slot survives the creation tick.
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, secondBed));
        Job pending = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        first.jobs.StartJob(pending, JobCondition.InterruptForced);
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        Expect.IsFalse(RestUtility.CanUseBedNow(secondBed, second, true));
        // Let the owner settle before the traveller arrives, then simulate the obstruction.
        second.Position = secondBed.GetSleepingSlotPos(1);
        StartPolyculeBedroomSleepJob(second, secondBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == secondBed && !second.Awake(), 1200);
        Expect.ReferencesAreEqual(second, secondBed.GetCurOccupant(1));
        // Simulate an unexpected occupant entering the booked place after admission.
        second.Position = secondBed.GetSleepingSlotPos(0);
        Expect.ReferencesAreEqual(second, secondBed.GetCurOccupant(0));
        Expect.IsFalse(RestUtility.CanUseBedNow(secondBed, first, true));
        Expect.IsFalse(RestUtility.IsValidBedFor(secondBed, first, first, true));
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, secondBed));
        Expect.IsFalse(pending.TryMakePreToilReservations(first, false));
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, secondBed));
        first.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
    }

    [Test]
    public IEnumerator OccupiedSleepingSlotIsUnavailableToPermanentOwner()
    {
        StartPolyculeBedroomSleepJob(first, secondBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed && !first.Awake(), 1200);
        Expect.ReferencesAreEqual(first, secondBed.GetCurOccupant(0));
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(second, secondBed));
        Expect.IsTrue(secondBed.AnyUnoccupiedSleepingSlot);
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, first, true));
        Expect.IsFalse(RestUtility.CanUseBedNow(secondBed, second, true));
        Expect.IsFalse(RestUtility.IsValidBedFor(secondBed, second, second, true));
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator IncomingTemporarySleeperKeepsSlotFromPermanentOwner()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, secondBed));
        Job incoming = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.IsNotNull(incoming);
        Expect.IsNull(first.CurrentBed());
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, first, true));
        Expect.IsFalse(RestUtility.CanUseBedNow(secondBed, second, true));
        Expect.IsFalse(RestUtility.IsValidBedFor(secondBed, second, second, true));
        Job ownerSleep = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.IsFalse(ownerSleep.TryMakePreToilReservations(second, false));
        Expect.IsTrue(incoming.TryMakePreToilReservations(first, false));
        first.ClearReservationsForJob(incoming);
        yield return new WaitTicks(1);
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, second, true));
        JobMaker.ReturnToPool(ownerSleep);
        JobMaker.ReturnToPool(incoming);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public IEnumerator BedroomSleepPrefersOwnersAssignedSlot(int ownerSlot)
    {
        IntVec3 bedPosition = firstBed.Position;
        firstBed.Destroy();
        firstBed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        firstBed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(firstBed, bedPosition, Find.CurrentMap);
        spawned.Add(firstBed);
        Pawn[] owners = [first, second, third];
        foreach (Pawn owner in owners)
            owner.ownership.ClaimBedIfNonMedical(firstBed);
        secondBed.Destroy();
        thirdBed.Destroy();
        Pawn sleeper = owners[ownerSlot];
        Expect.IsTrue(firstBed.IsOwner(sleeper, out int? assignedSlot));
        Expect.AreEqual(ownerSlot, assignedSlot);
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        Expect.IsFalse(firstBed.CurOccupants.Any());
        Job? sleep = GetSleepJob(sleeper);
        Expect.IsNotNull(sleep);
        Expect.ReferencesAreEqual(firstBed, sleep!.targetA.Thing);
        Expect.AreEqual(firstBed.GetSleepingSlotPos(ownerSlot), RestUtility.GetBedSleepingSlotPosFor(sleeper, firstBed));
        sleeper.jobs.StartJob(sleep, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => sleeper.CurrentBed() == firstBed, 600);
        Expect.AreEqual(firstBed.GetSleepingSlotPos(ownerSlot), sleeper.Position);
        Expect.IsTrue(firstBed.IsOwner(sleeper, out int? unchangedSlot));
        Expect.AreEqual(ownerSlot, unchangedSlot);
        Expect.AreEqual(3, firstBed.OwnersForReading.Count);
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator BedroomOwnerUsesAnotherPlaceWhenAssignedSlotIsTaken(bool occupantArrived)
    {
        // A polycule owner whose slot is occupied or held by an incoming temporary sleeper uses
        // another eligible slot. A stay surviving later ticks requires actual travel, not just a reservation.
        IntVec3 bedPosition = firstBed.Position;
        firstBed.Destroy();
        firstBed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        firstBed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(firstBed, bedPosition, Find.CurrentMap);
        spawned.Add(firstBed);
        foreach (Pawn owner in new[] { first, second, third })
            owner.ownership.ClaimBedIfNonMedical(firstBed);
        secondBed.Destroy();
        thirdBed.Destroy();
        Job? occupantSleep = BedSharingJobHelpers.TryMakeSleepJob(first, new SleepingPlace(firstBed, 2));
        Expect.IsNotNull(occupantSleep);
        first.jobs.StartJob(occupantSleep!, JobCondition.InterruptForced);
        if (occupantArrived)
        {
            yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed, 600);
        }
        try
        {
            Expect.IsTrue(firstBed.IsOwner(third, out int? assignedSlot));
            Expect.AreEqual(2, assignedSlot);
            Expect.IsTrue(firstBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out int occupiedSlot));
            Expect.AreEqual(2, occupiedSlot);
            Job? sleep = GetSleepJob(third);
            Expect.IsNotNull(sleep);
            Expect.AreEqual(firstBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(third, firstBed));
            third.jobs.StartJob(sleep!, JobCondition.InterruptForced);
            yield return new WaitUntilTimeout(() => third.CurrentBed() == firstBed, 600);
            Expect.AreEqual(firstBed.GetSleepingSlotPos(0), third.Position);
            Expect.AreEqual(firstBed.GetSleepingSlotPos(2), RestUtility.GetBedSleepingSlotPosFor(first, firstBed));
            Expect.IsTrue(firstBed.IsOwner(third, out int? unchangedSlot));
            Expect.AreEqual(2, unchangedSlot);
        }
        finally
        {
            if (!occupantArrived)
            {
                first.ClearReservationsForJob(occupantSleep!);
                first.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
            }
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator NightVisitPrefersOwnersAssignedSlotWhenAvailable(bool ownPlaceAvailable)
    {
        IntVec3 bedPosition = firstBed.Position;
        firstBed.Destroy();
        firstBed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        firstBed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(firstBed, bedPosition, Find.CurrentMap);
        spawned.Add(firstBed);
        first.ownership.ClaimBedIfNonMedical(firstBed);
        second.ownership.ClaimBedIfNonMedical(firstBed);
        secondBed.Destroy();
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake(), 1200);
        Job? incoming = null;
        Job? visit = null;
        try
        {
            if (!ownPlaceAvailable)
            {
                incoming = BedSharingJobHelpers.TryMakeSleepJob(third, new SleepingPlace(firstBed, 1));
                Expect.IsNotNull(incoming);
                Expect.IsNull(third.CurrentBed());
            }
            Expect.IsTrue(firstBed.IsOwner(second, out int? assignedSlot));
            Expect.AreEqual(1, assignedSlot);
            visit = NightVisitHelpers.TryMakeVisitJob(second, firstBed);
            Expect.IsNotNull(visit);
            Expect.ReferencesAreEqual(first, visit!.targetA.Thing);
            Expect.AreEqual(firstBed.GetSleepingSlotPos(ownPlaceAvailable ? 1 : 2),
                RestUtility.GetBedSleepingSlotPosFor(second, firstBed));
            Expect.IsTrue(firstBed.IsOwner(second, out int? unchangedSlot));
            Expect.AreEqual(1, unchangedSlot);
            if (incoming != null)
            {
                Expect.AreEqual(firstBed.GetSleepingSlotPos(1), RestUtility.GetBedSleepingSlotPosFor(third, firstBed));
            }
        }
        finally
        {
            if (visit != null)
            {
                second.ClearReservationsForJob(visit);
                JobMaker.ReturnToPool(visit);
            }
            if (incoming != null)
            {
                third.ClearReservationsForJob(incoming);
                JobMaker.ReturnToPool(incoming);
            }
        }
    }

    [Test]
    public IEnumerator ReturningOwnerCanVisitWhenUnassignedPlaceIsBooked()
    {
        IntVec3 bedPosition = firstBed.Position;
        firstBed.Destroy();
        firstBed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Yobai_TestThreeSlotBed"), ThingDefOf.WoodLog);
        firstBed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(firstBed, bedPosition, Find.CurrentMap);
        spawned.Add(firstBed);
        first.ownership.ClaimBedIfNonMedical(firstBed);
        second.ownership.ClaimBedIfNonMedical(firstBed);
        secondBed.Destroy();
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake(), 1200);
        Job? incoming = BedSharingJobHelpers.TryMakeSleepJob(third, new SleepingPlace(firstBed, 2));
        Expect.IsNotNull(incoming);
        Job? visit = null;
        try
        {
            Expect.IsTrue(firstBed.GetComp<CompBedSharing>().IsSleepingPlaceAvailable(second, firstBed.GetSleepingSlotPos(1)));
            Expect.IsTrue(firstBed.AnyUnownedSleepingSlot);
            visit = NightVisitHelpers.TryMakeVisitJob(second, firstBed);
            Expect.IsNotNull(visit);
            Expect.AreEqual(firstBed.GetSleepingSlotPos(1), RestUtility.GetBedSleepingSlotPosFor(second, firstBed));
            Expect.AreEqual(firstBed.GetSleepingSlotPos(2), RestUtility.GetBedSleepingSlotPosFor(third, firstBed));
        }
        finally
        {
            if (visit != null)
            {
                second.ClearReservationsForJob(visit);
                JobMaker.ReturnToPool(visit);
            }
            third.ClearReservationsForJob(incoming!);
            JobMaker.ReturnToPool(incoming!);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator ReturningOwnerCanVisitFullyAssignedBedOnlyWhenOwnPlaceIsFree(bool ownPlaceAvailable)
    {
        second.ownership.ClaimBedIfNonMedical(firstBed);
        secondBed.Destroy();
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        StartPolyculeBedroomSleepJob(second, thirdBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && second.CurrentBed() == thirdBed && !second.Awake(), 1200);
        Job? incoming = null;
        try
        {
            if (!ownPlaceAvailable)
            {
                incoming = BedSharingJobHelpers.TryMakeSleepJob(third, new SleepingPlace(firstBed, 1));
                Expect.IsNotNull(incoming);
            }
            Expect.IsFalse(firstBed.AnyUnownedSleepingSlot);
            Expect.IsTrue(firstBed.GetComp<CompBedSharing>().CompGetGizmosExtra().Any());
            firstBed.GetComp<CompBedSharing>().nightVisitsEnabled = false;
            Expect.IsFalse(NightVisitHelpers.FindVisits(second, firstBed).Any());
            firstBed.GetComp<CompBedSharing>().nightVisitsEnabled = true;
            first.mindState.canLovinTick = 0;
            second.mindState.canLovinTick = 0;
            Job? visit = NightVisitHelpers.TryMakeVisitJob(second, firstBed);
            if (ownPlaceAvailable)
            {
                Expect.IsNotNull(visit);
                second.jobs.StartJob(visit!, JobCondition.InterruptForced);
                yield return new WaitUntilTimeout(() => second.CurrentBed() == firstBed, 1200);
                Expect.AreEqual(firstBed.GetSleepingSlotPos(1), second.Position);
                Expect.ReferencesAreEqual(firstBed, second.ownership.OwnedBed);
                Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
            }
            else
            {
                Expect.IsNull(visit);
                Expect.ReferencesAreEqual(thirdBed, second.CurrentBed());
                Expect.AreEqual(firstBed.GetSleepingSlotPos(1), RestUtility.GetBedSleepingSlotPosFor(third, firstBed));
            }
        }
        finally
        {
            if (incoming != null)
            {
                third.ClearReservationsForJob(incoming);
                JobMaker.ReturnToPool(incoming);
            }
        }
    }

    [Test]
    public IEnumerator IncomingOwnerKeepsAssignedSleepingSlot()
    {
        // An owner travelling to its assigned slot keeps that place against polycule bedroom
        // borrowers. Cancelling the trip makes the otherwise eligible slot available for borrowing.
        Job ownerSleep = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        second.jobs.StartJob(ownerSleep, JobCondition.InterruptForced);
        Expect.IsNull(second.CurrentBed());
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, second, true));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, secondBed));
        Job incoming = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.AreEqual(secondBed.GetSleepingSlotPos(1), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, first, true));
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, second, true));
        first.ClearReservationsForJob(incoming);
        second.ClearReservationsForJob(ownerSleep);
        JobMaker.ReturnToPool(incoming);
        second.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
        yield return new WaitTicks(1);
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, secondBed));
        Job replacement = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.IsNotNull(replacement);
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        first.ClearReservationsForJob(replacement);
        JobMaker.ReturnToPool(replacement);
    }

    [Test]
    public void IncomingOwnerAllowsWillingNonPartners()
    {
        // An arriving owner counts as a bedmate. Vanilla-permitted non-partners may share an
        // available place, but willingness alone does not grant access to polycule bedroom selection.
        Expect.IsFalse(LovePartnerRelationUtility.LovePartnerRelationExists(outsider, second));
        Expect.IsTrue(BedUtility.WillingToShareBed(outsider, second));
        Job ownerSleep = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        try
        {
            second.jobs.StartJob(ownerSleep, JobCondition.InterruptForced);
            Expect.IsNull(second.CurrentBed());
            CompBedSharing assignments = secondBed.GetComp<CompBedSharing>();
            Expect.IsTrue(assignments.GetBedmates().Contains(second));
            Expect.IsTrue(BedSharingHelpers.CanSleepInSharedBed(outsider, secondBed));
            Expect.IsTrue(assignments.CanAssignSleepingSlot(outsider, 1));
            Expect.IsFalse(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(outsider, secondBed));
        }
        finally
        {
            second.ClearReservationsForJob(ownerSleep);
            second.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
        }
    }

    [Test, LoadIfIdeologyActive]
    public void IncomingOwnerCountsAsBedmate()
    {
        // An arriving owner counts for bedmate compatibility. A pawn whose ideology forbids
        // sharing must be rejected even before the owner physically reaches the bed.
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        outsider.ideo.SetIdeo(ideo);
        Expect.IsFalse(BedUtility.WillingToShareBed(outsider, second));
        Job ownerSleep = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        second.jobs.StartJob(ownerSleep, JobCondition.InterruptForced);
        Expect.IsNull(second.CurrentBed());
        Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(outsider, secondBed));
        Expect.IsFalse(secondBed.GetComp<CompBedSharing>().CanAssignSleepingSlot(outsider, 1));
        Expect.IsTrue(BedSharingHelpers.CanSleepInSharedBed(first, secondBed));
        second.ClearReservationsForJob(ownerSleep);
        second.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
    }

    [Test]
    public void UnrelatedOwnerReservationDoesNotOccupySleepingSlot()
    {
        Job repair = JobMaker.MakeJob(JobDefOf.Repair, secondBed);
        Expect.IsTrue(second.Reserve(secondBed, repair, secondBed.SleepingSlotsCount, 0));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, secondBed));
        Job incoming = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.IsNotNull(incoming);
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        first.ClearReservationsForJob(incoming);
        second.ClearReservationsForJob(repair);
        JobMaker.ReturnToPool(incoming);
        JobMaker.ReturnToPool(repair);
    }

    [TestCase(false)]
    [TestCase(true)]
    public IEnumerator BreakupPreservesWillingBedmates(bool firstArrived)
    {
        // Breaking a romance does not revoke vanilla-permitted bed sharing. The first stay
        // uses actual travel or occupancy; the second is selected in the same tick as the breakup,
        // so its creation-tick grace covers the interval before its job is accepted.
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job firstArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        first.jobs.StartJob(firstArrival, JobCondition.InterruptForced);
        if (firstArrived)
            yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        else
        {
            yield return new WaitTicks(1);
            Expect.IsNull(first.CurrentBed());
        }
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, thirdBed));
        Job secondArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        try
        {
            Expect.IsTrue(secondArrival.TryMakePreToilReservations(second, false));
            first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
            Expect.IsFalse(LovePartnerRelationUtility.LovePartnerRelationExists(first, second));
            Expect.IsTrue(BedUtility.WillingToShareBed(first, second));
            Expect.IsNull(second.CurrentBed());
            Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, first, true));
            Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, second, true));
            Expect.IsTrue(RestUtility.IsValidBedFor(thirdBed, first, first, true));
            Expect.IsTrue(RestUtility.IsValidBedFor(thirdBed, second, second, true));
            Expect.IsTrue(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
            Expect.IsTrue(BedSharingHelpers.CanUseTemporarySleepingSlot(second, thirdBed));
            Expect.IsTrue(secondArrival.TryMakePreToilReservations(second, false));
            Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
            Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        }
        finally
        {
            second.ClearReservationsForJob(secondArrival);
            JobMaker.ReturnToPool(secondArrival);
        }
    }

    [Test, LoadIfIdeologyActive]
    public IEnumerator RelationshipChangeRejectsIncompatibleArrivalsBeforeEitherArrives()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        first.ideo.SetIdeo(ideo);
        Expect.IsTrue(BedUtility.WillingToShareBed(first, second));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job firstArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, thirdBed));
        Job secondArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(firstArrival.TryMakePreToilReservations(first, false));
        Expect.IsTrue(secondArrival.TryMakePreToilReservations(second, false));
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        Expect.IsFalse(BedUtility.WillingToShareBed(first, second));
        Expect.IsNull(first.CurrentBed());
        Expect.IsNull(second.CurrentBed());
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, first, true));
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, second, true));
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(second, thirdBed));
        Expect.IsFalse(firstArrival.TryMakePreToilReservations(first, false));
        yield return new WaitTicks(1);
        Expect.IsTrue(secondArrival.TryMakePreToilReservations(second, false));
        second.ClearReservationsForJob(secondArrival);
        JobMaker.ReturnToPool(firstArrival);
        JobMaker.ReturnToPool(secondArrival);
    }

    [Test]
    public IEnumerator IncomingPartnerAndEmptyDoubleBedsAllHaveAChance()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, secondBed));
        Job incoming = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.IsTrue(incoming.TryMakePreToilReservations(second, false));
        Expect.IsNull(second.CurrentBed());
        HashSet<Building_Bed> selected = [];
        try
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                yield return new WaitTicks(1);
                using RandBlock random = new(13579 + attempt);
                Job? sleep = GetSleepJob(first);
                Expect.IsNotNull(sleep);
                try
                {
                    Building_Bed bed = (Building_Bed)sleep!.targetA.Thing;
                    Expect.IsTrue(bed == firstBed || bed == secondBed || bed == thirdBed);
                    selected.Add(bed);
                }
                finally
                {
                    first.ClearReservationsForJob(sleep!);
                    JobMaker.ReturnToPool(sleep!);
                }
            }
            Expect.AreEqual(3, selected.Count);
            Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        }
        finally
        {
            second.ClearReservationsForJob(incoming);
            JobMaker.ReturnToPool(incoming);
        }
    }

    [Test]
    public IEnumerator SleepingHostStaysForIncomingPartner()
    {
        first.relations.AddDirectRelation(PawnRelationDefOf.Lover, third);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        third.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, thirdBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && third.CurrentBed() == thirdBed && !third.Awake(), 1200);
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, firstBed));
        Job incoming = JobMaker.MakeJob(JobDefOf.LayDown, firstBed);
        Expect.IsNull(second.CurrentBed());
        Expect.IsFalse(NightVisitHelpers.CanLeaveBed(first));
        Expect.ReferencesAreEqual(firstBed, first.CurrentBed());
        second.ClearReservationsForJob(incoming);
        JobMaker.ReturnToPool(incoming);
    }

    [Test]
    public IEnumerator SavingDiscardsCancelledSleepingAssignments()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job sleep = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        first.ClearReservationsForJob(sleep);
        JobMaker.ReturnToPool(sleep);
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().HasTemporaryAssignments);
        yield return new WaitTicks(1);
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Yobai-cancelled-assignment-test.xml");
        Scribe.saver.InitSaving(path, "YobaiTest");
        thirdBed.GetComp<CompBedSharing>().PostExposeData();
        Scribe.saver.FinalizeSaving();
        System.Xml.XmlDocument saved = new();
        saved.Load(path);
        Expect.AreEqual(0, saved.SelectNodes("//Yobai_TemporarySleepingSlots/li")!.Count);
        File.Delete(path);
    }

    [Test]
    public IEnumerator TemporaryAssignmentSurvivesScribeRoundTrip()
    {
        // Save/load must preserve an actively used temporary slot without changing ownership.
        // Creation-tick grace is not saved; actual travel keeps the restored assignment active
        // during this comp-only round trip.
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job booking = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        first.jobs.StartJob(booking, JobCondition.InterruptForced);
        yield return new WaitTicks(1);
        IntVec3 slot = RestUtility.GetBedSleepingSlotPosFor(first, thirdBed);
        CompBedSharing comp = thirdBed.GetComp<CompBedSharing>();
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Yobai-assignment-test.xml");
        Scribe.saver.InitSaving(path, "YobaiTest");
        comp.PostExposeData();
        Scribe.saver.FinalizeSaving();
        comp.RemoveAssignment(comp.TemporaryAssignments.Single());
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        Scribe.loader.InitLoading(path);
        comp.PostExposeData();
        // A colony load registers its pawns before resolving references. This comp-only load
        // reuses the live pawn instead of loading a second copy into the test map.
        LoadedObjectDirectory directory = (LoadedObjectDirectory)typeof(CrossRefHandler)
            .GetField("loadedObjectDirectory", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
        directory.RegisterLoaded(first);
        Scribe.loader.FinalizeLoading();
        Expect.IsTrue(comp.TryGetSleepingSlot(first, out int restoredSlot));
        Expect.AreEqual(slot, thirdBed.GetSleepingSlotPos(restoredSlot));
        Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, first, true));
        File.Delete(path);
    }

    [Test]
    public void OrdinarySleepDoesNotRequireATemporaryAssignment()
    {
        Job sleep = JobMaker.MakeJob(JobDefOf.LayDown, firstBed);
        Expect.IsTrue(sleep.TryMakePreToilReservations(first, false));
        Expect.IsFalse(firstBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        first.ClearReservationsForJob(sleep);
        JobMaker.ReturnToPool(sleep);
    }

    [Test]
    public void OrdinaryGroundSleepStillReservesItsCell()
    {
        IntVec3 spot = first.Position;
        Job sleep = JobMaker.MakeJob(JobDefOf.LayDown, spot);
        Expect.IsTrue(sleep.TryMakePreToilReservations(first, false));
        first.ClearReservationsForJob(sleep);
        JobMaker.ReturnToPool(sleep);
    }

    [Test]
    public void VanillaReservationFailureDoesNotReleaseAnotherBooking()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job booking = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        IntVec3 bookedPlace = RestUtility.GetBedSleepingSlotPosFor(first, thirdBed);
        Job blocker = JobMaker.MakeJob(JobDefOf.Wait, 100);
        // A physical interaction blocks new reservations even when this pawn already booked the bed.
        first.Map.physicalInteractionReservationManager.Reserve(outsider, blocker, thirdBed);
        Expect.IsFalse(first.CanReserve(thirdBed, thirdBed.SleepingSlotsCount, 0));
        Job continuation = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsFalse(continuation.TryMakePreToilReservations(first, false));
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        first.ClearReservationsForJob(booking);
        first.Map.physicalInteractionReservationManager.Release(outsider, blocker, thirdBed);
        JobMaker.ReturnToPool(booking);
        JobMaker.ReturnToPool(blocker);
        JobMaker.ReturnToPool(continuation);
    }

    [Test]
    public IEnumerator ContinuingVisitDoesNotRepeatAdmissionRules()
    {
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        second.ownership.ClaimBedIfNonMedical(visitBed);
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == visitBed && !second.Awake(), 1200);
        first.mindState.canLovinTick = 0;
        second.mindState.canLovinTick = 0;
        Job? visit = NightVisitHelpers.TryMakeVisitJob(first, visitBed);
        Expect.IsNotNull(visit);
        second.jobs.curDriver.asleep = false;
        visitBed.GetComp<CompBedSharing>().nightVisitsEnabled = false;
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(third, visitBed));
        Expect.IsTrue(visit!.TryMakePreToilReservations(first, false));
        first.jobs.StartJob(visit, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == visitBed, 1200);
        Expect.ReferencesAreEqual(visitBed, first.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test, LoadIfIdeologyActive]
    public void TravellersMustBeCompatibleBedmates()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        outsider.ideo.SetIdeo(ideo);
        Expect.IsFalse(BedUtility.WillingToShareBed(outsider, first));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job firstArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsNotNull(firstArrival);
        Expect.IsNull(first.CurrentBed());
        Expect.IsNull(BedSharingJobHelpers.TryMakeSleepJob(outsider, new SleepingPlace(thirdBed, 1)));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, thirdBed));
        Job partnerArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.AreNotEqual(RestUtility.GetBedSleepingSlotPosFor(first, thirdBed),
            RestUtility.GetBedSleepingSlotPosFor(second, thirdBed));
        first.ClearReservationsForJob(firstArrival);
        second.ClearReservationsForJob(partnerArrival!);
        JobMaker.ReturnToPool(firstArrival);
        JobMaker.ReturnToPool(partnerArrival!);
    }

    [Test]
    public void UnreservableBedDoesNotCreateASleepJob()
    {
        Job blocker = JobMaker.MakeJob(JobDefOf.Wait, 100);
        first.Map.physicalInteractionReservationManager.Reserve(outsider, blocker, thirdBed);
        try
        {
            Expect.IsFalse(first.CanReserve(thirdBed, thirdBed.SleepingSlotsCount, 0));
            Expect.IsFalse(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
            Job next = JobMaker.MakeJob(JobDefOf.Wait, 100);
            try
            {
                Expect.AreEqual(blocker.loadID + 1, next.loadID);
                Expect.IsFalse(thirdBed.GetComp<CompBedSharing>().HasTemporaryAssignments);
            }
            finally
            {
                JobMaker.ReturnToPool(next);
            }
        }
        finally
        {
            first.Map.physicalInteractionReservationManager.Release(outsider, blocker, thirdBed);
            JobMaker.ReturnToPool(blocker);
        }
    }

    [Test]
    public void UnreservablePartnerDoesNotCreateALovinJob()
    {
        Job blocker = JobMaker.MakeJob(JobDefOf.Wait, 100);
        first.Map.physicalInteractionReservationManager.Reserve(outsider, blocker, second);
        try
        {
            Expect.IsFalse(first.CanReserve(second));
            Expect.IsTrue(first.CanReserve(secondBed, secondBed.SleepingSlotsCount, 0));
            Expect.IsNull(BedSharingJobHelpers.TryMakeLovinJob(first, second, new SleepingPlace(secondBed, 1)));
            Job next = JobMaker.MakeJob(JobDefOf.Wait, 100);
            try
            {
                Expect.AreEqual(blocker.loadID + 1, next.loadID);
                Expect.IsFalse(secondBed.GetComp<CompBedSharing>().HasTemporaryAssignments);
            }
            finally
            {
                JobMaker.ReturnToPool(next);
            }
        }
        finally
        {
            first.Map.physicalInteractionReservationManager.Release(outsider, blocker, second);
            JobMaker.ReturnToPool(blocker);
        }
    }

    [Test]
    public void RejectedSecondAssignmentPreservesTheExistingStay()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job original = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        CompBedSharing assignments = thirdBed.GetComp<CompBedSharing>();
        Expect.IsTrue(assignments.TryGetSleepingSlot(first, out int originalSlot));
        Expect.IsNull(BedSharingJobHelpers.TryMakeSleepJob(first, new SleepingPlace(thirdBed, 1 - originalSlot)));
        Expect.IsTrue(assignments.TryGetSleepingSlot(first, out int keptSlot));
        Expect.AreEqual(originalSlot, keptSlot);
        Expect.IsTrue(original.TryMakePreToilReservations(first, false));
        first.ClearReservationsForJob(original);
        JobMaker.ReturnToPool(original);
    }

    [Test]
    public void AutomaticSleepCannotClaimAnIneligibleTemporaryBed()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job booking = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsNotNull(booking);
        first.drafter.Drafted = true;
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, thirdBed), JobCondition.InterruptForced,
            preToilReservationsCanFail: true);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
        Expect.AreEqual(1, thirdBed.OwnersForReading.Count);
        first.ClearReservationsForJob(booking);
        JobMaker.ReturnToPool(booking);
    }

    [Test, LoadIfIdeologyActive]
    public IEnumerator RelationshipChangeCannotAdmitIncompatibleBookedBedmates()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        first.ideo.SetIdeo(ideo);
        Expect.IsTrue(BedUtility.WillingToShareBed(first, second));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job firstArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, thirdBed));
        Job secondArrival = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        first.jobs.StartJob(firstArrival, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        Expect.IsFalse(BedUtility.WillingToShareBed(first, second));
        // Second remains the permanent owner's spouse, but First's ideology now forbids
        // sharing with Second. The owner's permission cannot override that restriction.
        Expect.IsTrue(PolyculeHelpers.AreLovePartnersWillingToShareBed(second, third));
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, second, true));
        Expect.IsFalse(RestUtility.IsValidBedFor(thirdBed, second, second, true));
        Expect.IsFalse(secondArrival.TryMakePreToilReservations(second, false));
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        JobMaker.ReturnToPool(secondArrival);
    }

    [Test]
    public IEnumerator ReturningOwnerPrefersEmptyBedToIndirectPartner()
    {
        StartPolyculeBedroomSleepJob(first, thirdBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, third));
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, third, true));
        Expect.IsFalse(RestUtility.IsValidBedFor(thirdBed, third, third, true));
        Job? sleep = new JobGiver_GetRest().TryIssueJobPackage(third, default).Job;
        Expect.IsNotNull(sleep);
        Expect.ReferencesAreNotEqual(thirdBed, sleep?.targetA.Thing);
        third.jobs.StartJob(sleep!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => third.CurrentBed() != null, 600);
        Expect.IsNotNull(third.CurrentBed());
        Expect.ReferencesAreNotEqual(thirdBed, third.CurrentBed());
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator GroundSleepFallbackStillFindsAPolyculeBed()
    {
        // Vanilla cannot use either forbidden bed or the indirect partner's assigned bed.
        foreach (Building_Bed bed in new[] { firstBed, secondBed })
        {
            CompForbiddable comp = new()
            {
                parent = bed,
                props = new CompProperties { compClass = typeof(CompForbiddable) }
            };
            bed.AllComps.Add(comp);
            bed.compForbiddable = comp;
            bed.SetForbidden(true, false);
        }
        Expect.IsFalse(RestUtility.IsValidBedFor(thirdBed, first, first, true));
        Job? sleep = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
        Expect.IsNotNull(sleep);
        Expect.AreEqual(JobDefOf.LayDown, sleep?.def);
        Expect.ReferencesAreEqual(thirdBed, sleep?.targetA.Thing);
        first.jobs.StartJob(sleep!,
            JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public void CaravanWaitingDoesNotReplaceGroundSleepWithABed()
    {
        Lord caravanGroup = LordMaker.MakeNewLord(Faction.OfPlayer,
            new LordJob_DefendPoint(first.Position), first.Map, new[] { first });
        try
        {
            // The graph is already initialized, so a newly added state needs its owner set.
            LordToil_PrepareCaravan_Wait waiting = new(first.Position) { lord = caravanGroup };
            caravanGroup.Graph.AddToil(waiting);
            caravanGroup.GotoToil(waiting);
            Expect.IsFalse(caravanGroup.CurLordToil.AllowRestingInBed);
            Job? sleep = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
            Expect.IsNotNull(sleep);
            Expect.AreEqual(JobDefOf.LayDown, sleep?.def);
            Expect.IsFalse(sleep!.targetA.HasThing);
            Expect.IsFalse(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, secondBed));
            JobMaker.ReturnToPool(sleep);
        }
        finally
        {
            first.Map.lordManager.RemoveLord(caravanGroup);
        }
    }

    [Test]
    public void MedicalConversionEndsTemporaryAssignments()
    {
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job booking = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().HasTemporaryAssignments);
        thirdBed.Medical = true;
        Expect.IsFalse(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        // With the assignment gone, this is an ordinary vanilla sleep job again.
        Expect.IsTrue(booking!.TryMakePreToilReservations(first, false));
        first.ClearReservationsForJob(booking);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        JobMaker.ReturnToPool(booking);
    }

    [Test, LoadIfBiotechActive]
    public void ChildMayShareBedroomWithPolyculeParents()
    {
        Pawn child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, fixedBiologicalAge: 8, fixedChronologicalAge: 8,
            canGeneratePawnRelations: false, developmentalStages: DevelopmentalStage.Child));
        if (ModsConfig.IdeologyActive)
            child.ideo.SetIdeo(null);
        child.relations.AddDirectRelation(PawnRelationDefOf.Parent, first);
        GenSpawn.Spawn(child, new IntVec3(39, 0, 42), first.Map);
        spawned.Add(child);
        Building_Bed childBed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Bed"), ThingDefOf.WoodLog);
        childBed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(childBed, new IntVec3(38, 0, 41), first.Map);
        spawned.Add(childBed);
        childBed.CompAssignableToPawn.TryAssignPawn(child);
        Expect.IsFalse(PolyculeHelpers.GetPolycule(first).Contains(child));
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        child.relations.RemoveDirectRelation(PawnRelationDefOf.Parent, first);
        child.relations.AddDirectRelation(PawnRelationDefOf.Parent, outsider);
        firstBed.NotifyRoomAssignedPawnsChanged();
        Expect.AreEqual(RoomRoleDefOf.Barracks, firstBed.GetRoom().Role);
        child.relations.RemoveDirectRelation(PawnRelationDefOf.Parent, outsider);
        child.relations.AddDirectRelation(PawnRelationDefOf.Parent, second);
        firstBed.NotifyRoomAssignedPawnsChanged();
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
    }

    [Test, LoadIfBiotechActive]
    public void UnrelatedChildDoesNotQualifyForPolyculeBedroom()
    {
        Pawn child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, fixedBiologicalAge: 8, fixedChronologicalAge: 8,
            canGeneratePawnRelations: false, developmentalStages: DevelopmentalStage.Child));
        if (ModsConfig.IdeologyActive)
            child.ideo.SetIdeo(null);
        child.relations.AddDirectRelation(PawnRelationDefOf.Parent, outsider);
        GenSpawn.Spawn(child, new IntVec3(39, 0, 42), first.Map);
        spawned.Add(child);
        Building_Bed childBed = (Building_Bed)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Bed"), ThingDefOf.WoodLog);
        childBed.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(childBed, new IntVec3(38, 0, 41), first.Map);
        spawned.Add(childBed);
        childBed.CompAssignableToPawn.TryAssignPawn(child);
        Expect.AreEqual(RoomRoleDefOf.Barracks, firstBed.GetRoom().Role);
    }

    [Test]
    public void ResurrectionRestoresConnectionBetweenBedroomOwners()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        secondBed.Destroy();
        second.Position = new IntVec3(52, 0, 43);
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        second.Kill(null);
        if (second.Corpse != null)
            spawned.Add(second.Corpse);
        Expect.AreEqual(1, PolyculeHelpers.GetPolycule(first).Count);
        firstBed.NotifyRoomAssignedPawnsChanged();
        Expect.AreEqual(RoomRoleDefOf.Barracks, firstBed.GetRoom().Role);
        Expect.IsTrue(ResurrectionUtility.TryResurrect(second));
        Expect.IsFalse(second.Dead);
        Expect.AreEqual(3, PolyculeHelpers.GetPolycule(first).Count);
        firstBed.NotifyRoomAssignedPawnsChanged();
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
    }

    [Test]
    public IEnumerator LastResortSharingPreservesBedsAndDoesNotEnableLovin()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        third.ownership.ClaimBedIfNonMedical(firstBed);
        secondBed.Destroy();
        thirdBed.Destroy();
        StartPolyculeBedroomSleepJob(third, firstBed);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == firstBed, 600);
        Job? sleep = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
        Expect.IsNotNull(sleep);
        Expect.AreEqual(JobDefOf.LayDown, sleep?.def);
        Expect.ReferencesAreEqual(firstBed, sleep?.targetA.Thing);
        first.jobs.StartJob(sleep!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed, 600);
        Expect.ReferencesAreEqual(firstBed, first.CurrentBed());
        Expect.AreNotEqual(first.Position, third.Position);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(firstBed, third.ownership.OwnedBed);
        Expect.AreEqual(2, firstBed.OwnersForReading.Count);
        Expect.IsTrue(RestUtility.CanUseBedNow(firstBed, first, false));
        first.mindState.canLovinTick = 0;
        third.mindState.canLovinTick = 0;
        Expect.IsNull(LovePartnerRelationUtility.GetPartnerInMyBed(first));
        Expect.IsNull(new JobGiver_DoLovin().TryIssueJobPackage(first, default).Job);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
            && first.CurrentBed() == firstBed, 600);
        Expect.ReferencesAreEqual(firstBed, first.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public void LastResortSharingIncludesIncomingPolyculeMembers()
    {
        second.ownership.ClaimBedIfNonMedical(visitBed);
        third.ownership.ClaimBedIfNonMedical(firstBed);
        secondBed.Destroy();
        thirdBed.Destroy();
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(third, firstBed));
        Job incoming = JobMaker.MakeJob(JobDefOf.LayDown, firstBed);
        Expect.IsNotNull(incoming);
        Expect.IsNull(third.CurrentBed());
        Job? sleep = GetSleepJob(first);
        Expect.IsNotNull(sleep);
        Expect.ReferencesAreEqual(firstBed, sleep?.targetA.Thing);
        Expect.AreNotEqual(RestUtility.GetBedSleepingSlotPosFor(first, firstBed),
            RestUtility.GetBedSleepingSlotPosFor(third, firstBed));
        Expect.IsTrue(sleep!.TryMakePreToilReservations(first, false));
        Expect.IsTrue(incoming!.TryMakePreToilReservations(third, false));
        first.ClearReservationsForJob(sleep);
        third.ClearReservationsForJob(incoming);
        JobMaker.ReturnToPool(sleep);
        JobMaker.ReturnToPool(incoming);
    }

    [Test]
    public IEnumerator EmptyBedsArePreferredToIncomingNonPartners()
    {
        // Bedtime prefers empty double beds over a non-romantic polycule companion.
        // A companion travelling to or occupying its slot counts as a bedmate for that preference.
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(third, thirdBed));
        Job incoming = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        third.jobs.StartJob(incoming, JobCondition.InterruptForced);
        for (int attempt = 0; attempt < 20; attempt++)
        {
            yield return new WaitTicks(1);
            Job? sleep = GetSleepJob(first);
            Expect.IsNotNull(sleep);
            Expect.ReferencesAreNotEqual(thirdBed, sleep?.targetA.Thing);
            first.ClearReservationsForJob(sleep!);
            JobMaker.ReturnToPool(sleep!);
        }
        third.ClearReservationsForJob(incoming);
        third.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
    }

    [Test]
    public IEnumerator NonPartnersCannotReceiveNightVisitsInPolyculeBedroom()
    {
        StartPolyculeBedroomSleepJob(third, thirdBed);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == thirdBed && !third.Awake(), 1200);
        Expect.IsTrue(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
        Expect.IsFalse(NightVisitHelpers.FindVisits(first, thirdBed).Any());
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, thirdBed));
        Expect.IsNull(new JobGiver_NightVisit().TryIssueJobPackage(first, default).Job);
    }

    [Test]
    public IEnumerator WillingNonPartnerOutsideBedroomDoesNotEnablePolyculeSelection()
    {
        third.ownership.ClaimBedIfNonMedical(visitBed);
        third.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == visitBed && !third.Awake(), 1200);
        Expect.IsFalse(LovePartnerRelationUtility.LovePartnerRelationExists(first, third));
        Expect.IsTrue(BedUtility.WillingToShareBed(first, third));
        Expect.IsTrue(BedSharingHelpers.CanSleepInSharedBed(first, visitBed));
        Expect.IsFalse(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, visitBed));
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, visitBed));
    }

    [Test]
    public IEnumerator WillingUnrelatedOccupantCanSharePolyculeBedroom()
    {
        outsider.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => outsider.CurrentBed() == visitBed, 600);
        // Put an unexpected sleeper in the bedroom without changing its owners.
        outsider.Position = thirdBed.GetSleepingSlotPos(0);
        Expect.ReferencesAreEqual(thirdBed, outsider.CurrentBed());
        Expect.IsFalse(PolyculeHelpers.GetPolycule(first).Contains(outsider));
        Expect.IsTrue(BedUtility.WillingToShareBed(first, outsider));
        Expect.IsTrue(BedSharingHelpers.CanSleepInSharedBed(first, thirdBed));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Expect.IsTrue(RestUtility.IsValidBedFor(thirdBed, first, first, true));
        Expect.AreNotEqual(outsider.Position, RestUtility.GetBedSleepingSlotPosFor(first, thirdBed));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(visitBed, outsider.ownership.OwnedBed);
    }

    [Test, LoadIfIdeologyActive]
    public IEnumerator PolyculeFallbackDoesNotApplyOutsideBedroom()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        first.ideo.SetIdeo(ideo);
        Expect.IsFalse(BedUtility.WillingToShareBed(first, third));
        third.ownership.ClaimBedIfNonMedical(visitBed);
        third.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == visitBed && !third.Awake(), 1200);
        Expect.IsTrue(PolyculeHelpers.GetPolycule(first).Contains(third));
        Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(first, visitBed));
        Expect.IsFalse(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, visitBed));
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, visitBed));
    }

    [Test, LoadIfIdeologyActive]
    public IEnumerator IdeologyIncompatibleOccupantCannotBecomeAFallbackBedmate()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        outsider.ideo.SetIdeo(ideo);
        Expect.IsFalse(BedUtility.WillingToShareBed(first, outsider));
        outsider.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => outsider.CurrentBed() == visitBed, 600);
        // Put an unrelated sleeper in the room without changing its bed assignments.
        outsider.Position = thirdBed.GetSleepingSlotPos(0);
        Expect.ReferencesAreEqual(thirdBed, outsider.CurrentBed());
        Expect.IsTrue(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
        Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(first, thirdBed));
        Expect.IsFalse(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
    }

    [Test, LoadIfIdeologyActive]
    public IEnumerator SpouseOnlyIdeologyAllowsLastResortPolyculeSharing()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        // Conversion removes spouses above the ideoligion's limit; preserve the test polycule.
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue?.defName
            is "SpouseCount_Male" or "SpouseCount_Female").ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("SpouseCount_Male_Unlimited")));
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("SpouseCount_Female_Unlimited")));
        third.relations.AddDirectRelation(PawnRelationDefOf.Spouse, outsider);
        foreach (Pawn member in new[] { first, second, third, outsider })
            member.ideo.SetIdeo(ideo);
        Expect.IsFalse(BedUtility.WillingToShareBed(first, third));
        Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, third));
        Expect.AreEqual(4, PolyculeHelpers.GetPolycule(first).Count);
        second.ownership.ClaimBedIfNonMedical(firstBed);
        outsider.ownership.ClaimBedIfNonMedical(thirdBed);
        secondBed.Destroy();
        // Bed shuffling fills First's assigned bed while Third's still has one place.
        StartPolyculeBedroomSleepJob(second, firstBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == firstBed, 600);
        StartPolyculeBedroomSleepJob(outsider, firstBed);
        yield return new WaitUntilTimeout(() => outsider.CurrentBed() == firstBed, 600);
        StartPolyculeBedroomSleepJob(third, thirdBed);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == thirdBed, 600);
        Job? sleep = new JobGiver_GetRest().TryIssueJobPackage(first, default).Job;
        Expect.IsNotNull(sleep);
        Expect.AreEqual(JobDefOf.LayDown, sleep?.def);
        Expect.ReferencesAreEqual(thirdBed, sleep?.targetA.Thing);
        first.jobs.StartJob(sleep!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, first, false));
        Expect.IsTrue(RestUtility.IsValidBedFor(thirdBed, first, first, true));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
        Expect.AreEqual(4, PolyculeHelpers.GetPolycule(first).Count);
        Expect.IsFalse(BedUtility.WillingToShareBed(first, third));
        Expect.IsNull(LovePartnerRelationUtility.GetPartnerInMyBed(first));
    }

    [Test, LoadIfIdeologyActive]
    public IEnumerator ProhibitedLoversKeepVanillaLovinPenaltiesDuringFallbackSharing()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        // Conversion removes spouses above the ideoligion's limit; preserve the test polycule.
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue?.defName
            is "SpouseCount_Male" or "SpouseCount_Female").ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("SpouseCount_Male_Unlimited")));
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("SpouseCount_Female_Unlimited")));
        third.relations.AddDirectRelation(PawnRelationDefOf.Spouse, outsider);
        foreach (Pawn member in new[] { first, second, third, outsider })
            member.ideo.SetIdeo(ideo);
        first.relations.AddDirectRelation(PawnRelationDefOf.Lover, third);
        Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, third));
        Expect.AreEqual(4, PolyculeHelpers.GetPolycule(first).Count);
        second.ownership.ClaimBedIfNonMedical(firstBed);
        outsider.ownership.ClaimBedIfNonMedical(thirdBed);
        secondBed.Destroy();
        // Bed shuffling fills First's assigned bed while Third's still has one place.
        StartPolyculeBedroomSleepJob(second, firstBed);
        yield return new WaitUntilTimeout(() => second.CurrentBed() == firstBed, 600);
        StartPolyculeBedroomSleepJob(outsider, firstBed);
        yield return new WaitUntilTimeout(() => outsider.CurrentBed() == firstBed, 600);
        StartPolyculeBedroomSleepJob(third, thirdBed);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == thirdBed, 600);
        Job? sleep = GetSleepJob(first);
        Expect.IsNotNull(sleep);
        first.jobs.StartJob(sleep!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        first.mindState.canLovinTick = 0;
        third.mindState.canLovinTick = 0;
        Expect.ReferencesAreEqual(third, LovePartnerRelationUtility.GetPartnerInMyBed(first));
        Job? lovin = new JobGiver_DoLovin().TryIssueJobPackage(first, default).Job;
        Expect.IsNotNull(lovin);
        Expect.AreEqual(JobDefOf.Lovin, lovin?.def);
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, thirdBed));
        first.jobs.StartJob(lovin!, JobCondition.InterruptForced);
        ThoughtDef penalty = DefDatabase<ThoughtDef>.GetNamed("GotLovin_Abhorrent");
        yield return new WaitUntilTimeout(() => first.CurJobDef == JobDefOf.LayDown
            && third.CurJobDef == JobDefOf.LayDown
            && first.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == penalty)
            && third.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == penalty), 6000);
        Expect.IsTrue(first.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == penalty));
        Expect.IsTrue(third.needs.mood.thoughts.memories.Memories.Any(memory => memory.def == penalty));
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        Expect.ReferencesAreEqual(thirdBed, third.CurrentBed());
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
    }

    [Test, LoadIfIdeologyActive]
    public void ProhibitedLovinDoesNotChangeVanillaBedroomRole()
    {
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_Prohibited");
        first.ideo.SetIdeo(ideo);
        // Recompute the role so this checks classification rather than a cached result.
        firstBed.NotifyRoomAssignedPawnsChanged();
        Expect.AreEqual(3, first.GetLoveCluster().Count);
        Expect.AreEqual(1, PolyculeHelpers.GetPolycule(first).Count);
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        Expect.IsFalse(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
        Job? sleep = GetSleepJob(first);
        try
        {
            Expect.IsNotNull(sleep);
            Expect.AreEqual(JobDefOf.LayDown, sleep!.def);
            Expect.ReferencesAreEqual(firstBed, sleep.targetA.Thing);
            foreach (Building_Bed bed in new[] { firstBed, secondBed, thirdBed })
                Expect.IsFalse(bed.GetComp<CompBedSharing>().HasTemporaryAssignments);
        }
        finally
        {
            if (sleep != null)
            {
                first.ClearReservationsForJob(sleep);
                JobMaker.ReturnToPool(sleep);
            }
        }
    }

    [TestCase("Lover"), TestCase("Fiance"), LoadIfIdeologyActive]
    public void SpouseOnlyIdeologyKeepsNonSpousePartnersBedroom(string relationship)
    {
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        first.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed(relationship), second);
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        first.ideo.SetIdeo(ideo);
        // Recompute the role so this checks classification rather than a cached result.
        firstBed.NotifyRoomAssignedPawnsChanged();
        Expect.AreEqual(3, first.GetLoveCluster().Count);
        Expect.AreEqual(1, PolyculeHelpers.GetPolycule(first).Count);
        Expect.AreEqual(RoomRoleDefOf.Bedroom, firstBed.GetRoom().Role);
        Expect.IsFalse(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
        Job? sleep = GetSleepJob(first);
        try
        {
            Expect.IsNotNull(sleep);
            Expect.AreEqual(JobDefOf.LayDown, sleep!.def);
            Expect.ReferencesAreEqual(firstBed, sleep.targetA.Thing);
            foreach (Building_Bed bed in new[] { firstBed, secondBed, thirdBed })
                Expect.IsFalse(bed.GetComp<CompBedSharing>().HasTemporaryAssignments);
        }
        finally
        {
            if (sleep != null)
            {
                first.ClearReservationsForJob(sleep);
                JobMaker.ReturnToPool(sleep);
            }
        }
    }

    [Test]
    public IEnumerator TemporaryStayReleasesHomeSlotForNightVisits()
    {
        // Active-assignment rules: a temporary stay releases its permanent home slot. Night
        // visits may use the released slot even though the bed is permanently full.
        second.ownership.ClaimBedIfNonMedical(firstBed);
        // Both temporary sleepers need permanent homes in this polycule bedroom.
        outsider.ownership.ClaimBedIfNonMedical(secondBed);
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, outsider);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, firstBed), JobCondition.InterruptForced);
        StartPolyculeBedroomSleepJob(second, thirdBed);
        StartPolyculeBedroomSleepJob(outsider, thirdBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed && !first.Awake()
            && second.CurrentBed() == thirdBed && !second.Awake()
            && outsider.CurrentBed() == thirdBed && !outsider.Awake(), 1200);
        Expect.IsFalse(firstBed.AnyUnownedSleepingSlot);
        Expect.IsNull(SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 1)));
        Expect.ReferencesAreEqual(thirdBed, SleepingAssignments.GetActiveAssignment(second)?.Bed);
        first.mindState.canLovinTick = 0;
        outsider.mindState.canLovinTick = 0;
        // The visitor also has a temporary stay. A night visit transfers that assignment,
        // releasing its source slot while retaining one active booking.
        Expect.IsTrue(NightVisitHelpers.CanLeaveBed(outsider));
        Job? visit = NightVisitHelpers.TryMakeVisitJob(outsider, firstBed);
        Expect.IsNotNull(visit);
        Expect.IsFalse(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(outsider, out _));
        Expect.ReferencesAreEqual(outsider, SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 1)));
        outsider.jobs.StartJob(visit!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => outsider.CurrentBed() == firstBed, 1200);
        Expect.AreEqual(firstBed.GetSleepingSlotPos(1), outsider.Position);
        Expect.ReferencesAreEqual(firstBed, second.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, outsider.ownership.OwnedBed);

        // Ending the owner's stay cannot displace the visitor. The home assignment returns
        // only after that slot's temporary holder also leaves; ownership never changes.
        second.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Goto, new IntVec3(40, 0, 43)), JobCondition.InterruptForced);
        yield return new WaitTicks(1);
        Expect.IsNull(SleepingAssignments.GetActiveAssignment(second));
        Expect.ReferencesAreEqual(outsider, SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 1)));
        outsider.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Goto, new IntVec3(42, 0, 43)), JobCondition.InterruptForced);
        yield return new WaitTicks(1);
        Expect.ReferencesAreEqual(firstBed, SleepingAssignments.GetActiveAssignment(second)?.Bed);
        Expect.AreEqual(1, SleepingAssignments.GetActiveAssignment(second)?.Slot);
        Expect.ReferencesAreEqual(second, SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 1)));
    }

    [Test]
    public IEnumerator ActiveAssignmentCollectionAgreesWithPawnAndSlotViews()
    {
        // The map collection is the source of every active-assignment view. Temporary stays
        // suppress both their holders' permanent places and the owners of the borrowed slots;
        // each pawn and slot appears at most once. Expired stays restore the permanent defaults.
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, firstBed));
        Pawn[] pawns = [first, second, third, outsider];
        SleepingAssignments.ActiveAssignment[] active = SleepingAssignments.GetActiveAssignments(first.Map)
            .Where(assignment => pawns.Contains(assignment.Pawn)).ToArray();
        Expect.AreEqual(3, active.Length);
        Expect.AreEqual(active.Length, active.Select(assignment => assignment.Pawn).Distinct().Count());
        Expect.AreEqual(active.Length, active.Select(assignment => (assignment.Place.Bed, assignment.Place.Slot)).Distinct().Count());
        foreach (SleepingAssignments.ActiveAssignment assignment in active)
        {
            SleepingPlace? byPawn = SleepingAssignments.GetActiveAssignment(assignment.Pawn);
            Expect.ReferencesAreEqual(assignment.Place.Bed, byPawn?.Bed);
            Expect.AreEqual(assignment.Place.Slot, byPawn?.Slot);
            Expect.ReferencesAreEqual(assignment.Pawn, SleepingAssignments.GetAssignedPawn(assignment.Place));
        }
        Expect.IsNull(SleepingAssignments.GetActiveAssignment(third));
        Expect.IsNull(SleepingAssignments.GetAssignedPawn(new SleepingPlace(secondBed, 0)));
        yield return new WaitTicks(1);
        active = SleepingAssignments.GetActiveAssignments(first.Map)
            .Where(assignment => pawns.Contains(assignment.Pawn)).ToArray();
        Expect.AreEqual(4, active.Length);
        Expect.IsFalse(active.Any(assignment => assignment.IsTemporary));
        foreach (SleepingAssignments.ActiveAssignment assignment in active)
            Expect.ReferencesAreEqual(assignment.Pawn.ownership.OwnedBed, assignment.Place.Bed);
    }

    [Test]
    public IEnumerator TravellingTemporarySleeperDoesNotNeedNativeReservations()
    {
        // A temporary assignment follows actual travel and occupancy, not ReservationManager.
        // Clearing the native bed reservation must not free a traveller's chosen slot.
        StartPolyculeBedroomSleepJob(first, thirdBed);
        Expect.IsTrue(first.pather.Moving);
        first.ClearReservationsForJob(first.CurJob);
        yield return new WaitTicks(1);
        Expect.IsFalse(first.HasReserved(thirdBed));
        Expect.ReferencesAreEqual(thirdBed, SleepingAssignments.GetTemporaryAssignment(first)?.Bed);
        Expect.ReferencesAreEqual(first, SleepingAssignments.GetAssignedPawn(new SleepingPlace(thirdBed, 0)));
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 1200);
        Expect.ReferencesAreEqual(thirdBed, SleepingAssignments.GetActiveAssignment(first)?.Bed);
        Expect.IsNull(SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 0)));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator UnstartedNativeReservationDoesNotRetainTemporaryAssignment()
    {
        // The creation-tick grace allows selection before job startup. After that tick, a
        // native reservation alone is not travel or occupancy, so the permanent default returns.
        Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Job unstarted = JobMaker.MakeJob(JobDefOf.LayDown, thirdBed);
        try
        {
            Expect.IsTrue(unstarted.TryMakePreToilReservations(first, false));
            yield return new WaitTicks(1);
            Expect.IsTrue(first.HasReserved(thirdBed));
            Expect.IsNull(SleepingAssignments.GetTemporaryAssignment(first));
            Expect.ReferencesAreEqual(firstBed, SleepingAssignments.GetActiveAssignment(first)?.Bed);
            Expect.ReferencesAreEqual(first, SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 0)));
            Expect.ReferencesAreEqual(third, SleepingAssignments.GetAssignedPawn(new SleepingPlace(thirdBed, 0)));
        }
        finally
        {
            first.ClearReservationsForJob(unstarted);
            JobMaker.ReturnToPool(unstarted);
        }
    }

    [Test]
    public IEnumerator TravelToAnotherSlotEndsTemporaryAssignment()
    {
        // A stay follows the selected slot, not merely a job targeting the same bed. If the
        // path is redirected to a different slot, the selected assignment expires despite its native
        // reservation, and the permanent default becomes active again.
        StartPolyculeBedroomSleepJob(first, thirdBed);
        yield return new WaitTicks(1);
        Expect.ReferencesAreEqual(thirdBed, SleepingAssignments.GetTemporaryAssignment(first)?.Bed);
        first.pather.StartPath(thirdBed.GetSleepingSlotPos(1), PathEndMode.OnCell);
        Expect.IsTrue(first.HasReserved(thirdBed));
        Expect.IsNull(SleepingAssignments.GetTemporaryAssignment(first));
        Expect.ReferencesAreEqual(firstBed, SleepingAssignments.GetActiveAssignment(first)?.Bed);
        Expect.ReferencesAreEqual(first, SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 0)));
        Expect.ReferencesAreEqual(third, SleepingAssignments.GetAssignedPawn(new SleepingPlace(thirdBed, 0)));
    }

    [Test]
    public IEnumerator MovingBetweenTemporaryBedsKeepsOneAssignment()
    {
        // Each pawn has one active temporary place among the bed comps on its map. Moving releases
        // the source slot and keeps the permanent home inactive, without rewriting owners.
        StartPolyculeBedroomSleepJob(first, secondBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed, 1200);
        Job? move = BedSharingJobHelpers.TryMakeSleepJob(first, new SleepingPlace(thirdBed, 0));
        Expect.IsNotNull(move);
        Expect.IsFalse(secondBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out _));
        Expect.ReferencesAreEqual(second, SleepingAssignments.GetAssignedPawn(new SleepingPlace(secondBed, 0)));
        Expect.ReferencesAreEqual(first, SleepingAssignments.GetAssignedPawn(new SleepingPlace(thirdBed, 0)));
        Expect.IsNull(SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 0)));
        // Assignment and physical occupancy are separate: the pawn remains in its source slot until travel starts.
        Expect.IsFalse(SleepingAssignments.IsAvailable(second, new SleepingPlace(secondBed, 0)));
        first.jobs.StartJob(move!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 1200);
        Expect.ReferencesAreEqual(thirdBed, SleepingAssignments.GetActiveAssignment(first)?.Bed);
        Expect.IsTrue(SleepingAssignments.IsAvailable(second, new SleepingPlace(secondBed, 0)));
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
    }

    [Test]
    public IEnumerator RejectedTransferPreservesPreviousTemporaryStay()
    {
        // A rejected destination must leave the existing active stay intact, including
        // its suppression of the permanent home assignment. A forbidden bed cannot accept a transfer.
        StartPolyculeBedroomSleepJob(first, secondBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == secondBed, 1200);
        CompForbiddable forbiddable = new()
        {
            parent = thirdBed,
            props = new CompProperties { compClass = typeof(CompForbiddable) }
        };
        thirdBed.AllComps.Add(forbiddable);
        thirdBed.compForbiddable = forbiddable;
        thirdBed.SetForbidden(true, false);
        Expect.IsTrue(thirdBed.IsForbidden(first));
        Expect.IsNull(BedSharingJobHelpers.TryMakeSleepJob(first, new SleepingPlace(thirdBed, 0)));
        Expect.ReferencesAreEqual(secondBed, SleepingAssignments.GetActiveAssignment(first)?.Bed);
        Expect.ReferencesAreEqual(first, SleepingAssignments.GetAssignedPawn(new SleepingPlace(secondBed, 0)));
        Expect.ReferencesAreEqual(third, SleepingAssignments.GetAssignedPawn(new SleepingPlace(thirdBed, 0)));
        Expect.IsNull(SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 0)));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void OrdinarySleepCanReplaceAnActiveOwnedBed(bool homeSlotBorrowed)
    {
        // Ordinary sleep in an unowned place follows vanilla claiming behavior when the pawn's
        // permanent assignment is active. A temporary assignment holding that home slot makes
        // the ownership inactive, so another bed must not replace it. Creation-tick grace keeps
        // the selected borrow active while its sleep job awaits startup.
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        if (homeSlotBorrowed)
            Expect.IsTrue(BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(second, firstBed));
        Expect.AreEqual(!homeSlotBorrowed, SleepingAssignments.GetActiveAssignment(first) != null);
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        Expect.ReferencesAreEqual(homeSlotBorrowed ? firstBed : visitBed, first.ownership.OwnedBed);
        Expect.AreEqual(homeSlotBorrowed, firstBed.IsOwner(first));
        Expect.AreEqual(!homeSlotBorrowed, visitBed.IsOwner(first));
        if (homeSlotBorrowed)
            Expect.ReferencesAreEqual(second, SleepingAssignments.GetAssignedPawn(new SleepingPlace(firstBed, 0)));
    }

    [TestCase(true)]
    [TestCase(false)]
    public IEnumerator UnownedPawnClaimsBedOnlyForOrdinaryUse(bool temporaryStay)
    {
        // Temporary use never creates permanent ownership, even for an unowned pawn. Ordinary
        // vanilla sleep by an unowned pawn establishes ownership through its claim toil.
        visitBed.CompAssignableToPawn.TryUnassignPawn(outsider);
        Job? sleep = temporaryStay
            ? BedSharingJobHelpers.TryMakeSleepJob(outsider, new SleepingPlace(visitBed, 0))
            : JobMaker.MakeJob(JobDefOf.LayDown, visitBed);
        Expect.IsNotNull(sleep);
        outsider.jobs.StartJob(sleep!, JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => outsider.CurrentBed() == visitBed, 1200);
        Expect.ReferencesAreEqual(visitBed, outsider.CurrentBed());
        if (temporaryStay)
            Expect.IsNull(outsider.ownership.OwnedBed);
        else
            Expect.ReferencesAreEqual(visitBed, outsider.ownership.OwnedBed);
        Expect.ReferencesAreEqual(visitBed, SleepingAssignments.GetActiveAssignment(outsider)?.Bed);
        Expect.ReferencesAreEqual(outsider, SleepingAssignments.GetAssignedPawn(new SleepingPlace(visitBed, 0)));
    }

    [TestCase(true)]
    [TestCase(false)]
    [LoadIfBiotechActive]
    public IEnumerator PolyculeSelectionProtectsNonMemberOwnedSlots(bool childOwnsFreeSlot)
    {
        // The bedroom feature may borrow polycule-owned slots (active or inactive) and unowned
        // slots in their beds. A child's permanent slot is protected even though vanilla allows
        // the child to share this bedroom. Being a permitted roommate does not join the polycule.
        Pawn child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
            Faction.OfPlayer, fixedBiologicalAge: 8, fixedChronologicalAge: 8,
            canGeneratePawnRelations: false, developmentalStages: DevelopmentalStage.Child));
        if (ModsConfig.IdeologyActive)
            child.ideo.SetIdeo(null);
        child.relations.AddDirectRelation(PawnRelationDefOf.Parent, third);
        GenSpawn.Spawn(child, new IntVec3(39, 0, 42), first.Map);
        spawned.Add(child);
        if (childOwnsFreeSlot)
            child.ownership.ClaimBedIfNonMedical(thirdBed);
        StartPolyculeBedroomSleepJob(third, thirdBed);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == thirdBed, 1200);
        Expect.AreEqual(RoomRoleDefOf.Bedroom, thirdBed.GetRoom().Role);
        Expect.IsFalse(PolyculeHelpers.GetPolycule(first).Contains(child));
        Expect.IsTrue(BedSharingHelpers.CanSleepInSharedBed(first, thirdBed));
        Expect.AreEqual(!childOwnsFreeSlot, BedSelectionHelpers.TryMakePolyculeBedroomSleepJob(first, thirdBed));
        Expect.ReferencesAreEqual(third, thirdBed.OwnersForReading[0]);
        if (childOwnsFreeSlot)
            Expect.ReferencesAreEqual(child, thirdBed.OwnersForReading[1]);
        else
            Expect.AreEqual(thirdBed.GetSleepingSlotPos(1), RestUtility.GetBedSleepingSlotPosFor(first, thirdBed));
    }

    [Test, LoadIfIdeologyActive]
    public void ProhibitedLovinBreaksPolyculeEvenForSpouses()
    {
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_Prohibited");
        first.ideo.SetIdeo(ideo);
        Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, second));
        Expect.AreEqual(1, PolyculeHelpers.GetPolycule(first).Count);
        Expect.IsFalse(firstBed.GetComp<CompBedSharing>().CompGetGizmosExtra().Any());
    }

    [Test, LoadIfIdeologyActive]
    public void SpouseOnlyIdeologyExcludesLoversAndFiances()
    {
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        first.relations.AddDirectRelation(PawnRelationDefOf.Fiance, second);
        Ideo ideo = CreateIdeoWithLovinPrecept("Lovin_SpouseOnly_Strict");
        first.ideo.SetIdeo(ideo);
        Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, second));
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Fiance, second);
        first.relations.AddDirectRelation(PawnRelationDefOf.Lover, second);
        Expect.IsFalse(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, second));
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Lover, second);
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, second);
        Expect.IsTrue(PolyculeHelpers.AreLovePartnersWillingToShareBed(first, second));
    }
}
