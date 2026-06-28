using System.Collections.Generic;
using BepInEx.Logging;
using EFT;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    /// <summary>
    /// Быстрые повторные попытки ragdoll/visual сразу после смерти (0 / 0.1 / 0.25 / 0.5 / 1 с).
    /// </summary>
    internal static class BotDeathFastRecheck
    {
        private struct Entry
        {
            internal Player Player;
            internal float NextAttemptTime;
            internal int AttemptIndex;
        }

        private static readonly float[] AttemptDelays = { 0f, 0.1f, 0.25f, 0.5f, 1f };
        private static readonly List<Entry> Pending = new List<Entry>();

        internal static void ResetRaid()
        {
            Pending.Clear();
        }

        internal static void Schedule(Player player, float firstDelaySeconds = 0f)
        {
            if (player == null || !BotDeathPlayerHelper.IsAiPlayer(player))
            {
                return;
            }

            Pending.Add(new Entry
            {
                Player = player,
                NextAttemptTime = Time.unscaledTime + firstDelaySeconds,
                AttemptIndex = 0
            });
        }

        internal static void Process(PluginCore plugin)
        {
            if (plugin == null || !plugin.Enabled.Value || Pending.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                var entry = Pending[i];
                if (entry.Player == null || !entry.Player.gameObject.activeInHierarchy)
                {
                    Pending.RemoveAt(i);
                    continue;
                }

                if (now < entry.NextAttemptTime)
                {
                    continue;
                }

                TryFinalize(entry.Player, plugin);

                if (IsVisualComplete(entry.Player))
                {
                    Pending.RemoveAt(i);
                    continue;
                }

                entry.AttemptIndex++;
                if (entry.AttemptIndex >= AttemptDelays.Length)
                {
                    Pending.RemoveAt(i);
                    continue;
                }

                entry.NextAttemptTime = now + AttemptDelays[entry.AttemptIndex];
                Pending[i] = entry;
            }
        }

        private static void TryFinalize(Player player, PluginCore plugin)
        {
            if (BotDeathPlayerHelper.IsBossOrFollower(player) && plugin.BossCorpseFixEnabled.Value)
            {
                BotDeathBossCorpseFix.TryBossStuckFix(
                    player, "fast_recheck", plugin.ModLogger, plugin.VerboseLogging.Value, plugin);
            }

            BotDeathVisualFinalize.FixStuckDeathVisual(
                player, "fast_recheck", plugin.ModLogger, plugin.VerboseLogging.Value);
        }

        private static bool IsVisualComplete(Player player)
        {
            if (player == null)
            {
                return true;
            }

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
    }
}
