using System.Collections.Generic;
using BepInEx.Logging;
using EFT;
using EFT.HealthSystem;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    /// <summary>
    /// Боссы/фолловеры: отдельные эвристики (IsAlive/BotOwner/Corpse mismatch).
    /// Обычные боты не трогаем — у них другой death flow.
    /// </summary>
    internal static class BotDeathBossCorpseFix
    {
        private struct ScheduledRecheck
        {
            internal Player Player;
            internal float UntilTime;
            internal int Attempts;
        }

        private const float RecheckIntervalSeconds = 0.5f;
        private const int MaxRecheckAttempts = 8;
        private const float BossKillCooldownSeconds = 8f;

        private static readonly List<ScheduledRecheck> Scheduled = new List<ScheduledRecheck>();
        private static readonly Dictionary<string, float> LastKillByProfileId = new Dictionary<string, float>();
        private static readonly HashSet<string> LoggedStuckProfileIds = new HashSet<string>();

        internal static void ResetRaid()
        {
            Scheduled.Clear();
            LastKillByProfileId.Clear();
            LoggedStuckProfileIds.Clear();
        }

        internal static void ScheduleRecheck(Player player, float durationSeconds = 1.5f)
        {
            if (player == null || !BotDeathPlayerHelper.IsBossOrFollower(player))
            {
                return;
            }

            BotDeathFastRecheck.Schedule(player, 0f);

            Scheduled.Add(new ScheduledRecheck
            {
                Player = player,
                UntilTime = Time.unscaledTime + durationSeconds,
                Attempts = 0
            });
        }

        internal static void ProcessScheduledRechecks(PluginCore plugin)
        {
            if (plugin == null || !plugin.Enabled.Value || !plugin.BossCorpseFixEnabled.Value || Scheduled.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            for (int i = Scheduled.Count - 1; i >= 0; i--)
            {
                var entry = Scheduled[i];
                var player = entry.Player;
                if (player == null || !player.gameObject.activeInHierarchy)
                {
                    Scheduled.RemoveAt(i);
                    continue;
                }

                if (now > entry.UntilTime)
                {
                    Scheduled.RemoveAt(i);
                    continue;
                }

                if (BotDeathForceKill.GetCorpse(player) != null)
                {
                    if (BotDeathVisualFinalize.FixStuckDeathVisual(
                            player, "boss_recheck", plugin.ModLogger, plugin.VerboseLogging.Value))
                    {
                        // keep rechecking until ragdoll or timeout
                    }

                    if (corpseVisualComplete(player))
                    {
                        Scheduled.RemoveAt(i);
                        continue;
                    }
                }

                entry.Attempts++;
                if (entry.Attempts > MaxRecheckAttempts)
                {
                    Scheduled.RemoveAt(i);
                    continue;
                }

                Scheduled[i] = entry;
                TryBossStuckFix(player, "boss_recheck", plugin.ModLogger, plugin.VerboseLogging.Value, plugin);
            }
        }

        internal static int RunHostBossScan(PluginCore plugin)
        {
            if (!plugin.BossCorpseFixEnabled.Value || !FikaBackendUtils.IsServer)
            {
                return 0;
            }

            var fixedCount = 0;
            var bosses = BotDeathSnapshotCollector.CollectHostBossPlayers();
            for (int i = 0; i < bosses.Count; i++)
            {
                if (TryBossStuckFix(bosses[i], "boss_host_scan", plugin.ModLogger, plugin.VerboseLogging.Value, plugin))
                {
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        internal static int RunClientBossScan(PluginCore plugin)
        {
            if (!plugin.BossCorpseFixEnabled.Value || !FikaBackendUtils.IsClient)
            {
                return 0;
            }

            var fixedCount = 0;
            var bosses = BotDeathSnapshotCollector.CollectClientBossObservedPlayers();
            for (int i = 0; i < bosses.Count; i++)
            {
                if (TryBossStuckFix(bosses[i], "boss_client_scan", plugin.ModLogger, plugin.VerboseLogging.Value, plugin))
                {
                    fixedCount++;
                }
            }

            return fixedCount;
        }

        internal static bool TryBossStuckFix(
            Player player,
            string reason,
            ManualLogSource logger,
            bool verbose,
            PluginCore plugin)
        {
            if (player == null || !plugin.BossCorpseFixEnabled.Value || !BotDeathPlayerHelper.IsBossOrFollower(player))
            {
                return false;
            }

            if (player.IsYourPlayer)
            {
                return false;
            }

            var corpse = BotDeathForceKill.GetCorpse(player);
            if (corpse != null)
            {
                if (BotDeathVisualFinalize.FixStuckDeathVisual(player, reason + "_visual", logger, verbose))
                {
                    return true;
                }

                return false;
            }

            var health = player.HealthController;
            if (health == null)
            {
                return false;
            }

            var botOwner = player.AIData?.BotOwner;
            var isAlive = health.IsAlive;
            var botOwnerDead = botOwner != null && botOwner.IsDead;
            LogBossStuckOnce(player, reason, isAlive, botOwnerDead, logger, verbose);

            if (isAlive && (botOwnerDead || HasDestroyedVitals(health)))
            {
                return TryBossKill(player, reason + "_kill", logger, verbose);
            }

            if (!isAlive)
            {
                if (BotDeathCorpseFix.TryEnsureCorpse(player, reason + "_surgical", logger, verbose))
                {
                    return true;
                }

                player.OnDead(EDamageType.Undefined);
                logger?.LogInfo(
                    $"[BOSS_CORPSE_FIX] {reason} nick={player.Profile?.Nickname} action=OnDead(Undefined) alive=false botOwnerDead={botOwnerDead}");
                ScheduleRecheck(player, 1f);
                return true;
            }

            return false;
        }

        private static bool corpseVisualComplete(Player player)
        {
            var corpse = BotDeathForceKill.GetCorpse(player);
            if (corpse == null)
            {
                return false;
            }

            if (corpse.Ragdoll != null)
            {
                return true;
            }

            return !BotDeathVisualFinalize.NeedsVisualFinalize(player);
        }

        private static bool TryBossKill(Player player, string reason, ManualLogSource logger, bool verbose)
        {
            var profileId = player.ProfileId;
            if (!string.IsNullOrEmpty(profileId) &&
                LastKillByProfileId.TryGetValue(profileId, out var lastKill) &&
                Time.unscaledTime - lastKill < BossKillCooldownSeconds)
            {
                return false;
            }

            var active = player.ActiveHealthController as ActiveHealthController
                ?? player.HealthController as ActiveHealthController;
            if (active == null || !active.IsAlive)
            {
                return false;
            }

            active.Kill(EDamageType.Bullet);
            if (!string.IsNullOrEmpty(profileId))
            {
                LastKillByProfileId[profileId] = Time.unscaledTime;
            }

            logger?.LogInfo(
                $"[BOSS_CORPSE_FIX] {reason} nick={player.Profile?.Nickname} action=Kill(Bullet) alive=true botOwnerDead={player.AIData?.BotOwner?.IsDead}");
            ScheduleRecheck(player, 1.5f);
            if (verbose)
            {
                logger?.LogInfo($"[BOSS_CORPSE_FIX] pos={player.Position} type={BotDeathPlayerHelper.GetPlayerTypeLabel(player)}");
            }

            return true;
        }

        private static bool HasDestroyedVitals(IHealthController health)
        {
            try
            {
                return health.IsBodyPartDestroyed(EBodyPart.Head)
                    || health.IsBodyPartDestroyed(EBodyPart.Chest);
            }
            catch
            {
                return false;
            }
        }

        private static void LogBossStuckOnce(
            Player player,
            string reason,
            bool isAlive,
            bool botOwnerDead,
            ManualLogSource logger,
            bool verbose)
        {
            var profileId = player.ProfileId;
            if (string.IsNullOrEmpty(profileId) || !LoggedStuckProfileIds.Add(profileId))
            {
                if (!verbose)
                {
                    return;
                }
            }

            logger?.LogInfo(
                $"[BOSS_CORPSE_FIX] stuck {reason} nick={player.Profile?.Nickname} role={player.Profile?.Info?.Settings?.Role} " +
                $"alive={isAlive} botOwnerDead={botOwnerDead} corpse=false type={BotDeathPlayerHelper.GetPlayerTypeLabel(player)}");
        }
    }
}
