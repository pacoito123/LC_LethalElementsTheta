using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.AI;
using VoxxWeatherPlugin.Behaviours;
using VoxxWeatherPlugin.Weathers;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Patches
{
    [HarmonyPatch]
    internal sealed class FlarePatches
    {
        internal static System.Random random = new();
        internal static System.Random seededRandom = new(42);
        internal static Transform? originalTeleporterPosition;
        internal static float BatteryDrainMultiplier => Mathf.Clamp(LESettings.BatteryDrainMultiplier.Value, 0, 99);
        internal static bool DrainBatteryInFacility => LESettings.DrainBatteryInFacility.Value;
        internal static bool DoorMalfunctionEnabled => LESettings.DoorMalfunctionEnabled.Value;

        [HarmonyPatch(typeof(PlayerVoiceIngameSettings), nameof(PlayerVoiceIngameSettings.OnDisable))]
        [HarmonyPrefix]
        private static void FilterCacheCleanerPatch(PlayerVoiceIngameSettings __instance)
        {
            if (__instance.voiceAudio != null)
            {
                WalkieDistortionManager.ClearFilterCache(__instance.voiceAudio);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.UpdatePlayerVoiceEffects))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> VoiceDistorterPatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Stloc_S),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isPlayerDead))),
                new(OpCodes.Brfalse))
            .Advance(1);

            return codeMatcher.IsValid
                ? codeMatcher.Insert(
                        new(OpCodes.Ldloc_0), // Load voiceChatAudioSource
                        new(OpCodes.Ldloc_1), // Load allPlayerScript
                        new(OpCodes.Ldloc_S, 4), // Load walkie talkie flag
                        new(OpCodes.Call, AccessTools.Method(typeof(WalkieDistortionManager),
                            nameof(WalkieDistortionManager.UpdateVoiceChatDistortion))))
                    .InstructionEnumeration()
                : instructions;
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
        [HarmonyPrefix]
        private static void GrabbableDischargePatch(GrabbableObject __instance)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive &&
                SolarFlareWeather.Instance.flareData != null)
            {
                if (__instance.IsOwner && __instance.hasBeenHeld && __instance.itemProperties.requiresBattery && (!__instance.isInFactory || DrainBatteryInFacility))
                {
                    if (__instance.insertedBattery.charge > 0.0 && !__instance.itemProperties.itemIsTrigger)
                    {
                        __instance.insertedBattery.charge -= 2 * SolarFlareWeather.Instance.flareData.ScreenDistortionIntensity * BatteryDrainMultiplier * Time.deltaTime / __instance.itemProperties.batteryUsage;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UseSignalTranslatorClientRpc))]
        [HarmonyPrefix]
        private static void SignalTranslatorDistortionPatch(ref string signalMessage)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive &&
                SolarFlareWeather.Instance.flareData != null)
            {
                float distortionIntensity = SolarFlareWeather.Instance.flareData.RadioDistortionIntensity * 0.5f;
                char[] messageChars = signalMessage.ToCharArray();

                for (int i = 0; i < messageChars.Length; i++)
                {
                    if (random.NextDouble() < distortionIntensity)
                    {
                        messageChars[i] = (char)random.Next(32, 127); // Random ASCII printable character
                    }
                }

                signalMessage = new string(messageChars);
            }
        }

        [HarmonyPatch(typeof(ShipTeleporter), nameof(ShipTeleporter.beamUpPlayer), MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TeleporterDistortionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(ShipTeleporter), nameof(ShipTeleporter.teleporterPosition))),
                new(OpCodes.Callvirt, AccessTools.Method(typeof(Transform), "get_position")),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldc_R4, 160.0f),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Callvirt, AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer))));

            return codeMatcher.IsValid
                ? codeMatcher.InsertAndAdvance(
                        new(OpCodes.Ldloc_1), // Load `shipTeleporter` onto the stack
                        new(OpCodes.Call, AccessTools.Method(typeof(FlarePatches), nameof(TeleporterPositionDistorter))))
                    .Advance(10)
                    .Insert(
                        new(OpCodes.Ldloc_1),
                        new(OpCodes.Call, AccessTools.Method(typeof(FlarePatches), nameof(TeleporterPositionRestorer))))
                    .InstructionEnumeration()
                : instructions;
        }

        private static void TeleporterPositionDistorter(ShipTeleporter teleporter)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive &&
                SolarFlareWeather.Instance.flareData != null)
            {
                // Store the original teleporter position
                originalTeleporterPosition = teleporter.teleporterPosition;
                // Randomly teleport alive player to an AI node >:D
                GameObject[] outsideAINodes = RoundManager.Instance.outsideAINodes;
                if (outsideAINodes.Length > 0)
                {
                    int randomIndex = seededRandom.Next(0, outsideAINodes.Length);
                    Transform distortedPosition = outsideAINodes[randomIndex].transform;
                    if (NavMesh.SamplePosition(distortedPosition.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                    {
                        distortedPosition.position = hit.position;
                        teleporter.teleporterPosition = distortedPosition;
                    }
                }
            }
        }

        private static void TeleporterPositionRestorer(ShipTeleporter teleporter)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive &&
                SolarFlareWeather.Instance.flareData != null)
            {
                // Restore the original teleporter position
                teleporter.teleporterPosition = originalTeleporterPosition;
            }
        }

        [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.CallFunctionFromTerminal))]
        [HarmonyPrefix]
        private static bool DoorTerminalBlocker(TerminalAccessibleObject __instance)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive &&
                SolarFlareWeather.Instance.flareData != null && DoorMalfunctionEnabled)
            {
                if (SolarFlareWeather.Instance.flareData.IsDoorMalfunction && __instance.isBigDoor && seededRandom.NextDouble() < 0.5f)
                {
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(RadarBoosterItem), nameof(RadarBoosterItem.EnableRadarBooster))]
        [HarmonyPrefix]
        private static void SignalBoosterPrefix(RadarBoosterItem __instance, ref bool enable)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive &&
                SolarFlareWeather.Instance.flareData != null)
            {
                if (enable)
                {
                    // Decrease the distortion intensity
                    SolarFlareWeather.Instance.flareData.RadioDistortionIntensity /= 3f;
                    SolarFlareWeather.Instance.flareData.ScreenDistortionIntensity /= 3f;
                    SolarFlareWeather.Instance.flareData.RadioFrequencyShift /= 4f;
                    SolarFlareWeather.Instance.flareData.RadioBreakthroughLength += 0.25f;
                }
                else if (__instance.radarEnabled)
                {
                    // Restore the original values
                    SolarFlareWeather.Instance.flareData.RadioDistortionIntensity *= 3f;
                    SolarFlareWeather.Instance.flareData.ScreenDistortionIntensity *= 3f;
                    SolarFlareWeather.Instance.flareData.RadioFrequencyShift *= 4f;
                    SolarFlareWeather.Instance.flareData.RadioBreakthroughLength -= 0.25f;
                }

                // Update Glitch Effects
                foreach (GlitchEffect? glitchPass in SolarFlareWeather.Instance.glitchPasses.Values)
                {
                    if (glitchPass == null)
                    {
                        continue;
                    }

                    glitchPass.intensity.value = SolarFlareWeather.Instance.flareData.ScreenDistortionIntensity;
                }
            }
        }

        [HarmonyPatch(typeof(Turret), nameof(Turret.Start))]
        [HarmonyPostfix]
        private static void TurretMalfunctionPatch(Turret __instance)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsMatched)
            {
                SolarFlareWeather.Instance.CreateStaticParticle(__instance);
            }
        }

        [HarmonyPatch(typeof(RadMechAI), nameof(RadMechAI.Start))]
        [HarmonyPostfix]
        private static void RadMechMalfunctionPatch(RadMechAI __instance)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsMatched)
            {
                SolarFlareWeather.Instance.CreateStaticParticle(__instance);
            }
        }

    }

    [HarmonyPatch]
    internal sealed class FlareOptionalWalkiePatches
    {
        [HarmonyPatch(typeof(WalkieTalkie), nameof(WalkieTalkie.Start))]
        [HarmonyPrefix]
        private static void WalkieDistortionPatch(WalkieTalkie __instance)
        {
            _ = __instance.gameObject.AddComponent<WalkieDistortionManager>();
        }

        [HarmonyPatch(typeof(WalkieTalkie), nameof(WalkieTalkie.TimeAllAudioSources))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RadioDistorterPatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(true, // Replace audio source creation logic
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(WalkieTalkie), nameof(WalkieTalkie.audioSourcesReceiving))),
                new(OpCodes.Ldloc_3),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(WalkieTalkie), nameof(WalkieTalkie.target))),
                new(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_gameObject")));

            return codeMatcher.IsValid
                ? codeMatcher.Advance(1)
                    .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FlareOptionalWalkiePatches),
                        nameof(SplitWalkieTarget))))
                    .MatchForward(true, // Replace audio source disposal logic
                        new(OpCodes.Ldloc_1),
                        new(OpCodes.Call, AccessTools.Method(typeof(Object), nameof(Object.Destroy), [typeof(Object)])))
                    .Repeat(matcher => matcher.InsertAndAdvance(
                            new(OpCodes.Ldarg_0),
                            new(OpCodes.Callvirt, AccessTools.Method(typeof(Component), "get_gameObject")))
                        .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FlareOptionalWalkiePatches),
                            nameof(DisposeWalkieTarget)))))
                    .InstructionEnumeration()
                : instructions;
        }

        private static AudioSource SplitWalkieTarget(GameObject target)
        {
            WalkieDistortionManager subTargetsManager = target.transform.parent.gameObject.GetComponent<WalkieDistortionManager>();
            return subTargetsManager.SplitWalkieTarget(target);
        }

        private static void DisposeWalkieTarget(AudioSource audioSource, GameObject walkieObject)
        {
            WalkieDistortionManager subTargetsManager = walkieObject.GetComponent<WalkieDistortionManager>();
            subTargetsManager.DisposeWalkieTarget(audioSource);
        }
    }
}