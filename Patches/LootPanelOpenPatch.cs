using SPT.Reflection.Patching;
using EFT.Interactive;
using EFT.UI;
using EFT;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Comfort.Common;
using DrakiaXYZ.LootRadius.Helpers;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;

namespace DrakiaXYZ.LootRadius.Patches
{
    
    /// Fires after ItemsPanel.Show — populates the virtual loot-radius grid with any
    /// LootItems within range and injects it as the right-hand panel in the inventory UI.
    
    public class LootPanelOpenPatch : ModulePatch
    {
        // Reflects into ItemUiContext to register our virtual stash as a Ctrl+Click target.
        private static FieldInfo   _rightPaneField;
        // Reflects into SimpleStashPanel to access its internal SearchableItemView.
        private static FieldInfo   _simplePanelField;
        // Reflects into SearchableItemView to get the ContainedGridsView (and thus GridViews[]).
        private static FieldInfo   _containedGridsViewField;

        private static readonly LayerMask _interactiveLayerMask =
            1 << LayerMask.NameToLayer("Interactive");

        private static StashItemClass _stash
        {
            get => LootRadiusPlugin.RadiusStash;
            set => LootRadiusPlugin.RadiusStash = value;
        }

        protected override MethodBase GetTargetMethod()
        {
            // CompoundItem[] field on ItemUiContext drives Ctrl+Click behaviour.
            _rightPaneField = AccessTools
                .GetDeclaredFields(typeof(ItemUiContext))
                .Single(x => x.FieldType == typeof(CompoundItem[]));

            _simplePanelField = AccessTools.Field(
                typeof(SimpleStashPanel), "_simplePanel");

            _containedGridsViewField = AccessTools.Field(
                typeof(SearchableItemView), "containedGridsView_0");

            return typeof(ItemsPanel).GetMethod(nameof(ItemsPanel.Show));
        }

        [PatchPostfix]
        public static async void PatchPostfix(
            ItemsPanel              __instance,
            Task                    __result,
            ItemContextAbstractClass sourceContext,
            CompoundItem            lootItem,
            InventoryController     inventoryController,
            ItemsPanel.EItemsTab    currentTab,
            SimpleStashPanel        ____simpleStashPanel,
            AddViewListClass        ___UI)
        {
            // Wait for the vanilla Show() to finish before we inject.
            await __result;

            // If there's already a right-hand loot panel (opening a container etc.)
            // do nothing — we only activate when the bare player inventory opens.
            if (lootItem != null)
                return;

            var grid           = (LootRadiusStashGrid)_stash.Grids[0];
            var playerPosition = Singleton<GameWorld>.Instance.MainPlayer.Position;

            // 1. Items directly at feet (~0.35 m), ignoring line-of-sight.
            //    Catches items slightly below the floor the player is standing on.
            var floorColliders = Physics.OverlapSphere(
                playerPosition, 0.35f, _interactiveLayerMask);
            AddNearbyItems(grid, floorColliders, ignoreLineOfSight: true);

            // 2. Items in the configurable radius around the player's centre.
            var bodyColliders = Physics.OverlapSphere(
                playerPosition + Vector3.up * 0.5f,
                Settings.LootRadius.Value,
                _interactiveLayerMask);
            AddNearbyItems(grid, bodyColliders, ignoreLineOfSight: false);

            // SimpleStashPanel.Show signature in 4.0.13:
            // Show(CompoundItem, InventoryController, ItemContextAbstractClass, bool inRaid,
            //      SortingTableItemClass sortingTable, EStashSearchAvailability,
            //      InventoryController leftSide = null, EItemsTab tab = Gear)
            ____simpleStashPanel.Show(
                _stash,
                inventoryController,
                sourceContext.CreateChild(_stash),
                true,
                null,
                SimpleStashPanel.EStashSearchAvailability.Unavailable,
                null,
                currentTab);

            ___UI.AddDisposable<SimpleStashPanel>(____simpleStashPanel);

            _rightPaneField.SetValue(
                ItemUiContext.Instance, new CompoundItem[] { _stash });

            // Cache the GridViews so OwnerRemoveItemEvent can update the UI live.
            var simplePanel        = _simplePanelField.GetValue(____simpleStashPanel) as SearchableItemView;
            var containedGridsView = _containedGridsViewField.GetValue(simplePanel) as ContainedGridsView;
            grid.GridViews         = containedGridsView.GridViews;
        }

   
        /// Iterates colliders, finds LootItem components, and adds eligible items to the grid.
        /// Also wires up the RemoveItemEvent so the UI reacts when a looted item disappears.
   
        private static void AddNearbyItems(
            LootRadiusStashGrid grid, Collider[] colliders, bool ignoreLineOfSight)
        {
            foreach (var collider in colliders)
            {
                var lootItem = collider.gameObject.GetComponentInParent<LootItem>();
                if (lootItem == null)
                    continue;

                // Skip items already inside this grid or not in line of sight.
                if (lootItem.Item.Parent.Container.ID == grid.ID)
                    continue;

                if (!ignoreLineOfSight && !IsLineOfSight(lootItem.transform.position))
                    continue;

                lootItem.ItemOwner.RemoveItemEvent += grid.OwnerRemoveItemEvent;
                grid.AddInternal(lootItem.Item, grid.FindFreeSpace(lootItem.Item), false, true);
            }
        }

     
        /// Returns true if there is an unobstructed line from the player's head
        /// to <paramref name="endPos"/>.
     
        private static bool IsLineOfSight(Vector3 endPos)
        {
            Vector3 headPos = Singleton<GameWorld>.Instance
                .MainPlayer.MainParts[BodyPartType.head].Position;

            // Linecast hits HighPolyColliders (solid world geometry).
            // A hit means the item is obscured, so return false.
            return !Physics.Linecast(headPos, endPos, LayerMaskClass.HighPolyWithTerrainMask);
        }
    }
}
