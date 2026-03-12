using BepInEx;
using DrakiaXYZ.LootRadius.Helpers;
using DrakiaXYZ.LootRadius.Patches;

namespace DrakiaXYZ.LootRadius
{
    [BepInPlugin("xyz.drakia.lootradius", "DrakiaXYZ-LootRadius", "1.5.0")]
    // Minimum SPT core version for SPT 4.0.x.
    // BepInDependency uses semantic version comparison, so "4.0.0" requires
    // any 4.x core to be present at startup.
    [BepInDependency("com.SPT.core", "4.0.0")]
    public class LootRadiusPlugin : BaseUnityPlugin
    {
        public static StashItemClass RadiusStash;

        private void Awake()
        {
            Settings.Init(Config);

            new GameStartedPatch().Enable();
            new LootPanelOpenPatch().Enable();
            new LootPanelClosePatch().Enable();
            new QuestItemDragPatch().Enable();
            new LootRadiusQuickMovePatch().Enable();
        }
    }
}
