using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using HarmonyLib;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathAudioSilencer
    {
        private static readonly FieldInfo SpeechSourceField =
            AccessTools.Field(typeof(Player), "_speechSource");

        internal static void ProcessClientDeadBots(ManualLogSource logger, bool verbose)
        {
            if (!FikaBackendUtils.IsClient)
            {
                return;
            }

            var networkManager = Singleton<IFikaNetworkManager>.Instance;
            var observedPlayers = networkManager?.ObservedPlayers;
            if (observedPlayers == null)
            {
                return;
            }

            for (int i = 0; i < observedPlayers.Count; i++)
            {
                var observed = observedPlayers[i] as ObservedPlayer;
                if (observed == null || !observed.IsObservedAI)
                {
                    continue;
                }

                var health = observed.HealthController;
                if (health == null || health.IsAlive)
                {
                    continue;
                }

                SilenceDeadAiPlayer(observed, logger, verbose);
            }
        }

        /// <summary>
        /// Listen-host: FikaBot на хосте не ObservedPlayer — глушим стрельбу/фразы у мёртвых AI.
        /// </summary>
        internal static void ProcessHostDeadBots(ManualLogSource logger, bool verbose)
        {
            if (!FikaBackendUtils.IsServer)
            {
                return;
            }

            var candidates = BotDeathSnapshotCollector.CollectHostCorpseFixCandidates();
            ProcessDeadHostPlayers(candidates, logger, verbose);

            var bosses = BotDeathSnapshotCollector.CollectHostBossPlayers();
            ProcessDeadHostPlayers(bosses, logger, verbose);
        }

        private static void ProcessDeadHostPlayers(IEnumerable<Player> players, ManualLogSource logger, bool verbose)
        {
            if (players == null)
            {
                return;
            }

            foreach (var bot in players)
            {
                if (bot?.HealthController == null || bot.HealthController.IsAlive)
                {
                    continue;
                }

                SilenceDeadAiPlayer(bot, logger, verbose);
            }
        }

        internal static void SilenceDeadObservedBot(ObservedPlayer player, ManualLogSource logger, bool verbose)
        {
            SilenceDeadAiPlayer(player, logger, verbose);
        }

        internal static void Reset()
        {
        }

        internal static void SilenceDeadAiPlayer(Player player, ManualLogSource logger, bool verbose)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                if (player is ObservedPlayer observed)
                {
                    observed.ToggleMuteSpeechSource(true);
                }

                var speaker = player.Speaker;
                if (speaker != null)
                {
                    speaker.Shut();
                    speaker.Clip = null;
                    speaker.OnDestroy();
                }

                StopSpeechSource(player);
                BotDeathCombatAudioStop.StopAll(player);

                var audioSources = player.gameObject.GetComponentsInChildren<AudioSource>(true);
                for (int i = 0; i < audioSources.Length; i++)
                {
                    var source = audioSources[i];
                    if (source == null)
                    {
                        continue;
                    }

                    source.Stop();
                    source.mute = true;
                    source.volume = 0f;
                    source.enabled = false;
                }

                if (verbose)
                {
                    var nickname = player.Profile?.Nickname ?? "?";
                    logger?.LogInfo(
                        $"[BOT_RECONCILE] silenced dead ai nick={nickname} id={BotDeathPlayerHelper.GetReconcileId(player)} audioSources={audioSources.Length}");
                }
            }
            catch (System.Exception ex)
            {
                logger?.LogWarning($"[BOT_RECONCILE] silence failed id={BotDeathPlayerHelper.GetReconcileId(player)}: {ex.Message}");
            }
        }

        private static void StopSpeechSource(Player player)
        {
            if (SpeechSourceField == null || player == null)
            {
                return;
            }

            if (SpeechSourceField.GetValue(player) is BetterSource speechSource)
            {
                speechSource.Stop();
                speechSource.SetActive(false);
            }
        }
    }
}
