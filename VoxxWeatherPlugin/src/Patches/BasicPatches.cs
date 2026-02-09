using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using VoxxWeatherPlugin.Behaviours;
using VoxxWeatherPlugin.Utils;
using WeatherRegistry;

namespace VoxxWeatherPlugin.Patches
{
    [HarmonyPatch]
    internal sealed class BasicPatches
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        private static void CacheWeatherPatch()
        {
            if (LevelManipulator.Instance != null)
            {
                LevelManipulator.Instance.currentWeather = WeatherManager.GetCurrentLevelWeather();
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        private static void RegisterSynchronizerPatch()
        {
            _ = WeatherTypeLoader.LoadWeatherSynchronizer();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        [HarmonyPostfix]
        private static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                if (WeatherTypeLoader.weatherSynchronizerPrefab != null)
                {
                    GameObject networkHandlerHost = Object.Instantiate(WeatherTypeLoader.weatherSynchronizerPrefab, Vector3.zero, Quaternion.identity);
                    Object.DontDestroyOnLoad(networkHandlerHost);
                    networkHandlerHost.hideFlags = HideFlags.HideAndDontSave;
                    networkHandlerHost.GetComponent<NetworkObject>().Spawn();
                }
            }
        }
    }
}