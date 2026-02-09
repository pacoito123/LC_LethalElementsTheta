using UnityEngine;
using GameNetcodeStuff;
using VoxxWeatherPlugin.Utils;

namespace VoxxWeatherPlugin.Behaviours
{
    internal sealed class ToxicFumes : MonoBehaviour
    {
        private void OnTriggerStay(Collider other)
        {
            if (other.TryGetComponent(out PlayerControllerB player))
            {
                if (player == GameNetworkManager.Instance.localPlayerController)
                {
                    PlayerEffectsManager.isPoisoned = true;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PlayerControllerB player))
            {
                if (player == GameNetworkManager.Instance.localPlayerController)
                {
                    PlayerEffectsManager.isPoisoned = false;
                }
            }
        }
    }
}