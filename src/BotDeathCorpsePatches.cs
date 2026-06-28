using EFT;
using EFT.HealthSystem;
using Fika.Core.Main.Players;
using HarmonyLib;
using System.Reflection;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathCorpsePatches
    {
        private static readonly FieldInfo HealthPlayerField = AccessTools.Field(typeof(ActiveHealthController), "Player");

        internal static void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(PlayerOnDeadCorpsePostfix));
            harmony.PatchAll(typeof(FikaBotOnDeadCorpsePostfix));
            harmony.PatchAll(typeof(ActiveHealthControllerKillCorpsePostfix));
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDead))]
        internal static class PlayerOnDeadCorpsePostfix
        {
            private static void Postfix(Player __instance)
            {
                ApplyAfterDeath(__instance, "ondead_postfix");
            }
        }

        [HarmonyPatch(typeof(FikaBot), nameof(FikaBot.OnDead))]
        internal static class FikaBotOnDeadCorpsePostfix
        {
            private static void Postfix(FikaBot __instance)
            {
                ApplyAfterDeath(__instance, "fikabot_ondead_postfix");
            }
        }

        [HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.Kill))]
        internal static class ActiveHealthControllerKillCorpsePostfix
        {
            private static void Postfix(ActiveHealthController __instance)
            {
                var plugin = PluginCore.Instance;
                if (plugin == null || !plugin.Enabled.Value || !plugin.KillCorpseFixEnabled.Value)
                {
                    return;
                }

                if (HealthPlayerField == null)
                {
                    return;
                }

                var player = HealthPlayerField.GetValue(__instance) as Player;
                ApplyAfterDeath(player, "kill_postfix");
            }
        }

        private static void ApplyAfterDeath(Player player, string reason)
        {
            var plugin = PluginCore.Instance;
            if (plugin == null || !plugin.Enabled.Value || player == null)
            {
                return;
            }

            if (BotDeathPlayerHelper.IsBossOrFollower(player))
            {
                if (plugin.BossCorpseFixEnabled.Value)
                {
                    BotDeathBossCorpseFix.TryBossStuckFix(
                        player, reason, plugin.ModLogger, plugin.VerboseLogging.Value, plugin);
                    BotDeathVisualFinalize.FixStuckDeathVisual(
                        player, reason + "_immediate", plugin.ModLogger, plugin.VerboseLogging.Value);
                    BotDeathBossCorpseFix.ScheduleRecheck(player);
                }

                BotDeathFastRecheck.Schedule(player, 0f);
                return;
            }

            if (!plugin.OnDeadCorpseFixEnabled.Value || !BotDeathCorpseFix.ShouldApplyCorpseFix(player))
            {
                return;
            }

            BotDeathCorpseFix.TryEnsureCorpse(player, reason, plugin.ModLogger, plugin.VerboseLogging.Value);
            BotDeathVisualFinalize.FixStuckDeathVisual(
                player, reason + "_immediate", plugin.ModLogger, plugin.VerboseLogging.Value);
            BotDeathFastRecheck.Schedule(player, 0f);
        }
    }
}
