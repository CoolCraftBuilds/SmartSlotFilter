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

            // Buttons persist between openings, so reset any result text the last click
            // left behind -- otherwise "Pasted 4 filter(s)" is still sitting there the
            // next time the panel opens and reads as a fresh claim.
            if (buttonParent.Find("SetFromCurrentButton") != null)
            {
                ResetLabel(buttonParent, "CopyLayoutButton", CopyLabel);
                ResetLabel(buttonParent, "PasteLayoutButton", PasteLabel);
                return;
            }

            Melon<SmartSlotFilterMod>.Logger.Msg($"Adding buttons. Parent: {buttonParent.name}, children: {buttonParent.childCount}");

            AddButton(buttonParent, clearButton, "SetFromCurrentButton", "From current item", 0,
                      () => OnSetFromCurrentClicked(panel));

            AddButton(buttonParent, clearButton, "SetAllFromCurrentButton", "Filter all from items", 1,
                      () => OnSetAllFromItemsClicked(panel));

            // Whole-container copy/paste. The game's own copy button next to these does
            // one slot; with eight identical stations that is the same panel over and
            // over, and the quality settings are the part people give up on.
            AddButton(buttonParent, clearButton, "CopyLayoutButton", CopyLabel, 2,
                      () => Report(panel, buttonParent, "CopyLayoutButton", LayoutClipboard.Copy(panel.OpenSlot)));

            AddButton(buttonParent, clearButton, "PasteLayoutButton", PasteLabel, 3,
                      () => Report(panel, buttonParent, "PasteLayoutButton", LayoutClipboard.Paste(panel.OpenSlot)));

            // Force layout rebuild so container resizes
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent.GetComponent<RectTransform>());

            Melon<SmartSlotFilterMod>.Logger.Msg("Buttons added successfully.");
        }

        const string CopyLabel = "Copy all filters";
        const string PasteLabel = "Paste all filters";

        static void AddButton(Transform parent, Button template, string name, string text,
                              int siblingIndex, System.Action onClick)
        {
            var obj = GameObject.Instantiate(template.gameObject, parent);
            obj.name = name;
            obj.SetActive(true);

            var label = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
                label.enableWordWrapping = false;
            }

            obj.transform.SetSiblingIndex(siblingIndex);

            var button = obj.GetComponent<Button>();
            Neutralise(button);
            button.onClick.AddListener(onClick);
        }

        /// <summary>
        /// Answers where the player clicked rather than only in the log. A refusal the
        /// player cannot see reads as a broken button, and refusing is the normal
        /// outcome when two stations are not actually the same kind.
        /// </summary>
        static void Report(FilterConfigPanel panel, Transform parent, string buttonName, string message)
        {
            ResetLabel(parent, buttonName, message);

            // Clicking anything in the dropdown closes it, which would hide the answer.
            panel.OpenDropdown();
        }

        static void ResetLabel(Transform parent, string buttonName, string text)
        {
            var button = parent.Find(buttonName);
            if (button == null) return;

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = text;
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
