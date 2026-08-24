using System.Collections.Generic;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI.Items;
using MelonLoader;

namespace SmartSlotFilter
{
    /// <summary>
    /// Copies the filter layout of a whole container, not one slot at a time.
    ///
    /// The game already copies a single slot's filter. With eight chemistry stations
    /// running the same recipe that is eight panels times however many slots, and the
    /// quality settings have to be redone by hand every time -- which is the part
    /// people skip, and then wonder why the wrong grade turns up in the mix.
    ///
    /// Filters are duplicated with the game's own SlotFilter.Clone(), so everything a
    /// filter holds travels: item list, filter type, and allowed qualities. Rebuilding
    /// the object field by field would silently drop whatever the game adds next.
    /// </summary>
    internal static class LayoutClipboard
    {
        // Null entries are meaningful: they mean "this slot had no filter", which must
        // be reproduced, or pasting would leave stale filters behind on the target.
        private static readonly List<SlotFilter?> _filters = new();
        private static string _ownerType = "";
        private static int _slotCount;

        public static bool HasCopy => _filters.Count > 0;

        public static string Copy(ItemSlot slot)
        {
            var owner = slot.SlotOwner;
            if (owner == null) return "Nothing to copy from";

            var slots = owner.ItemSlots;
            _filters.Clear();
            _ownerType = owner.GetType().FullName ?? "";
            _slotCount = slots.Count;

            var withFilter = 0;
            foreach (var s in slots)
            {
                var f = s.PlayerFilter;
                if (f == null || f.IsDefault())
                {
                    _filters.Add(null);
                    continue;
                }

                _filters.Add(f.Clone());
                withFilter++;
            }

            Log($"copied {withFilter} filter(s) across {_slotCount} slot(s) from {Short(_ownerType)}");
            return withFilter == 0
                ? $"Copied: no filters set"
                : $"Copied {withFilter} filter(s)";
        }

        public static string Paste(ItemSlot slot)
        {
            if (!HasCopy) return "Copy a layout first";

            var owner = slot.SlotOwner;
            if (owner == null) return "Nothing to paste into";

            // Refusing beats a partial paste. Half a layout on a station you believed
            // was identical is the kind of wrong that only shows up later, in the mix.
            var type = owner.GetType().FullName ?? "";
            if (type != _ownerType)
            {
                Log($"refused paste: copied from {Short(_ownerType)}, target is {Short(type)}");
                return "Different kind of container";
            }

            var slots = owner.ItemSlots;
            if (slots.Count != _slotCount)
            {
                Log($"refused paste: copied {_slotCount} slot(s), target has {slots.Count}");
                return $"Needs {_slotCount} slots, has {slots.Count}";
            }

            var applied = 0;
            var skipped = 0;
            for (var i = 0; i < slots.Count; i++)
            {
                var target = slots[i];

                // Some slots are the game's to control, not the player's -- an output
                // slot, say. Writing there would either be ignored or fight the game.
                if (!target.CanPlayerSetFilter) { skipped++; continue; }

                var source = _filters[i];
                if (source == null)
                {
                    target.SetPlayerFilter(new SlotFilter(), true);
                    continue;
                }

                target.SetPlayerFilter(source.Clone(), true);
                applied++;
            }

            Log($"pasted {applied} filter(s) onto {Short(type)}"
                + (skipped > 0 ? $", skipped {skipped} slot(s) the player cannot filter" : ""));
            return $"Pasted {applied} filter(s)";
        }

        // Type names here are long and namespaced; the tail is the useful half.
        private static string Short(string fullName)
        {
            var dot = fullName.LastIndexOf('.');
            return dot < 0 ? fullName : fullName.Substring(dot + 1);
        }

        private static void Log(string message)
            => Melon<SmartSlotFilterMod>.Logger.Msg($"[layout] {message}");
    }
}
