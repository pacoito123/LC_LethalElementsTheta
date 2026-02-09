using BepInEx.Bootstrap;
using System;
using System.Runtime.CompilerServices;
using TMPro;
using WeatherRegistry;

namespace VoxxWeatherPlugin.Compatibility
{
    internal sealed class WeatherRegistryCompat
    {
        public static bool IsLegacyWR
        {
            get
            {
                _isLegacyWR ??= Chainloader.PluginInfos[WeatherRegistry.PluginInfo.PLUGIN_GUID].Metadata.Version.CompareTo(new Version(0, 8, 0)) < 0;

                return (bool)_isLegacyWR;
            }
        }
        private static bool? _isLegacyWR;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void SetColorGradient(Weather weather, TMP_ColorGradient colorGradient)
        {
            weather.ColorGradient = colorGradient;
        }
    }
}