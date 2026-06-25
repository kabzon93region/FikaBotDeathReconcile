using System;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathSnapshotCollector
    {
        public static BotDeathSnapshot CollectClientObservedBots()
        {
            var snapshot = new BotDeathSnapshot();
            if (!FikaBackendUtils.IsClient)
            {
                return snapshot;
            }

            var networkManager = Singleton<IFikaNetworkManager>.Instance;
            if (networkManager?.ObservedPlayers == null)
            {
                return snapshot;
            }

            var observedPlayers = networkManager.ObservedPlayers;
            for (int i = 0; i < observedPlayers.Count; i++)
            {
                var observed = observedPlayers[i];
                if (observed == null || !observed.IsObservedAI)
                {
                    continue;
                }

                var health = observed.HealthController;
                if (health == null || health.IsAlive)
                {
                    continue;
                }

                snapshot.Entries.Add(new BotDeathEntry
                {
                    NetId = observed.NetId,
                    IsAlive = false,
                    Position = observed.Position,
                    ProfileId = observed.ProfileId,
                    Nickname = observed.Profile?.Nickname
                });
            }

            return snapshot;
        }

        public static BotDeathSnapshot CollectHostBots()
        {
            var snapshot = new BotDeathSnapshot();
            if (!FikaBackendUtils.IsServer || !CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                return snapshot;
            }

            foreach (var pair in coopHandler.Players)
            {
                if (pair.Value is not FikaBot bot)
                {
                    continue;
                }

                if (bot == null)
                {
                    continue;
                }

                var health = bot.HealthController;
                if (health == null)
                {
                    continue;
                }

                snapshot.Entries.Add(new BotDeathEntry
                {
                    NetId = bot.NetId,
                    IsAlive = health.IsAlive,
                    Position = bot.Position,
                    ProfileId = bot.ProfileId,
                    Nickname = bot.Profile?.Nickname
                });
            }

            return snapshot;
        }

        public static bool TryGetHostBot(int netId, out FikaBot bot)
        {
            bot = null;
            if (!FikaBackendUtils.IsServer || !CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                return false;
            }

            if (!coopHandler.Players.TryGetValue(netId, out var player))
            {
                return false;
            }

            bot = player as FikaBot;
            return bot != null;
        }

        public static bool IsInActiveRaid()
        {
            try
            {
                if (!Singleton<GameWorld>.Instantiated)
                {
                    return false;
                }

                var world = Singleton<GameWorld>.Instance;
                if (world?.MainPlayer == null)
                {
                    return false;
                }

                return Singleton<IFikaNetworkManager>.Instantiated;
            }
            catch
            {
                return false;
            }
        }
    }
}
