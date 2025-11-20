using System.Collections.Generic;
using UnityEngine;

namespace VoxxWeatherPlugin.Utils
{
    public static class Yielders
    {
        private static readonly Dictionary<float, WaitForSeconds> cachedYielders = [];

        public static WaitForEndOfFrame WaitForEndOfFrame { get; } = new();

        public static WaitForSeconds WaitForSeconds(float seconds)
        {
            if (!cachedYielders.TryGetValue(seconds, out WaitForSeconds yielder))
            {
                yielder = new(seconds);
                cachedYielders.Add(seconds, yielder);
            }

            return yielder;
        }
    }
}