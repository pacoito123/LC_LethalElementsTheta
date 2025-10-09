using System.Runtime.CompilerServices;
using WeatherRegistry;
using WeatherTweaks.Definitions;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Compatibility
{
    internal sealed class WeatherTweaksCompat
    {
        /// <summary>
        ///     Whether LethalLevelLoader is present in the BepInEx Chainloader or not.
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

            if (LESettings.EnableSolarFlareWeather.Value)
            {
                _ = new CombinedWeatherType("Eclipsed Flare", [
                    new WeatherNameResolvable("solarflare"), new WeatherTypeResolvable(LevelWeatherType.Eclipsed)])
                {
                    Color = new(159.0f, 29.0f, 53.0f)
                };

                if (LESettings.EnableSnowfallWeather.Value)
                {
                    _ = new CombinedWeatherType("Aurora Borealis", [
                        new WeatherNameResolvable("solarflare"), new WeatherNameResolvable("snowfall")])
                    {
                        Color = new(159.0f, 0.0f, 255.0f)
                    };
                }

                if (LESettings.EnableHeatwaveWeather.Value)
                {
                    _ = new ProgressingWeatherType("Solar Flare > Heatwave", new WeatherNameResolvable("solarflare"), [
                        new ProgressingWeatherEntry() { DayTime = 0.6f, Chance = 1.0f, Weather = new WeatherNameResolvable("heatwave") }])
                    {
                        Color = new(253.0f, 94.0f, 83.0f)
                    };
                }
            }

            if (LESettings.EnableSnowfallWeather.Value)
            {
                _ = new ProgressingWeatherType("Snowfall > Rainy", new WeatherNameResolvable("snowfall"), [
                    new ProgressingWeatherEntry() { DayTime = 0.5f, Chance = 0.75f, Weather = new WeatherTypeResolvable(LevelWeatherType.Rainy) },
                    new ProgressingWeatherEntry() { DayTime = 0.75f, Chance = 1.0f, Weather = new WeatherTypeResolvable(LevelWeatherType.Rainy) }])
                {
                    Color = new(135.0f, 206.0f, 235.0f)
                };
            }

            IsWeatherRegistered = true;
            Debug.LogDebug("Registered custom combined and progressing weathers!");
        }
    }
}
