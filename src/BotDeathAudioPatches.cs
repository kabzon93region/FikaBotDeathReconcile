using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using HarmonyLib;

namespace FikaBotDeathReconcile
{
    [HarmonyPatch(typeof(ObservedPlayer), nameof(ObservedPlayer.OnDead))]
    internal static class ObservedPlayerOnDeadAudioPatch
    {
        private static void Postfix(ObservedPlayer __instance)
        {
            if (!ShouldSilence(__instance))
            {
                return;
            }

            var plugin = PluginCore.Instance;
            BotDeathAudioSilencer.SilenceDeadObservedBot(
                __instance,
                plugin?.ModLogger,
                plugin?.VerboseLogging.Value ?? false);
        }

        private static bool ShouldSilence(ObservedPlayer player)
        {
            return FikaBackendUtils.IsClient && player != null && player.IsObservedAI;
        }
    }

    [HarmonyPatch(typeof(ObservedPlayer), nameof(ObservedPlayer.OnPhraseTold))]
    internal static class ObservedPlayerOnPhraseToldBlockPatch
    {
        private static bool Prefix(ObservedPlayer __instance)
        {
            if (!FikaBackendUtils.IsClient || __instance == null || !__instance.IsObservedAI)
            {
                return true;
            }

            var health = __instance.HealthController;
            if (health != null && !health.IsAlive)
            {
                return false;
            }

            return true;
        }
    }
}
