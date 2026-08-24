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
        private static int _filled;

        public static bool HasCopy => _filters.Count > 0;

        public static string Copy(ItemSlot slot)
        {
            var owner = slot.SlotOwner;
            if (owner == null) return "Nothing to copy from";

            var slots = owner.ItemSlots;
            _filters.Clear();
            _ownerType = OwnerType(owner);
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

            _filled = withFilter;
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
            var type = OwnerType(owner);
            if (type.Length == 0 || _ownerType.Length == 0)
            {
                // Two unidentified containers are not evidence of being the same kind.
                Log("container type unknown on one side; slot count is the only guard");
            }
            else if (type != _ownerType)
            {
                Log($"refused paste: copied from {Short(_ownerType)}, target is {Short(type)}");
                return "Different kind of container";
            }

            // An empty layout pasted over a configured station wipes it, and nobody
            // copies "nothing" on purpose now that clearing has its own button. This
            // used to be the only way to clear a whole container, which is exactly why
            // it is worth refusing: the accident and the old workaround look identical.
            if (_filled == 0)
            {
                Log("refused paste: the copied layout has no filters");
                return "Nothing copied - use Clear all";
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

        /// <summary>
        /// Clears every filter in the container.
        ///
        /// The game clears one slot at a time. Clearing a whole station used to mean
        /// copying an unconfigured one over it -- which works, and is indistinguishable
        /// from doing that by accident. Giving the intent its own button is what makes
        /// refusing the accident reasonable.
        /// </summary>
        public static string ClearAll(ItemSlot slot)
        {
            var owner = slot.SlotOwner;
            if (owner == null) return "Nothing to clear";

            var cleared = 0;
            var skipped = 0;
            foreach (var s in owner.ItemSlots)
            {
                if (!s.CanPlayerSetFilter) { skipped++; continue; }

                var f = s.PlayerFilter;
                if (f != null && !f.IsDefault()) cleared++;

                s.SetPlayerFilter(new SlotFilter(), true);
            }

            Log($"cleared {cleared} filter(s) on {Short(OwnerType(owner))}"
                + (skipped > 0 ? $", skipped {skipped} slot(s) the player cannot filter" : ""));
            return $"Cleared {cleared} filter(s)";
        }

        /// <summary>
        /// Identifies what kind of container this is.
        ///
        /// owner.GetType() is useless here: IItemSlotOwner is a plain interface in the
        /// interop assembly, so every container answers "IItemSlotOwner" and a type
        /// comparison silently always passes. Most owners are Unity components, and a
        /// component knows its real Il2Cpp class -- so ask that, and say so plainly when
        /// it cannot be had rather than pretending the check ran.
        /// </summary>
        private static string OwnerType(IItemSlotOwner owner)
        {
            try
            {
                var component = owner.TryCast<UnityEngine.Component>();
                if (component != null)
                {
                    var name = component.GetIl2CppType().FullName;
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            catch { /* fall through to the honest answer */ }

            Log($"could not identify container type ({owner.GetType().FullName}); "
                + "falling back to slot count only");
            return "";
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
