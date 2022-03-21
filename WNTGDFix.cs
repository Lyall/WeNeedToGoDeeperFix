using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WeNeedToGoDeeperFix
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class WNTGDFix : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        public static ConfigEntry<bool> CustomResolution;
        public static ConfigEntry<float> DesiredResolutionX;
        public static ConfigEntry<float> DesiredResolutionY;
        public static ConfigEntry<bool> Fullscreen;
        public static ConfigEntry<bool> ResUnlock;
        public static ConfigEntry<bool> RefreshFix;
        public static ConfigEntry<bool> UIFix;

        private void Awake()
        {
            Log = Logger;

            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            UIFix = Config.Bind("General",
                                "UIFixes",
                                true,
                                "Fix UI scaling issues at ultrawide/wider");

            ResUnlock = Config.Bind("General",
                                "ResolutionUnlock",
                                true,
                                "Unlock resolution restrictions in the settings menu. (Credit: PhantomGamers)");

            RefreshFix = Config.Bind("General",
                                "RefreshFix",
                                true,
                                "Makes sure the refresh rate is set correctly in the settings menu. (Credit: PhantomGamers)");

            CustomResolution = Config.Bind("Set Custom Resolution",
                                "CustomResolution",
                                false,
                                "Enable the usage of a custom resolution.");

            DesiredResolutionX = Config.Bind("Set Custom Resolution",
                                "ResolutionWidth",
                                (float)Display.main.systemWidth, // Set default to display width so we don't leave an unsupported resolution as default
                                "Set desired resolution width.");

            DesiredResolutionY = Config.Bind("Set Custom Resolution",
                                "ResolutionHeight",
                                (float)Display.main.systemHeight, // Set default to display height so we don't leave an unsupported resolution as default
                                "Set desired resolution height.");

            Fullscreen = Config.Bind("Set Custom Resolution",
                                "Fullscreen",
                                true,
                                "Set to true for fullscreen or false for windowed.");


            SceneManager.sceneLoaded += OnSceneLoaded;

            Harmony.CreateAndPatchAll(typeof(Patches));

            if (WNTGDFix.ResUnlock.Value) { Harmony.CreateAndPatchAll(typeof(ResolutionPatch)); }
            if (WNTGDFix.RefreshFix.Value) { Harmony.CreateAndPatchAll(typeof(RefreshPatch)); }
        }

        // UI Fix
        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1) // Run the fix every time a scene is loaded
        {
            var NewAspectRatio = (float)Screen.width / (float)Screen.height;
            var AspectMultiplier = NewAspectRatio / (16f / 9f);
            var DefaultReferenceResolution = new Vector2(800, 600);
            var NewReferenceResolution = new Vector2(AspectMultiplier * 800, 600);
            if (WNTGDFix.UIFix.Value && NewAspectRatio > 1.8)
            {
                var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.referenceResolution = NewReferenceResolution;
                    WNTGDFix.Log.LogInfo("Scene Load: Changed " + GameObject.name + " reference resolution to " + GameObject.referenceResolution);
                }
            }
            else // Write back the default reference resolution
            {
                var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                foreach (var GameObject in CanvasObjects)
                {
                    GameObject.referenceResolution = DefaultReferenceResolution;
                    WNTGDFix.Log.LogInfo("Scene Load: Changed " + GameObject.name + " reference resolution to default");
                }
            }
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        // Disable cheat detection
        [HarmonyPatch(typeof(CheatCheckerBehavior), "CheatingDetected")]
        [HarmonyPrefix]
        public static bool RemoveCheatDetection()
        {
            WNTGDFix.Log.LogInfo("Cheat detection disabled.");
            return false;
        }

        // Set Screen Resolution
        [HarmonyPatch(typeof(MainMenuManagerBehavior), "Start")]
        [HarmonyPostfix]
        public static void SetResolution()
        {
            if (WNTGDFix.CustomResolution.Value && WNTGDFix.DesiredResolutionX.Value > 0 && WNTGDFix.DesiredResolutionY.Value > 0)
            {
                Screen.SetResolution((int)WNTGDFix.DesiredResolutionX.Value, (int)WNTGDFix.DesiredResolutionY.Value, (bool)WNTGDFix.Fullscreen.Value);
                WNTGDFix.Log.LogInfo($"Screen resolution set to = {(int)WNTGDFix.DesiredResolutionX.Value}x{(int)WNTGDFix.DesiredResolutionY.Value}");
            }
        }

        // UI Scaling Fix
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScreenResolutions), nameof(ScreenResolutions.Start))] // Make sure UI scaling updates if the user changes resolution ingame.
        public static void SetUIScaling(ScreenResolutions __instance)
        {
            __instance.dropdownMenu.onValueChanged.AddListener(delegate (int A_1)
            {
                var NewAspectRatio = (float)__instance.resolutions[__instance.dropdownMenu.value].width / (float)__instance.resolutions[__instance.dropdownMenu.value].height; // Must be set to dropdown resolution selected
                var AspectMultiplier = NewAspectRatio / (16f / 9f);
                var DefaultReferenceResolution = new Vector2(800, 600);
                var NewReferenceResolution = new Vector2(AspectMultiplier * 800, 600);
                int width = __instance.resolutions[__instance.dropdownMenu.value].width;
                int height = __instance.resolutions[__instance.dropdownMenu.value].height;

                if (WNTGDFix.UIFix.Value && NewAspectRatio > 1.8)
                {
                    Screen.SetResolution(width, height, Screen.fullScreen);
                    var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.referenceResolution = NewReferenceResolution;
                        WNTGDFix.Log.LogInfo("ScreenResolutions: Changed " + GameObject.name + " reference resolution to " + GameObject.referenceResolution);
                    }
                }
                else // Write back the default reference resolution
                {
                    Screen.SetResolution(width, height, Screen.fullScreen);
                    var CanvasObjects = GameObject.FindObjectsOfType<UnityEngine.UI.CanvasScaler>();
                    foreach (var GameObject in CanvasObjects)
                    {
                        GameObject.referenceResolution = DefaultReferenceResolution;
                        WNTGDFix.Log.LogInfo("ScreenResolutions: Changed " + GameObject.name + " reference resolution to default");
                    }
                }
            });
        }
    }

    public class ResolutionPatch
    {
        // Unlock Resolutions (Credit: PhantomGamers)
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ScreenResolutions), "<Start>m__1")]
        [HarmonyPatch(typeof(ScreenResolutions), "<Start>m__0")]
        [HarmonyPatch(typeof(OptionsData), nameof(OptionsData.SetResolution))]
        [HarmonyPatch(typeof(MainMenuManagerBehavior), nameof(MainMenuManagerBehavior.Start))]

        public static IEnumerable<CodeInstruction> RemoveMaxResolutionRestrictions(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                      .MatchForward(true,
                                    new CodeMatch(i => i.opcode == OpCodes.Ldc_I4 && ((int)i.operand == 2560 || (int)i.operand == 1440))
                                    )
                      .SetOperandAndAdvance(int.MaxValue)
                      .InstructionEnumeration();
        }
    }
    public class RefreshPatch
    {
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
