# Yobai

Yobai lets pawns share beds within polycule bedrooms and make night visits to partners without changing bed assignments by hand. It supports RimWorld 1.7 and requires no DLC.

A polycule includes a pawn's spouses, fiancés and lovers, their partners, and so on. For example, if Anya is married to Bo and Bo is Cora's lover, all three belong to the same polycule. Anya and Cora can share a bedroom even if Bo sleeps elsewhere. They prefer separate beds, but can share a bed as a last resort.

With Ideology, a relationship connects the polycule only when both partners' ideoligions allow them to share a bed.

## Polycule bedrooms

When a pawn goes to sleep in a polycule bedroom, they prefer beds with a partner already in them or with at least two free places. They choose randomly among those beds. If none is available, they can share another eligible bed with a member of the polycule, even if they aren't partners or their ideoligions normally forbid sharing.

They can borrow a polycule member's permanently assigned place when it isn't being used, or take an unowned place in that member's bed. A child or other roommate's permanently assigned place is protected. Pawns already walking to a bed count as bedmates, and their chosen places are kept for them during the trip.

Within a chosen bed, owners try their permanently assigned place first. If it is taken, they can use another eligible place. Temporary stays leave permanent ownership unchanged.

Bedroom classification follows vanilla rules. Connected romantic partners count as a bedroom group even when their ideoligions prohibit bed sharing. Children can share with their parents, and unassigned beds can make the room a barracks.

## Night visits

A pawn sleeping without a romantic partner can wake up to visit a sleeping partner. A non-romantic bedmate does not prevent departure. Both partners must be ready for lovin': their cooldowns must have expired, and the usual health and reservation checks must allow it. If several partners can receive a visit, one is chosen at random.

A visit needs a free place that is currently unassigned or assigned to the visitor. A returning owner tries their permanently assigned place first. Beds with three or more places can receive visits too. Other bedmates must be willing to share with the visitor; only the chosen host needs to be a romantic partner.

Sleeping or travelling to a temporary place on the same map releases the pawn's permanent place for visits. Simply being away from bed does not release it. When a temporary stay ends, the pawn's permanent place becomes assigned to them again unless another temporary sleeper holds it. A returning owner must find another available place while that stay continues.

Visits are considered at most once per in-game hour, using RimWorld's lovin' chance for the chosen pair. Going to bed does not trigger a separate visit roll. Pawns sharing with a romantic partner, or waiting for one on the way to join them, won't leave to make a visit.

Select a bed and use the **Night visits** button to allow or prevent visits to that bed. The button appears on beds with more than one place when a permanent owner has a partner assigned to another bed. Visits are enabled by default, and the setting is saved with the bed. Turning visits off prevents new visits; an arranged visit can finish. The toggle does not prevent polycule bedroom sharing.

A visitor travels to the chosen partner, does lovin', then stays to sleep. If the partner leaves or becomes unavailable during the journey, the visit is cancelled. Sharing a bed does not let non-partners do lovin'. Romantic partners sharing despite their ideoligions retain vanilla lovin' and its mood and opinion penalties. Bedroom mood effects come from the room where the pawn sleeps.

Normal bed restrictions apply. Pawns who are drafted, downed, in a mental state, deathresting, imprisoned or in need of medical rest cannot use these features. Pawns ordered to sleep by the player won't wake up for visits. Medical beds, prisoner beds, cribs and deathrest caskets are excluded.

## Building and installing

Build from the repository root with the .NET 10 SDK:

```powershell
dotnet build .\Yobai\Yobai.csproj
```

Copy or link `ModBin/Yobai` into RimWorld's `Mods` folder, then enable Yobai and the [Disharmony mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3818084845), loaded before Yobai. Building the repository solution also builds Yobai. Builds made with a local Disharmony checkout bundle Disharmony instead; see [Building the mods](../BUILDING.md).

## For mod authors

An XML patch adds `Yobai.CompBedSharing` to `BedBase`. Beds with a separate XML inheritance chain need to add that comp to support Yobai. Bedroom sharing uses the regular `LayDown` job and driver. Night visits use the regular `Lovin` job and driver, then resume normal sleep.

See the [design notes](DESIGN.md) for assignment rules and patch integration, and the [test instructions](../Yobai.Tests/README.md) for development testing.
