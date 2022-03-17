using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WeNeedToGoDeeperFix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class WNTGDFix : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        public static ConfigEntry<float> DesiredResolutionX;
        public static ConfigEntry<float> DesiredResolutionY;
        public static ConfigEntry<bool> Fullscreen;
        public static ConfigEntry<bool> UIFix;

        private void Awake()
        {
            Log = Logger;

            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            DesiredResolutionX = Config.Bind("General",
                                "ResolutionWidth",
                                (float)Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default
                                "Set desired resolution width.");

            DesiredResolutionY = Config.Bind("General",
                                "ResolutionHeight",
                                (float)Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default
                                "Set desired resolution height.");

            Fullscreen = Config.Bind("General",
                                "Fullscreen",
                                true,
                                "Set to true for fullscreen or false for windowed.");

            UIFix = Config.Bind("General",
                                "UI Fixes",
                                true,
                                "Fix UI scaling issues at ultrawide/wider");

            SceneManager.sceneLoaded += OnSceneLoaded;

            Harmony.CreateAndPatchAll(typeof(Patches));
        }
        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            var NewAspectRatio = WNTGDFix.DesiredResolutionX.Value / WNTGDFix.DesiredResolutionY.Value;
            var AspectMultiplier = NewAspectRatio / (16f / 9f);
            var NewReferenceResolution = new Vector2(AspectMultiplier * 800, 600);
            if (WNTGDFix.UIFix.Value)
            {
                var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.referenceResolution = NewReferenceResolution;
                    WNTGDFix.Log.LogInfo("Changed " + GameObject.name + " reference resolution to " + GameObject.referenceResolution);
                }
            }
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        // Set screen resolution
        [HarmonyPatch(typeof(MainMenuManagerBehavior), "Start")]
        [HarmonyPostfix]
        public static void SetResolution()
        {
            Screen.SetResolution((int)WNTGDFix.DesiredResolutionX.Value, (int)WNTGDFix.DesiredResolutionY.Value, (bool)WNTGDFix.Fullscreen.Value);
            WNTGDFix.Log.LogInfo($"Screen resolution set to = {(int)WNTGDFix.DesiredResolutionX.Value}x{(int)WNTGDFix.DesiredResolutionY.Value}");
        }

        // Refresh Rate Fix (Credit: PhantomGamers)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScreenResolutions), nameof(ScreenResolutions.SetDropdownResolution))]
        public static bool ApplyRefreshRate(ScreenResolutions __instance, int width, int height)
        {
            for (int i = 0; i < __instance.resolutions.Length; i++)
            {
                Resolution res = __instance.resolutions[i];
                if (res.width == width && res.height == height && res.refreshRate == Screen.currentResolution.refreshRate)
                {
                    __instance.dropdownMenu.value = i;
                }
            }
            return false;
        }
    }
}
