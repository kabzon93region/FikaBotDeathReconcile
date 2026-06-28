using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathCorpseFix
    {
        private static readonly HashSet<int> CompletedKeys = new HashSet<int>();

        internal static void ResetRaid()
        {
            CompletedKeys.Clear();
            BotCorpseGraceTracker.ResetRaid();
            BotDeathBossCorpseFix.ResetRaid();
            BotDeathFastRecheck.ResetRaid();
        }

        internal static bool ShouldApplyCorpseFix(Player player)
        {
            if (player == null || player.IsYourPlayer || !player.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (player is ObservedPlayer observed && !observed.IsObservedAI)
            {
                return false;
            }

            return BotDeathPlayerHelper.IsAiPlayer(player);
        }

        internal static bool TryEnsureCorpse(Player player, string reason, ManualLogSource logger, bool verbose)
        {
            if (player == null || !ShouldApplyCorpseFix(player))
            {
                return false;
            }

            var health = player.HealthController;
            if (health == null || health.IsAlive)
            {
                return false;
            }

            if (BotDeathForceKill.GetCorpse(player) != null)
            {
                return BotDeathVisualFinalize.FixStuckDeathVisual(player, reason + "_existing", logger, verbose);
            }

            try
            {
                BotDeathVisualFinalize.PrepareForCorpse(player);
                var corpse = player.CreateCorpse();
                if (corpse == null)
                {
                    return false;
                }

                BotDeathForceKill.SetCorpse(player, corpse);
                corpse.IsZombieCorpse = player.UsedSimplifiedSkeleton;
                BotDeathVisualFinalize.FinalizeCorpse(player, corpse);

                var key = BotDeathPlayerHelper.GetReconcileId(player);
                CompletedKeys.Add(key);

                var nickname = player.Profile?.Nickname ?? "?";
                logger?.LogInfo(
                    $"[BOT_CORPSE_FIX] {reason} netId={key} profile={player.ProfileId} nick={nickname} action=Prepare+CreateCorpse+Finalize ragdoll={(corpse.Ragdoll != null)}");
                if (verbose)
                {
                    logger?.LogInfo($"[BOT_CORPSE_FIX] pos={player.Position}");
                }

                return true;
            }
            catch (System.Exception ex)
            {
                logger?.LogWarning($"[BOT_CORPSE_FIX] {reason} CreateCorpse failed nick={player.Profile?.Nickname}: {ex.Message}");
                return false;
            }
        }

        internal static int RunHostFix(PluginCore plugin)
        {
            if (!FikaBackendUtils.IsServer || !plugin.HostCorpseFixEnabled.Value)
            {
                return 0;
            }

            var fixedCount = 0;
            var candidates = BotDeathSnapshotCollector.CollectHostCorpseFixCandidates();
            for (int i = 0; i < candidates.Count; i++)
            {
                var bot = candidates[i];
                if (bot == null || !NeedsTimerFallback(bot))
                {
                    continue;
                }

                var key = bot.NetId;
                if (CompletedKeys.Contains(key))
                {
                    continue;
                }

                if (!BotCorpseGraceTracker.IsGraceElapsed(key, plugin.HostCorpseFixGraceSeconds.Value))
                {
                    continue;
                }

                if (TryTimerFallback(bot, "host_corpse_timer", plugin.ModLogger, plugin.VerboseLogging.Value))
                {
                    CompletedKeys.Add(key);
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        internal static int RunClientFix(PluginCore plugin)
        {
            if (!FikaBackendUtils.IsClient || !plugin.ClientCorpseFixEnabled.Value)
            {
                return 0;
            }

            var networkManager = Singleton<IFikaNetworkManager>.Instance;
            if (networkManager?.ObservedPlayers == null)
            {
                return 0;
            }

            var fixedCount = 0;
            var observedPlayers = networkManager.ObservedPlayers;
            for (int i = 0; i < observedPlayers.Count; i++)
            {
                var observed = observedPlayers[i];
                if (observed == null || !observed.IsObservedAI || !NeedsTimerFallback(observed))
                {
                    continue;
                }

                var key = observed.NetId;
                if (CompletedKeys.Contains(key))
                {
                    continue;
                }

                if (!BotCorpseGraceTracker.IsGraceElapsed(key, plugin.ClientCorpseFixGraceSeconds.Value))
                {
                    continue;
                }

                if (TryTimerFallback(observed, "client_corpse_timer", plugin.ModLogger, plugin.VerboseLogging.Value))
                {
                    CompletedKeys.Add(key);
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        /// <summary>
        /// П.3: таймер — если OnDead/Kill postfix не помогли.
        /// </summary>
        internal static bool NeedsTimerFallback(Player player)
        {
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                return false;
            }

            var health = player.HealthController;
            if (health == null || health.IsAlive)
            {
                return false;
            }

            if (BotDeathForceKill.GetCorpse(player) != null)
            {
                return false;
            }

            var botOwner = player.AIData?.BotOwner;
            if (botOwner != null && !botOwner.IsDead)
            {
                return false;
            }

            return true;
        }

        private static bool TryTimerFallback(Player player, string reason, ManualLogSource logger, bool verbose)
        {
            if (player == null || !NeedsTimerFallback(player))
            {
                return false;
            }

            if (TryEnsureCorpse(player, reason + "_surgical", logger, verbose))
            {
                return true;
            }

            player.OnDead(EDamageType.Undefined);
            var nickname = player.Profile?.Nickname ?? "?";
            logger?.LogInfo(
                $"[BOT_CORPSE_FIX] {reason} netId={BotDeathPlayerHelper.GetReconcileId(player)} profile={player.ProfileId} nick={nickname} action=OnDead(Undefined)");
            if (verbose)
            {
                logger?.LogInfo(
                    $"[BOT_CORPSE_FIX] pos={player.Position} botOwnerDead={player.AIData?.BotOwner?.IsDead}");
            }

            return true;
        }
    }
}
