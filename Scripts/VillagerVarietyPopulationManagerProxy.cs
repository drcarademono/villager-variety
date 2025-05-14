using System;
using System.Reflection;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Game.Entity;

namespace VillagerVariety
{
    static class VillagerVarietyPopulationManagerProxy
    {
        // cache the private 'maxPopulation' field on PopulationManager
        static FieldInfo _maxPopulationField;

        // short density fields
        static int farm, village, town, city;

        public static void SetDensities(int farmDensity, int villageDensity, int townDensity, int cityDensity)
        {
            farm    = farmDensity;
            village = villageDensity;
            town    = townDensity;
            city    = cityDensity;
        }


        public static void Register()
        {
            // grab the private field once
            _maxPopulationField = typeof(PopulationManager)
                .GetField("maxPopulation", BindingFlags.Instance | BindingFlags.NonPublic);

            // hook the moment a mobile is enabled
            PopulationManager.OnMobileNPCEnable += PopulationManager_OnMobileNPCEnable;

            // also bump the cap every hour so day/night transitions re‑compute
            WorldTime.OnNewHour += UpdatePopulationCap;
        }

        private static void PopulationManager_OnMobileNPCEnable(PopulationManager.PoolItem poolItem)
        {
            // Recompute the cap
            UpdatePopulationCap();

            // Climate → Redguard swap
            var gps = GameManager.Instance.PlayerGPS;
            if (gps.CurrentClimateIndex == (int)MapsFile.Climates.Swamp
             || gps.CurrentClimateIndex == (int)MapsFile.Climates.Subtropical
             || gps.CurrentRegionIndex == (int)DaggerfallRegions.DragontailMountains
             || gps.CurrentRegionIndex == (int)DaggerfallRegions.Ephesus)
            {
                if (poolItem.npc.Race != Races.Redguard)
                    poolItem.npc.RandomiseNPC(Races.Redguard);
            }
        }

        static void UpdatePopulationCap()
        {
            var locationRoot = GameManager.Instance.StreamingWorld.CurrentPlayerLocationObject;
            if (locationRoot == null) return;
            var pm = locationRoot.GetComponent<PopulationManager>();
            if (pm == null)      return;

            // figure out how many "16‑block chunks" this location has
            var loc = pm.GetComponent<DaggerfallLocation>();
            int totalBlocks = loc.Summary.BlockWidth * loc.Summary.BlockHeight;
            int popChunks  = Mathf.Clamp(totalBlocks / 16, 1, 4);

            // decide the per‑chunk multiplier
            var gps    = GameManager.Instance.PlayerGPS;
            bool storm = GameManager.Instance.WeatherManager.IsRaining
                      || GameManager.Instance.WeatherManager.IsStorming
                      || GameManager.Instance.WeatherManager.IsSnowing;
            bool day   = DaggerfallUnity.Instance.WorldTime.Now.IsDay;

            int perChunk;
            switch (gps.CurrentLocationType)
            {
                case DFRegion.LocationTypes.HomeFarms:
                    perChunk = farm;
                    break;
                case DFRegion.LocationTypes.ReligionTemple:
                case DFRegion.LocationTypes.Tavern:
                case DFRegion.LocationTypes.HomeWealthy:
                    perChunk = village / 2;
                    break;
                case DFRegion.LocationTypes.TownVillage:
                    perChunk = village;
                    break;
                case DFRegion.LocationTypes.TownHamlet:
                    perChunk = town;
                    break;
                case DFRegion.LocationTypes.TownCity:
                    perChunk = city;
                    break;
                default:
                    perChunk = 4;
                    break;
            }

            // if it’s night, divide by 5
            if (!day)
                perChunk = perChunk / 5;

            // if it’s storming, divide by 3
            if (storm)
                perChunk = perChunk / 3;

            // compute and shove it into the private field
            int newMax = popChunks * perChunk;
            //Debug.Log($"Villagers: day {day}, storm {storm}, perChunk {perChunk}, newMax {newMax}.");
            _maxPopulationField.SetValue(pm, newMax);
        }
    }
}

