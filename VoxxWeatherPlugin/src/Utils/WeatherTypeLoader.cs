using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;
using VoxxWeatherPlugin.Weathers;
using VoxxWeatherPlugin.Behaviours;
using WeatherRegistry;

using static VoxxWeatherPlugin.VoxxWeatherPlugin;

namespace VoxxWeatherPlugin.Utils
{
    public class WeatherTypeLoader
    {
        internal static string bundleName = "voxxweather.assetbundle";
        internal static GameObject? weatherSynchronizerPrefab = null!;

        public static void RegisterHeatwaveWeather()
        {
            GameObject? heatwavePrefab = WeatherAssetLoader.LoadAsset<GameObject>(bundleName, "HeatwaveWeatherContainer");

            if (heatwavePrefab == null)
            {
                Debug.LogError("Failed to load Heatwave Weather assets. Weather registration failed.");
                return;
            }

            heatwavePrefab.SetActive(false);
            GameObject heatwaveContainer = Object.Instantiate(heatwavePrefab);
            heatwaveContainer.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(heatwaveContainer);

            HeatwaveWeather heatwaveWeatherController = heatwaveContainer.GetComponentInChildren<HeatwaveWeather>(true);
            GameObject effectPermanentObject = heatwaveWeatherController.gameObject;
            effectPermanentObject.SetActive(false);

            HeatwaveVFXManager heatwaveVFXManager = heatwaveContainer.GetComponentInChildren<HeatwaveVFXManager>(true);
            heatwaveVFXManager.heatwaveIntensityCurve.SetKeys([
                new(0.0f, 0.001f, 0.0f, 0.0f, 0.3333f, 0.3333f),
                new(0.3323f, 0.2222f, 0.8851f, 0.8851f, 0.3333f, 0.3333f),
                new(1.0f, 1.0f, 0.0f, 0.0f, 0.3333f, 0.3333f)]);
            //Possibly setup vfx configuration here
            GameObject effectObject = heatwaveVFXManager.gameObject;
            effectObject.SetActive(false);

            heatwaveWeatherController.VFXManager = heatwaveVFXManager;

            // Fix broken references (WHY, UNITY, WHY)

            VisualEffectAsset? heatwaveVFXAsset = WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "HeatwaveVFX");

            if (heatwaveVFXAsset == null)
            {
                Debug.LogError("Failed to load Heatwave Weather visual assets. Weather registration failed.");
                return;
            }

            VisualEffect heatwaveVFX = heatwaveVFXManager.heatwaveParticlePrefab!.GetComponent<VisualEffect>();
            heatwaveVFX.visualEffectAsset = heatwaveVFXAsset;

            // Configure VFX settings
            heatwaveVFX.SetFloat("particleSpawnRate", LESettings.HeatwaveParticlesSpawnRate.Value);
            heatwaveVFX.SetFloat("distortionScale", LESettings.HeathazeDistortionStrength.Value);

            heatwaveContainer.SetActive(true);

            Weather HeatwaveWeather = new("Heatwave", new(effectObject, effectPermanentObject) { SunAnimatorBool = "" })
            {
                Color = new Color32(255, 56, 0, 255), // Coquelicot - #FF3800
                Config =
                {
                    LevelFilters = new(["Assurance", "Offense", "Embrion", "Artifice", "Alcatras", "Atlantica", "Asteroid13", "Berunah", "Core",
                        "Crowd", "Demetrica", "Descent", "Dreck", "Empra", "Etern", "Facto", "FissionC", "Hyve", "Hyx", "Lecaro", "Thalasso", "Cabal", "Makron", "Mazon", "Praetor",
                        "Scald", "Arelion", "Affliction", "Desperation", "Solitude", "Symbiosis", "Thallasic", "Viscera", "Attenuation", "Retinue", "Acheron", "Terra", "Kiln", "Nostalgia",
                        "Row", "Court", "Dreg", "Confiscation", "Solidarity", "Brutality", "Burrow", "Collateral", "Humidity", "Hydro", "Integrity", "Vaporization", "Vertigo", "Rorm",
                        "Kronodile", "Malice", "EGypt", "EchoReach", "Orion", "RelayStation", "Aquatis", "Detritus", "Argent", "Sierra", "BlackMesa", "Pelagia", "Vacuity", "Vulcan9", "Ganimedes",
                        "$Beach", "$Canyon", "$Desert", "$Volcanic", "$Warm"]),
                    FilteringOption = new(true),
                    ScrapAmountMultiplier = new(0.85f),
                    ScrapValueMultiplier = new(1.15f),
                    LevelWeights = new(["Assurance@120", "Offense@75", "Embrion@50", "Artifice@100", "$Volcanic@150", "$Canyon@100", "$Desert@100",
                        "$Beach@30", "$Warm@70"]),
                    WeatherToWeatherWeights = new(["Cloudy@100", "Solar Flare@150", "DustClouds@75", "Windy@50", "Toxic Smog@25", "Eclipsed@10",
                        "Foggy@0", "Rainy@0", "Stormy@0", "Blizzard@0", "Snowfall@0", "Flooded@0"]),
                    DefaultWeight = new(50),
                },
            };

