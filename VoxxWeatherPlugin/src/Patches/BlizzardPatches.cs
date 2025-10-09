using HarmonyLib;
using VoxxWeatherPlugin.Weathers;
using UnityEngine;
using GameNetcodeStuff;
using VoxxWeatherPlugin.Utils;

namespace VoxxWeatherPlugin.Patches
{
    [HarmonyPatch]
    internal sealed class BlizzardPatches
    {

        [HarmonyPatch(typeof(MouthDogAI), "DetectNoise")]
        [HarmonyPrefix]
        private static void DogSoundMufflingPatch(MouthDogAI __instance, ref float noiseLoudness)
        {
            if (BlizzardWeather.Instance != null && BlizzardWeather.Instance.IsActive &&
                 __instance.isOutside)
            {
                // Muffle dogs hearing during blizzard, depending on wind force. Muffled by 40% at wind force > 0.4, not muffled at wind force = 0
                noiseLoudness *= Mathf.Clamp(1 - BlizzardWeather.Instance.windForce, 0.60f, 1f);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "CancelSpecialTriggerAnimations")]
        [HarmonyPrefix]
        private static bool LadderWindPatch(PlayerControllerB __instance)
        {
            // When player is too cold, players can't climb ladders during blizzards
            return BlizzardWeather.Instance == null || !BlizzardWeather.Instance.IsActive || !__instance.isClimbingLadder ||
                !BlizzardWeather.Instance.isLocalPlayerInWind || PlayerEffectsManager.ColdSeverity >= 0.8f;
        }

    }

}


