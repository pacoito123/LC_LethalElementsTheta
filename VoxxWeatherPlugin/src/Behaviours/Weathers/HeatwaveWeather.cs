using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using VoxxWeatherPlugin.Behaviours;
using VoxxWeatherPlugin.Utils;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Weathers
{
    internal sealed class HeatwaveWeather : BaseWeather
    {
        public static HeatwaveWeather Instance { get; private set; } = null!;
        [SerializeField]
        internal Volume? exhaustionFilter; // Filter for visual effects
        private BoxCollider? heatwaveTrigger; // Trigger collider for the heatwave zone
        private static float TimeUntilStrokeMin => LESettings.TimeUntilStrokeMin.Value; // Minimum time until a heatstroke occurs
        private static float TimeUntilStrokeMax => LESettings.TimeUntilStrokeMax.Value; // Maximum time until a heatstroke occurs
        [SerializeField]
        internal float timeInHeatZoneMax = 50f; // Time before maximum effects are applied
        [SerializeField]
        internal float timeOfDayFactor = 1f; // Factor for the time of day
        [SerializeField]
        internal HeatwaveVFXManager? VFXManager; // Manager for heatwave visual effects

        private void Awake()
        {
            Instance = this;
            // Add a BoxCollider component as a trigger to the GameObject
            if (heatwaveTrigger == null)
                heatwaveTrigger = gameObject.AddComponent<BoxCollider>();
            heatwaveTrigger.isTrigger = true;
            heatwaveTrigger.includeLayers = 0;
            heatwaveTrigger.excludeLayers = ~LayerMask.GetMask("Player", "PlayerRagdoll");
            PlayerEffectsManager.heatEffectVolume = exhaustionFilter;
        }

        private void OnEnable()
        {
            LevelManipulator.Instance.InitializeLevelProperties(1.3f);
            if (VFXManager != null)
                VFXManager.PopulateLevelWithVFX();
            SetupHeatwaveWeather();
        }

        private void OnDisable()
        {
            LevelManipulator.Instance.ResetLevelProperties();
            if (VFXManager != null)
                VFXManager.Reset();
            PlayerEffectsManager.normalizedTemperature = 0f;
        }

        private void SetupHeatwaveWeather()
        {
            // Set the size, position and rotation of the trigger zone
            heatwaveTrigger!.size = LevelBounds.size;
            heatwaveTrigger.transform.SetPositionAndRotation(LevelBounds.center, Quaternion.identity);
            VFXManager!.heatwaveVFXContainer!.transform.parent = transform; // Parent the container to the weather instance to make it stationary

            Debug.LogDebug($"Heatwave zone size: {LevelBounds.size}. Placed at {LevelBounds.center}");

            // Set exhaustion time for the player
            timeInHeatZoneMax = SeededRandom!.NextDouble(TimeUntilStrokeMin, TimeUntilStrokeMax);
            Debug.LogDebug($"Set time until heatstroke: {timeInHeatZoneMax} seconds");
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.TryGetComponent(out PlayerControllerB player))
            {
                if (player != GameNetworkManager.Instance.localPlayerController)
                    return;

                PlayerEffectsManager.isInHeatZone = true;
            }
            // else if (other.CompareTag("Aluminum") && LayerMask.LayerToName(other.gameObject.layer) == "Vehicle")
            // {
            //     if (other.TryGetComponent(out VehicleHeatwaveHandler cruiserHandler))
            //     {
            //         cruiserHandler.isInHeatwave = true;
            //     }
            // }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PlayerControllerB player))
            {
                if (player != GameNetworkManager.Instance.localPlayerController)
                    return;

                PlayerEffectsManager.heatTransferRate = 1f;
                PlayerEffectsManager.isInHeatZone = false;
            }
            // else if (other.CompareTag("Aluminum") && LayerMask.LayerToName(other.gameObject.layer) == "Vehicle")
            // {
            //     if (other.TryGetComponent(out VehicleHeatwaveHandler cruiserHandler))
            //     {
            //         cruiserHandler.isInHeatwave = false;
            //     }
            // }
        }
    }


    public class HeatwaveVFXManager : BaseVFXManager
    {
        public GameObject heatwaveParticlePrefab = null!; // Prefab for the heatwave particle effect
        public GameObject? heatwaveVFXContainer; // GameObject for the particles
        [SerializeField]
        internal AnimationCurve heatwaveIntensityCurve = null!; // Curve for the intensity of the heatwave
        private Coroutine? cooldownCoroutine; // Coroutine for cooling down the heatwave VFX
        private readonly List<VisualEffect> cachedVFX = []; // Cached VFX for the heatwave particles
        private bool isPopulated;

        // Variables for emitter placement
        private float emitterSize;
        private readonly int spawnRatePropertyID = Shader.PropertyToID("particleSpawnRate"); // Property ID for the spawn rate of the particles

        internal void CalculateEmitterRadius()
        {
            Transform transform = heatwaveParticlePrefab.transform;
            emitterSize = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) * 5f;
        }

        internal override void PopulateLevelWithVFX()
        {
            if (LevelBounds == null || SeededRandom == null || heatwaveParticlePrefab == null)
            {
                Debug.LogError("Level bounds, random seed or heatwave particle prefab not set!");
                return;
            }

            CalculateEmitterRadius();

            if (heatwaveVFXContainer == null)
                heatwaveVFXContainer = new GameObject("HeatwaveVFXContainer");

            _ = StartCoroutine(PlaceHeatwaveVFXCoroutine());
        }

        private IEnumerator PlaceHeatwaveVFXCoroutine()
        {
            int placedEmittersNum = 0;

            int xCount = Mathf.CeilToInt(LevelBounds.size.x / emitterSize);
            int zCount = Mathf.CeilToInt(LevelBounds.size.z / emitterSize);
            Debug.LogDebug($"Placing {xCount * zCount} emitters...");

            Vector3 startPoint = LevelBounds.center - (LevelBounds.size * 0.5f);
            float raycastHeight = 500f; // Height from which to cast rays

            float minY = -1f;
            float maxY = 1f;

            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    // Randomize the position of the emitter within the grid cell
                    float dx = (float)SeededRandom.NextDouble() - 0.5f;
                    float dz = (float)SeededRandom.NextDouble() - 0.5f;
                    Vector3 rayOrigin = startPoint + new Vector3((x + dx) * emitterSize, raycastHeight, (z + dz) * emitterSize);
                    //Debug.LogDebug($"Raycast origin: {rayOrigin}");
                    (Vector3 position, Vector3 normal) = CastRayAndSampleNavMesh(rayOrigin);
                    //Debug.LogDebug($"NavMesh hit position and normal: {position}, {normal}");

                    if (position != Vector3.zero)
                    {
                        float randomRotation = (float)SeededRandom.NextDouble() * 360f;
                        Quaternion rotation = Quaternion.AngleAxis(randomRotation, normal) * Quaternion.LookRotation(normal);
                        //position.y -= 0.5f; // Offset the emitter slightly below the ground
                        GameObject emitter = Instantiate(heatwaveParticlePrefab, position, rotation);
                        VisualEffect vfx = emitter.GetComponent<VisualEffect>();
                        cachedVFX.Add(vfx);
                        emitter.SetActive(true);
                        emitter.transform.parent = (heatwaveVFXContainer != null) ? heatwaveVFXContainer.transform : null; // Parent the emitter to the VFX container
                        placedEmittersNum++;

                        minY = Mathf.Min(minY, position.y);
                        maxY = Mathf.Max(maxY, position.y);
                    }

                    yield return null;
                }
            }

            //Adjust the height of the heatwave zone based on the placed emitters
            float newHeight = (maxY - minY) * 1.1f;
            float newYPos = (minY + maxY) * 0.5f;

            // Adjust the level bounds to fit the heatwave zone
            Bounds adjustedBounds = new(LevelBounds.center, LevelBounds.size)
            {
                size = new Vector3(LevelBounds.size.x, newHeight, LevelBounds.size.z),
                center = new Vector3(LevelBounds.center.x, newYPos, LevelBounds.center.z)
            };
            LevelManipulator.Instance.levelBounds = adjustedBounds;

            isPopulated = true;
            Debug.LogDebug($"Placed {placedEmittersNum} emitters.");
        }

        private static (Vector3, Vector3) CastRayAndSampleNavMesh(Vector3 rayOrigin)
        {
            int layerMask = LayerMask.GetMask("Default", "Room");

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1000, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 3f, -1)) //places only where player can walk
                {
                    Bounds doubledBounds = new(StartOfRound.Instance.shipBounds.bounds.center,
                                  StartOfRound.Instance.shipBounds.bounds.size * 2f);
                    if (!doubledBounds.Contains(navHit.position))
                        return (navHit.position, hit.normal);
                }
            }
            return (Vector3.zero, Vector3.up);
        }

        internal void Update()
        {
            if (isPopulated && cooldownCoroutine == null)
                cooldownCoroutine = StartCoroutine(CooldownHeatwaveVFX());
        }

        internal override void Reset()
        {
            if (heatwaveVFXContainer != null)
            {
                Destroy(heatwaveVFXContainer);
            }
            heatwaveVFXContainer = null;
            isPopulated = false;
            Debug.LogDebug("Heatwave VFX container destroyed.");

            cachedVFX.Clear();

            PlayerEffectsManager.isInHeatZone = false;
            PlayerEffectsManager.heatTransferRate = 1f;
        }

        private void OnEnable()
        {
            if (heatwaveVFXContainer != null)
                heatwaveVFXContainer.SetActive(true);
        }

        private void OnDisable()
        {
            if (cooldownCoroutine != null)
            {
                StopCoroutine(cooldownCoroutine);
                cooldownCoroutine = null;
            }

            if (heatwaveVFXContainer != null)
                heatwaveVFXContainer.SetActive(false);
        }

        internal IEnumerator CooldownHeatwaveVFX()
        {
            float reductionFactor = 1.0f;
            if (LevelManipulator.Instance != null && LevelManipulator.Instance.sunLightData != null)
                reductionFactor = Mathf.Clamp01(LevelManipulator.Instance.sunLightData.intensity / LevelManipulator.Instance.startingSunIntensity);
            reductionFactor = heatwaveIntensityCurve.Evaluate(reductionFactor); // Min value in curve is 0.001 to avoid division by zero
            foreach (VisualEffect vfx in cachedVFX)
            {
                if (vfx != null)
                    vfx.SetFloat(spawnRatePropertyID, (LESettings.HeatwaveParticlesSpawnRate.Value * reductionFactor) + 0.1f);
                yield return Yielders.WaitForEndOfFrame;
            }

            HeatwaveWeather.Instance.timeOfDayFactor = reductionFactor;
            cooldownCoroutine = null;
        }
    }
}