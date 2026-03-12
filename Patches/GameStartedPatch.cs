using SPT.Reflection.Patching;
using EFT.InventoryLogic;
using EFT;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Comfort.Common;
using DrakiaXYZ.LootRadius.Helpers;

namespace DrakiaXYZ.LootRadius.Patches
{
    
    /// Fires on GameWorld.OnGameStarted — creates the per-player virtual "nearby loot"
    /// stash that the inventory panel will display while in-raid.
   
    public class GameStartedPatch : ModulePatch
    {
        private static StashItemClass _stash
        {
            get => LootRadiusPlugin.RadiusStash;
            set => LootRadiusPlugin.RadiusStash = value;
        }

        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPostfix]
        public static void PatchPostfix()
        {
            if (_stash != null)
                return;

            foreach (var player in Singleton<GameWorld>.Instance.RegisteredPlayers)
            {
                if (player.IsAI)
                    continue;

                // Derive a deterministic 24-char hex stash ID from the profile ID
                // so it's constant across sessions but won't collide with BSG IDs.
                string stashId = GetProfileStashId(player.ProfileId);

                var stash        = Singleton<ItemFactoryClass>.Instance.CreateFakeStash(stashId);
                var stashGrid    = new LootRadiusStashGrid("lootRadiusGrid", stash);
                stash.Grids      = new StashGridClass[] { stashGrid };

                // Register as an item owner so the game's inventory system can route
                // pick-up events correctly.
                var controller = new TraderControllerClass(
                    stash,
                    LootRadiusStashGrid.GRIDID,
                    "Nearby Items",
                    false,
                    EOwnerType.Profile);

                Singleton<GameWorld>.Instance.ItemOwners.Add(controller, default(GameWorld.GStruct162));

                if (player.ProfileId == GamePlayerOwner.MyPlayer.ProfileId)
                    _stash = stash;
            }
        }

      
        /// Produces a stable 24-char lowercase hex ID derived from a SHA-256 of the
        /// profile ID. This gives us a deterministic MongoId-format string that is
        /// unique to the player without hardcoding anything.
       
        private static string GetProfileStashId(string profileId)
        {
            byte[] input     = Encoding.UTF8.GetBytes(profileId);
            byte[] hashBytes = SHA256.Create().ComputeHash(input);
            var sb = new StringBuilder(64);
            foreach (var b in hashBytes)
                sb.Append(b.ToString("X2"));
            return sb.ToString().Substring(0, 24).ToLower();
        }
    }
}
