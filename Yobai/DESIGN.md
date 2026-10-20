# How Yobai assigns sleeping places

Permanent bed ownership and temporary sleeping assignments are separate. Yobai never turns a temporary stay into a permanent bed claim.

## Choosing a place

[BedSelectionHelpers](BedSelectionHelpers.cs) contains the two admission policies:

- Bedroom sharing considers beds assigned to the pawn's polycule in their bedroom. It tries beds occupied by a direct partner first, then empty beds, then beds shared with other polycule members as a last resort.
- Night visits consider double beds occupied by one sleeping partner, with visits enabled. Only unassigned sleeping slots are candidates.

New choices also honor vanilla restrictions on resting in beds, including caravan duties, roping, and medical rest. A ground-sleep fallback from vanilla still gets a chance to find a shared bed.

Beds are tried in random order, so each usable bed has an equal chance regardless of its number of available slots. Each policy offers concrete `SleepingPlace` values: a bed and a slot within it. There is no visit-mode flag passed to the assignment or reservation code.

`ChooseBedtimeSleepJob` expresses the bedtime priority: join a partner in the bedroom; otherwise try a night visit with 50% probability; otherwise choose an empty bedroom bed; finally share with another polycule member if no preferred place is available. `NightVisits` handles the separate MTB opportunities for pawns already asleep. A sleeping pawn stays put while another sleeper is on the way to join them.

## Keeping assignments consistent

[CompBedSharing](CompBedSharing.cs) holds temporary assignments. Its assignment operations enforce three rules:

- A pawn has at most one temporary sleeping place in this bed.
- A sleeping place has at most one temporary assignee and cannot displace another occupant or booked arrival.
- Current occupants and incoming sleepers must be compatible bedmates, including permanent owners on their way to bed.

Bedmates are compatible when they are permitted direct romantic partners, or members of the same polycule sharing within the pawn's assigned polycule bedroom. The latter permission overrides pairwise ideoligion restrictions on sharing a bed. It does not change the romantic graph. The chooser gives this option last priority, while assignment and continuation use the same compatibility rule regardless of the calling job. Incoming non-partners also make a bed a last-resort choice, even while it is physically empty.

Night visit admission still requires a permitted direct partner. Vanilla lovin' is unchanged: non-partners cannot do lovin', while existing romantic partners may do so and receive any applicable ideoligion penalties.

The comp combines current occupants, temporary assignments, and native sleep reservations made by permanent owners into a common view of sleeping places. Owner reservations for repair or other work do not claim a sleeping place. A temporary assignment takes precedence over that owner's permanent slot. Native reservation cleanup releases the owner's place when the trip is cancelled. The comp does not choose slots or decide whether a pawn qualifies for a night visit. Its saved keys and the serialized [TemporarySleepingSlot](TemporarySleepingSlot.cs) fields are unchanged. RimWorld saves comp data within the bed, rather than saving the comp class name.

## Creating and continuing jobs

`SharedSleepJobHelpers.TryMakeSleepJob` takes a chosen place and performs one transaction:

1. Reserve capacity in the bed for the new job.
2. Add the temporary assignment.
3. Run vanilla bed validation with that assignment present.
4. Return the job, or remove the new assignment and release the job's reservations on failure.

Rollback removes only the assignment created by that transaction. A rejected second booking cannot remove an existing stay. The comp gives each temporary sleeper a distinct slot; no map-cell reservation is needed for that assignment. Vanilla bed reservations retain it while a pawn travels or waits for a queued job.

All sleep jobs use vanilla `LayDown` and `JobDriver_LayDown`. [Patch_JobDriver_LayDown](Patches/Patch_JobDriver_LayDown.cs) runs after the normal reservation check. If it succeeded and the bed has any temporary assignments, the patch also validates the bed and the sleeper's place, including when that sleeper is a returning owner. A rejected arrival releases only its own job reservations. The patch preserves vanilla reservation failures and leaves beds without temporary assignments and ground sleep alone. Once a temporary assignment ends, reservations follow vanilla behavior again, including medical rest. It never chooses another place or repeats admission policy, so a host waking or the visits toggle changing does not undo a committed visit.

## During the stay

The patches read the comp's assignment for bed access and sleeping position. They apply to sleep and lovin'. While a bed has temporary assignments, every pawn using it must be compatible with its occupants and incoming sleepers, including permanent owners returning to it. The sleeping place they would use must also be neither occupied nor booked by anyone else, whether its assignment is temporary or permanent. These checks still apply when relationships change after booking. Automatic bed claims are suppressed whenever the temporary assignment exists; eligibility to continue sleeping is a separate check. Manual bed assignments still use vanilla ownership behavior.

`TemporarySleepingSlot.IsInUse` defines the assignment's lifetime. Reservations retain it while travelling or waiting for a queued job. Physical presence in the sleeping place retains it during transitions between sleep and lovin'. Once neither applies, the comp removes it. A bed changing to an excluded use, such as medical use, also ends its temporary assignments. Cleanup runs on lookup and periodically from the map component.

The in-game tests cover admission, assignment conflicts, failed-booking rollback, cancellation, continuation, ownership preservation, and Scribe persistence. See [verification results](../Yobai.Tests/VERIFICATION.md) for the runs performed and the checks still outstanding.

## Bedroom roles

Vanilla's love cluster already includes the transitive closure of living spouses, fiancés and lovers. Yobai leaves bedroom classification unchanged, including relationships whose ideoligions prohibit bed sharing, children sharing with parents, and empty beds making a room a barracks. The stricter physical-partner polycule is used only for temporary bed sharing and night visits.

Vanilla manages room-role caching and updates. Bed selection reads `Room.Role` and computes the physical-partner polycule afresh.
