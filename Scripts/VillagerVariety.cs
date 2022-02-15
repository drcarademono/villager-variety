using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;
using System.Collections;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using System.Reflection;

namespace VillagerVariety
{
    public class VillagerVariety : MonoBehaviour
    {
        #region Fields

        public static Mod mod;

        public const int NUM_VARIANTS = 2;  // Number of variants to generate, a variant falls back to 0 if no images found.
        public readonly static string[] seasonStrs = { "f", "p", "m", "w" };

        // Face data from MobilePersonNPC
        static int[] maleRedguardFaceRecordIndex = new int[] { 336, 312, 336, 312 };
        static int[] femaleRedguardFaceRecordIndex = new int[] { 144, 144, 120, 96 };
        static int[] maleNordFaceRecordIndex = new int[] { 240, 264, 168, 192 };
        static int[] femaleNordFaceRecordIndex = new int[] { 72, 0, 48, 0 };
        static int[] maleBretonFaceRecordIndex = new int[] { 192, 216, 288, 240 };
        static int[] femaleBretonFaceRecordIndex = new int[] { 72, 72, 24, 72 };

        static VillagerVariety instance;

        #endregion

        #region Public Methods

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            instance = new GameObject(mod.Title).AddComponent<VillagerVariety>();

            mod.MessageReceiver = MessageReceiver;

            // Register events
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransition;
            PlayerEnterExit.OnTransitionExterior += OnTransition;
            PlayerGPS.OnEnterLocationRect += OnTransition;

            mod.IsReady = true;
        }

        public static string GetImageName(int archive, int record, int frame, int face, int variant, string season)
        {
            return string.Format("{0:000}.{3}.{4}{5}_{1}-{2}", archive, record, frame, face, variant, season);
        }
        public static string GetImageName(int archive, int record, int frame, int variant, string season)
        {
            return string.Format("{0:000}.{3}.{4}{5}_{1}-{2}", archive, record, frame, "X", variant, season);
        }

        #endregion

        #region Private Methods

        static void OnTransition(PlayerEnterExit.TransitionEventArgs args)
        {
            OnTransition();
        }
        static void OnTransition(DaggerfallConnect.DFLocation location)
        {
            OnTransition();
        }
        static void OnTransition()
        {
            instance.StartCoroutine(PreloadPopulation());
        }

        static IEnumerator PreloadPopulation()
        {
            // If in town
            PopulationManager pm = GameObject.FindObjectOfType<PopulationManager>();
            if (pm == null) yield break;

            // Wait for PopulationManager to call Start
            yield return null;

            // Ready billboard
            VillagerVarietyMobilePerson billboard = new GameObject().AddComponent<VillagerVarietyMobilePerson>();

            // Get population race
            Races race =
                (Races)pm.GetType().
                GetMethod("GetEntityRace", BindingFlags.NonPublic | BindingFlags.Instance).
                Invoke(pm, null);

            // Preload population
            for (int genderInd = 0; genderInd < 2; genderInd++)
            {
                Genders gender = genderInd == 0 ? Genders.Female : Genders.Male;
                for (int outfit = 0; outfit < 4; outfit++)    // MobilePersonNPC.numPersonOutfitVariants
                {
                    for (int face = 0; face < 24; face++) // MobilePersonNPC.numPersonFaceVariants
                    {
                        // Get face data (from MobilePersonNPC.SetPerson)
                        int[] recordIndices = null;
                        switch (race)
                        {
                            case Races.Redguard:
                                recordIndices = (gender == Genders.Male) ? maleRedguardFaceRecordIndex : femaleRedguardFaceRecordIndex;
                                break;
                            case Races.Nord:
                                recordIndices = (gender == Genders.Male) ? maleNordFaceRecordIndex : femaleNordFaceRecordIndex;
                                break;
                            case Races.Breton:
                            default:
                                recordIndices = (gender == Genders.Male) ? maleBretonFaceRecordIndex : femaleBretonFaceRecordIndex;
                                break;
                        }
                        int faceId = recordIndices[outfit] + face;
                        for (int variety = 0; variety < NUM_VARIANTS; variety++)
                            billboard.SetPerson(race, gender, outfit, false, face, faceId, variety);
                    }
                }
            }

            // Preload guards
            billboard.SetPerson(race, Genders.Male, 0, true, 0, 0, 0);

            // Destroy billboard
            GameObject.Destroy(billboard.gameObject);
        }

        #endregion

        #region Mod messages

        public const string GET_NUM_VARIANTS = "getNumVariants";
        public const string GET_SEASON_STR = "getSeasonStr";
        public const string GET_IMAGE_NAME = "getImageName";

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
                        callBack?.Invoke(GET_SEASON_STR, seasonStrs[(int)DaggerfallUnity.Instance.WorldTime.Now.SeasonValue]);
                        break;

                    case GET_IMAGE_NAME:
                        object[] paramArr = (object[])data;
                        callBack?.Invoke(GET_IMAGE_NAME, GetImageName((int)paramArr[0], (int)paramArr[1], (int)paramArr[2], (int)paramArr[3], (int)paramArr[4], (string)paramArr[5]));
                        break;

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

        #endregion
    }
}
