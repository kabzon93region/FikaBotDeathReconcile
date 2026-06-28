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
            if (bot == null || reason != "client_report")
            {
                return false;
            }

            var health = ResolveActiveHealth(bot);
            if (health == null || !health.IsAlive)
            {
                return false;
            }

            health.Kill(EDamageType.Bullet);
            Log(logger, verbose, reason, bot, "Kill(Bullet)");
            return true;
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

            if (GetCorpse(bot) != null)
            {
                return false;
            }

            var botOwner = bot.AIData?.BotOwner;
            return botOwner != null && !botOwner.IsDead;
        }

        internal static ActiveHealthController ResolveActiveHealth(FikaBot bot)
        {
            if (bot == null)
            {
                return null;
            }

            return bot.ActiveHealthController as ActiveHealthController
                ?? bot.HealthController as ActiveHealthController;
        }

        internal static Corpse GetCorpse(Player player)
        {
            if (player == null || CorpseField == null)
            {
                return null;
            }

            return CorpseField.GetValue(player) as Corpse;
        }

        internal static void SetCorpse(Player player, Corpse corpse)
        {
            if (player == null || CorpseField == null)
            {
                return;
            }

            CorpseField.SetValue(player, corpse);
        }

        private static void Log(ManualLogSource logger, bool verbose, string reason, FikaBot bot, string action)
        {
            var nickname = bot.Profile?.Nickname ?? "?";
            logger?.LogInfo($"[BOT_RECONCILE] {reason} netId={bot.NetId} profile={bot.ProfileId} nick={nickname} action={action}");
            if (verbose)
            {
                logger?.LogInfo(
                    $"[BOT_RECONCILE] pos={bot.Position} alive={bot.HealthController?.IsAlive} corpse={(GetCorpse(bot) != null)}");
            }
        }
    }
}
