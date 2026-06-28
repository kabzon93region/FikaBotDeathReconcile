using EFT;
using Fika.Core.Main.Players;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathPlayerHelper
    {
        internal static bool IsAiPlayer(Player player)
        {
            if (player == null || player.IsYourPlayer)
            {
                return false;
            }

            if (player is FikaBot)
            {
                return true;
            }

            if (player.AIData?.IsAI == true)
            {
                return true;
            }

            if (player.AIData?.BotOwner != null)
            {
                return true;
            }

            return IsBossOrFollower(player);
        }

        internal static bool IsBossOrFollower(Player player)
        {
            var role = player?.Profile?.Info?.Settings?.Role;
            if (!role.HasValue)
            {
                return false;
            }

            return role.Value.IsBoss() || role.Value.IsFollower();
        }

        internal static bool IsPriorityBoss(Player player)
        {
            return IsAiPlayer(player) && IsBossOrFollower(player);
        }

        internal static int GetReconcileId(Player player)
        {
            if (player is FikaBot fikaBot)
            {
                return fikaBot.NetId;
            }

            var profileId = player?.ProfileId;
            if (string.IsNullOrEmpty(profileId))
            {
                return player != null ? player.GetInstanceID() : 0;
            }

            unchecked
            {
                var hash = 23;
                for (int i = 0; i < profileId.Length; i++)
                {
                    hash = (hash * 31) + profileId[i];
                }

                return hash;
            }
        }

        internal static FikaBot AsFikaBot(Player player)
        {
            return player as FikaBot;
        }

        internal static string GetPlayerTypeLabel(Player player)
        {
            if (player == null)
            {
                return "null";
            }

            if (player is FikaBot)
            {
                return "FikaBot";
            }

            return player.GetType().Name;
        }
    }
}
