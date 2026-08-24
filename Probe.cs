using System;
using System.Reflection;
using System.Text;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI.Items;
using MelonLoader;

namespace SmartSlotFilter
{
    /// <summary>
    /// Ground truth for the copy-whole-layout feature.
    ///
    /// Copying a filter means reproducing everything the panel shows, and the panel
    /// shows more than this mod has ever written: there is an "Allowed Quality" row
    /// next to the item list. Building the copy against a guessed shape would produce
    /// a paste that looks complete and silently drops the quality settings -- the part
    /// that is tedious enough by hand to be worth copying in the first place.
    ///
    /// IL2CPP strips method bodies, so the only honest source for the shape is the
    /// wrapper Il2CppInterop generated for this build. Runs once, at startup.
    /// </summary>
    internal static class Probe
    {
        public static void DumpShapes()
        {
            Dump(typeof(SlotFilter));
            Dump(typeof(ItemSlot), membersOnly: "Filter,SetPlayerFilter,SetIsAddLocked,ItemInstance,SlotOwner");
            Dump(typeof(FilterConfigPanel), membersOnly: "Copy,Paste,OpenSlot,RefreshDisplay");
        }

        private static void Dump(Type type, string? membersOnly = null)
        {
            var wanted = membersOnly?.Split(',');
            var log = Melon<SmartSlotFilterMod>.Logger;
            log.Msg($"[probe] ===== {type.FullName} =====");

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                                     | BindingFlags.Instance | BindingFlags.Static
                                     | BindingFlags.DeclaredOnly;

            foreach (var f in type.GetFields(flags))
                if (Match(wanted, f.Name))
                    log.Msg($"[probe]   field  {f.FieldType.Name} {f.Name}");

            foreach (var p in type.GetProperties(flags))
                if (Match(wanted, p.Name))
                    log.Msg($"[probe]   prop   {p.PropertyType.Name} {p.Name}"
                            + $" {{{(p.CanRead ? " get;" : "")}{(p.CanWrite ? " set;" : "")} }}");

            foreach (var m in type.GetMethods(flags))
            {
                if (!Match(wanted, m.Name)) continue;
                if (m.Name.StartsWith("get_") || m.Name.StartsWith("set_")) continue;

                var args = new StringBuilder();
                foreach (var a in m.GetParameters())
                {
                    if (args.Length > 0) args.Append(", ");
                    args.Append(a.ParameterType.Name).Append(' ').Append(a.Name);
                }
                log.Msg($"[probe]   method {m.ReturnType.Name} {m.Name}({args})");
            }
        }

        private static bool Match(string[]? wanted, string name)
        {
            if (wanted == null) return true;
            foreach (var w in wanted)
                if (name.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
