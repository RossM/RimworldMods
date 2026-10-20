# How Yobai assigns sleeping places

Permanent bed ownership and temporary sleeping assignments are separate. Yobai never turns a temporary stay into a permanent bed claim.

## Choosing a bedroom bed

[BedSelectionHelpers](BedSelectionHelpers.cs) handles sleep within the pawn's assigned polycule bedroom. It tries beds shared with direct partners first, empty beds next, and beds shared with indirect polycule members as a last resort. Incoming sleepers count as bedmates when choosing among these options. Each usable bed has an equal chance, regardless of its number of available slots. Within the chosen bed, an owner tries their permanently assigned place first. An occupied or booked place is skipped, so the owner can still use another compatible place.

The chooser honors vanilla restrictions on resting in beds, including caravan duties, roping and medical rest. It returns a regular `LayDown` job. A vanilla ground-sleep fallback still gets a chance to find a shared bedroom bed. Bedtime does not initiate night visits.

## Choosing a night visit

An XML patch adds [JobGiver_NightVisit](JobGiver_NightVisit.cs) to the humanlike think tree's lying-down branch, after vanilla's lovin' branch. The job giver considers a pawn sleeping alone, and excludes player-forced sleep and beds with an incoming companion.

The job giver owns the complete decision. It considers at most one opportunity per in-game hour, stored in the pawn's normal `thinkData` under the node's save key. Eligible destinations have at least one sleeping direct partner, visits enabled, and a free place belonging to the visitor or assigned to nobody. Bed capacity is unrestricted; every current or incoming bedmate must be a permitted direct partner of the visitor. Each eligible host is considered separately, so a host on cooldown does not hide another ready host in the same bed. A returning owner tries their own place first, then unassigned places. Guests only consider unassigned places. This also allows an owner to return to a fully assigned bed. Both partners must have finished their lovin' cooldowns, be able to be awake, be mutually reservable, and pass `LovePartnerRelationUtility.GetLovinMtbHours`.

One eligible partner is chosen at random. The job giver then rolls `Rand.MTBEventOccurs` using that pair's vanilla lovin' MTB and the same hourly interval and pawn-specific seeding scheme as `ThinkNode_ChancePerHour`. It restores the global random state afterward. There is one roll for the chosen pair, rather than a roll for each possible partner. The partner and sleeping place travel together as an immutable [NightVisit](NightVisit.cs), so the chance calculation and issued job concern the same pair.

There is no additional four-hour visit timer or 50% bedtime roll. Vanilla's existing lovin' branch is unchanged.

## Booking a place and travelling

[BedSharingJobHelpers](BedSharingJobHelpers.cs) creates normal `LayDown` or `Lovin` jobs. Both use the same booking transaction:

1. Reserve capacity in the bed for the job.
2. Add the temporary sleeping assignment.
3. Run vanilla bed validation with that assignment present.
4. Return the job, or remove the new assignment and release the job's reservations on failure.

A visit also reserves its chosen partner before booking the bed. Rollback clears that reservation too. It removes only the assignment created by the failed transaction; a rejected second booking cannot remove an existing stay. No map-cell reservation is needed.

A visitor uses vanilla `Lovin` from the start, including its trip to the destination. During that trip, the bed toil checks that the chosen partner is still there and still ready for lovin'. It does not repeat the random roll or require the host to remain asleep or the toggle to remain enabled. Ordinary lovin' already starting at its sleeping place retains vanilla's checks.

Vanilla's `KeepLyingDown` queues sleep even when a lovin' job fails. For a travelling visitor, Yobai queues that continuation only if the visitor actually reached the bed. Otherwise a cancelled visit could send a normal sleep job to the abandoned destination and change bed ownership. Successful visits use vanilla lovin' effects, cooldowns and sleep continuation.

## Keeping assignments consistent

[CompBedSharing](CompBedSharing.cs) combines current occupants, temporary assignments, and permanent owners' native sleep reservations into a common view of sleeping places. Work reservations do not count as incoming sleepers. A temporary assignment overrides the same owner's permanent slot. Native reservation cleanup releases an owner's place when its trip is cancelled.

The comp enforces these rules:

- A pawn has at most one temporary sleeping place in this bed.
- A place cannot be assigned to a second temporary sleeper or displace an occupant or booked arrival.
- Current occupants and incoming sleepers must be compatible bedmates.

Bedmates are compatible when they are permitted direct romantic partners, or members of the same polycule sharing within the pawn's assigned polycule bedroom. The latter permission overrides pairwise ideoligion restrictions on sleeping together without changing the romantic graph. Bedroom selection gives this option last priority. Night visits still require permitted direct partners. Non-partners cannot do lovin'; existing romantic partners sharing as a last resort retain vanilla's lovin' penalties.

Bed access and continuation apply the same compatibility and availability rules to temporary sleepers and returning permanent owners. The sleep-driver postfix validates successful reservations when a temporary stay exists in the bed. Automatic claims preserve temporary assignments; manual bed assignments retain vanilla behavior.

[TemporarySleepingSlot](TemporarySleepingSlot.cs) retains an assignment while the pawn reserves the bed or is physically lying in its chosen place. Cleanup runs when the comp is queried and before saving. Saved comp keys and assignment fields are unchanged. The old `NightVisits` map component remains empty so existing maps can load its saved entry; it runs no checks.

## Bedroom roles

Vanilla's love cluster already includes the transitive closure of living spouses, fiancés and lovers. Yobai leaves bedroom classification and room-role caching unchanged, including relationships whose ideoligions prohibit bed sharing, children sharing with parents, and empty beds making a room a barracks. The stricter physical-partner polycule is computed afresh for Yobai's bed-sharing rules.

See the [in-game test instructions](../Yobai.Tests/README.md) and [verification results](../Yobai.Tests/VERIFICATION.md) for coverage and remaining checks.
