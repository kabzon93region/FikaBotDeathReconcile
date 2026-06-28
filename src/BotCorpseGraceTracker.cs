using System.Collections.Generic;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    internal static class BotCorpseGraceTracker
    {
        private static readonly Dictionary<int, float> FirstSeenByKey = new Dictionary<int, float>();

        internal static bool IsGraceElapsed(int key, float graceSeconds)
        {
            if (graceSeconds <= 0f)
            {
                return true;
            }

            var now = Time.unscaledTime;
            if (!FirstSeenByKey.TryGetValue(key, out var firstSeen))
            {
                FirstSeenByKey[key] = now;
                return false;
            }

            return now - firstSeen >= graceSeconds;
        }

        internal static void Clear(int key)
        {
            FirstSeenByKey.Remove(key);
        }

        internal static void ResetRaid()
        {
            FirstSeenByKey.Clear();
        }
    }
}
