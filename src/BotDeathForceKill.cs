using System.Reflection;
using BepInEx.Logging;
using EFT;
using EFT.HealthSystem;
using EFT.Interactive;
using Fika.Core.Main.Players;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathForceKill
    {
        private static readonly FieldInfo CorpseField = typeof(Player).GetField(
            "Corpse",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static bool TryReconcile(FikaBot bot, string reason, ManualLogSource logger, bool verbose)
        {
            if (bot == null)
            {
                return false;
            }

            var health = bot.ActiveHealthController as ActiveHealthController;
            if (health == null)
            {
                return false;
            }

            if (health.IsAlive)
            {
                health.Kill(EDamageType.Bullet);
                Log(logger, verbose, reason, bot, "Kill(Bullet)");
                return true;
            }

            if (TryCompleteStuckDeath(bot, reason, logger, verbose))
            {
                return true;
            }

            return false;
        }

        public static bool IsHostZombie(FikaBot bot)
        {
            if (bot == null || !bot.gameObject.activeInHierarchy)
            {
                return false;
            }

            var health = bot.HealthController;
            if (health == null || health.IsAlive)
            {
                return false;
            }

            if (GetCorpse(bot) == null)
            {
                return true;
            }

            var botOwner = bot.AIData?.BotOwner;
            return botOwner != null && !botOwner.IsDead;
        }

        private static Corpse GetCorpse(Player player)
        {
            if (player == null || CorpseField == null)
            {
                return null;
            }

            return CorpseField.GetValue(player) as Corpse;
        }

        private static bool TryCompleteStuckDeath(FikaBot bot, string reason, ManualLogSource logger, bool verbose)
        {
            if (!IsHostZombie(bot))
            {
                return false;
            }

            try
            {
                bot.OnDead(EDamageType.Undefined);
                Log(logger, verbose, reason, bot, "OnDead(Undefined)");
                return true;
            }
            catch (System.Exception ex)
            {
                logger?.LogWarning($"[BOT_RECONCILE] OnDead failed netId={bot.NetId}: {ex.Message}");
                return false;
            }
        }

        private static void Log(ManualLogSource logger, bool verbose, string reason, FikaBot bot, string action)
        {
            var nickname = bot.Profile?.Nickname ?? "?";
            logger?.LogInfo($"[BOT_RECONCILE] {reason} netId={bot.NetId} profile={bot.ProfileId} nick={nickname} action={action}");
            if (verbose)
            {
                logger?.LogInfo($"[BOT_RECONCILE] pos={bot.Position} alive={bot.HealthController?.IsAlive} corpse={(GetCorpse(bot) != null)}");
            }
        }
    }
}
