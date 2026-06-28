using EFT;
using Fika.Core.Main.Players;
using HarmonyLib;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    /// <summary>
    /// Останавливает зацикленный звук стрельбы, если HandsController/Firearm не получил OnPlayerDead.
    /// </summary>
    internal static class BotDeathCombatAudioStop
    {
        private static readonly System.Reflection.FieldInfo SpeechSourceField =
            AccessTools.Field(typeof(Player), "_speechSource");

        internal static void StopAll(Player player)
        {
            if (player == null)
            {
                return;
            }

            StopFirearmHands(player);
            StopSpeechSource(player);

            if (player is ObservedPlayer observed)
            {
                try
                {
                    observed.ToggleMuteSpeechSource(true);
                }
                catch
                {
                    // ignore
                }
            }

            var audioSources = player.gameObject.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
            {
                var source = audioSources[i];
                if (source == null)
                {
                    continue;
                }

                source.Stop();
                source.loop = false;
                source.mute = true;
                source.volume = 0f;
                source.enabled = false;
            }
        }

        private static void StopFirearmHands(Player player)
        {
            try
            {
                player.HandsController?.OnPlayerDead();
            }
            catch
            {
                // ignore
            }

            try
            {
                player.HandsController?.FastForwardCurrentState();
            }
            catch
            {
                // ignore
            }
        }

        private static void StopSpeechSource(Player player)
        {
            if (SpeechSourceField == null)
            {
                return;
            }

            try
            {
                if (SpeechSourceField.GetValue(player) is BetterSource speechSource)
                {
                    speechSource.Stop();
                    speechSource.SetActive(false);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