            heatwaveWeatherController.WeatherDefinition = HeatwaveWeather;
            WeatherManager.RegisterWeather(HeatwaveWeather);
            Debug.Log($"{PluginInfo.PLUGIN_GUID}: Heatwave weather registered!");
        }

        public static void RegisterFlareWeather()
        {
            GameObject? flareWeatherPrefab = WeatherAssetLoader.LoadAsset<GameObject>(bundleName, "SolarFlareWeatherContainer");

            if (flareWeatherPrefab == null)
            {
                Debug.LogError("Failed to load Solar Flare Weather assets. Weather registration failed.");
                return;
            }

            flareWeatherPrefab.SetActive(false);
            GameObject flareContainer = Object.Instantiate(flareWeatherPrefab);
            flareContainer.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(flareContainer);

            SolarFlareWeather flareWeatherController = flareContainer.GetComponentInChildren<SolarFlareWeather>(true);
            GameObject effectPermanentObject = flareWeatherController.gameObject;
            effectPermanentObject.SetActive(false);

            SolarFlareVFXManager flareVFXManager = flareContainer.GetComponentInChildren<SolarFlareVFXManager>(true);
            GameObject effectObject = flareVFXManager.gameObject;
            effectObject.SetActive(false);

            flareWeatherController.VFXManager = flareVFXManager;

            // Fix broken references (WHY, UNITY, WHY)

            VisualEffectAsset? flareVFXAsset = WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "CoronaVFX");
            VisualEffectAsset? auroraVFXAsset = WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "AuroraVFX");
            if (flareVFXAsset == null || auroraVFXAsset == null)
            {
                Debug.LogError("Failed to load Solar Flare Weather visual assets. Weather registration failed.");
                return;
            }

            flareVFXManager.flareObject!.GetComponent<VisualEffect>().visualEffectAsset = flareVFXAsset;

            VisualEffect auroraVFX = flareVFXManager.auroraObject!.GetComponent<VisualEffect>();
            auroraVFX.visualEffectAsset = auroraVFXAsset;

            // Configure VFX settings

            auroraVFX.SetUInt("spawnHeight", LESettings.AuroraHeight.Value);
            auroraVFX.SetFloat("spawnBoxSize", LESettings.AuroraSpawnAreaBox.Value);
            auroraVFX.SetFloat("auroraSize", LESettings.AuroraSize.Value);
            auroraVFX.SetFloat("particleSpawnRate", LESettings.AuroraSpawnRate.Value);

            flareContainer.SetActive(true);

            Weather FlareWeather = new("Solar Flare", new(effectObject, effectPermanentObject) { SunAnimatorBool = "" })
            {
                Color = Color.yellow,
                Config =
                {
                    LevelFilters = new(["Gordion", "Galetry"]),
                    FilteringOption = new(false),
                    ScrapAmountMultiplier = new(1.25f),
                    ScrapValueMultiplier = new(0.95f),
                    LevelWeights = new(["Arcadia@200", "Embrion@200", "Summit@300", "Incalescence@125", "Sierra@350",
                        "$Canyon@100", "$Wasteland@100", "$Tundra@90"]),
                    WeatherToWeatherWeights = new(["Solar Flare@25", "Blackout@100", "Heatwave@100", "Eclipsed@10"]),
                    DefaultWeight = new(60),
                },
            };

            flareWeatherController.WeatherDefinition = FlareWeather;
            WeatherManager.RegisterWeather(FlareWeather);
            Debug.Log($"{PluginInfo.PLUGIN_GUID}: Solar flare weather registered!");
        }

        public static void RegisterBlizzardWeather()
        {
            GameObject? blizzardPrefab = WeatherAssetLoader.LoadAsset<GameObject>(bundleName, "BlizzardWeatherContainer");

            if (blizzardPrefab == null)
            {
                Debug.LogError("Failed to load Blizzard Weather assets. Weather registration failed.");
                return;
            }

            blizzardPrefab.SetActive(false);
            GameObject blizzardContainer = Object.Instantiate(blizzardPrefab);
            blizzardContainer.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(blizzardContainer);

            BlizzardWeather blizzardWeatherController = blizzardContainer.GetComponentInChildren<BlizzardWeather>(true);
            BlizzardWeather.Instance = blizzardWeatherController;
            GameObject effectPermanentObject = blizzardWeatherController.gameObject;
            effectPermanentObject.SetActive(false);

            BlizzardVFXManager blizzardVFXManager = blizzardContainer.GetComponentInChildren<BlizzardVFXManager>(true);
            //Possibly setup vfx configuration here
            GameObject effectObject = blizzardVFXManager.gameObject;
            effectObject.SetActive(false);

            // Add dynamic sky override to fix snow lighting issues.
            ModifyFrostyFilter(effectObject);

            blizzardWeatherController.VFXManager = blizzardVFXManager;

            // Fix broken references (WHY, UNITY, WHY)

            VisualEffectAsset? blizzardVFXAsset = LESettings.snowVfxLighting.Value ?
                WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "BlizzardVFXLit") :
                WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "BlizzardVFX");
            VisualEffectAsset? blizzardWaveVFXAsset = LESettings.blizzardWaveVfxLighting.Value ?
                WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "BlizzardWaveVFXLit") :
                WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "BlizzardWaveVFX");
            Shader? blizzardFogShader = WeatherAssetLoader.LoadAsset<Shader>(bundleName, "BlizzardFogVolumetricCollision");

            if (blizzardVFXAsset == null || blizzardWaveVFXAsset == null || blizzardFogShader == null)
            {
                Debug.LogError("Failed to load Blizzard Weather visual assets. Weather registration failed.");
                return;
            }

            VisualEffect blizzardVFX = blizzardVFXManager.snowVFXContainer!.GetComponent<VisualEffect>();
            blizzardVFX.visualEffectAsset = blizzardVFXAsset;
            blizzardVFX.SetFloat("spawnRateMultiplier", LESettings.snowParticlesMultiplier.Value);
            blizzardVFX.SetBool("isCollisionEnabled", LESettings.enableVFXCollisions.Value);
            blizzardVFX.SetBool("fogEnabled", LESettings.useParticleBlizzardFog.Value);
            Camera blizzardCamera = blizzardVFX.GetComponentInChildren<Camera>(true);
            blizzardCamera.enabled = LESettings.enableVFXCollisions.Value;
            LocalVolumetricFog blizzardFog = blizzardVFXManager.snowVFXContainer!.GetComponentInChildren<LocalVolumetricFog>(true);
            Material blizzardFogMaterial = blizzardFog.parameters.materialMask;
            blizzardFogMaterial.shader = blizzardFogShader;
            VisualEffect chillWaveVFX = blizzardVFXManager.blizzardWaveContainer!.GetComponentInChildren<VisualEffect>(true);
            chillWaveVFX.visualEffectAsset = blizzardWaveVFXAsset;
            chillWaveVFX.SetFloat("spawnRateMultiplier", LESettings.blizzardWaveParticlesMultiplier.Value);
            chillWaveVFX.SetBool("isCollisionEnabled", LESettings.enableVFXCollisions.Value);
            chillWaveVFX.SetBool("fogEnabled", LESettings.useParticleBlizzardFog.Value);
            Camera chillWaveCamera = blizzardVFXManager.blizzardWaveContainer!.GetComponentInChildren<Camera>(true);
            chillWaveCamera.enabled = LESettings.enableVFXCollisions.Value;
            blizzardFog = blizzardVFXManager.blizzardWaveContainer!.GetComponentInChildren<LocalVolumetricFog>(true);
            blizzardFogMaterial = blizzardFog.parameters.materialMask;
            blizzardFogMaterial.shader = blizzardFogShader;
            AudioSource blizzardAudio = blizzardVFXManager.GetComponent<AudioSource>();
            blizzardAudio.volume = LESettings.blizzardAmbientVolume.Value;
            AudioSource waveAudio = blizzardVFXManager.blizzardWaveContainer.GetComponentInChildren<AudioSource>(true);
            waveAudio.volume = LESettings.blizzardWaveVolume.Value;

            blizzardContainer.SetActive(true);

            ImprovedWeatherEffect blizzardWeatherEffect = new(effectObject, effectPermanentObject)
            {
                SunAnimatorBool = "overcast",
            };

            Weather BlizzardWeatherType = new("Blizzard", blizzardWeatherEffect)
            {
                Color = new Color32(0, 112, 255, 255), // Brandeis Blue - #0070FF
                Config =
                {
                    LevelFilters = new(["Gordion", "Galetry", "Assurance", "Embrion", "Acidir", "Alcatras", "Atlantica", "Asteroid13",
                        "Berunah", "Calist", "Core", "Cosmocos", "Demetrica", "Desolation", "Dreck", "Empra", "Etern", "Extort", "Facto", "FissionC", "Gloom", "Hyve", "Hyx",
                        "Junic", "Lecaro", "Narcotic", "Oldred", "Release", "Repress", "Roart", "Thalasso", "Baykal", "Cabal", "CTFFace", "Lithium", "Makron", "Mazon", "Praetor",
                        "Scald", "TheIris", "Trepidation", "Arelion", "USCVortex", "Elasticity", "Affliction", "Sector0", "Desperation", "Solitude", "Symbiosis", "Thallasic",
                        "Viscera", "Attenuation", "Retinue", "Acheron", "Bilge", "Terra", "Obscura", "Kiln", "Lua", "Nostalgia", "Row", "Taldor", "Court", "Dreg", "Confiscation",
                        "Solidarity", "Brutality", "Burrow", "Collateral", "Corrosion", "Humidity", "Integrity", "Landslide", "Submersion", "Vaporization", "Vertigo", "Phuket",
                        "Rorm", "Starship13", "Kronodile", "Malice", "EGypt", "EchoReach", "Orion", "RelayStation", "AtlasAbyss", "Aquatis", "Detritus", "CaltPrime", "Argent",
                        "Sierra", "BlackMesa", "Pelagia", "Zeranos", "Vacuity", "Vulcan9", "Ganimedes",
                        "$Desert", "$Beach", "$Volcanic", "$Warm"]),
                    FilteringOption = new(false),
                    ScrapAmountMultiplier = new(0.9f),
                    ScrapValueMultiplier = new(1.15f),
                    LevelWeights = new(["Artifice@200", "Polarus@200", "$Tundra@120", "$Snow@120", "$Cold@75"]),
                    WeatherToWeatherWeights = new(["Snowfall@150", "Cloudy@150", "Foggy@90", "Stormy@80", "Rainy@25",
                        "Blizzard@75", "Windy@75", "Flooded@30", "Heatwave@0", "DustClouds@0"]),
                    DefaultWeight = new(50),
                },
            };

            blizzardWeatherController.WeatherDefinition = BlizzardWeatherType;
            WeatherManager.RegisterWeather(BlizzardWeatherType);
            Debug.Log($"{PluginInfo.PLUGIN_GUID}: Blizzard weather registered!");
        }

        public static void RegisterSnowfallWeather()
        {
            GameObject? snowfallPrefab = WeatherAssetLoader.LoadAsset<GameObject>(bundleName, "SnowWeatherContainer");
            if (snowfallPrefab == null)
            {
                Debug.LogError("Failed to load Snowfall Weather assets. Weather registration failed.");
                return;
            }
            snowfallPrefab.SetActive(false);
            GameObject snowfallContainer = Object.Instantiate(snowfallPrefab);
            snowfallContainer.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(snowfallContainer);

            SnowfallWeather snowfallWeatherController = snowfallContainer.GetComponentInChildren<SnowfallWeather>(true);
            SnowfallWeather.Instance = snowfallWeatherController;
            GameObject effectPermanentObject = snowfallWeatherController.gameObject;
            effectPermanentObject.SetActive(false);

            SnowfallVFXManager snowfallVFXManager = snowfallContainer.GetComponentInChildren<SnowfallVFXManager>(true);

            GameObject effectObject = snowfallVFXManager.gameObject;
            effectObject.SetActive(false);

            // Add dynamic sky override to fix snow lighting issues.
            ModifyFrostyFilter(effectObject);

            snowfallWeatherController.VFXManager = snowfallVFXManager;

            VisualEffectAsset? snowVFXAsset = LESettings.snowVfxLighting.Value ?
                WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "SnowVFXLit") :
                WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "SnowVFX");

            if (snowVFXAsset == null)
            {
                Debug.LogError("Failed to load Snowfall Weather visual assets. Weather registration failed.");
                return;
            }

            VisualEffect snowVFX = snowfallVFXManager.snowVFXContainer!.GetComponent<VisualEffect>();
            snowVFX.visualEffectAsset = snowVFXAsset;
            snowVFX.SetFloat("spawnRateMultiplier", LESettings.snowParticlesMultiplier.Value);

            snowfallContainer.SetActive(true);

            ImprovedWeatherEffect snowyWeatherEffect = new(effectObject, effectPermanentObject)
            {
                SunAnimatorBool = "overcast",
            };

            Weather SnowfallWeatherEffect = new("Snowfall", snowyWeatherEffect)
            {
                Color = new Color32(224, 255, 255, 255), // LightCyan1 - #E0FFFF
                Config =
                {
                    LevelFilters = new(["Gordion", "Galetry", "Assurance", "Embrion", "Acidir", "Alcatras", "Atlantica", "Asteroid13",
                        "Berunah", "Calist", "Core", "Cosmocos", "Demetrica", "Desolation", "Dreck", "Empra", "Etern", "Extort", "Facto", "FissionC", "Gloom", "Hyve", "Hyx",
                        "Junic", "Lecaro", "Narcotic", "Oldred", "Release", "Repress", "Roart", "Thalasso", "Baykal", "Cabal", "CTFFace", "Lithium", "Makron", "Mazon", "Praetor",
                        "Scald", "TheIris", "Trepidation", "Arelion", "USCVortex", "Elasticity", "Affliction", "Sector0", "Desperation", "Solitude", "Symbiosis", "Thallasic",
                        "Viscera", "Attenuation", "Retinue", "Acheron", "Bilge", "Terra", "Obscura", "Kiln", "Lua", "Nostalgia", "Row", "Taldor", "Court", "Dreg", "Confiscation",
                        "Solidarity", "Brutality", "Burrow", "Collateral", "Corrosion", "Humidity", "Integrity", "Landslide", "Submersion", "Vaporization", "Vertigo", "Phuket",
                        "Rorm", "Starship13", "Kronodile", "Malice", "EGypt", "EchoReach", "Orion", "RelayStation", "AtlasAbyss", "Aquatis", "Detritus", "CaltPrime", "Argent",
                        "Sierra", "BlackMesa", "Pelagia", "Zeranos", "Vacuity", "Vulcan9", "Ganimedes",
                        "$Desert", "$Beach", "$Volcanic", "$Warm"]),
                    FilteringOption = new(false),
                    ScrapAmountMultiplier = new(0.85f),
                    ScrapValueMultiplier = new(1.2f),
                    LevelWeights = new(["Artifice@300", "Polarus@300", "Vow@100", "Rockwell@200",
                        "$Tundra@150", "$Snow@200", "$Cold@100"]),
                    WeatherToWeatherWeights = new(["Snowfall@75", "Cloudy@150", "Foggy@100", "Stormy@80", "Rainy@25",
                        "Blizzard@100", "Windy@75", "Heatwave@0", "DustClouds@0", "ToxicSmog@0"]),
                    DefaultWeight = new(60),
                },
            };

            snowfallWeatherController.WeatherDefinition = SnowfallWeatherEffect;
            WeatherManager.RegisterWeather(SnowfallWeatherEffect);
            Debug.Log($"{PluginInfo.PLUGIN_GUID}: Snowfall weather registered!");
        }

        private static void ModifyFrostyFilter(GameObject vfxManager)
        {
            Transform? frostyFilter = vfxManager.transform.Find("FrostyFilter");
            if (frostyFilter == null || !frostyFilter.TryGetComponent(out Volume frostyVolume) || frostyVolume.sharedProfile == null)
            {
                Debug.LogWarning("Could not find frosty volume filter, snow will look oddly dark.");
                return;
            }

            frostyVolume.priority = 10; // Upped from a value of 2, so it's not overridden by Stormy volumes (typically a priority of 3).
            if (!frostyVolume.sharedProfile.Has<VisualEnvironment>())
            {
                // I know not why, I know not how; light returneth to snow beneath a sky of the dynamic kind.
                VisualEnvironment dynamicSky = frostyVolume.sharedProfile.Add<VisualEnvironment>();
                dynamicSky.skyAmbientMode = new(SkyAmbientMode.Dynamic, overrideState: true);
                // dynamicSky.displayName = "Visual Environment"; // (They forgor to put this in the VisualEnvironment constructor...)
                dynamicSky.name = "Visual Environment";

                if (!frostyVolume.HasInstantiatedProfile())
                {
                    // Magical getter call that actually applies the profile override...
                    _ = frostyVolume.profile;
                }
            }
        }

        public static bool LoadLevelManipulator()
        {
            GameObject? levelManipulatorPrefab = WeatherAssetLoader.LoadAsset<GameObject>(bundleName, "LevelManipulatorContainer");
            if (levelManipulatorPrefab == null)
            {
                Debug.LogError("Failed to load Level Manipulator assets. Disabling weather effects.");
                return false;
            }

            levelManipulatorPrefab.SetActive(true);
            GameObject levelManipulator = Object.Instantiate(levelManipulatorPrefab);
            Object.DontDestroyOnLoad(levelManipulator);
            levelManipulator.hideFlags = HideFlags.HideAndDontSave;

            LevelManipulator levelManipulatorController = levelManipulator.GetComponent<LevelManipulator>();

            //Create a dictionary of the snowfall VFX variants                                
            string[] keys = ["footprintsTrackerVFX", "lowcapFootprintsTrackerVFX", "itemTrackerVFX", "shovelVFX"];
            LevelManipulator.snowTrackersDict = keys.Zip(levelManipulatorController.footprintsTrackerVFX,
                                                            (k, v) => new { k, v })
                                                            .ToDictionary(x => x.k, x => x.v);

            // Fix broken references (WHY, UNITY, WHY)

            Shader? overlayShader = WeatherAssetLoader.LoadAsset<Shader>(bundleName, "SnowLitPass");
            Shader? vertexSnowShader = WeatherAssetLoader.LoadAsset<Shader>(bundleName, "SnowLitVertBakedPass");
            Shader? opaqueVertexSnowShader = WeatherAssetLoader.LoadAsset<Shader>(bundleName, "SnowLitVertBakedOpaquePass");

            if (overlayShader == null || vertexSnowShader == null || opaqueVertexSnowShader == null)
            {
                Debug.LogError("Failed to restore Snow visual assets. Visual effects may not work correctly.");
                return false;
            }

            levelManipulatorController.snowOverlayMaterial!.shader = overlayShader;
            levelManipulatorController.snowVertexMaterial!.shader = vertexSnowShader;
            levelManipulatorController.snowVertexOpaqueMaterial!.shader = opaqueVertexSnowShader;

            levelManipulatorController.snowVertexMaterial.SetFloat(SnowfallShaderIDs.IsDepthFade, LESettings.softSnowEdges.Value ? 1f : 0f);
            levelManipulatorController.snowVertexMaterial.SetFloat(SnowfallShaderIDs.TessellationFadeDistance, LESettings.tesselationFadeDistance.Value);
            levelManipulatorController.snowVertexMaterial.SetFloat(SnowfallShaderIDs.TessellationMaxDistance, LESettings.tesselationMaxDistance.Value);
            levelManipulatorController.snowVertexMaterial.SetFloat(SnowfallShaderIDs.TessellationFadeDistance, LESettings.tesselationFadeDistance.Value);
            levelManipulatorController.snowVertexMaterial.SetFloat(SnowfallShaderIDs.TessellationMaxDistance, LESettings.tesselationMaxDistance.Value);

            return true;
        }

        public static bool LoadWeatherSynchronizer()
        {
            weatherSynchronizerPrefab = WeatherAssetLoader.LoadAsset<GameObject>(bundleName, "WeatherSynchronizerContainer");
            if (weatherSynchronizerPrefab == null)
            {
                Debug.LogError("Failed to load Weather Synchronizer. Brace yourself for desyncs!");
                return false;
            }

            NetworkManager.Singleton.AddNetworkPrefab(weatherSynchronizerPrefab);

            return true;
        }

        public static void RegisterToxicSmogWeather()
        {
            GameObject? toxicSmogPrefab = WeatherAssetLoader.LoadAsset<GameObject>(bundleName, "ToxicSmogWeatherContainer");
            if (toxicSmogPrefab == null)
            {
                Debug.LogError("Failed to load Toxic Fog Weather assets. Weather registration failed.");
                return;
            }
            toxicSmogPrefab.SetActive(false);
            GameObject toxicSmogContainer = Object.Instantiate(toxicSmogPrefab);
            toxicSmogContainer.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(toxicSmogContainer);

            ToxicSmogWeather toxicSmogWeatherController = toxicSmogContainer.GetComponentInChildren<ToxicSmogWeather>(true);
            GameObject effectPermanentObject = toxicSmogWeatherController.gameObject;
            effectPermanentObject.SetActive(false);

            ToxicSmogVFXManager toxicSmogVFXManager = toxicSmogContainer.GetComponentInChildren<ToxicSmogVFXManager>(true);
            //Possibly setup vfx configuration here
            GameObject effectObject = toxicSmogVFXManager.gameObject;
            effectObject.SetActive(false);

            toxicSmogWeatherController.VFXManager = toxicSmogVFXManager;

            // Fix broken references (WHY, UNITY, WHY)
            VisualEffectAsset? toxicFumesVFXAsset = WeatherAssetLoader.LoadAsset<VisualEffectAsset>(bundleName, "ToxicFumesVFX");

            if (toxicFumesVFXAsset == null)
            {
                Debug.LogError("Failed to load Toxic Fog Weather visual assets. Weather registration failed.");
                return;
            }

            VisualEffect? toxicFumesVFX = toxicSmogVFXManager.hazardPrefab != null ? toxicSmogVFXManager.hazardPrefab.GetComponent<VisualEffect>() : null;
            toxicFumesVFX!.visualEffectAsset = toxicFumesVFXAsset;

            toxicSmogContainer.SetActive(true);

            Weather ToxicSmogWeatherEffect = new("Toxic Smog", new(effectObject, effectPermanentObject) { SunAnimatorBool = "" })
            {
                Color = new(0.413f, 0.589f, 0.210f), // dark lime green
                Config =
                {
                    LevelFilters = new(["Gordion", "Derelict", "Galetry", "Elasticity"]),
                    FilteringOption = new(false),
                    ScrapAmountMultiplier = new(1.3f),
                    ScrapValueMultiplier = new(0.8f),
                    LevelWeights = new(["FissionC@300", "Makron@300", "Asteroid13@150", "Collateral@150", "Quasara@200",
                        "$Atomic@200", "$Toxic@200", "$Ocean@30"]),
                    WeatherToWeatherWeights = new(["Cloudy@150", "Foggy@120", "Toxic Smog@60", "Heatwave@100", "DustClouds@40",
                        "Rainy@25", "Windy@0", "Stormy@0", "Blizzard@0"]),
                    DefaultWeight = new(80),
                },
            };

            toxicSmogWeatherController.WeatherDefinition = ToxicSmogWeatherEffect;
            WeatherManager.RegisterWeather(ToxicSmogWeatherEffect);
            Debug.Log($"{PluginInfo.PLUGIN_GUID}: Toxic Smog weather registered!");

        }
    }
}