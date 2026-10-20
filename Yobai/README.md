# Yobai

Yobai lets pawns share beds within polycule bedrooms and make night visits to partners without having to change their bed assignments by hand.

A polycule includes a pawn's spouses, fiancés and lovers, their partners, and so on. For example, if Anya is married to Bo and Bo is Cora's lover, all three belong to the same polycule. Anya and Cora can share a bedroom. They prefer separate beds, but can share a bed as a last resort.

If you use Ideology, a romantic relationship connects the polycule only when both partners' ideoligions allow them to share a bed. The last-resort sleeping rule below does not change which relationships connect the polycule.

## Polycule bedrooms

Vanilla already treats connected spouses, fiancés and lovers as a bedroom group, even when their ideoligions prohibit bed sharing. Yobai keeps those bedroom rules. Children can still share with their parents, and unassigned beds can still make the room a barracks.

The whole polycule does not need to sleep in one room. In the example above, Anya and Cora can have a shared bedroom even if Bo sleeps elsewhere.

When a pawn goes to sleep in a polycule bedroom, they choose a bed as follows:

1. Join a partner already in bed, if there is space. If several beds are available, choose one at random.
2. Otherwise, choose a random empty bed in that room assigned to a member of the polycule.
3. If neither choice works, share an available bed with another member of the polycule, even if they aren't partners or their ideoligions normally forbid sharing.

Pawns already on their way to a bed count as bedmates when making these choices, including owners returning to their assigned beds. Their chosen places are kept for them during the trip. Each pair has a chance to get together, and the choice stays the same while the pawn walks to bed and sleeps. Their assigned beds stay the same.

## Night visits

A pawn going to bed first tries to join a partner in their own polycule bedroom. If they don't find one there, they have a 50% chance to visit a partner sleeping alone in a double bed. If more than one partner can receive a visit, each bed has an equal chance of being chosen.

Night visits require a free place that isn't assigned to anyone. A place assigned to an absent pawn is kept for that pawn.

A pawn sleeping alone may also wake up and go to a partner's bed. These opportunities occur at random, with an average wait of about four in-game hours when a visit is possible. Pawns already sharing a bed, or waiting for someone on the way to join them, won't leave to make a visit.

Select a double bed and use the **Night visits** button to allow or prevent visits to that bed. Night visits are enabled by default, and the setting is saved with the bed. The button appears when the bed has an unassigned place and an assigned pawn has a partner assigned to a different bed. Turning visits off prevents new visits; an already arranged visit can finish. It does not stop pawns in a polycule bedroom from sharing its beds.

Normal bed restrictions still apply, and the visitor must be able to reach the bed. Pawns who are drafted, downed, in a mental state, deathresting or in need of medical rest don't take part. Prisoners are excluded, and pawns ordered to sleep by the player won't wake up for visits.

Once partners are together, RimWorld decides whether they do lovin' as usual. Sharing a bed does not make non-partners eligible for lovin'. If romantic partners share as a last resort despite their ideoligions, vanilla lovin' and its mood and opinion penalties still apply. Bedroom mood effects come from the room where they actually sleep.

## Building and installing

Yobai is for RimWorld 1.7 and does not require any DLC. To build it, run this command from the repository root:

```powershell
dotnet build .\Yobai\Yobai.csproj
```

Copy or link the resulting `ModBin/Yobai` folder into RimWorld's `Mods` folder, then enable Yobai in the game's mod list. Building the repository solution also builds Yobai.

The default build requires the separate Disharmony mod, loaded before Yobai. If you build with a local Disharmony checkout, the build includes Disharmony for you. See [Building the mods](../BUILDING.md) for setup instructions.

## For mod authors

An XML patch adds `Yobai.CompBedSharing` to `BedBase`, so beds that inherit from it support temporary sleeping assignments and night visits. Beds with a separate XML inheritance chain need to add that comp themselves. Yobai excludes medical beds, prisoner beds, cribs and deathrest caskets from bed sharing. Temporary sleepers use the regular `LayDown` job and `JobDriver_LayDown`.

The [DevTools tests](../Yobai.Tests/README.md) are an optional development project. They are not needed to play.

The [design notes](DESIGN.md) describe bed selection, temporary assignments and their interaction with vanilla jobs.
