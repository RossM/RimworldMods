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

    [Test]
    public void PolyculeIsTransitiveButBedSharingPartnersAreNot()
    {
        Expect.AreEqual(3, PolyculeHelpers.GetPolycule(first).Count);
        Expect.IsTrue(PolyculeHelpers.AreBedSharingPartners(first, second));
        Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, third));
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
        Expect.IsFalse(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, secondBed));
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
        Job pending = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed)!;
        Expect.ReferencesAreNotEqual(pending, first.CurJob);
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
        first.jobs.StartJob(NightVisitHelpers.TryMakeVisitJob(first, visitBed)!,
            JobCondition.InterruptForced);
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
        first.jobs.StartJob(selected!,
            JobCondition.InterruptForced);
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
        first.jobs.StartJob(NightVisitHelpers.TryMakeVisitJob(first, visitBed)!,
            JobCondition.InterruptForced);
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
    public IEnumerator ThreeSlotBedVisitRejectsIncompatibleBedmate(bool bedmateArriving)
    {
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
        if (bedmateArriving)
            Expect.IsTrue(bedmateSleep.TryMakePreToilReservations(outsider, false));
        else
        {
            outsider.jobs.StartJob(bedmateSleep, JobCondition.InterruptForced);
            yield return new WaitUntilTimeout(() => outsider.CurrentBed() == bed && !outsider.Awake(), 1200);
        }
        try
        {
            Expect.IsTrue(BedSharingHelpers.IsBedEligibleForNightVisits(bed));
            Expect.IsTrue(bed.GetComp<CompBedSharing>().GetBedmates().Contains(outsider));
            Expect.IsTrue(PolyculeHelpers.AreBedSharingPartners(first, second));
            Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, outsider));
            Expect.IsFalse(NightVisitHelpers.FindVisits(first, bed).Any());
            Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, bed));
        }
        finally
        {
            if (bedmateArriving)
            {
                outsider.ClearReservationsForJob(bedmateSleep);
                JobMaker.ReturnToPool(bedmateSleep);
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
        Expect.IsNull(BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, secondBed));
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
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, first, true));
        Job pending = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed)!;
        Expect.IsFalse(first.HasReserved(thirdBed));
        Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, first, true));
        Expect.IsTrue(pending.TryMakePreToilReservations(first, false));
        yield return new WaitTicks(1);
        Expect.ReferencesAreNotEqual(pending, first.CurJob);
        Expect.IsTrue(RestUtility.CanUseBedNow(thirdBed, first, true));
        Expect.IsTrue(RestUtility.IsValidBedFor(thirdBed, first, first, true));
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out int bookedSlot));
        Expect.AreEqual(thirdBed.GetSleepingSlotPos(bookedSlot), RestUtility.GetBedSleepingSlotPosFor(first, thirdBed));
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, outsider, true));
        first.ClearReservationsForJob(pending);
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, first, true));
        JobMaker.ReturnToPool(pending);
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
        Job replacement = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(second, thirdBed)!;
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
        Job? sleep = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed);
        Expect.IsNotNull(sleep);
        Expect.IsTrue(thirdBed.GetComp<CompBedSharing>().TryGetSleepingSlot(first, out int slot));
        Expect.AreEqual(0, slot);
        Expect.IsTrue(sleep!.TryMakePreToilReservations(first, false));
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
        Job booking = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed)!;
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
        Job pending = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, secondBed)!;
        Expect.IsTrue(pending.TryMakePreToilReservations(first, false));
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        Expect.IsFalse(RestUtility.CanUseBedNow(secondBed, second, true));
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
        JobMaker.ReturnToPool(pending);
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
        Job incoming = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, secondBed)!;
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
        if (occupantArrived)
        {
            first.jobs.StartJob(occupantSleep!, JobCondition.InterruptForced);
            yield return new WaitUntilTimeout(() => first.CurrentBed() == firstBed, 600);
        }
        else
            Expect.IsTrue(occupantSleep!.TryMakePreToilReservations(first, false));
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
                JobMaker.ReturnToPool(occupantSleep!);
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
        Job ownerSleep = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.IsTrue(ownerSleep.TryMakePreToilReservations(second, false));
        Expect.IsNull(second.CurrentBed());
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, second, true));
        Job? incoming = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, secondBed);
        Expect.IsNotNull(incoming);
        Expect.AreEqual(secondBed.GetSleepingSlotPos(1), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, first, true));
        Expect.IsTrue(RestUtility.CanUseBedNow(secondBed, second, true));
        first.ClearReservationsForJob(incoming!);
        second.ClearReservationsForJob(ownerSleep);
        JobMaker.ReturnToPool(incoming!);
        JobMaker.ReturnToPool(ownerSleep);
        yield return new WaitTicks(1);
        Job? replacement = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, secondBed);
        Expect.IsNotNull(replacement);
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        first.ClearReservationsForJob(replacement!);
        JobMaker.ReturnToPool(replacement!);
    }

    [Test]
    public void IncomingOwnerCountsAsBedmate()
    {
        Job ownerSleep = JobMaker.MakeJob(JobDefOf.LayDown, secondBed);
        Expect.IsTrue(ownerSleep.TryMakePreToilReservations(second, false));
        Expect.IsNull(second.CurrentBed());
        Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(outsider, secondBed));
        Expect.IsFalse(secondBed.GetComp<CompBedSharing>().CanAssignSleepingSlot(outsider, 1));
        Expect.IsTrue(BedSharingHelpers.CanSleepInSharedBed(first, secondBed));
        second.ClearReservationsForJob(ownerSleep);
        JobMaker.ReturnToPool(ownerSleep);
    }

    [Test]
    public void UnrelatedOwnerReservationDoesNotOccupySleepingSlot()
    {
        Job repair = JobMaker.MakeJob(JobDefOf.Repair, secondBed);
        Expect.IsTrue(second.Reserve(secondBed, repair, secondBed.SleepingSlotsCount, 0));
        Job? incoming = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, secondBed);
        Expect.IsNotNull(incoming);
        Expect.AreEqual(secondBed.GetSleepingSlotPos(0), RestUtility.GetBedSleepingSlotPosFor(first, secondBed));
        first.ClearReservationsForJob(incoming!);
        second.ClearReservationsForJob(repair);
        JobMaker.ReturnToPool(incoming!);
        JobMaker.ReturnToPool(repair);
    }

    [Test]
    public IEnumerator RelationshipChangeRejectsIncompatibleArrivalsBeforeEitherArrives()
    {
        Job firstArrival = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed)!;
        Job secondArrival = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(second, thirdBed)!;
        Expect.IsNotNull(firstArrival);
        Expect.IsNotNull(secondArrival);
        Expect.IsTrue(firstArrival.TryMakePreToilReservations(first, false));
        Expect.IsTrue(secondArrival.TryMakePreToilReservations(second, false));
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
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
        Job incoming = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(second, secondBed)!;
        Expect.IsNotNull(incoming);
        Expect.IsTrue(incoming!.TryMakePreToilReservations(second, false));
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
        Job incoming = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(second, firstBed)!;
        Expect.IsNotNull(incoming);
        Expect.IsNull(second.CurrentBed());
        Expect.IsFalse(NightVisitHelpers.CanLeaveBed(first));
        Expect.ReferencesAreEqual(firstBed, first.CurrentBed());
        second.ClearReservationsForJob(incoming);
        JobMaker.ReturnToPool(incoming);
    }

    [Test]
    public IEnumerator SavingDiscardsCancelledSleepingAssignments()
    {
        Job? sleep = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed);
        Expect.IsNotNull(sleep);
        first.ClearReservationsForJob(sleep!);
        JobMaker.ReturnToPool(sleep!);
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
        Job booking = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed)!;
        Expect.IsTrue(booking.TryMakePreToilReservations(first, false));
        IntVec3 slot = RestUtility.GetBedSleepingSlotPosFor(first, thirdBed);
        CompBedSharing comp = thirdBed.GetComp<CompBedSharing>();
        string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "Yobai-assignment-test.xml");
        Scribe.saver.InitSaving(path, "YobaiTest");
        comp.PostExposeData();
        Scribe.saver.FinalizeSaving();
        first.ClearReservationsForJob(booking);
        yield return new WaitTicks(1);
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        Expect.IsTrue(first.Reserve(thirdBed, booking, thirdBed.SleepingSlotsCount, 0));
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
        first.ClearReservationsForJob(booking);
        JobMaker.ReturnToPool(booking);
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
        Job booking = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed)!;
        Expect.IsNotNull(booking);
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

    [Test]
    public void TravellersMustBeCompatibleBedmates()
    {
        Job? firstArrival = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed);
        Expect.IsNotNull(firstArrival);
        Expect.IsNull(first.CurrentBed());
        Expect.IsNull(BedSharingJobHelpers.TryMakeSleepJob(outsider, new SleepingPlace(thirdBed, 1)));
        Job? partnerArrival = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(second, thirdBed);
        Expect.IsNotNull(partnerArrival);
        Expect.AreNotEqual(RestUtility.GetBedSleepingSlotPosFor(first, thirdBed),
            RestUtility.GetBedSleepingSlotPosFor(second, thirdBed));
        first.ClearReservationsForJob(firstArrival!);
        second.ClearReservationsForJob(partnerArrival!);
        JobMaker.ReturnToPool(firstArrival!);
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
            Expect.IsNull(BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed));
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
        Job? original = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed);
        Expect.IsNotNull(original);
        CompBedSharing assignments = thirdBed.GetComp<CompBedSharing>();
        Expect.IsTrue(assignments.TryGetSleepingSlot(first, out int originalSlot));
        Expect.IsNull(BedSharingJobHelpers.TryMakeSleepJob(first, new SleepingPlace(thirdBed, 1 - originalSlot)));
        Expect.IsTrue(assignments.TryGetSleepingSlot(first, out int keptSlot));
        Expect.AreEqual(originalSlot, keptSlot);
        Expect.IsTrue(original!.TryMakePreToilReservations(first, false));
        first.ClearReservationsForJob(original);
        JobMaker.ReturnToPool(original);
    }

    [Test]
    public void AutomaticSleepCannotClaimAnIneligibleTemporaryBed()
    {
        Job? booking = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed);
        Expect.IsNotNull(booking);
        first.drafter.Drafted = true;
        Expect.IsFalse(BedSharingHelpers.CanUseTemporarySleepingSlot(first, thirdBed));
        first.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, thirdBed), JobCondition.InterruptForced,
            preToilReservationsCanFail: true);
        Expect.ReferencesAreEqual(firstBed, first.ownership.OwnedBed);
        Expect.ReferencesAreEqual(thirdBed, third.ownership.OwnedBed);
        Expect.AreEqual(1, thirdBed.OwnersForReading.Count);
        first.ClearReservationsForJob(booking!);
        JobMaker.ReturnToPool(booking!);
    }

    [Test]
    public IEnumerator RelationshipChangeCannotAdmitIncompatibleBookedBedmates()
    {
        Job? firstArrival = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed);
        Job? secondArrival = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(second, thirdBed);
        Expect.IsNotNull(firstArrival);
        Expect.IsNotNull(secondArrival);
        first.jobs.StartJob(firstArrival!,
            JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        // Second remains the permanent owner's spouse. Vanilla owner-based access must not
        // admit them beside First after their own relationship has ended.
        Expect.IsTrue(PolyculeHelpers.AreBedSharingPartners(second, third));
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, second, true));
        Expect.IsFalse(RestUtility.IsValidBedFor(thirdBed, second, second, true));
        Expect.IsFalse(secondArrival!.TryMakePreToilReservations(second, false));
        Expect.ReferencesAreEqual(secondBed, second.ownership.OwnedBed);
        JobMaker.ReturnToPool(secondArrival);
    }

    [Test]
    public IEnumerator ReturningOwnerPrefersEmptyBedToIndirectPartner()
    {
        StartPolyculeBedroomSleepJob(first, thirdBed);
        yield return new WaitUntilTimeout(() => first.CurrentBed() == thirdBed, 600);
        Expect.ReferencesAreEqual(thirdBed, first.CurrentBed());
        Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, third));
        Expect.IsFalse(RestUtility.CanUseBedNow(thirdBed, third, true));
        Expect.IsFalse(RestUtility.IsValidBedFor(thirdBed, third, third, true));
        Job? sleep = new JobGiver_GetRest().TryIssueJobPackage(third, default).Job;
        Expect.IsNotNull(sleep);
        Expect.ReferencesAreNotEqual(thirdBed, sleep?.targetA.Thing);
        third.jobs.StartJob(sleep!,
            JobCondition.InterruptForced);
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
            Expect.IsNull(BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, secondBed));
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
        Job? booking = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed);
        Expect.IsNotNull(booking);
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
        Job? incoming = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(third, firstBed);
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
        Job? incoming = BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(third, thirdBed);
        Expect.IsNotNull(incoming);
        Expect.IsTrue(incoming!.TryMakePreToilReservations(third, false));
        for (int attempt = 0; attempt < 20; attempt++)
        {
            yield return new WaitTicks(1);
            Job? sleep = GetSleepJob(first);
            Expect.IsNotNull(sleep);
            Expect.ReferencesAreNotEqual(thirdBed, sleep?.targetA.Thing);
            first.ClearReservationsForJob(sleep!);
            JobMaker.ReturnToPool(sleep!);
        }
        third.ClearReservationsForJob(incoming!);
        JobMaker.ReturnToPool(incoming!);
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
    public IEnumerator PolyculeFallbackDoesNotApplyOutsideBedroom()
    {
        third.ownership.ClaimBedIfNonMedical(visitBed);
        third.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => third.CurrentBed() == visitBed && !third.Awake(), 1200);
        Expect.IsTrue(PolyculeHelpers.GetPolycule(first).Contains(third));
        Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(first, visitBed));
        Expect.IsNull(BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, visitBed));
        Expect.IsNull(NightVisitHelpers.TryMakeVisitJob(first, visitBed));
    }

    [Test]
    public IEnumerator UnrelatedOccupantCannotBecomeAFallbackBedmate()
    {
        outsider.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, visitBed), JobCondition.InterruptForced);
        yield return new WaitUntilTimeout(() => outsider.CurrentBed() == visitBed, 600);
        // Put an unrelated sleeper in the room without changing its bed assignments.
        outsider.Position = thirdBed.GetSleepingSlotPos(0);
        Expect.ReferencesAreEqual(thirdBed, outsider.CurrentBed());
        Expect.IsTrue(BedSharingHelpers.IsBedInAssignedPolyculeBedroom(first, thirdBed));
        Expect.IsFalse(BedSharingHelpers.CanSleepInSharedBed(first, thirdBed));
        Expect.IsNull(BedSelectionHelpers.TryMakePolyculeBedroomSleepJobExt(first, thirdBed));
    }

    [Test, LoadIfIdeologyActive]
    public IEnumerator SpouseOnlyIdeologyAllowsLastResortPolyculeSharing()
    {
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(FactionDefOf.PlayerColony));
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")).ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("Lovin_SpouseOnly_Strict")));
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
        Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, third));
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
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(FactionDefOf.PlayerColony));
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")).ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("Lovin_SpouseOnly_Strict")));
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
        Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, third));
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
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(FactionDefOf.PlayerColony));
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")).ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("Lovin_Prohibited")));
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
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(FactionDefOf.PlayerColony));
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")).ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("Lovin_SpouseOnly_Strict")));
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

    [Test, LoadIfIdeologyActive]
    public void ProhibitedLovinBreaksPolyculeEvenForSpouses()
    {
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(FactionDefOf.PlayerColony));
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")).ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("Lovin_Prohibited")));
        first.ideo.SetIdeo(ideo);
        Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, second));
        Expect.AreEqual(1, PolyculeHelpers.GetPolycule(first).Count);
        Expect.IsFalse(firstBed.GetComp<CompBedSharing>().CompGetGizmosExtra().Any());
    }

    [Test, LoadIfIdeologyActive]
    public void SpouseOnlyIdeologyExcludesLoversAndFiances()
    {
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Spouse, second);
        first.relations.AddDirectRelation(PawnRelationDefOf.Fiance, second);
        Ideo ideo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(FactionDefOf.PlayerColony));
        foreach (Precept precept in ideo.PreceptsListForReading.Where(p => p.def.issue == DefDatabase<IssueDef>.GetNamed("Lovin")).ToList())
            ideo.RemovePrecept(precept, true);
        ideo.AddPrecept(PreceptMaker.MakePrecept(DefDatabase<PreceptDef>.GetNamed("Lovin_SpouseOnly_Strict")));
        first.ideo.SetIdeo(ideo);
        Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, second));
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Fiance, second);
        first.relations.AddDirectRelation(PawnRelationDefOf.Lover, second);
        Expect.IsFalse(PolyculeHelpers.AreBedSharingPartners(first, second));
        first.relations.RemoveDirectRelation(PawnRelationDefOf.Lover, second);
        first.relations.AddDirectRelation(PawnRelationDefOf.Spouse, second);
        Expect.IsTrue(PolyculeHelpers.AreBedSharingPartners(first, second));
    }
}
