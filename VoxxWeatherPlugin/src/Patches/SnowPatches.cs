using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using VoxxWeatherPlugin.Behaviours;
using VoxxWeatherPlugin.Utils;
using VoxxWeatherPlugin.Weathers;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Patches
{
    [HarmonyPatch]
    internal sealed class SnowPatches
    {
        internal static bool SnowAffectsEnemies => LESettings.snowAffectsEnemies.Value && LESettings.enableSnowTracks.Value;
        public static float TimeToWarmUp => LESettings.timeToWarmUp.Value;   // Time to warm up from cold to room temperature
        internal static float FrostbiteDamageInterval => LESettings.frostbiteDamageInterval.Value;
        internal static float FrostbiteDamage => LESettings.frostbiteDamage.Value;
        internal static float frostbiteThreshold = 0.5f; // Severity at which frostbite starts to occur, should be below 0.9
        internal static float frostbiteTimer;
        internal static HashSet<Type> unaffectedEnemyTypes = [typeof(ForestGiantAI), typeof(RadMechAI), typeof(DoublewingAI), typeof(ButlerBeesEnemyAI),
            typeof(DocileLocustBeesAI), typeof(RedLocustBees), typeof(DressGirlAI), typeof(SandWormAI)];
        public static HashSet<string>? EnemySpawnBlacklist => (LevelManipulator.Instance != null) ? LevelManipulator.Instance.enemySnowBlacklist : null;
        public static HashSet<SpawnableEnemyWithRarity> enemiesToRestore = [];

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
        [HarmonyTranspiler]
        [HarmonyPriority(Priority.VeryHigh)]
        private static IEnumerable<CodeInstruction> SnowHindranceTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.movementSpeed))),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.carryWeight))),
                new(OpCodes.Div),
                new(OpCodes.Stloc_S));

            if (codeMatcher.IsInvalid)
            {
                Debug.LogError("Failed to match code in SnowHindranceTranspiler");
                return instructions;
            }

            _ = codeMatcher.Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldloc_S, 8), // Load V_8 onto the stack
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(SnowfallVFXManager),
                    nameof(SnowfallVFXManager.snowMovementHindranceMultiplier))),
                new CodeInstruction(OpCodes.Div),        // Divide V_8 by hindrance multiplier
                new CodeInstruction(OpCodes.Stloc_S, 8));  // Store the modified value back

            Debug.Log("Patched PlayerControllerB.Update to include snow hindrance!");
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GetCurrentMaterialStandingOn))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GroundSamplingTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.interactRay))),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldflda, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.hit))),
                new CodeMatch(OpCodes.Ldc_R4), // 6f
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(StartOfRound), nameof(StartOfRound.Instance))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(StartOfRound), nameof(StartOfRound.walkableSurfacesMask)))
            );

            if (codeMatcher.IsInvalid)
            {
                Debug.LogError("Failed to match code in GroundSamplingTranspiler");
                return instructions;
            }

            _ = codeMatcher.RemoveInstructions(20)
            .InsertAndAdvance(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, AccessTools.Method(typeof(SnowPatches), nameof(SurfaceSamplingOverride)))
            );

            Debug.Log("Patched PlayerControllerB.GetCurrentMaterialStandingOn to include snow thickness!");
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CalculateGroundNormal))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GroundNormalTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.hit))),
                new(OpCodes.Call, AccessTools.Method(typeof(RaycastHit), "get_normal")),
                new(OpCodes.Stfld, AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.playerGroundNormal)))
            );

            if (codeMatcher.IsInvalid)
            {
                Debug.LogError("Failed to match code in GroundNormalTranspiler");
                return instructions;
            }

            _ = codeMatcher.Advance(1)
            .Insert(new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SnowPatches), nameof(LocalGroundUpdate))));

            Debug.Log("Patched PlayerControllerB.CalculateGroundNormal to include snow thickness!");
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
        [HarmonyTranspiler]
        [HarmonyPriority(Priority.High)]
        private static IEnumerable<CodeInstruction> IceRebakeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator).MatchForward(false,
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ble),
                new(OpCodes.Ldstr))
            .Advance(2);

            if (codeMatcher.IsInvalid)
            {
                return instructions;
            }

            // Get the original target of the fail jump
            Label originalTarget = (Label)codeMatcher.Operand;

            // Define a new label for the fail jump target
            Label failJumpTarget = generator.DefineLabel();

            List<Label> labels = codeMatcher.Advance(-2)
            .InsertAndAdvance( // Insert an additional condition
                new(OpCodes.Call, AccessTools.Method(typeof(SnowPatches), nameof(DelayRebakeForIce))),
                new(OpCodes.Brfalse, failJumpTarget))
            .MatchForward(false, new CodeMatch(operand: originalTarget)).Labels;

            labels.Add(failJumpTarget); // Add the new label

            Debug.Log("Patched RoundManager.SpawnOutsideHazards to include ice rebake condition!");
            return codeMatcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LateUpdate))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        private static void FrostbiteLatePostfix(PlayerControllerB __instance)
        {
            if (!IsSnowActive() || __instance != (GameNetworkManager.Instance != null ? GameNetworkManager.Instance.localPlayerController : null))
                return;

            // Gradually reduce heat severity when not in heat zone
            if (!PlayerEffectsManager.isInColdZone)
            {
                PlayerEffectsManager.ResetPlayerTemperature(Time.deltaTime / TimeToWarmUp);
            }
            else
            {
                float timeUntilFrostbite = SnowfallWeather.Instance!.IsActive ? SnowfallWeather.Instance.timeUntilFrostbite : BlizzardWeather.Instance!.timeUntilFrostbite;
                PlayerEffectsManager.SetPlayerTemperature(-Time.deltaTime / timeUntilFrostbite);
            }

            if (PlayerEffectsManager.isUnderSnow)
            {
                PlayerEffectsManager.SetUnderSnowEffect(Time.deltaTime);
            }
            else
            {
                PlayerEffectsManager.SetUnderSnowEffect(-Time.deltaTime);
            }

            float severity = PlayerEffectsManager.ColdSeverity;

            // Debug.LogDebug($"Severity: {severity}, inColdZone: {PlayerEffectsManager.isInColdZone}, frostbiteTimer: {frostbiteTimer}, heatTransferRate: {PlayerEffectsManager.heatTransferRate}");

            if (severity >= frostbiteThreshold)
            {
                frostbiteTimer += Time.deltaTime;
                int damage = Mathf.CeilToInt(FrostbiteDamage * severity);
                if (frostbiteTimer > FrostbiteDamageInterval && damage > 0)
                {
                    __instance.DamagePlayer(damage, causeOfDeath: CauseOfDeath.Unknown);
                    frostbiteTimer = 0f;
                }
            }
            else if (frostbiteTimer > 0)
            {
                frostbiteTimer -= Time.deltaTime;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.OnControllerColliderHit))]
        [HarmonyPostfix]
        private static void PlayerFeetPositionPatch(ControllerColliderHit hit)
        {
            if (IsSnowActive() && SnowThicknessManager.Instance != null)
            {
                SnowThicknessManager.Instance.feetPositionY = hit.point.y;
            }
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.DoAIInterval))]
        [HarmonyPrefix]
        private static void EnemyGroundSamplerPatch(EnemyAI __instance)
        {
            if (__instance.isOutside && __instance.IsHost && SnowAffectsEnemies && IsSnowActive()
                && SnowThicknessManager.Instance != null)
            {
                // Check if enemy is affected by snow hindrance
                if (!unaffectedEnemyTypes.Contains(__instance.GetType()))
                {
                    if (Physics.Raycast(__instance.serverPosition, -Vector3.up, out __instance.raycastHit, 6f,
                        StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
                    {
                        SnowThicknessManager.Instance.UpdateEntityData(__instance, __instance.raycastHit);
                    }
                }
            }
        }

        //Generic patch for all enemies, we patch manually since each derived enemy type overrides the base implementation
        private static void EnemySnowHindrancePatch(EnemyAI __instance)
        {
            if (__instance.isOutside && __instance.IsHost && SnowAffectsEnemies && IsSnowActive()
                && SnowThicknessManager.Instance != null)
            {
                float snowThickness = SnowThicknessManager.Instance.GetSnowThickness(__instance);
                // Slow down if the entity in snow (only if snow thickness is above 0.4, caps at 2.5 height)
                float snowMovementHindranceMultiplier = 1 + (5 * Mathf.Clamp01((snowThickness - 0.4f) / 2.1f));

                __instance.agent.speed /= snowMovementHindranceMultiplier;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Start))]
        [HarmonyPostfix]
        private static void SnowCleanupPatch()
        {
            SnowTrackersManager.CleanupTrackers();
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Start))]
        [HarmonyPrefix]
        private static void PlayerSnowTracksPatch(PlayerControllerB __instance)
        {
            SnowTrackersManager.AddFootprintTracker(__instance, 2.6f, 1f, 0.3f, new Vector3(0, 0, -1f));
        }

        //TODO MaskedPlayerEnemy doesn't work for some reason
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        [HarmonyPrefix]
        private static void EnemySnowTracksPatch(EnemyAI __instance)
        {
            switch (__instance)
            {
                case ForestGiantAI:
                    SnowTrackersManager.AddFootprintTracker(__instance, 10f, 0.167f, 0.35f);
                    break;
                case RadMechAI:
                    SnowTrackersManager.AddFootprintTracker(__instance, 8f, 0.167f, 0.35f);
                    break;
                case SandWormAI:
                    SnowTrackersManager.AddFootprintTracker(__instance, 25f, 0.167f, 1f);
                    break;
                default:
                    if (!unaffectedEnemyTypes.Contains(__instance.GetType()))
                    {
                        SnowTrackersManager.AddFootprintTracker(__instance, 2f, 0.167f, 0.35f);
                    }
                    break;
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        [HarmonyPrefix]
        private static void GrabbableSnowTracksPatch(GrabbableObject __instance)
        {
            SnowTrackersManager.AddFootprintTracker(__instance, 2f, 0.167f, 0.7f);
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Start))]
        [HarmonyPrefix]
        private static void VehicleSnowTracksPatch(VehicleController __instance)
        {
            SnowTrackersManager.AddFootprintTracker(__instance, 6f, 0.75f, 1f, new Vector3(0, 0, 1.5f));
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
        [HarmonyPrefix]
        private static void PlayerSnowTracksUpdatePatch(PlayerControllerB __instance)
        {
            if (!IsSnowActive())
            {
                return;
            }
            bool enableTracker = !__instance.isInsideFactory && SnowThicknessManager.Instance != null
                && SnowThicknessManager.Instance.IsEntityOnNaturalGround(__instance);
            // We need this check to prevent updating tracker's position after player death, as players get moved out of bounds on their death, causing VFX to be culled
            if (!__instance.isPlayerDead)
            {
                SnowTrackersManager.UpdateFootprintTracker(__instance, enableTracker);
            }

        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Update))]
        [HarmonyPrefix]
        private static void EnemySnowTracksUpdatePatch(EnemyAI __instance)
        {
            if (!IsSnowActive())
            {
                return;
            }
            // __instance.isOutside is a simplified check for clients, may cause incorrect behaviour in some cases

            bool enableTracker = __instance.isOutside || (SnowThicknessManager.Instance != null
                && SnowThicknessManager.Instance.IsEntityOnNaturalGround(__instance));
            if (__instance is SandWormAI worm)
            {
                enableTracker &= worm.emerged || worm.inEmergingState;
            }
            SnowTrackersManager.UpdateFootprintTracker(__instance, enableTracker);
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Update))]
        [HarmonyPrefix]
        private static void VehicleSnowTracksUpdatePatch(VehicleController __instance)
        {
            if (!IsSnowActive())
            {
                return;
            }

            bool enableTracker = __instance.FrontLeftWheel.isGrounded ||
                                    __instance.FrontRightWheel.isGrounded ||
                                    __instance.BackLeftWheel.isGrounded ||
                                    __instance.BackRightWheel.isGrounded;
            SnowTrackersManager.UpdateFootprintTracker(__instance, enableTracker);
        }

        // Not required cause players are never destroyed as an object
        // [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        // [HarmonyPostfix]
        // private static void PlayerSnowTracksDeathPatch(PlayerControllerB __instance)
        // {
        //     SnowTrackersManager.TempSaveTracker(__instance, TrackerType.Footprints);
        // }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnDestroy))]
        [HarmonyPostfix]
        private static void EnemySnowTracksDestroyPatch(EnemyAI __instance)
        {
            SnowTrackersManager.TempSaveTracker(__instance, TrackerType.FootprintsLowCapacity);
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.DestroyCar))]
        [HarmonyPostfix]
        private static void VehicleSnowTracksDestroyPatch(VehicleController __instance)
        {
            SnowTrackersManager.TempSaveTracker(__instance, TrackerType.Footprints);
        }

        [HarmonyPatch(typeof(ItemDropship), nameof(ItemDropship.ShipLandedAnimationEvent))]
        [HarmonyPrefix]
        private static void ItemShipSnowPatch(ItemDropship __instance)
        {
            SnowTrackersManager.RegisterFootprintTracker(__instance, TrackerType.Item, particleSize: 18f);
            SnowTrackersManager.PlayFootprintTracker(__instance, TrackerType.Item, true);
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.PlayDropSFX))]
        [HarmonyPrefix]
        private static void GrabbableFallSnowPatch(GrabbableObject __instance)
        {
            SnowTrackersManager.PlayFootprintTracker(__instance, TrackerType.Item, !__instance.isInFactory);
        }

        [HarmonyPatch(typeof(Shovel), nameof(Shovel.ReelUpSFXClientRpc))]
        [HarmonyPrefix]
        private static void ShovelSnowPatch(Shovel __instance)
        {
            SnowTrackersManager.PlayFootprintTracker(__instance, TrackerType.Shovel, !__instance.isInFactory);
        }

        internal static void RemoveEnemiesSnow()
        {
            // Some enemies might not have been restored due to going to menu
            if (enemiesToRestore.Count > 0)
            {
                RestoreEnemies();
            }

            if (!NetworkManager.Singleton.IsHost || !IsSnowActive())
            {
                return;
            }

            foreach (SpawnableEnemyWithRarity enemy in RoundManager.Instance.currentLevel.DaytimeEnemies)
            {
                if (EnemySpawnBlacklist!.Contains(enemy.enemyType.enemyName.ToLower(CultureInfo.InvariantCulture)))
                {
                    if (!enemy.enemyType.spawningDisabled)
                    {
                        Debug.LogDebug($"Removing {enemy.enemyType.enemyName} due to cold conditions.");
                        _ = enemiesToRestore.Add(enemy);
                        enemy.enemyType.spawningDisabled = true;
                    }
                }
            }

            foreach (SpawnableEnemyWithRarity enemy in RoundManager.Instance.currentLevel.OutsideEnemies)
            {
                if (EnemySpawnBlacklist!.Contains(enemy.enemyType.enemyName.ToLower(CultureInfo.InvariantCulture)))
                {
                    if (!enemy.enemyType.spawningDisabled)
                    {
                        Debug.LogDebug($"Removing {enemy.enemyType.enemyName} due to cold conditions.");
                        _ = enemiesToRestore.Add(enemy);
                        enemy.enemyType.spawningDisabled = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void RestoreBeesSnowPatch(StartOfRound __instance)
        {
            if (!__instance.IsHost || !IsSnowActive() || enemiesToRestore.Count == 0)
                return;

            RestoreEnemies();
        }

        private static void RestoreEnemies()
        {
            foreach (SpawnableEnemyWithRarity enemy in enemiesToRestore)
            {
                enemy.enemyType.spawningDisabled = false;
                Debug.LogDebug($"Restoring {enemy.enemyType.enemyName} after cold.");
            }

            enemiesToRestore.Clear();
        }

        private static bool SurfaceSamplingOverride(PlayerControllerB playerScript)
        {
            bool isOnGround = Physics.Raycast(playerScript.interactRay, out playerScript.hit, 6f, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore);
            bool isSameSurface = !isOnGround || playerScript.hit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[playerScript.currentFootstepSurfaceIndex].surfaceTag);
            bool snowOverride = false;

            if (!IsSnowActive() || !(LevelManipulator.Instance != null && LevelManipulator.Instance.IsSnowReady))
            {
                return !isOnGround || isSameSurface;
            }

            if (SnowThicknessManager.Instance != null && isOnGround)
            {
                // TODO for the local player update data in PlayerControllerB.CalculateGroundNormal
                SnowThicknessManager.Instance.UpdateEntityData(playerScript, playerScript.hit);

                // Override footstep sound if snow is thick enough
                if (SnowfallVFXManager.snowFootstepIndex != -1 &&
                    SnowThicknessManager.Instance.IsEntityOnNaturalGround(playerScript) &&
                     SnowThicknessManager.Instance.GetSnowThickness(playerScript) > 0.1f // offset is not applied here for nonlocal player so they would produce normal footstep sounds at edge cases
                    )
                {
                    snowOverride = true;
                    playerScript.currentFootstepSurfaceIndex = SnowfallVFXManager.snowFootstepIndex;
                }
            }

            return !isOnGround || isSameSurface || snowOverride;
        }

        private static void LocalGroundUpdate(PlayerControllerB playerScript, int index)
        {
            if (IsSnowActive() && index == 0 && SnowThicknessManager.Instance != null)
            {
                SnowThicknessManager.Instance.UpdateEntityData(playerScript, playerScript.hit);
            }
        }

        // Patch for ice rebake condition
        // true if we should NOT delay rebaking navmesh for ice
        public static bool DelayRebakeForIce()
        {
            bool delayRebake = IsSnowActive() && LESettings.freezeWater.Value;
            Debug.LogDebug($"Should we delay NavMesh rebaking for ice: {delayRebake}");
            return !delayRebake;
        }

        // TODO Check if this is working
        public static bool IsSnowActive()
        {
            return (SnowfallWeather.Instance != null && SnowfallWeather.Instance.IsActive) || (BlizzardWeather.Instance != null && BlizzardWeather.Instance.IsActive);
        }

        // public static void DebugSnowCheck()
        // {
        //     bool snowActive = SnowfallWeather.Instance?.gameObject.activeInHierarchy ?? false;
        //     bool blizzardActive = BlizzardWeather.Instance?.gameObject.activeInHierarchy ?? false;

        //     bool snowNameMatch = SnowfallWeather.Instance?.WeatherName.ToLower() == WeatherManager.GetCurrentLevelWeather().Name.ToLower();
        //     bool blizzardNameMatch = BlizzardWeather.Instance?.WeatherName.ToLower() == WeatherManager.GetCurrentLevelWeather().Name.ToLower();

        //     bool isLanding = !(StartOfRound.Instance?.inShipPhase ?? false);

        //     Debug.LogDebug($"SnowActive: {snowActive}, BlizzardActive: {blizzardActive}, SnowNameMatch: {snowNameMatch}, BlizzardNameMatch: {blizzardNameMatch}, InOrbit: {isLanding}");
        //     //Inspect names of the current weather and the weather in the level
        //     Debug.LogDebug($"Current weather: '{WeatherManager.GetCurrentLevelWeather().Name}', SnowfallWeather: '{SnowfallWeather.Instance?.WeatherName}', BlizzardWeather: '{BlizzardWeather.Instance?.WeatherName}'");
        // }

    }

    [HarmonyPatch]
    internal sealed class SnowPatchesOptional
    {
        [HarmonyPatch(typeof(HDRenderPipeline), "RenderTransparency")]
        [HarmonyTranspiler]
        [HarmonyDebug]
        [HarmonyPriority(Priority.First)]
        private static IEnumerable<CodeInstruction> LowResTransparencyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1), // renderGraph
                new(OpCodes.Ldarg_2), // hdCamera
                new(OpCodes.Ldarg_3), // colorBuffer
                new(OpCodes.Ldarg_S), // normalBuffer 
                new(OpCodes.Ldarg_S), // prepassOutput
                new(OpCodes.Ldfld),
                new(OpCodes.Call, AccessTools.Method(typeof(HDRenderPipeline), "RenderUnderWaterVolume")),
                new(OpCodes.Starg_S)  // colorBuffer
            );

            if (codeMatcher.IsInvalid)
            {
                Debug.LogError("Failed to find a match for RenderUnderWaterVolume");
                return instructions;
            }

            // Save next instruction index
            int beginIndex = codeMatcher.Pos + 1;

            _ = codeMatcher.MatchForward(true,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(HDRenderPipeline), "UpsampleTransparent")));

            if (codeMatcher.IsInvalid)
            {
                Debug.LogError("Failed to find a match for UpsampleTransparent");
                return instructions;
            }

            // Save current instruction index
            int endIndex = codeMatcher.Pos;

            // Cut out and save the block of code that renders transparent objects in low resolution
            List<CodeInstruction> lowResBlock = codeMatcher.InstructionsInRange(beginIndex, endIndex);
            // // Print the block of code into the console
            // Debug.LogDebug("LowResBlock:");
            // foreach (var instruction in lowResBlock)
            // {
            //     Debug.LogDebug(instruction.ToString());
            // }
            // Debug.LogDebug("End of LowResBlock");
            // Remove the block of code from the original instructions
            _ = codeMatcher.RemoveInstructionsInRange(beginIndex, endIndex)
            .Start()
            .MatchForward(true, // Find the insertion point
                new(OpCodes.Call, AccessTools.Method(typeof(HDRenderPipeline), "RenderForwardTransparent")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Call, AccessTools.Method(typeof(HDRenderPipeline), "ResetCameraMipBias"))
            );

            if (codeMatcher.IsInvalid)
            {
                Debug.LogError("Failed to find a match for RenderForwardTransparent");
                return instructions;
            }

            // Insert the block of code that renders transparent objects in low resolution after SetGlobalColorForCustomPass
            return codeMatcher.Advance(1)
            .Insert(lowResBlock)
            .InstructionEnumeration();
        }
    }
}
