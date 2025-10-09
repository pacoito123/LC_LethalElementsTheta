using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VoxxWeatherPlugin.Utils
{
    public class WeatherAssetLoader
    {
        private static readonly Dictionary<string, AssetBundle> loadedBundles = [];

        public static T? LoadAsset<T>(string bundleName, string assetName) where T : Object
        {
            AssetBundle? bundle = LoadBundle(bundleName);
            if (bundle == null)
            {
                return null;
            }

            return bundle.LoadAsset<T>(assetName);
        }

        private static AssetBundle? LoadBundle(string bundleName)
        {
            if (loadedBundles.ContainsKey(bundleName))
            {
                return loadedBundles[bundleName];
            }

            string dllPath = Assembly.GetExecutingAssembly().Location;
            string dllDirectory = System.IO.Path.GetDirectoryName(dllPath);
            string bundlePath = System.IO.Path.Combine(dllDirectory, bundleName);
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle != null)
            {
                loadedBundles.Add(bundleName, bundle);
            }
            else
            {
                Debug.LogError($"Failed to load AssetBundle: {bundleName}");
            }

            return bundle;
        }

        public static void UnloadAllBundles()
        {
            foreach (AssetBundle bundle in loadedBundles.Values)
            {
                bundle.Unload(true); // Unload assets as well
            }
            loadedBundles.Clear();
        }
    }
}