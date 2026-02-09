using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;
using VoxxWeatherPlugin.Behaviours;
using VoxxWeatherPlugin.Compatibility;
using VoxxWeatherPlugin.Utils;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Weathers
{
    internal sealed class ToxicSmogWeather : BaseWeather
    {
        public static ToxicSmogWeather? Instance { get; private set; }

        [SerializeField]
        internal ToxicSmogVFXManager? VFXManager;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            LevelManipulator.Instance.InitializeLevelProperties(1.4f);
            if (VFXManager != null)
                VFXManager.PopulateLevelWithVFX();
        }

        private void OnDisable()
        {
            LevelManipulator.Instance.ResetLevelProperties();
            if (VFXManager != null)
                VFXManager.Reset();
        }
    }

    internal sealed class ToxicSmogVFXManager : BaseVFXManager
    {
        [Header("Smog")]
        [SerializeField]
        private float smogFreePath = 24f;
        private static float MinFreePath => LESettings.MinFreePath.Value;
        private static float MaxFreePath => LESettings.MaxFreePath.Value;
        [SerializeField]
        private LocalVolumetricFog? toxicVolumetricFog;

        [Header("Fumes")]
        [SerializeField]
        internal GameObject? hazardPrefab; // Assign in the inspector
        [SerializeField]
        private int fumesAmount = 24;
        private static int MinFumesAmount => LESettings.MinFumesAmount.Value;
        private static int MaxFumesAmount => LESettings.MaxFumesAmount.Value;
        private static float FactoryAmountMultiplier => LESettings.FactoryAmountMultiplier.Value;
        [SerializeField]
        private int factoryFumesAmount = 12;
        [SerializeField]
        private GameObject? fumesContainerInside;
        [SerializeField]
        private GameObject? fumesContainerOutside;
        private readonly float spawnRadius = 20f;
        private readonly float minDistanceBetweenHazards = 5f;
        private readonly float minDistanceFromBlockers = 20f;
        private List<Vector3>? spawnedPositions;
        private int maxAttempts;
        [SerializeField]
        [ColorUsage(true, true)]
        internal Color toxicFumesColor = new(0.413f, 0.589f, 0.210f, 0f);
        [SerializeField]
        [ColorUsage(true, true)]
        internal Color toxicFogColor = new(0.413f, 0.589f, 0.210f); //dark lime green

        private void Awake()
        {
            if (hazardPrefab != null)
                hazardPrefab.SetActive(false);
        }

        private void OnEnable()
        {
            if (toxicVolumetricFog != null)
                toxicVolumetricFog.gameObject.SetActive(true);
            if (fumesContainerOutside != null)
            {
                fumesContainerOutside.SetActive(true);
            }
            if (fumesContainerInside != null)
            {
                fumesContainerInside.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (toxicVolumetricFog != null)
                toxicVolumetricFog.gameObject.SetActive(false);
            if (fumesContainerOutside != null)
            {
                fumesContainerOutside.SetActive(false);
            }
            if (fumesContainerInside != null)
            {
                fumesContainerInside.SetActive(true);
            }
        }

        internal override void PopulateLevelWithVFX()
        {

            if (toxicVolumetricFog == null)
            {
                GameObject toxicFogContainer = new("ToxicFog");
                toxicVolumetricFog = toxicFogContainer.AddComponent<LocalVolumetricFog>();
                toxicFogContainer.transform.SetParent(ToxicSmogWeather.Instance!.transform);
                toxicVolumetricFog.parameters.albedo = toxicFogColor;
                toxicVolumetricFog.parameters.blendingMode = LocalVolumetricFogBlendingMode.Additive;
                toxicVolumetricFog.parameters.falloffMode = LocalVolumetricFogFalloffMode.Linear;
            }
            else
            {
                toxicVolumetricFog.gameObject.SetActive(true);
            }

            if (LLLCompat.Enabled)
            {
                LLLCompat.TagRecolorToxic();
            }

            // Find the dungeon scale
            float dungeonSize = StartOfRound.Instance.currentLevel.factorySizeMultiplier;

            // Randomly select density
            smogFreePath = SeededRandom.NextDouble(MinFreePath, MaxFreePath);
            toxicVolumetricFog.parameters.meanFreePath = smogFreePath;
            // Position in the center of the level
            toxicVolumetricFog.parameters.size = LevelBounds.size;
            toxicVolumetricFog.transform.position = LevelBounds.center;
            toxicVolumetricFog.parameters.distanceFadeStart = LevelBounds.size.x * 0.9f;
            toxicVolumetricFog.parameters.distanceFadeEnd = LevelBounds.size.x;

            fumesAmount = SeededRandom.Next(MinFumesAmount, MaxFumesAmount);
            factoryFumesAmount = Mathf.CeilToInt(fumesAmount * FactoryAmountMultiplier * dungeonSize);

            // Cache entrance positions and map objects
            EntranceTeleport[] entrances = FindObjectsByType<EntranceTeleport>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Transform mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer").transform;

            if (fumesContainerOutside == null)
            {
                fumesContainerOutside = new GameObject("FumesContainerOutside");
                fumesContainerOutside.transform.SetParent(mapPropsContainer);
            }

            // Use outside AI nodes as anchors
            List<Vector3> anchorPositions = [.. RoundManager.Instance.outsideAINodes.Select(node => node.transform.position)];
            // Use outside entrances as blockers
            List<Vector3> blockersPositions = [.. entrances.Select(entrance => entrance.transform.position)];
            //Add ship bounds to the list of blockers
            blockersPositions.AddRange([StartOfRound.Instance.shipBounds.transform.position, Vector3.zero]);
            Debug.LogDebug($"Outdoor fumes: Anchor positions: {anchorPositions.Count}, Blockers positions: {blockersPositions.Count}");
            SpawnFumes(anchorPositions, blockersPositions, fumesAmount, fumesContainerOutside, SeededRandom);

            if (fumesContainerInside == null)
            {
                fumesContainerInside = new GameObject("FumesContainerInside");
                fumesContainerInside.transform.SetParent(mapPropsContainer);
            }

            // Use item spawners AND AI nodes as anchors
            // CAUSES DESYNC OF FUMES
            // anchorPositions = RoundManager.Instance.spawnedSyncedObjects.Select(obj => obj.transform.position).ToList();
            anchorPositions = [.. RoundManager.Instance.insideAINodes.Select(obj => obj.transform.position)];
            // Use entrances as blockers
            blockersPositions = [.. entrances.Select(entrance => entrance.transform.position)];
            Debug.LogDebug($"Indoor fumes: Anchor positions: {anchorPositions.Count}, Blockers positions: {blockersPositions.Count}");
            SpawnFumes(anchorPositions, blockersPositions, factoryFumesAmount, fumesContainerInside, SeededRandom);
        }

        private void SpawnFumes(List<Vector3> anchors, List<Vector3> blockedPositions, int amount, GameObject container, System.Random random)
        {
            if (hazardPrefab == null)
            {
                Debug.LogError("Hazard Spawner: hazardPrefab is not set");
                return;
            }

            if (container == null)
            {
                Debug.LogError("Hazard Spawner: container is not set");
                return;
            }

            spawnedPositions = new List<Vector3>(amount);

            maxAttempts = amount * 3;

            NavMeshHit navHit = new();

            for (int i = 0; i < maxAttempts && spawnedPositions.Count < amount; i++)
            {
                // Randomly select an object to spawn around
                int randomObjectIndex = random.Next(anchors.Count);
                Vector3 objectPosition = anchors[randomObjectIndex];

                Vector3 potentialPosition = GetValidSpawnPosition(objectPosition, blockedPositions, ref navHit, random);
                if (potentialPosition != Vector3.zero)
                {
                    GameObject spawnedHazard = Instantiate(hazardPrefab, potentialPosition, Quaternion.identity, container.transform);
                    spawnedHazard.SetActive(true);
                    spawnedPositions.Add(potentialPosition);
                }
            }

            Debug.LogDebug($"Spawned {spawnedPositions.Count} hazards out of {amount}");
        }

        private Vector3 GetValidSpawnPosition(Vector3 objectPosition, List<Vector3> blockedPositions, ref NavMeshHit navHit, System.Random random)
        {
            Vector3 potentialPosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(
                                                objectPosition, spawnRadius, navHit, random, NavMesh.AllAreas);

            return IsPositionValid(potentialPosition, blockedPositions) ? potentialPosition : Vector3.zero;
        }

        private bool IsPositionValid(Vector3 position, List<Vector3> blockedPositions)
        {
            float sqrMinDistanceBetweenHazards = minDistanceBetweenHazards * minDistanceBetweenHazards;
            float sqrMinDistanceFromBlockers = minDistanceFromBlockers * minDistanceFromBlockers;

            // Check distance from other hazards
            for (int i = 0; i < spawnedPositions!.Count; i++)
            {
                if ((position - spawnedPositions[i]).sqrMagnitude < sqrMinDistanceBetweenHazards)
                {
                    return false;
                }
            }

            // Check distance from EntranceTeleport objects
            for (int i = 0; i < blockedPositions.Count; i++)
            {
                if ((position - blockedPositions[i]).sqrMagnitude < sqrMinDistanceFromBlockers)
                {
                    return false;
                }
            }

            return true;
        }

        internal override void Reset()
        {
            if (fumesContainerInside != null)
            {
                Destroy(fumesContainerInside);
            }

            if (fumesContainerOutside != null)
            {
                Destroy(fumesContainerOutside);
            }

            fumesContainerInside = null;
            fumesContainerOutside = null;

            if (toxicVolumetricFog != null)
                toxicVolumetricFog.gameObject.SetActive(false);
        }

        internal void SetToxicFumesColor(Color fogColor, Color fumesColor)
        {
            if (toxicVolumetricFog != null)
            {
                toxicVolumetricFog.parameters.albedo = fogColor;
            }

            VisualEffect? fumesVFX = hazardPrefab != null ? hazardPrefab.GetComponent<VisualEffect>() : null;

            if (fumesVFX != null)
            {
                Debug.LogDebug($"Setting fumes color to {fumesColor}");
                fumesVFX.SetVector4(ToxicShaderIDs.FumesColor, fumesColor);
            }

            Debug.LogDebug($"Current fumes color: {(fumesVFX != null ? fumesVFX.GetVector4(ToxicShaderIDs.FumesColor) : null)}");
        }
    }
}