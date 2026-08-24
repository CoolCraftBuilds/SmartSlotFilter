using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.UI.Items;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Core.Items.Framework;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;

// The fifth argument is the download link. MelonLoader reads it to tell players
// where an installed mod came from, so it has to be the Nexus page rather than
// the repo -- that is where an update would appear.
[assembly: MelonInfo(typeof(SmartSlotFilter.SmartSlotFilterMod), "Smart Slot Filter", "1.0.0", "CoolCraftBuilds",
    "https://www.nexusmods.com/schedule1/mods/2481")]
// The UpdatesChecker mod is what tells a player their copy is out of date, and
// it reads neither the download link above nor MelonInfo: it looks for an
// AssemblyMetadata entry keyed "NexusModID" and otherwise falls back to a file
// the player has to fill in by hand. Confirmed by reading its assembly, since
// almost no Schedule I mod ships this and the convention is undocumented.
[assembly: System.Reflection.AssemblyMetadata("NexusModID", "2481")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SmartSlotFilter
{
    public class SmartSlotFilterMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Smart Slot Filter initialized.");

            // Temporary, for the copy-whole-layout work: see Probe.cs.
            Probe.DumpShapes();
        }
    }

    [HarmonyPatch(typeof(FilterConfigPanel), nameof(FilterConfigPanel.OpenDropdown))]
    public static class FilterConfigPanel_OpenDropdown_Patch
    {
        static void Postfix(FilterConfigPanel __instance)
        {
            AddSetFromCurrentButton(__instance);
        }

        static void AddSetFromCurrentButton(FilterConfigPanel panel)
        {
            var dropdown = panel.Dropdown;
            if (dropdown == null)
            {
                Melon<SmartSlotFilterMod>.Logger.Msg("Dropdown is null!");
                return;
            }

            var clearButton = panel.ClearButton;
            if (clearButton == null)
            {
                Melon<SmartSlotFilterMod>.Logger.Msg("ClearButton is null!");
                return;
            }

            var buttonParent = clearButton.transform.parent;

            // Don't add twice — check in the actual parent, not panel.Dropdown
            if (buttonParent.Find("SetFromCurrentButton") != null) return;
            // (SetAllFromCurrentButton is added in the same pass, so one check suffices)

            Melon<SmartSlotFilterMod>.Logger.Msg($"Adding button. Parent: {buttonParent.name}, children: {buttonParent.childCount}");

            // Clone the Clear button for matching style
            var newButtonObj = GameObject.Instantiate(clearButton.gameObject, buttonParent);
            newButtonObj.name = "SetFromCurrentButton";

            // Update label text
            var label = newButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "From current item";
                Melon<SmartSlotFilterMod>.Logger.Msg("Label set.");
            }
            else
            {
                Melon<SmartSlotFilterMod>.Logger.Msg("No TextMeshProUGUI found on button!");
            }

            // Place at top of dropdown
            newButtonObj.transform.SetAsFirstSibling();

            // Wire up click
            var button = newButtonObj.GetComponent<Button>();
            Neutralise(button);
            button.onClick.AddListener(new System.Action(() =>
            {
                OnSetFromCurrentClicked(panel);
            }));

            // --- "Filter all slots" button ---
            var allButtonObj = GameObject.Instantiate(clearButton.gameObject, buttonParent);
            allButtonObj.name = "SetAllFromCurrentButton";

            var allLabel = allButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (allLabel != null) allLabel.text = "Filter all from items";

            allButtonObj.transform.SetSiblingIndex(1); // just below the first button

            var allButton = allButtonObj.GetComponent<Button>();
            Neutralise(allButton);
            allButton.onClick.AddListener(new System.Action(() =>
            {
                OnSetAllFromItemsClicked(panel);
            }));

            // Force layout rebuild so container resizes
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());

            Melon<SmartSlotFilterMod>.Logger.Msg("Button added successfully.");
        }

        /// <summary>
        /// Strips a cloned button of the behaviour it came with.
        ///
        /// Both buttons here are clones of the panel's Clear button, and
        /// RemoveAllListeners only drops listeners added in code -- the one serialised
        /// on the prefab survives. Each button was therefore clearing the slot's filter
        /// alongside its own action. Mostly invisible, because both immediately set a
        /// filter of their own; on a slot with no item the clear was all that happened.
        /// </summary>
        static void Neutralise(Button button)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);

            button.onClick.RemoveAllListeners();
        }

        static void OnSetAllFromItemsClicked(FilterConfigPanel panel)
        {
            var slot = panel.OpenSlot;
            if (slot == null || slot.SlotOwner == null) return;

            var allSlots = slot.SlotOwner.ItemSlots;
            int count = 0;

            foreach (var s in allSlots)
            {
                if (s.ItemInstance == null) continue;
                var baseInstance = s.ItemInstance.Cast<BaseItemInstance>();
                string id = baseInstance.ID;

                var filter = new SlotFilter();
                filter.Type = SlotFilter.EType.Whitelist;
                filter.ItemIDs = new Il2CppSystem.Collections.Generic.List<string>();
                filter.ItemIDs.Add(id);

                s.SetPlayerFilter(filter, true);
                count++;
            }

            Melon<SmartSlotFilterMod>.Logger.Msg($"Set filters on {count} slot(s).");
            panel.RefreshDisplay();
            panel.CloseDropdown();
        }

        static void OnSetFromCurrentClicked(FilterConfigPanel panel)
        {
            var slot = panel.OpenSlot;
            if (slot == null)
            {
                Melon<SmartSlotFilterMod>.Logger.Msg("OpenSlot is null.");
                return;
            }

            var itemInstance = slot.ItemInstance;
            if (itemInstance == null)
            {
                Melon<SmartSlotFilterMod>.Logger.Msg("Slot is empty — no item to set filter from.");
                return;
            }

            var baseInstance = itemInstance.Cast<BaseItemInstance>();
            string itemId = baseInstance.ID;
            Melon<SmartSlotFilterMod>.Logger.Msg($"Setting filter to item ID: {itemId}");

            var newFilter = new SlotFilter();
            newFilter.Type = SlotFilter.EType.Whitelist;
            newFilter.ItemIDs = new Il2CppSystem.Collections.Generic.List<string>();
            newFilter.ItemIDs.Add(itemId);

            slot.SetPlayerFilter(newFilter, true);
            panel.RefreshDisplay();
            panel.CloseDropdown();
        }
    }
}
