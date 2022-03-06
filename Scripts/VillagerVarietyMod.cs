using UnityEngine;
using System.Linq;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;

namespace VillagerVariety
{
    public class VillagerVarietyMod : MonoBehaviour
    {
        private static Mod mod;
        public static Mod Mod { get { return mod; } }


        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            mod.MessageReceiver = MessageReceiver;

            var go = new GameObject(mod.Title);
            go.AddComponent<VillagerVarietyMod>();

            mod.IsReady = true;
        }

        private void Start()
        {
            VillagerVarietyPopulationManagerProxy.Register();
        }

        public static readonly int[] MALE_REDGUARD_TEXTURES = new int[] { 381, 382, 383, 384 };
        public static readonly int[] FEMALE_REDGUARD_TEXTURES = new int[] { 395, 396, 397, 398 };

        public static readonly int[] MALE_NORD_TEXTURES = new int[] { 387, 388, 389, 390 };
        public static readonly int[] FEMALE_NORD_TEXTURES = new int[] { 392, 393, 451, 452 };

        public static readonly int[] MALE_BRETON_TEXTURES = new int[] { 385, 386, 391, 394 };
        public static readonly int[] FEMALE_BRETON_TEXTURES = new int[] { 453, 454, 455, 456 };

        public static readonly int[] GUARD_TEXTURES = { 399 };


        public static string GetClimateVariant(int archive)
        {
            var PlayerGPS = GameManager.Instance.PlayerGPS;
            if (MALE_REDGUARD_TEXTURES.Contains(archive) || FEMALE_REDGUARD_TEXTURES.Contains(archive))
            {
                // "Subtropical" Redguards, in swamp and subtropical climates
                if (PlayerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Swamp
                || PlayerGPS.CurrentClimateIndex == (int)MapsFile.Climates.Subtropical)
                {
                    return "S"; 
                }
                else if (PlayerGPS.CurrentRegionIndex == (int)DaggerfallRegions.DragontailMountains
                    || PlayerGPS.CurrentRegionIndex == (int)DaggerfallRegions.Ephesus)
                {
                    // Reusing the subtropical redguard skins
                    return "S";
                }
            }

            return "";
        }

        public static string GetImageName(int archive, int record, int frame, int face, int variant, string climate, string season)
        {
            return string.Format("{0:000}.{3}.{5}{4}{6}_{1}-{2}", archive, record, frame, face, variant, climate, season);
        }
        public static string GetImageName(int archive, int record, int frame, int variant, string climate, string season)
        {
            return string.Format("{0:000}.{3}.{5}{4}{6}_{1}-{2}", archive, record, frame, "X", variant, climate, season);
        }

        public const int NUM_VARIANTS = 2;  // Number of variants to generate, a variant falls back to 0 if no images found.

        public const string GET_NUM_VARIANTS = "getNumVariants";
        public const string GET_SEASON_STR = "getSeasonStr";
        public const string GET_ARCHIVE_CURRENT_CLIMATE = "getArchiveCurrentClimate"; // Returns the current climate variant for a given archive
        public const string GET_IMAGE_NAME = "getImageName";
        public const string GET_IMAGE_NAME_CLIMATE = "getImageNameClimate"; // Like getImageName but you can specify a custom climate

        public readonly static string[] SEASON_STRS = { "f", "p", "m", "w" };

        private static void MessageReceiver(string message, object data, DFModMessageCallback callBack)
        {
            try
            {

                switch (message)
                {
                    case GET_NUM_VARIANTS:
                        callBack?.Invoke(GET_NUM_VARIANTS, NUM_VARIANTS);
                        break;

                    case GET_SEASON_STR:
                        callBack?.Invoke(GET_SEASON_STR, SEASON_STRS[(int)DaggerfallUnity.Instance.WorldTime.Now.SeasonValue]);
                        break;

                    case GET_ARCHIVE_CURRENT_CLIMATE:
                        callBack?.Invoke(GET_ARCHIVE_CURRENT_CLIMATE, GetClimateVariant((int)data));
                        break;

                    case GET_IMAGE_NAME:
                        {
                            object[] paramArr = (object[])data;
                            callBack?.Invoke(GET_IMAGE_NAME, GetImageName((int)paramArr[0], (int)paramArr[1], (int)paramArr[2], (int)paramArr[3], (int)paramArr[4], "", (string)paramArr[5]));
                            break;
                        }

                    case GET_IMAGE_NAME_CLIMATE:
                        {
                            object[] paramArr = (object[])data;
                            callBack?.Invoke(GET_IMAGE_NAME_CLIMATE, GetImageName((int)paramArr[0], (int)paramArr[1], (int)paramArr[2], (int)paramArr[3], (int)paramArr[4], "", (string)paramArr[5]));
                            break;
                        }

                    default:
                        Debug.LogErrorFormat("{0}: unknown message received ({1}).", mod.Title, message);
                        break;
                }
            }
            catch
            {
                Debug.LogErrorFormat("{0}: error handling message ({1}).", mod.Title, message);
                callBack?.Invoke("error", "Data passed is invalid for " + message);
            }
        }
    }
}
