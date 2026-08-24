# Smart Slot Filter

[On Nexus Mods](https://www.nexusmods.com/schedule1/mods/2481)

A small quality-of-life mod for **Schedule I** that sets a storage slot's filter from
whatever is already sitting in it.

Slot filters are the game's way of reserving a slot for one kind of item, which is what
keeps employees from stuffing the wrong thing into the wrong place. Setting one up means
opening the filter panel, searching a list, and picking the item — for every slot. If the
container is already sorted the way you want it, all of that information is right there in
the slots.

## What it adds

Two buttons in the slot filter dropdown:

| Button | What it does |
|---|---|
| **From current item** | Sets this slot's filter to a whitelist of the item currently in it |
| **Filter all from items** | Does the same for every non-empty slot in the container, in one click |
| **Copy all filters** | Copies the whole container's filter layout |
| **Paste all filters** | Applies it to another container of the same kind |
| **Clear all filters** | Clears every filter in the container |

Empty slots are skipped by *Filter all from items* — there is nothing to read a filter from.

Every entry this mod adds says "all", so a glance separates them from the game's own
per-slot Copy, Paste and Clear sitting in the same menu.

### Copying a layout

Filters are duplicated with the game's own `SlotFilter.Clone()`, so everything a filter
holds travels with it — the item list, the filter type, and the **allowed qualities**. The
quality row is the part that is tedious to redo by hand, and it is why copying a single
slot at a time falls short when eight stations run the same recipe.

A paste onto a different kind of container, or one with a different number of slots, is
**refused** rather than partly applied: half a layout on a station you believed was
identical only shows up later, in the mix. The reason appears on the button you clicked.

A slot that had no filter in the copy clears the matching slot on the target. A layout is a
whole; leaving old filters in place would blend two layouts. Pasting a layout that contains
no filters at all is refused — use *Clear all filters*, which exists so that clearing is a
choice rather than a side effect.

## Requirements

- Schedule I (IL2CPP build)
- [MelonLoader](https://melonwiki.xyz/) 0.7.3 or newer

## Installing

Drop `SmartSlotFilter.dll` into the game's `Mods` folder.

If you use the MLVScan plugin, add `SmartSlotFilter.dll` to `WhitelistedMods` under
`[MLVScan]` in `UserData/MelonPreferences.cfg`, or it will be disabled on startup.

## Building

```bash
dotnet build -c Release
```

The project references MelonLoader and the game's Il2Cpp assemblies **in place** from the
Steam install, so nothing from the game is redistributed here. If your install is
elsewhere, copy `Directory.Build.props.example` to `Directory.Build.props` and set
`GameDir`; that file is gitignored.

Output lands in `bin/Release/net6.0/`.

## Compatibility

Built on **0.4.6f5** and confirmed working on **0.4.6f13**. The mod hooks
`FilterConfigPanel.OpenDropdown` and reads
`ItemSlot` / `SlotFilter`, so a rename in any of those breaks it — expect to rebuild after
larger game updates.

## Licence

[MIT](LICENSE). Reuse it, fork it, ship a fixed build if this one breaks on a game update
and I am not around — just keep the copyright notice.
