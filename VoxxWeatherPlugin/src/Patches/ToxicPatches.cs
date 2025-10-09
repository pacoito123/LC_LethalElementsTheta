using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using VoxxWeatherPlugin.Utils;
using VoxxWeatherPlugin.Weathers;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Patches
{
    [HarmonyPatch]
    internal sealed class ToxicPatches
    {
        private static float DamageInterval => LESettings.ToxicDamageInterval.Value;
        private static int DamageAmount => LESettings.ToxicDamageAmount.Value;
        private static float PoisoningRemovalMultiplier => LESettings.PoisoningRemovalMultiplier.Value;

        private static float damageTimer;

        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        private static void PoisoningPatchPrefix(PlayerControllerB __instance)
        {
            if (!(ToxicSmogWeather.Instance != null && ToxicSmogWeather.Instance.IsActive) || __instance != ((GameNetworkManager.Instance != null)
                ? GameNetworkManager.Instance.localPlayerController : null))
                return;

            if (__instance.isPlayerDead || __instance.isInHangarShipRoom || __instance.isInElevator)
            {
                PlayerEffectsManager.isPoisoned = false;
            }

            if (PlayerEffectsManager.isPoisoned)
            {
                damageTimer += Time.deltaTime;
                PlayerEffectsManager.SetPoisoningEffect(Time.deltaTime);
                if (damageTimer >= DamageInterval)
                {
                    __instance.DamagePlayer(DamageAmount, true, true, CauseOfDeath.Suffocation, 0, false, default);
                    damageTimer = 0;
                }
            }
            else
            {
                PlayerEffectsManager.SetPoisoningEffect(-Time.deltaTime * PoisoningRemovalMultiplier);
                if (damageTimer > 0)
                {
                    damageTimer -= Time.deltaTime * PoisoningRemovalMultiplier;
                }
            }

            PlayerEffectsManager.isPoisoned = false;
        }
    }
}
