# Sleeping assignments and job integration

[SleepingAssignments](SleepingAssignments.cs) is the shared API for resolving active sleeping assignments and managing temporary stays. It builds a collection of all active assignments on a map; pawn, slot, bedmate and availability queries use views of that collection. The API's doc comment defines the assignment rules.

## Ownership, assignments and occupancy

These describe different things:

| Concept | Meaning |
| --- | --- |
| Permanent ownership | The pawn's normal bed and slot, used to determine its assigned bedroom. |
| Active assignment | The place currently allocated to the pawn after applying temporary overrides. |
| Physical occupancy | The slot the pawn is actually lying in, which blocks another pawn regardless of assignment. |

Temporary-assignment rules are scoped to one map:

- A temporary assignment stays active during its creation tick, then while the pawn lies in its slot or travels there for sleep or lovin'. The creation-tick grace lets job selection finish before travel starts. Native reservation records do not extend its lifetime.
- A permanent assignment is active when its owner has no temporary assignment on the map and nobody temporarily holds its slot.
- Each pawn and slot can have at most one temporary assignment. Transfers release the source assignment; failed transfers restore it.
- Temporary stays never change permanent ownership. When a stay ends, the permanent assignment resumes unless another temporary sleeper holds its slot.

An absent permanent owner retains an active assignment, so night visitors cannot take that place. Polycule bedroom selection can borrow it if the owner is not using it. Actual occupants, temporary assignees and travelling permanent owners count as bedmates for compatibility and preference checks.

[CompBedSharing](CompBedSharing.cs) stores temporary records and the night-visit toggle. Inactive records are removed on query and before saving. The creation tick is not serialized, so loaded assignments depend on the pawn's actual activity.

## Bedroom selection

[BedSelectionHelpers](BedSelectionHelpers.cs) considers beds in the pawn's permanently assigned polycule bedroom. Eligible slots are either permanently owned by a polycule member or unowned in a bed with a permanent polycule owner. An unrelated roommate's empty owned slot is protected.

The chooser collects eligible beds, shuffles them, then uses a stable sort to prefer beds containing a permitted romantic partner or having at least two free places. It takes the first successful assignment. Shuffling before sorting gives each bed in a preference group an equal chance, regardless of its number of available slots. An owner tries its permanently assigned slot first, then the remaining eligible slots.

Compatibility follows `BedUtility.WillingToShareBed`, with an additional permission for members of the same polycule in their assigned bedroom. That permission allows last-resort sharing under restrictive ideoligions. It does not add romantic relationships or alter vanilla lovin' penalties.

An inner postfix on `RestUtility.FindBedFor` inside `JobGiver_GetRest.TryGiveJob` selects the bed and lets vanilla create the `LayDown` job. A travelling pawn keeps its chosen destination. Other bed searches use vanilla behavior. Medical rest, caravan duties, roping and other vanilla eligibility restrictions apply.

Bedroom classification and room-role caching belong to vanilla. Its love cluster includes relationships that do not connect Yobai's stricter polycule. Its bedroom rules also allow children to share with their parents. [PolyculeHelpers](PolyculeHelpers.cs) computes Yobai's graph from living romantic partners willing to share a bed.

## Night-visit selection

An XML patch adds [JobGiver_NightVisit](JobGiver_NightVisit.cs) after vanilla's lovin' branch in the humanlike lying-down think tree. It considers sleeping pawns without a romantic bedmate present or arriving. Player-forced sleep is excluded.

The job giver checks at most once per in-game hour, recording the attempt in the pawn's `thinkData`. A destination needs visits enabled, a sleeping direct partner, and an available slot that is unassigned or assigned to the visitor. Other bedmates need only be compatible. Every eligible host in a bed is considered, so one partner's cooldown does not hide another ready partner.

Both partners must have finished their lovin' cooldowns, be able to be awake, be mutually reservable, and pass `LovePartnerRelationUtility.GetLovinMtbHours`. The job giver selects one eligible partner and rolls `Rand.MTBEventOccurs` using that pair's vanilla lovin' MTB. Pawn-specific hourly seeding follows `ThinkNode_ChancePerHour` and preserves the global random stream. [NightVisit](NightVisit.cs) keeps the chosen partner and place together through job creation.

## Admission, travel and claims

[BedSharingJobHelpers](BedSharingJobHelpers.cs) checks bed capacity and slot availability before creating a job; lovin' also requires a reservable partner. The assignment API installs the temporary assignment, validates the bed through vanilla with that assignment present, and restores the previous stay if validation fails. Native job startup makes the reservations. Temporary slots do not require map-cell reservations.

A night visitor uses `Lovin` throughout the journey. The bed toil checks that the selected partner is present and ready while the visitor travels. The visit does not repeat its random roll or require the host to remain asleep or the toggle to remain enabled. Sleep continuation is queued only after arrival, so cancelling a journey does not send a sleep job back to the abandoned destination.

Bed access and continuation apply compatibility and slot availability to both temporary sleepers and permanent owners. Automatic bed claims preserve ownership during a temporary stay or while another temporary assignee holds the owner's slot on the same map. Otherwise, vanilla may claim an unowned sleeping place even when the pawn owns another bed, including one on a different map. Manual assignments use vanilla behavior.

See the [in-game test instructions](../Yobai.Tests/README.md) for running tests and constructing fixtures.
