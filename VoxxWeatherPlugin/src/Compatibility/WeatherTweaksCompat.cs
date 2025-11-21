using System.Runtime.CompilerServices;
using UnityEngine;
using WeatherRegistry;
using WeatherTweaks.Definitions;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Compatibility
{
    internal sealed class WeatherTweaksCompat
    {
        /// <summary>
        ///     Whether WeatherTweaks is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("WeatherTweaks");

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;

        public static bool IsWeatherRegistered { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0011:ScriptableObject instance creation", Justification = "MORV!")]
        public static void RegisterCombinedWeathers()
        {
            if (IsWeatherRegistered)
            {
                return;
            }

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("zigzag.combinedweatherstoolkit"))
            {
                Debug.LogDebug("Registering custom combined and progressing weathers through CombinedWeathersToolkit, for it is loaded.");
                IsWeatherRegistered = true;
                return;
            }

            if (LESettings.EnableSolarFlareWeather.Value)
            {
                _ = new CombinedWeatherType("Eclipsed Flare", [
                    new WeatherNameResolvable("solarflare"), new WeatherTypeResolvable(LevelWeatherType.Eclipsed)])
                {
                    Color = new Color32(159, 29, 53, 255) // Vivid Burgundy - #9F1D35
                };

                if (LESettings.EnableSnowfallWeather.Value)
                {
                    _ = new CombinedWeatherType("Aurora Borealis", [
                        new WeatherNameResolvable("solarflare"), new WeatherNameResolvable("snowfall")])
                    {
                        Color = new Color32(159, 0, 255, 255) // Hex Vivid Violet - #9F00FF
                    };
                }

                if (LESettings.EnableHeatwaveWeather.Value)
                {
                    _ = new ProgressingWeatherType("Solar Flare > Heatwave", new WeatherNameResolvable("solarflare"), [
                        new ProgressingWeatherEntry() { DayTime = 0.6f, Chance = 1.0f, Weather = new WeatherNameResolvable("heatwave") }])
                    {
                        Color = new Color32(255, 64, 64, 255) // Brown1 - #FF4040
                    };
                }
            }

            if (LESettings.EnableSnowfallWeather.Value)
            {
                _ = new ProgressingWeatherType("Snowfall > Rainy", new WeatherNameResolvable("snowfall"), [
                    new ProgressingWeatherEntry() { DayTime = 0.5f, Chance = 0.75f, Weather = new WeatherTypeResolvable(LevelWeatherType.Rainy) },
                    new ProgressingWeatherEntry() { DayTime = 0.75f, Chance = 1.0f, Weather = new WeatherTypeResolvable(LevelWeatherType.Rainy) }])
                {
                    Color = new Color32(135, 206, 235, 255) // SkyBlue - #87CEEB
                };
            }

            Debug.LogDebug("Registered custom combined and progressing weathers!");
            IsWeatherRegistered = true;
        }
    }
}
