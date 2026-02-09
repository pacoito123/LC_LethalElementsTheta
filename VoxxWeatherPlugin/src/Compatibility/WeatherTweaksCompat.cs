using BepInEx.Bootstrap;
using HarmonyLib;
using System.Runtime.CompilerServices;
using VoxxWeatherPlugin.Utils;
using WeatherRegistry;
using WeatherTweaks.Definitions;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Compatibility
{
    [HarmonyPatch]
    internal sealed class WeatherTweaksCompat
    {
        /// <summary>
        ///     Whether WeatherTweaks is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= Chainloader.PluginInfos.ContainsKey("WeatherTweaks");

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;

        public static bool WeathersInitialized { get; private set; }

        [HarmonyPatch(typeof(WeatherManager), "Reset")]
        [HarmonyPostfix]
        internal static void WeatherManagerResetPost()
        {
            if (WeathersInitialized)
            {
                return;
            }

            if (Enabled)
            {
                Debug.LogDebug("WeatherTweaks detected!");
                RegisterCombinedWeathers();
            }

            WeathersInitialized = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void RegisterCombinedWeathers()
        {
            if (Chainloader.PluginInfos.ContainsKey("zigzag.combinedweatherstoolkit"))
            {
                Debug.LogDebug("Registering custom combined and progressing weathers through CombinedWeathersToolkit, for it is loaded.");
                return;
            }

            if (LESettings.EnableSolarFlareWeather.Value)
            {
                CombinedWeatherType eclipsedFlare = new("Eclipsed Flare", [
                    new WeatherNameResolvable("solarflare"), new WeatherTypeResolvable(LevelWeatherType.Eclipsed)]);
                eclipsedFlare.CreateColorGradient(TMPro.ColorMode.Single, new(159, 29, 53, 255)); // Vivid Burgundy - #9F1D35

                if (LESettings.EnableSnowfallWeather.Value)
                {
                    CombinedWeatherType auroraBorealis = new("Aurora Borealis", [
                        new WeatherNameResolvable("solarflare"), new WeatherNameResolvable("snowfall")]);
                    auroraBorealis.CreateColorGradient(TMPro.ColorMode.Single, new(159, 0, 255, 255)); // Hex Vivid Violet - #9F00FF
                }

                if (LESettings.EnableHeatwaveWeather.Value)
                {
                    ProgressingWeatherType solarFlareToHeatwave = new("Solar Flare > Heatwave", new WeatherNameResolvable("solarflare"), [
                        new ProgressingWeatherEntry() { DayTime = 0.6f, Chance = 1.0f, Weather = new WeatherNameResolvable("heatwave") }]);
                    solarFlareToHeatwave.CreateColorGradient(TMPro.ColorMode.Single, new(255, 64, 64, 255)); // Brown1 - #FF4040
                }
            }

            if (LESettings.EnableSnowfallWeather.Value)
            {
                ProgressingWeatherType snowfallToRainy = new("Snowfall > Rainy", new WeatherNameResolvable("snowfall"), [
                    new ProgressingWeatherEntry() { DayTime = 0.5f, Chance = 0.75f, Weather = new WeatherTypeResolvable(LevelWeatherType.Rainy) },
                    new ProgressingWeatherEntry() { DayTime = 0.75f, Chance = 1.0f, Weather = new WeatherTypeResolvable(LevelWeatherType.Rainy) }]);
                snowfallToRainy.CreateColorGradient(TMPro.ColorMode.Single, new(135, 206, 235, 255)); // SkyBlue - #87CEEB
            }

            Debug.LogDebug("Registered custom combined and progressing weathers!");
        }
    }
}