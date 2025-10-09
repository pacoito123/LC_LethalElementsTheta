using Unity.Netcode;
using VoxxWeatherPlugin.Weathers;

namespace VoxxWeatherPlugin.Behaviours
{
    public class WeatherEventSynchronizer : NetworkBehaviour
    {
        public static WeatherEventSynchronizer Instance { get; private set; } = null!;

        internal void Awake()
        {
            Instance = this;
        }

        internal void StartMalfunction(ElectricMalfunctionData malfunctionData)
        {
            if (IsServer)
            {
                if (malfunctionData.malfunctionObject is EnemyAINestSpawnObject radMechNest)
                {
                    ResolveMalfunctionClientRpc(RoundManager.Instance.enemyNestSpawnObjects.IndexOf(radMechNest));
                }
                else if (malfunctionData.malfunctionObject is NetworkBehaviour malfunctionObject)
                {
                    NetworkBehaviourReference malfunctionDataRef = new(malfunctionObject);
                    ResolveMalfunctionClientRpc(malfunctionDataRef);
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        internal void ResolveMalfunctionClientRpc(NetworkBehaviourReference malfunctionObjectRef)
        {
            if (malfunctionObjectRef.TryGet(out NetworkBehaviour malfunctionObject))
            {
                if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.electricMalfunctionData != null
                    && SolarFlareWeather.Instance.electricMalfunctionData.TryGetValue(malfunctionObject, out ElectricMalfunctionData malfunctionData))
                {
                    StartCoroutine(SolarFlareWeather.Instance.ElectricMalfunctionCoroutine(malfunctionData));
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        internal void ResolveMalfunctionClientRpc(int radMechNestIndex)
        {
            EnemyAINestSpawnObject radMechNest = RoundManager.Instance.enemyNestSpawnObjects[radMechNestIndex];
            if (SolarFlareWeather.Instance != null && SolarFlareWeather.Instance.electricMalfunctionData != null
                && SolarFlareWeather.Instance.electricMalfunctionData.TryGetValue(radMechNest, out ElectricMalfunctionData malfunctionData))
            {
                StartCoroutine(SolarFlareWeather.Instance.ElectricMalfunctionCoroutine(malfunctionData));
            }
        }

    }
}