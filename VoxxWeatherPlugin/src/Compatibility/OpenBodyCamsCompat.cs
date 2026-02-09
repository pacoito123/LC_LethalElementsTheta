using System.Runtime.CompilerServices;
using OpenBodyCams;
using OpenBodyCams.API;
using UnityEngine;
using VoxxWeatherPlugin.Behaviours;
using VoxxWeatherPlugin.Weathers;

namespace VoxxWeatherPlugin.Compatibility
{
    internal sealed class OpenBodyCamsCompat
    {
        /// <summary>
        ///     Whether OpenBodyCams is present in the BepInEx Chainloader or not.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Zaggy1024.OpenBodyCams");

                return (bool)_enabled;
            }
        }
        private static bool? _enabled;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void GlitchBodyCameras()
        {
            BodyCamComponent[] bodyCamComps = BodyCamComponent.GetAllBodyCams();

            foreach (BodyCamComponent bodyCamComp in bodyCamComps)
            {
                GlitchBodyCam(bodyCamComp);
            }

            //Subscribe to know about cameras instantiated after this point
            BodyCam.OnBodyCamInstantiated += GlitchBodyCam;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void GlitchBodyCam(MonoBehaviour bodyCamBehaviour)
        {
            BodyCamComponent? bodyCamComp = bodyCamBehaviour as BodyCamComponent;
            GlitchBodyCam(bodyCamComp);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void GlitchBodyCam(BodyCamComponent? bodyCamComp, bool subscribe = true)
        {
            if (bodyCamComp == null)
                return;

            Camera? bodyCam = bodyCamComp.GetCamera();
            GlitchEffect? glitchEffect = (SolarFlareWeather.Instance != null) ? SolarFlareWeather.Instance.GlitchCamera(bodyCam) : null;
            RefreshGlitchEffect(bodyCamComp, glitchEffect);
            if (subscribe)
            {
                bodyCamComp.OnTargetChanged += OnCameraStatusChanged;
                bodyCamComp.OnCameraCreated += _ => OnCameraStatusChanged(bodyCamComp);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void OnCameraStatusChanged(MonoBehaviour bodyCamBehaviour)
        {
            if (SolarFlareWeather.Instance == null || !SolarFlareWeather.Instance.IsActive)
                return;

            BodyCamComponent? bodyCamComp = bodyCamBehaviour as BodyCamComponent;
            Camera? bodyCam = (bodyCamComp != null) ? bodyCamComp.GetCamera() : null;
            if (bodyCam == null)
                return;

            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.glitchPasses.TryGetValue(bodyCam, out GlitchEffect? glitchEffect))
            {
                RefreshGlitchEffect(bodyCamComp, glitchEffect);
            }
            else
            {
                GlitchBodyCam(bodyCamComp, false);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void RefreshGlitchEffect(BodyCamComponent? bodyCamComp, GlitchEffect? glitchEffect)
        {
            if (bodyCamComp == null)
                return;

            if (glitchEffect == null)
            {
                Debug.LogWarning("GlitchEffect is null for bodyCam: " + bodyCamComp.name);
                return;
            }

            glitchEffect.enabled = bodyCamComp.IsRemoteCamera && SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.flareData != null;
            glitchEffect.intensity.value = (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.flareData != null)
                ? SolarFlareWeather.Instance.flareData.ScreenDistortionIntensity : 0f;
        }
    }
}