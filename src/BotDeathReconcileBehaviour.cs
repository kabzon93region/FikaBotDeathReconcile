using System.Collections.Generic;
using Fika.Core.Main.Utils;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    internal sealed class BotDeathReconcileBehaviour : MonoBehaviour
    {
        private const float ReconcileCooldownSeconds = 5f;

        private float _timer;
        private readonly Dictionary<int, float> _lastActionByNetId = new Dictionary<int, float>();

        private void Update()
        {
            var plugin = PluginCore.Instance;
            if (plugin == null || !plugin.Enabled.Value)
            {
                return;
            }

            if (!BotDeathSnapshotCollector.IsInActiveRaid())
            {
                _timer = 0f;
                BotDeathAudioSilencer.Reset();
                BotDeathCorpseFix.ResetRaid();
                BotDeathFastRecheck.ResetRaid();
                return;
            }

            if (!BotDeathReconcileNetwork.Instance.IsCoopActive())
            {
                return;
            }

            BotDeathBossCorpseFix.ProcessScheduledRechecks(plugin);
            BotDeathFastRecheck.Process(plugin);

            if (plugin.ClientSilenceDeadBotAudio.Value)
            {
                if (FikaBackendUtils.IsClient && BotDeathReconcileNetwork.Instance.IsRemoteClient())
                {
                    BotDeathAudioSilencer.ProcessClientDeadBots(plugin.ModLogger, plugin.VerboseLogging.Value);
                }

                if (BotDeathReconcileNetwork.Instance.IsHost())
                {
                    BotDeathAudioSilencer.ProcessHostDeadBots(plugin.ModLogger, plugin.VerboseLogging.Value);
                }
            }

            _timer += Time.unscaledDeltaTime;
            if (_timer < plugin.IntervalSeconds.Value)
            {
                return;
            }

            _timer = 0f;
            RunTick(plugin);
        }

        private void RunTick(PluginCore plugin)
        {
            if (FikaBackendUtils.IsClient)
            {
                if (BotDeathReconcileNetwork.Instance.IsRemoteClient() && plugin.ClientReportsEnabled.Value)
                {
                    var clientSnapshot = BotDeathSnapshotCollector.CollectClientObservedBots();
                    if (clientSnapshot.Entries.Count > 0)
                    {
                        BotDeathReconcileNetwork.Instance.SendClientReport(clientSnapshot);
                        if (plugin.VerboseLogging.Value)
                        {
                            plugin.ModLogger.LogInfo($"[BOT_RECONCILE] client report sent entries={clientSnapshot.Entries.Count}");
                        }
                    }
                }

                var clientFixed = BotDeathCorpseFix.RunClientFix(plugin);
                var clientBossFixed = BotDeathBossCorpseFix.RunClientBossScan(plugin);
                if ((clientFixed > 0 || clientBossFixed > 0) && plugin.VerboseLogging.Value)
                {
                    plugin.ModLogger.LogInfo($"[BOT_CORPSE_FIX] client fixed={clientFixed} boss={clientBossFixed}");
                }
            }

            if (!BotDeathReconcileNetwork.Instance.IsHost())
            {
                return;
            }

            if (plugin.ClientReportsEnabled.Value)
            {
                foreach (var report in BotDeathReconcileNetwork.Instance.ConsumeClientReports())
                {
                    if (report.IsAlive)
                    {
                        continue;
                    }

                    if (!BotDeathSnapshotCollector.TryGetHostBot(report.NetId, out var bot))
                    {
                        continue;
                    }

                    if (bot.HealthController == null || !bot.HealthController.IsAlive)
                    {
                        continue;
                    }

                    if (!CanActOnNetId(report.NetId))
                    {
                        continue;
                    }

                    if (BotDeathForceKill.TryReconcile(bot, "client_report", plugin.ModLogger, plugin.VerboseLogging.Value))
                    {
                        MarkActed(report.NetId);
                    }
                }
            }

            var hostFixed = BotDeathCorpseFix.RunHostFix(plugin);
            var hostBossFixed = BotDeathBossCorpseFix.RunHostBossScan(plugin);
            if ((hostFixed > 0 || hostBossFixed > 0) && plugin.VerboseLogging.Value)
            {
                plugin.ModLogger.LogInfo($"[BOT_CORPSE_FIX] host fixed={hostFixed} boss={hostBossFixed}");
            }

            if (plugin.HostLocalScanEnabled.Value && plugin.VerboseLogging.Value)
            {
                LogHostScanMonitor(plugin);
            }
        }

        /// <summary>
        /// Только логи — никаких Kill/OnDead на хосте (listen-host безопасен).
        /// </summary>
        private static void LogHostScanMonitor(PluginCore plugin)
        {
            var hostSnapshot = BotDeathSnapshotCollector.CollectHostBots();
            var zombieCount = 0;
            for (int i = 0; i < hostSnapshot.Entries.Count; i++)
            {
                if (!BotDeathSnapshotCollector.TryGetHostBot(hostSnapshot.Entries[i].NetId, out var bot))
                {
                    continue;
                }

                if (BotDeathForceKill.IsHostZombie(bot))
                {
                    zombieCount++;
                }
            }

            plugin.ModLogger.LogInfo(
                $"[BOT_RECONCILE] host monitor (no actions) bots={hostSnapshot.Entries.Count} would_zombie={zombieCount}");
        }

        private bool CanActOnNetId(int netId)
        {
            if (!_lastActionByNetId.TryGetValue(netId, out var lastTime))
            {
                return true;
            }

            return Time.unscaledTime - lastTime >= ReconcileCooldownSeconds;
        }

        private void MarkActed(int netId)
        {
            _lastActionByNetId[netId] = Time.unscaledTime;
        }
    }
}
