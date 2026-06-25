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

                SilenceDeadObservedBot(observed, logger, verbose);
            }
        }

        internal static void Reset()
        {
        }

        internal static void SilenceDeadObservedBot(ObservedPlayer player, ManualLogSource logger, bool verbose)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                player.ToggleMuteSpeechSource(true);

                var speaker = player.Speaker;
                if (speaker != null)
                {
                    speaker.Shut();
                    speaker.Clip = null;
                    speaker.OnDestroy();
                }

                StopSpeechSource(player);

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
                        $"[BOT_RECONCILE] silenced dead observed bot netId={player.NetId} nick={nickname} audioSources={audioSources.Length}");
                }
            }
            catch (System.Exception ex)
            {
                logger?.LogWarning($"[BOT_RECONCILE] silence failed netId={player.NetId}: {ex.Message}");
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
