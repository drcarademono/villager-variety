using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility;
using UnityEngine;

namespace VillagerVariety
{
    static class VillagerVarietyPopulationManagerProxy
    {
        public static void Register()
        {
            PopulationManager.OnMobileNPCEnable += PopulationManager_OnMobileNPCEnable;
        }

        private static void PopulationManager_OnMobileNPCEnable(PopulationManager.PoolItem poolItem)
        {
            var PlayerGPS = GameManager.Instance.PlayerGPS;

            // Daggerfall makes Swamp and Subtropical climates into Bretons using Redguard names
            // For VV, we replace these with Redguard textures, which the MobilePerson script will replace with a Subtropical variant if available
            if (PlayerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Swamp
                || PlayerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Subtropical)
            {
                if (poolItem.npc.Race != Races.Redguard)
                {
                    poolItem.npc.RandomiseNPC(Races.Redguard);
                }
            }
            else if(PlayerGPS.CurrentRegionIndex == (int)DaggerfallRegions.DragontailMountains
                    || PlayerGPS.CurrentRegionIndex == (int)DaggerfallRegions.Ephesus)
            {
                if (poolItem.npc.Race != Races.Redguard)
                {
                    poolItem.npc.RandomiseNPC(Races.Redguard);
                }
            }
        }
    }
}
