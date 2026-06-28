using System.Reflection;
using BepInEx.Logging;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathVisualFinalize
    {
        private static readonly FieldInfo CharacterControllerField =
            AccessTools.Field(typeof(Player), "_characterController");

        private static readonly MethodInfo CorpseStartRagdollMethod =
            AccessTools.Method(typeof(Corpse), "method_16", new[] { typeof(bool) });

        internal static bool NeedsVisualFinalize(Player player)
        {
            if (player == null)
            {
                return false;
            }

            try
            {
                if (IsWrappedAnimatorEnabled(player, "BodyAnimatorCommon") ||
                    IsWrappedAnimatorEnabled(player, "ArmsAnimatorCommon"))
                {
                    return true;
                }

                foreach (var animator in player.GetComponentsInChildren<Animator>(true))
                {
                    if (animator != null && animator.enabled)
                    {
                        return true;
                    }
                }

                var corpse = BotDeathForceKill.GetCorpse(player);
                if (corpse != null && corpse.HasRagdoll && corpse.Ragdoll == null)
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        internal static bool FixStuckDeathVisual(
            Player player,
            string reason,
            ManualLogSource logger,
            bool verbose)
        {
            if (player == null)
            {
                return false;
            }

            var corpse = BotDeathForceKill.GetCorpse(player);
            if (corpse == null && (player.HealthController == null || player.HealthController.IsAlive))
            {
                return false;
            }

            if (!NeedsVisualFinalize(player) && (corpse == null || corpse.Ragdoll != null))
            {
                return false;
            }

            try
            {
                PrepareForCorpse(player);
                if (corpse != null)
                {
                    FinalizeCorpse(player, corpse);
                }

                BotDeathCombatAudioStop.StopAll(player);

                if (verbose || BotDeathPlayerHelper.IsBossOrFollower(player))
                {
                    logger?.LogInfo(
                        $"[BOT_DEATH_VISUAL] {reason} nick={player.Profile?.Nickname} corpse={(corpse != null)} ragdoll={(corpse?.Ragdoll != null)}");
                }

                return true;
            }
            catch (System.Exception ex)
            {
                logger?.LogWarning($"[BOT_DEATH_VISUAL] {reason} failed nick={player.Profile?.Nickname}: {ex.Message}");
                return false;
            }
        }

        internal static void PrepareForCorpse(Player player)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                player.FastForwardCurrentOperations();
            }
            catch
            {
                // ignore
            }

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

            DisableAnimators(player);
            DisableCharacterController(player);
            ShutSpeaker(player);
            BotDeathCombatAudioStop.StopAll(player);
        }

        internal static void FinalizeCorpse(Player player, Corpse corpse)
        {
            if (player == null || corpse == null)
            {
                return;
            }

            EnsureRagdollStarted(corpse);

            try
            {
                player.ApplyCorpseImpulse();
            }
            catch
            {
                // ignore
            }

            BotDeathCombatAudioStop.StopAll(player);
        }

        private static void EnsureRagdollStarted(Corpse corpse)
        {
            if (corpse == null || !corpse.HasRagdoll || corpse.Ragdoll != null || CorpseStartRagdollMethod == null)
            {
                return;
            }

            try
            {
                CorpseStartRagdollMethod.Invoke(corpse, new object[] { false });
            }
            catch
            {
                // ignore
            }
        }

        private static bool IsWrappedAnimatorEnabled(Player player, string propertyName)
        {
            var animator = Traverse.Create(player).Property(propertyName).GetValue<object>();
            if (animator == null)
            {
                return false;
            }

            var enabledProp = animator.GetType().GetProperty("enabled");
            return enabledProp?.GetValue(animator) is bool enabled && enabled;
        }

        private static void DisableAnimators(Player player)
        {
            SetWrappedAnimatorEnabled(player, "BodyAnimatorCommon", false);
            SetWrappedAnimatorEnabled(player, "ArmsAnimatorCommon", false);

            try
            {
                var playable = player.PlayerBones?.PlayableAnimator;
                playable?.GetType().GetMethod("Stop")?.Invoke(playable, null);
            }
            catch
            {
                // ignore
            }

            foreach (var animator in player.GetComponentsInChildren<Animator>(true))
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }
            }
        }

        private static void SetWrappedAnimatorEnabled(Player player, string propertyName, bool enabled)
        {
            try
            {
                var animator = Traverse.Create(player).Property(propertyName).GetValue<object>();
                if (animator == null)
                {
                    return;
                }

                animator.GetType().GetProperty("enabled")?.SetValue(animator, enabled);
            }
            catch
            {
                // ignore
            }
        }

        private static void DisableCharacterController(Player player)
        {
            try
            {
                if (CharacterControllerField?.GetValue(player) is ICharacterController controller)
                {
                    controller.isEnabled = false;
                }
            }
            catch
            {
                // ignore
            }
        }

        private static void ShutSpeaker(Player player)
        {
            try
            {
                var speaker = player.Speaker;
                if (speaker == null)
                {
                    return;
                }

                speaker.Shut();
                speaker.Clip = null;
            }
            catch
            {
                // ignore
            }
        }
    }
}
