using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
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
                return;
            }

            if (!BotDeathReconcileNetwork.Instance.IsCoopActive())
            {
                return;
            }

            if (BotDeathReconcileNetwork.Instance.IsRemoteClient() && plugin.ClientSilenceDeadBotAudio.Value)
            {
                BotDeathAudioSilencer.ProcessClientDeadBots(plugin.ModLogger, plugin.VerboseLogging.Value);
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

            if (!plugin.HostLocalScanEnabled.Value)
            {
                return;
            }

            var hostSnapshot = BotDeathSnapshotCollector.CollectHostBots();
            for (int i = 0; i < hostSnapshot.Entries.Count; i++)
            {
                if (!BotDeathSnapshotCollector.TryGetHostBot(hostSnapshot.Entries[i].NetId, out var bot))
                {
                    continue;
                }

                if (!BotDeathForceKill.IsHostZombie(bot))
                {
                    continue;
                }

                if (!CanActOnNetId(bot.NetId))
                {
                    continue;
                }

                if (BotDeathForceKill.TryReconcile(bot, "host_zombie", plugin.ModLogger, plugin.VerboseLogging.Value))
                {
                    MarkActed(bot.NetId);
                }
            }
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
