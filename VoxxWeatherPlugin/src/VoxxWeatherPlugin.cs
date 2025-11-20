using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System;
using System.Collections.Generic;
using VoxxWeatherPlugin.Compatibility;
using VoxxWeatherPlugin.Patches;
using VoxxWeatherPlugin.Utils;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("voxx.TerraMesh", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Zaggy1024.OpenBodyCams", BepInDependency.DependencyFlags.SoftDependency)]
    // [BepInDependency("WeatherTweaks", BepInDependency.DependencyFlags.SoftDependency)] Disabled due to dependency loop
    public class VoxxWeatherPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource StaticLogger { get; private set; } = null!;

        internal static Harmony Harmony { get; private set; } = null!;
        public static Configuration LESettings { get; private set; } = null!;

        private void Awake()
        {
            StaticLogger = Logger;

            try
            {
                LESettings = new(cfg: Config);
                Harmony = new Harmony(PluginInfo.PLUGIN_GUID);

                WeatherTypeLoader.LoadLevelManipulator();
                Harmony.PatchAll(typeof(BasicPatches));

                if (LESettings.EnableSolarFlareWeather.Value)
                {
                    WeatherTypeLoader.RegisterFlareWeather();
                    Harmony.PatchAll(typeof(FlarePatches));
                    if (!LESettings.DistortOnlyVoiceDuringSolarFlare.Value)
                    {
                        Harmony.PatchAll(typeof(FlareOptionalWalkiePatches));
                        Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} optional solar flare patches successfully applied!");
                    }
                    Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} solar flare patches successfully applied!");
                }

                if (LESettings.EnableHeatwaveWeather.Value)
                {
                    WeatherTypeLoader.RegisterHeatwaveWeather();
                    Harmony.PatchAll(typeof(HeatwavePatches));
                    Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} heatwave patches successfully applied!");
                }

                if (LESettings.EnableToxicSmogWeather.Value)
                {
                    WeatherTypeLoader.RegisterToxicSmogWeather();
                    Harmony.PatchAll(typeof(ToxicPatches));
                    Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} toxic smog patches successfully applied!");
                }

                if (LESettings.EnableBlizzardWeather.Value || LESettings.EnableSnowfallWeather.Value)
                {
                    Harmony.PatchAll(typeof(SnowPatches));
                    // Harmony.PatchAll(typeof(SnowPatchesOptional));
                    Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} snow patches successfully applied!");

                    if (LESettings.EnableSnowfallWeather.Value)
                    {
                        WeatherTypeLoader.RegisterSnowfallWeather();
                    }

                    if (LESettings.EnableBlizzardWeather.Value)
                    {
                        Harmony.PatchAll(typeof(BlizzardPatches));
                        Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} blizzard patches successfully applied!");
                        WeatherTypeLoader.RegisterBlizzardWeather();
                    }

                    MethodInfo patchMethod = typeof(SnowPatches).GetMethod("EnemySnowHindrancePatch", BindingFlags.NonPublic | BindingFlags.Static);
                    DynamicHarmonyPatcher.PatchAllTypes(typeof(EnemyAI), "Update", patchMethod, PatchType.Postfix, Harmony, SnowPatches.unaffectedEnemyTypes);
                    Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} enemy snow hindrance patches successfully applied!");
                }

                // Delayed registering of the combined weathers for WeatherTweaks compatibility
                WeatherRegistry.EventManager.BeforeSetupStart.AddListener(() =>
                {
                    if (WeatherTweaksCompat.Enabled)
                    {
                        Logger.LogDebug("Weather Tweaks detected!");
                        WeatherTweaksCompat.RegisterCombinedWeathers();
                    }
                });

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            }
            catch (Exception e)
            {
                StaticLogger.LogError($"Error while initializing '{PluginInfo.PLUGIN_NAME}': {e}");
            }

