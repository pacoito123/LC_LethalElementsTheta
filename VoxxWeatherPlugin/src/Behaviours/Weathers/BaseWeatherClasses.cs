using UnityEngine;
using VoxxWeatherPlugin.Behaviours;
using WeatherRegistry;

namespace VoxxWeatherPlugin.Weathers
{
    public abstract class BaseWeather : MonoBehaviour
    {
        //Define the weather type (from the WeatherRegistry)
        public Weather WeatherDefinition { get; internal set; } = null!;
        public bool IsActive => (gameObject.activeInHierarchy && enabled) ||
                                ((StartOfRound.Instance == null || !StartOfRound.Instance.inShipPhase) && // To prevent weather counted as activated in orbit
                                IsMatched);
        public bool IsMatched => WeatherDefinition == (LevelManipulator.Instance != null ? LevelManipulator.Instance.currentWeather : null);

        protected static System.Random SeededRandom => LevelManipulator.Instance.seededRandom ?? new();
        protected static Bounds LevelBounds => LevelManipulator.Instance != null ? LevelManipulator.Instance.levelBounds : default;
        // protected abstract BaseVFXManager VFXManager { get; }
    }

    public abstract class BaseVFXManager : MonoBehaviour
    {
        protected static System.Random SeededRandom => (LevelManipulator.Instance != null && LevelManipulator.Instance.seededRandom != null)
            ? LevelManipulator.Instance.seededRandom : new();
        protected static Bounds LevelBounds => (LevelManipulator.Instance != null) ? LevelManipulator.Instance.levelBounds : default;

        internal abstract void Reset();
        internal abstract void PopulateLevelWithVFX();
    }
}