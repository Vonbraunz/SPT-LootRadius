using EFT.InventoryLogic;
using System;
using EFT.UI.DragAndDrop;
using EFT;
using Comfort.Common;

// -----------------------------------------------------------------------
// GClass alias map — verified against SPT 4.0.13 Assembly-CSharp.dll
// via dnSpy. If you upgrade SPT, re-check these against the new DLL.
// -----------------------------------------------------------------------
using StashGridCollectionClass         = GClass3120;
using FreeSpaceInventoryErrorClass     = StashGridClass.GClass1544;
using FilterInventoryErrorClass        = StashGridClass.GClass1545;
using RemoveInventoryErrorClass        = StashGridClass.GClass1546;
using MaxCountInventoryErrorClass      = StashGridClass.GClass1548;
using ContainerRemoveEventClass        = GClass3413;
using ContainerAddEventClass           = GClass3415;
using ContainerRemoveEventResultStruct = GStruct154<GClass3413>;
using ContainerAddEventResultStruct    = GStruct154<GClass3415>;

namespace DrakiaXYZ.LootRadius.Helpers
{
   
    /// Custom StashGrid that bypasses parent-ownership validation and only
    /// allows removing items (no internal moves). This is the virtual "nearby
    /// loot" grid shown in the player's inventory panel while in-raid.
    class LootRadiusStashGrid : StashGridClass
    {
    
        /// Stable owner/grid ID used to identify this virtual grid throughout
        /// the codebase. Must be a valid 24-char hex MongoId.
        public static readonly string GRIDID = "67e0b18aeef9ae200b0495f0";

        /// GridView array set by LootPanelOpenPatch so live removal events update the UI.
        public GridView[] GridViews { get; set; } = null;

        public override StashGridCollectionClass ItemCollection { get; } = new LootRadiusStashGridCollection();

        public LootRadiusStashGrid(string id, CompoundItem parentItem)
            : base(id, 10, 10, true, false, Array.Empty<ItemFilter>(), parentItem, -1) { }

        /// Prevent moving items already present in the grid; new items can always be added.
        public override bool CheckCompatibility(Item item) => !Contains(item);

        /// Simplified add — we know the data is sane and intentionally avoid changing
        /// the item's existing address during the add.
        public override ContainerAddEventResultStruct AddInternal(
            Item item, LocationInGrid location, bool simulate, bool ignoreRestrictions)
        {
            if (location == null)
                return new FreeSpaceInventoryErrorClass(item, this);

            if (!ignoreRestrictions && !CheckCompatibility(item))
                return new FilterInventoryErrorClass(item, this);

            GInterface407 resizeResult = default(GStruct424);
            var newAddress = CreateItemAddress(location);

            if (simulate)
                return new ContainerAddEventClass(this, item, newAddress, item.StackObjectsCount, resizeResult, true);

            XYCellSizeStruct originalGridSize = new XYCellSizeStruct(GridWidth, GridHeight);
            method_9(item, location);
            XYCellSizeStruct newGridSize = new XYCellSizeStruct(GridWidth, GridHeight);

            if (originalGridSize != newGridSize)
                resizeResult = new GStruct425(this, originalGridSize, newGridSize);

            return new ContainerAddEventClass(this, item, newAddress, item.StackObjectsCount, resizeResult, false);
        }
        
        /// Simplified remove — no extra validation needed for a virtual read-only grid.
        public override ContainerRemoveEventResultStruct RemoveInternal(
            Item item, bool simulate, bool ignoreRestrictions)
        {
            if (!base.Contains(item))
                return new RemoveInventoryErrorClass(item, this);

            LocationInGrid locationInGrid = ItemCollection[item];
            if (!simulate)
                base.method_10(item, locationInGrid, true);

            return new ContainerRemoveEventClass(item, base.CreateItemAddress(locationInGrid), simulate);
        }

        /// Called when an item owner fires a RemoveItemEvent (i.e. the player picks up
        /// or the item despawns in-world). Propagates the removal to any open GridViews
        /// so the UI stays in sync without requiring a relog.
        public void OwnerRemoveItemEvent(GEventArgs3 args)
        {
            if (args.Status != CommandStatus.Succeed)
                return;

            // Ignore child-item events; the grid only tracks top-level loot items.
            if (args.From.Container.ParentItem != args.Item)
                return;

            var owner = Singleton<GameWorld>.Instance.FindOwnerById(args.OwnerId);
            owner.RemoveItemEvent -= OwnerRemoveItemEvent;

            if (GridViews != null && ItemCollection.ContainsKey(args.Item))
            {
                var locationInGrid = ItemCollection[args.Item];
                var item     = args.Item;
                var location = CreateItemAddress(locationInGrid);

                foreach (var gridView in GridViews)
                {
                    gridView.OnItemRemoved(new GEventArgs3(item, location, CommandStatus.Begin,   owner));
                    gridView.OnItemRemoved(new GEventArgs3(item, location, CommandStatus.Succeed, owner));
                }
            }

            RemoveInternal(args.Item, false, false);
        }

        // ----------------------------------------------------------------
        // Inner collection — skips address validation so items can live in
        // multiple containers simultaneously (world + virtual grid).
        // ----------------------------------------------------------------
        internal class LootRadiusStashGridCollection : StashGridCollectionClass
        {
            public override void Add(Item item, StashGridClass grid, LocationInGrid location)
            {
                Dictionary_0[item] = location;
                List_0.Add(item);

                if (item.CurrentAddress == null)
                    item.CurrentAddress = grid.CreateItemAddress(location);
            }

            public override void Remove(Item item, StashGridClass grid)
            {
                Dictionary_0.Remove(item);
                List_0.Remove(item);

                if (item.CurrentAddress?.Container?.ID == grid.ID)
                    item.CurrentAddress = null;
            }
        }
    }
}