#if DEBUG
            // disable overhead of stack trace in dev build
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
#endif
        }
    }

    public static class Debug
    {
        private static ManualLogSource Logger => StaticLogger;

        public static void Log(string message) => Logger.LogInfo(message);
        public static void LogError(string message) => Logger.LogError(message);
        public static void LogWarning(string message) => Logger.LogWarning(message);
        public static void LogDebug(string message) => Logger.LogDebug(message);
        public static void LogMessage(string message) => Logger.LogMessage(message);
        public static void LogFatal(string message) => Logger.LogFatal(message);
    }

    public enum PatchType
    {
        Prefix,
        Postfix,
        Transpiler
    }

    public class DynamicHarmonyPatcher
    {
        public static void PatchAllTypes(Type baseType, string methodToPatch, MethodInfo patchMethod,
                                         PatchType patchType, Harmony harmonyInstance, HashSet<Type>? blackList = null)
        {
            List<Type> derivedTypes = FindDerivedTypes(baseType, methodToPatch);

            // Filter out blacklisted types
            if (blackList != null)
            {
                _ = derivedTypes.RemoveAll(blackList.Contains);
            }

            PatchMethodsInTypes(derivedTypes, methodToPatch, patchMethod, patchType, harmonyInstance);
        }

        private static List<Type> FindDerivedTypes(Type baseType, string methodName)
        {
            List<Type> derivedTypes = [];
            Assembly[] assemblies;
            // Get all loaded assemblies in the current AppDomain
            if (LESettings.patchModdedEnemies.Value)
            {
                Debug.LogDebug("Searching for modded enemy types...");
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            else
            {
                Debug.LogDebug("Patching vanilla enemy types...");
                //Only load the main game assembly
                assemblies = [typeof(EnemyAI).Assembly];
            }

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (baseType.IsAssignableFrom(type) && type != baseType && !type.IsAbstract && !type.IsInterface)
                        {
                            MethodInfo originalMethod = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                            //Check if a method is an override of the base method and not the base method itself.
                            if (originalMethod != null && originalMethod.DeclaringType != baseType)
                            {
                                derivedTypes.Add(type);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Debug.LogDebug($"Error loading types from assembly: {assembly.FullName}");
                }
            }

            return derivedTypes;
        }

        private static void PatchMethodsInTypes(List<Type> typesToPatch, string methodName, MethodInfo patchMethod, PatchType patchType, Harmony harmonyInstance)
        {
            if (typesToPatch == null || typesToPatch.Count == 0)
            {
                Debug.LogWarning("No types to patch provided.");
                return;
            }

            if (string.IsNullOrEmpty(methodName))
            {
                Debug.LogError("Method name cannot be null or empty.");
                return;
            }

            if (patchMethod == null)
            {
                Debug.LogError("Patch method cannot be null.");
                return;
            }

            if (harmonyInstance == null)
            {
                Debug.LogError("Harmony instance cannot be null.");
                return;
            }

            foreach (Type type in typesToPatch)
            {
                try
                {
                    MethodInfo originalMethod = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (originalMethod == null)
                    {
                        Debug.LogWarning($"Method '{methodName}' not found on type '{type.FullName}'. Skipping.");
                        continue;
                    }

                    HarmonyMethod harmonyPatchMethod = new(patchMethod);

                    switch (patchType)
                    {
                        case PatchType.Prefix:
                            _ = harmonyInstance.Patch(originalMethod, prefix: harmonyPatchMethod);
                            break;
                        case PatchType.Postfix:
                            _ = harmonyInstance.Patch(originalMethod, postfix: harmonyPatchMethod);
                            break;
                        case PatchType.Transpiler:
                            _ = harmonyInstance.Patch(originalMethod, transpiler: harmonyPatchMethod);
                            break;
                        default:
                            Debug.LogError($"Invalid patch type: '{patchType}'.");
                            break;
                    }
                    Debug.LogDebug($"Patched '{methodName}' in '{type.FullName}' using method {patchMethod.Name}");

                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error patching '{methodName}' in '{type.FullName}': {ex}");
                }
            }
        }
    }
}
