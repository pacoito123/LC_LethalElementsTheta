using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using VoxxWeatherPlugin.Utils;
using VoxxWeatherPlugin.Weathers;

namespace VoxxWeatherPlugin.Patches
{
    [HarmonyPatch]
    internal sealed class HeatwavePatches
    {
        private static float prevSprintMeter;
        private static readonly float severityInfluenceMultiplier = 1.25f;
        private static readonly float timeToCool = 17f;

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        private static void HeatStrokePatchPrefix(PlayerControllerB __instance)
        {
            if (HeatwaveWeather.Instance == null || !HeatwaveWeather.Instance.IsActive || GameNetworkManager.Instance == null
                || __instance != GameNetworkManager.Instance.localPlayerController) return;

            prevSprintMeter = __instance.sprintMeter;
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LateUpdate))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        private static void HeatStrokePatchLatePostfix(PlayerControllerB __instance)
        {
            if (HeatwaveWeather.Instance == null || !HeatwaveWeather.Instance.IsActive || GameNetworkManager.Instance == null
                || __instance != GameNetworkManager.Instance.localPlayerController) return;

            if (CheckConditionsForHeatingStop(__instance))
            {
                PlayerEffectsManager.heatTransferRate = 1f;
                PlayerEffectsManager.isInHeatZone = false;
            }
            else if (CheckConditionsForHeatingPause(__instance))
            {
                PlayerEffectsManager.heatTransferRate = .25f; //heat slower when in special interact animation or in a car
            }
            else
            {
                PlayerEffectsManager.heatTransferRate = 1f;
            }

            // Gradually reduce heat severity when not in heat zone
            if (!PlayerEffectsManager.isInHeatZone)
            {
                PlayerEffectsManager.ResetPlayerTemperature(Time.deltaTime / timeToCool);
            }
            else
            {
                PlayerEffectsManager.SetPlayerTemperature(Time.deltaTime / HeatwaveWeather.Instance.timeInHeatZoneMax * HeatwaveWeather.Instance.timeOfDayFactor);
            }

            float severity = PlayerEffectsManager.HeatSeverity;

            //Debug.Log($"Severity: {severity}, inHeatZone: {PlayerEffectsManager.isInHeatZone}, heatMultiplier {PlayerEffectsManager.heatSeverityMultiplier}, isInside {__instance.isInsideFactory}");

            if (severity > 0)
            {
                float delta = __instance.sprintMeter - prevSprintMeter;
                if (delta < 0.0) //Stamina consumed
                    __instance.sprintMeter = Mathf.Max(prevSprintMeter + (delta * (1 + (severity * severityInfluenceMultiplier))), 0.0f);
                else if (delta > 0.0) //Stamina regenerated
                    __instance.sprintMeter = Mathf.Min(prevSprintMeter + (delta / (1 + (severity * severityInfluenceMultiplier))), 1f);
            }
        }

        private static bool CheckConditionsForHeatingPause(PlayerControllerB playerController)
        {
            return playerController.inSpecialInteractAnimation || playerController.inAnimationWithEnemy || playerController.isClimbingLadder || playerController.physicsParent != null;
        }

        private static bool CheckConditionsForHeatingStop(PlayerControllerB playerController)
        {
            return playerController.beamUpParticle.isPlaying || playerController.isInElevator ||
                    playerController.isInHangarShipRoom || playerController.isUnderwater ||
                     playerController.isPlayerDead || playerController.isInsideFactory ||
                     (playerController.currentAudioTrigger != null && playerController.currentAudioTrigger.insideLighting);
        }

        [HarmonyPatch(typeof(SoundManager), nameof(SoundManager.SetAudioFilters))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> HeatstrokeAudioPatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            return new CodeMatcher(instructions, generator).MatchForward(true,
                new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.drunkness))),
                new(OpCodes.Callvirt, AccessTools.Method(typeof(AnimationCurve), nameof(AnimationCurve.Evaluate))),
                new(OpCodes.Ldc_R4, 0.6f))
            .Advance(2)
            .CreateLabel(out Label jumpTarget)
            .Advance(-1)
            .InsertAndAdvance(
                new(OpCodes.Bgt_S, jumpTarget),
                new(OpCodes.Call, AccessTools.PropertyGetter(typeof(PlayerEffectsManager),
                    nameof(PlayerEffectsManager.HeatSeverity))),
                new(OpCodes.Ldc_R4, 0.85f))
            .InstructionEnumeration();
        }

        //     [HarmonyPatch(typeof(VehicleController), "Start")]
        //     [HarmonyPrefix]
        //     private static void VehicleHeaterPatch(VehicleController __instance)
        //     {
        //         VehicleHeatwaveHandler vehicleHeater = __instance.gameObject.AddComponent<VehicleHeatwaveHandler>();
        //     }
    }
}
