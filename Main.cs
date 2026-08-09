using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.UI.Items;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Core.Items.Framework;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;

[assembly: MelonInfo(typeof(SlotFilterFromItem.SlotFilterFromItemMod), "SlotFilterFromItem", "1.0.0", "SFox")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SlotFilterFromItem
{
    public class SlotFilterFromItemMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("SlotFilterFromItem initialized.");
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
                Melon<SlotFilterFromItemMod>.Logger.Msg("Dropdown is null!");
                return;
            }

            var clearButton = panel.ClearButton;
            if (clearButton == null)
            {
                Melon<SlotFilterFromItemMod>.Logger.Msg("ClearButton is null!");
                return;
            }

            var buttonParent = clearButton.transform.parent;

            // Don't add twice — check in the actual parent, not panel.Dropdown
            if (buttonParent.Find("SetFromCurrentButton") != null) return;
            // (SetAllFromCurrentButton is added in the same pass, so one check suffices)

            Melon<SlotFilterFromItemMod>.Logger.Msg($"Adding button. Parent: {buttonParent.name}, children: {buttonParent.childCount}");

            // Clone the Clear button for matching style
            var newButtonObj = GameObject.Instantiate(clearButton.gameObject, buttonParent);
            newButtonObj.name = "SetFromCurrentButton";

            // Update label text
            var label = newButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "From current item";
                Melon<SlotFilterFromItemMod>.Logger.Msg("Label set.");
            }
            else
            {
                Melon<SlotFilterFromItemMod>.Logger.Msg("No TextMeshProUGUI found on button!");
            }

            // Place at top of dropdown
            newButtonObj.transform.SetAsFirstSibling();

            // Wire up click
            var button = newButtonObj.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
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
            allButton.onClick.RemoveAllListeners();
            allButton.onClick.AddListener(new System.Action(() =>
            {
                OnSetAllFromItemsClicked(panel);
            }));

            // Force layout rebuild so container resizes
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());

            Melon<SlotFilterFromItemMod>.Logger.Msg("Button added successfully.");
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

            Melon<SlotFilterFromItemMod>.Logger.Msg($"Set filters on {count} slot(s).");
            panel.RefreshDisplay();
            panel.CloseDropdown();
        }

        static void OnSetFromCurrentClicked(FilterConfigPanel panel)
        {
            var slot = panel.OpenSlot;
            if (slot == null)
            {
                Melon<SlotFilterFromItemMod>.Logger.Msg("OpenSlot is null.");
                return;
            }

            var itemInstance = slot.ItemInstance;
            if (itemInstance == null)
            {
                Melon<SlotFilterFromItemMod>.Logger.Msg("Slot is empty — no item to set filter from.");
                return;
            }

            var baseInstance = itemInstance.Cast<BaseItemInstance>();
            string itemId = baseInstance.ID;
            Melon<SlotFilterFromItemMod>.Logger.Msg($"Setting filter to item ID: {itemId}");

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
