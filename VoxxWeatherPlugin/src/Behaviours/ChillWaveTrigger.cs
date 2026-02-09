using GameNetcodeStuff;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using VoxxWeatherPlugin.Utils;
using VoxxWeatherPlugin.Weathers;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Behaviours
{
    public class ChillWaveTrigger : MonoBehaviour
    {
        public AudioSource? audioSourceTemplate;
        private AudioSource[]? audioSources;
        public Camera? collisionCamera;
        private LocalVolumetricFog? blizzardWaveFog;
        internal static int WaveDamage => LESettings.chillingWaveDamage.Value;
        [SerializeField]
        internal float waveForce = 40f;
        internal Coroutine? temperatureChangeCoroutine;
        [SerializeField]
        internal bool collidedWithLocalPlayer = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerControllerB player))
            {
                if (player != GameNetworkManager.Instance.localPlayerController || collidedWithLocalPlayer || player.isInsideFactory)
                    return;

                if (PlayerEffectsManager.isInColdZone)
                {
                    temperatureChangeCoroutine ??= StartCoroutine(TemperatureChangeCoroutine());
                    if (WaveDamage > 0)
                    {
                        player.DamagePlayer(WaveDamage, causeOfDeath: CauseOfDeath.Unknown);
                    }
                    player.externalForceAutoFade += transform.forward * waveForce;
                    if (BlizzardWeather.Instance != null)
                    {
                        BlizzardVFXManager? blizzardVFX = BlizzardWeather.Instance.VFXManager;
                        if (blizzardVFX != null)
                            blizzardVFX.PlayWavePassSFX();
                    }
                    collidedWithLocalPlayer = true;
                }
            }
        }

        // Decrease the player's temperature to simulate the cold wave effect (0.5 seconds duration)
        internal static IEnumerator TemperatureChangeCoroutine()
        {
            float targetTemperature = -0.8f;
            float initialTemperature = PlayerEffectsManager.normalizedTemperature;
            if (initialTemperature > targetTemperature)
            {
                float duration = 0.5f; // half of a second
                float elapsedTime = 0f;

                while (elapsedTime < duration)
                {
                    float newTemperature = Mathf.Lerp(initialTemperature, targetTemperature, elapsedTime / duration);
                    // Calculate the delta to reach the new temperature
                    float temperatureDelta = newTemperature - PlayerEffectsManager.normalizedTemperature;
                    PlayerEffectsManager.SetPlayerTemperature(temperatureDelta);
                    yield return null;
                }

                float finalDelta = targetTemperature - PlayerEffectsManager.normalizedTemperature;
                PlayerEffectsManager.SetPlayerTemperature(finalDelta);
            }
        }

        private void Update()
        {
            //Adjust LOCAL x position of the camera so it follows the player on the x axis
            //Project the player's position on the x axis to the camera's local space

            if (collisionCamera != null)
            {
                Vector3 playerPosition = GameNetworkManager.Instance.localPlayerController.transform.position;
                Vector3 playerPositionLocal = transform.InverseTransformPoint(playerPosition);
                collisionCamera.transform.localPosition = new Vector3(playerPositionLocal.x, collisionCamera.transform.localPosition.y, collisionCamera.transform.localPosition.z);
                collisionCamera.LimitFrameRate(LESettings.collisionCamerasFPS.Value);
            }
        }

        internal void OnDisable()
        {
            if (temperatureChangeCoroutine != null)
            {
                StopCoroutine(temperatureChangeCoroutine);
                temperatureChangeCoroutine = null;
            }

            collidedWithLocalPlayer = false;
        }

        internal void SetupChillWave(Bounds levelBounds)
        {
            blizzardWaveFog = (blizzardWaveFog != null) ? blizzardWaveFog : gameObject.GetComponentInChildren<LocalVolumetricFog>(true);

            audioSourceTemplate = (audioSourceTemplate != null) ? audioSourceTemplate : gameObject.GetComponentInChildren<AudioSource>(true);
            audioSourceTemplate.gameObject.SetActive(false);

            collisionCamera = (collisionCamera != null) ? collisionCamera : gameObject.GetComponentInChildren<Camera>(true);

            BoxCollider waveCollider = gameObject.GetComponent<BoxCollider>();

            //Change the center and scale y size so the lower edge is at LevelManipulator.heightThreshold level, but current top edge is preserved
            float newHeightSpan = levelBounds.extents.y - LevelManipulator.Instance.heightThreshold;
            waveCollider.center = new Vector3(0f, LevelManipulator.Instance.heightThreshold + (newHeightSpan / 2), waveCollider.center.z);
            waveCollider.size = new Vector3(levelBounds.size.x, newHeightSpan, waveCollider.size.z);

            if (blizzardWaveFog != null)
            {
                blizzardWaveFog.gameObject.SetActive(LESettings.useVolumetricBlizzardFog.Value);
                blizzardWaveFog.parameters.size = new Vector3(waveCollider.size.x, waveCollider.size.z, waveCollider.size.y);
                blizzardWaveFog.transform.localPosition = waveCollider.center;
            }
            // Set the camera size to cover the whole box (this will require LOD bias to be set to 100 to stop culling)
            // float maxLength = Mathf.Max(waveCollider.size.x, waveCollider.size.y, waveCollider.size.z) / 2f;
            // collisionCamera!.orthographicSize = maxLength;

            float audioRange = audioSourceTemplate.maxDistance;
            // Destroy previous audio sources
            if (audioSources != null)
            {
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource != null)
                    {
                        Destroy(audioSource.gameObject);
                    }
                }
            }
            // Place audio sources along collider x axis so that their range covers the whole box with 10% overlap between them
            audioSources = new AudioSource[Mathf.CeilToInt(waveCollider.size.x / (0.9f * audioRange))];
            for (int i = 0; i < audioSources.Length; i++)
            {
                audioSources[i] = Instantiate(audioSourceTemplate, transform);
                audioSources[i].transform.localPosition = new Vector3((0.9f * audioRange * i) - (waveCollider.size.x / 2f), 0, 0);
                audioSources[i].maxDistance = audioRange;
                audioSources[i].gameObject.SetActive(true);
            }
        }

    }
}