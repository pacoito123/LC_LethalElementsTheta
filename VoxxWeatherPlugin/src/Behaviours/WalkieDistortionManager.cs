using UnityEngine;
using VoxxWeatherPlugin.Weathers;
using System.Collections.Generic;
using GameNetcodeStuff;

namespace VoxxWeatherPlugin.Behaviours
{
    public class WalkieDistortionManager : MonoBehaviour
    {
        internal Dictionary<AudioSource, GameObject> walkieSubTargets = [];
        private static readonly Dictionary<AudioSource, InterferenceDistortionFilter> cachedFilters = [];

        internal AudioSource SplitWalkieTarget(GameObject target)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive && SolarFlareWeather.Instance.flareData != null)
            {
                GameObject subTarget = new("SubTarget");
                subTarget.transform.position = target.transform.position;
                subTarget.transform.SetParent(gameObject.transform);
                AudioSource audioSource = subTarget.AddComponent<AudioSource>();
                InterferenceDistortionFilter interferenceFilter = subTarget.AddComponent<InterferenceDistortionFilter>();
                interferenceFilter.distortionChance = SolarFlareWeather.Instance.flareData.RadioDistortionIntensity;
                interferenceFilter.maxClarityDuration = SolarFlareWeather.Instance.flareData.RadioBreakthroughLength;
                interferenceFilter.maxFrequencyShift = SolarFlareWeather.Instance.flareData.RadioFrequencyShift;
                walkieSubTargets.Add(audioSource, subTarget);
                return audioSource;
            }
            else
            {
                return target.AddComponent<AudioSource>();
            }
        }

        internal void DisposeWalkieTarget(AudioSource audioSource)
        {
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive && SolarFlareWeather.Instance.flareData != null)
            {
                if (walkieSubTargets.TryGetValue(audioSource, out GameObject subTarget))
                {
                    _ = walkieSubTargets.Remove(audioSource);
                    Destroy(subTarget);
                }
                else
                    Debug.LogError("Failed to dispose walkie target: target not found in cache!");
            }
            else
                Destroy(audioSource);
        }

        public static void UpdateVoiceChatDistortion(AudioSource voiceSource, PlayerControllerB allPlayerScript, bool isUsingWalkieTalkie)
        {

            bool shouldEnableDistortion = SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.IsActive && !allPlayerScript.isPlayerDead
                && isUsingWalkieTalkie && SolarFlareWeather.Instance.flareData != null;

            InterferenceDistortionFilter? interferenceFilter = GetOrAddFilter(voiceSource);

            if (interferenceFilter == null)
            {
                return;
            }

            if (shouldEnableDistortion)
            {
                EnableVoiceChatDistortion(interferenceFilter);
            }
            else
            {
                DisableVoiceChatDistortion(interferenceFilter);
            }
        }

        private static void EnableVoiceChatDistortion(InterferenceDistortionFilter interferenceFilter)
        {
            if (!interferenceFilter.enabled && SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.flareData != null)
            {
                interferenceFilter.enabled = true;
                interferenceFilter.distortionChance = SolarFlareWeather.Instance.flareData.RadioDistortionIntensity;
                interferenceFilter.maxClarityDuration = SolarFlareWeather.Instance.flareData.RadioBreakthroughLength;
                interferenceFilter.maxFrequencyShift = SolarFlareWeather.Instance.flareData.RadioFrequencyShift;
            }
        }

        private static void DisableVoiceChatDistortion(InterferenceDistortionFilter interferenceFilter)
        {
            if (interferenceFilter.enabled)
            {
                interferenceFilter.enabled = false;
            }
        }

        private static InterferenceDistortionFilter? GetOrAddFilter(AudioSource voiceSource)
        {
            if (voiceSource == null)
            {
                Debug.LogError("Attempted to get or add filter for a null voice source!");
                return null;
            }


            if (!cachedFilters.TryGetValue(voiceSource, out InterferenceDistortionFilter filter))
            {
                filter = voiceSource.GetComponent<InterferenceDistortionFilter>();
                if (filter == null)
                {
                    filter = voiceSource.gameObject.AddComponent<InterferenceDistortionFilter>();
                }
                cachedFilters[voiceSource] = filter;
            }
            return filter;
        }

        public static void ClearFilterCache(AudioSource? voiceSource = null)
        {
            if (voiceSource != null)
            {
                if (cachedFilters.TryGetValue(voiceSource, out InterferenceDistortionFilter filter))
                {
                    Destroy(filter);
                }
                _ = cachedFilters.Remove(voiceSource);
            }
            else
            {
                cachedFilters.Clear();
            }
        }
    }
}