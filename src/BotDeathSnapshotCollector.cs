using System;
using System.Collections.Generic;
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
        public static List<FikaBot> CollectHostCorpseFixCandidates()
        {
            var bots = new List<FikaBot>();
            if (!FikaBackendUtils.IsServer)
            {
                return bots;
            }

            var seenProfileIds = new HashSet<string>(StringComparer.Ordinal);
            if (CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                foreach (var pair in coopHandler.Players)
                {
                    if (pair.Value is FikaBot bot && bot != null)
                    {
                        AppendCorpseFixCandidate(bots, seenProfileIds, bot);
                    }
                }
            }

            AppendCorpseFixCandidatesFromBotOwners(bots, seenProfileIds);
            AppendCorpseFixCandidatesFromGameWorld(bots, seenProfileIds);
            return bots;
        }

        private static void AppendCorpseFixCandidatesFromBotOwners(List<FikaBot> bots, HashSet<string> seenProfileIds)
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            var botOwners = Singleton<IBotGame>.Instance?.BotsController?.Bots?.BotOwners;
            if (botOwners == null)
            {
                return;
            }

            foreach (var owner in botOwners)
            {
                AppendCorpseFixCandidate(bots, seenProfileIds, owner?.GetPlayer as FikaBot);
            }
        }

        private static void AppendCorpseFixCandidatesFromGameWorld(List<FikaBot> bots, HashSet<string> seenProfileIds)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            var world = Singleton<GameWorld>.Instance;
            AppendCorpseFixCandidatesFromPlayers(bots, seenProfileIds, world.AllAlivePlayersList);
            AppendCorpseFixCandidatesFromRegistered(bots, seenProfileIds, world.RegisteredPlayers);
            AppendCorpseFixCandidatesFromPlayers(bots, seenProfileIds, world.AllPlayersEverExisted);
        }

        private static void AppendCorpseFixCandidatesFromRegistered(
            List<FikaBot> bots,
            HashSet<string> seenProfileIds,
            List<IPlayer> registeredPlayers)
        {
            if (registeredPlayers == null)
            {
                return;
            }

            for (int i = 0; i < registeredPlayers.Count; i++)
            {
                if (registeredPlayers[i] is Player candidate)
                {
                    AppendCorpseFixCandidate(bots, seenProfileIds, candidate as FikaBot);
                }
            }
        }

        private static void AppendCorpseFixCandidatesFromPlayers(
            List<FikaBot> bots,
            HashSet<string> seenProfileIds,
            IEnumerable<Player> players)
        {
            if (players == null)
            {
                return;
            }

            foreach (var player in players)
            {
                AppendCorpseFixCandidate(bots, seenProfileIds, player as FikaBot);
            }
        }

        private static void AppendCorpseFixCandidate(List<FikaBot> bots, HashSet<string> seenProfileIds, FikaBot bot)
        {
            if (bot == null || string.IsNullOrEmpty(bot.ProfileId) || !seenProfileIds.Add(bot.ProfileId))
            {
                return;
            }

            bots.Add(bot);
        }

        public static List<Player> CollectHostBossPlayers()
        {
            var players = new List<Player>();
            if (!FikaBackendUtils.IsServer)
            {
                return players;
            }

            var seenProfileIds = new HashSet<string>(StringComparer.Ordinal);
            if (CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                foreach (var pair in coopHandler.Players)
                {
                    AppendBossPlayer(players, seenProfileIds, pair.Value);
                }
            }

            AppendBossPlayersFromBotOwners(players, seenProfileIds);
            AppendBossPlayersFromGameWorld(players, seenProfileIds);
            return players;
        }

        public static List<ObservedPlayer> CollectClientBossObservedPlayers()
        {
            var players = new List<ObservedPlayer>();
            if (!FikaBackendUtils.IsClient)
            {
                return players;
            }

            var networkManager = Singleton<IFikaNetworkManager>.Instance;
            if (networkManager?.ObservedPlayers == null)
            {
                return players;
            }

            var observedPlayers = networkManager.ObservedPlayers;
            for (int i = 0; i < observedPlayers.Count; i++)
            {
                var observed = observedPlayers[i];
                if (observed == null || !observed.IsObservedAI || !BotDeathPlayerHelper.IsBossOrFollower(observed))
                {
                    continue;
                }

                if (BotDeathForceKill.GetCorpse(observed) != null)
                {
                    continue;
                }

                players.Add(observed);
            }

            return players;
        }

        private static void AppendBossPlayersFromBotOwners(List<Player> players, HashSet<string> seenProfileIds)
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            var botOwners = Singleton<IBotGame>.Instance?.BotsController?.Bots?.BotOwners;
            if (botOwners == null)
            {
                return;
            }

            foreach (var owner in botOwners)
            {
                AppendBossPlayer(players, seenProfileIds, owner?.GetPlayer);
            }
        }

        private static void AppendBossPlayersFromGameWorld(List<Player> players, HashSet<string> seenProfileIds)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            var world = Singleton<GameWorld>.Instance;
            AppendBossPlayersFromList(players, seenProfileIds, world.AllAlivePlayersList);
            AppendBossPlayersFromRegistered(players, seenProfileIds, world.RegisteredPlayers);
            AppendBossPlayersFromList(players, seenProfileIds, world.AllPlayersEverExisted);
        }

        private static void AppendBossPlayersFromRegistered(
            List<Player> players,
            HashSet<string> seenProfileIds,
            List<IPlayer> registeredPlayers)
        {
            if (registeredPlayers == null)
            {
                return;
            }

            for (int i = 0; i < registeredPlayers.Count; i++)
            {
                AppendBossPlayer(players, seenProfileIds, registeredPlayers[i] as Player);
            }
        }

        private static void AppendBossPlayersFromList(
            List<Player> players,
            HashSet<string> seenProfileIds,
            IEnumerable<Player> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var player in source)
            {
                AppendBossPlayer(players, seenProfileIds, player);
            }
        }

        private static void AppendBossPlayer(List<Player> players, HashSet<string> seenProfileIds, Player player)
        {
            if (player == null || string.IsNullOrEmpty(player.ProfileId) || !seenProfileIds.Add(player.ProfileId))
            {
                return;
            }

            if (!BotDeathPlayerHelper.IsBossOrFollower(player))
            {
                return;
            }

            players.Add(player);
        }

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
            if (!FikaBackendUtils.IsServer)
            {
                return snapshot;
            }

            var seenProfileIds = new HashSet<string>(StringComparer.Ordinal);
            if (CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                foreach (var pair in coopHandler.Players)
                {
                    if (pair.Value is not FikaBot bot || bot == null)
                    {
                        continue;
                    }

                    AppendHostPlayer(snapshot, seenProfileIds, bot);
                }
            }

            AppendHostPlayersFromBotOwners(snapshot, seenProfileIds);
            return snapshot;
        }

        private static void AppendHostPlayersFromBotOwners(BotDeathSnapshot snapshot, HashSet<string> seenProfileIds)
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            var botOwners = Singleton<IBotGame>.Instance?.BotsController?.Bots?.BotOwners;
            if (botOwners == null)
            {
                return;
            }

            foreach (var owner in botOwners)
            {
                AppendHostPlayer(snapshot, seenProfileIds, owner?.GetPlayer);
            }
        }

        private static void AppendHostPlayersFromGameWorld(BotDeathSnapshot snapshot, HashSet<string> seenProfileIds)
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            var world = Singleton<GameWorld>.Instance;
            AppendPlayersFromList(snapshot, seenProfileIds, world.AllAlivePlayersList);
            AppendPlayersFromRegistered(snapshot, seenProfileIds, world.RegisteredPlayers);
            AppendPlayersFromList(snapshot, seenProfileIds, world.AllPlayersEverExisted);
        }

        private static void AppendPlayersFromRegistered(
            BotDeathSnapshot snapshot,
            HashSet<string> seenProfileIds,
            List<IPlayer> registeredPlayers)
        {
            if (registeredPlayers == null)
            {
                return;
            }

            for (int i = 0; i < registeredPlayers.Count; i++)
            {
                AppendHostPlayer(snapshot, seenProfileIds, registeredPlayers[i] as Player);
            }
        }

        private static void AppendPlayersFromList(
            BotDeathSnapshot snapshot,
            HashSet<string> seenProfileIds,
            IEnumerable<Player> players)
        {
            if (players == null)
            {
                return;
            }

            foreach (var player in players)
            {
                AppendHostPlayer(snapshot, seenProfileIds, player);
            }
        }

        private static void AppendHostPlayer(BotDeathSnapshot snapshot, HashSet<string> seenProfileIds, Player player)
        {
            if (player is not FikaBot bot)
            {
                return;
            }

            if (string.IsNullOrEmpty(bot.ProfileId) || !seenProfileIds.Add(bot.ProfileId))
            {
                return;
            }

            var health = bot.HealthController;
            if (health == null)
            {
                return;
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

        public static bool TryGetHostBot(int netId, out FikaBot bot)
        {
            bot = null;
            if (!TryGetHostPlayerByReconcileId(netId, out var player))
            {
                return false;
            }

            bot = player as FikaBot;
            return bot != null;
        }

        public static bool TryGetHostPlayer(string profileId, out Player player)
        {
            player = null;
            if (string.IsNullOrEmpty(profileId) || !FikaBackendUtils.IsServer)
            {
                return false;
            }

            if (CoopHandler.TryGetCoopHandler(out var coopHandler))
            {
                foreach (var pair in coopHandler.Players)
                {
                    if (pair.Value is Player candidate &&
                        string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal))
                    {
                        player = candidate;
                        return true;
                    }
                }
            }

            if (TryFindPlayerInGameWorld(profileId, out player))
            {
                return true;
            }

            return TryFindPlayerInBotOwners(profileId, out player);
        }

        private static bool TryGetHostPlayerByReconcileId(int reconcileId, out Player player)
        {
            player = null;
            if (!FikaBackendUtils.IsServer)
            {
                return false;
            }

            if (CoopHandler.TryGetCoopHandler(out var coopHandler) &&
                coopHandler.Players.TryGetValue(reconcileId, out var mapped) &&
                mapped is Player coopPlayer)
            {
                player = coopPlayer;
                return true;
            }

            return TryFindPlayerByReconcileId(reconcileId, out player);
        }

        private static bool TryFindPlayerByReconcileId(int reconcileId, out Player player)
        {
            player = null;
            if (!Singleton<GameWorld>.Instantiated)
            {
                return false;
            }

            var world = Singleton<GameWorld>.Instance;
            if (TryFindInPlayers(world.AllAlivePlayersList, reconcileId, out player))
            {
                return true;
            }

            if (TryFindInRegistered(world.RegisteredPlayers, reconcileId, out player))
            {
                return true;
            }

            return TryFindInPlayers(world.AllPlayersEverExisted, reconcileId, out player);
        }

        private static bool TryFindPlayerInGameWorld(string profileId, out Player player)
        {
            player = null;
            if (!Singleton<GameWorld>.Instantiated)
            {
                return false;
            }

            var world = Singleton<GameWorld>.Instance;
            if (TryFindInPlayersByProfile(world.AllAlivePlayersList, profileId, out player))
            {
                return true;
            }

            if (TryFindInRegisteredByProfile(world.RegisteredPlayers, profileId, out player))
            {
                return true;
            }

            return TryFindInPlayersByProfile(world.AllPlayersEverExisted, profileId, out player);
        }

        private static bool TryFindPlayerInBotOwners(string profileId, out Player player)
        {
            player = null;
            if (!Singleton<IBotGame>.Instantiated)
            {
                return false;
            }

            var botOwners = Singleton<IBotGame>.Instance?.BotsController?.Bots?.BotOwners;
            if (botOwners == null)
            {
                return false;
            }

            foreach (var owner in botOwners)
            {
                var candidate = owner?.GetPlayer;
                if (candidate != null &&
                    string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal))
                {
                    player = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindInPlayers(IEnumerable<Player> players, int reconcileId, out Player player)
        {
            player = null;
            if (players == null)
            {
                return false;
            }

            foreach (var candidate in players)
            {
                if (candidate != null && BotDeathPlayerHelper.GetReconcileId(candidate) == reconcileId)
                {
                    player = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindInPlayersByProfile(IEnumerable<Player> players, string profileId, out Player player)
        {
            player = null;
            if (players == null)
            {
                return false;
            }

            foreach (var candidate in players)
            {
                if (candidate != null &&
                    string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal))
                {
                    player = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindInRegistered(List<IPlayer> registeredPlayers, int reconcileId, out Player player)
        {
            player = null;
            if (registeredPlayers == null)
            {
                return false;
            }

            for (int i = 0; i < registeredPlayers.Count; i++)
            {
                if (registeredPlayers[i] is Player candidate &&
                    BotDeathPlayerHelper.GetReconcileId(candidate) == reconcileId)
                {
                    player = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindInRegisteredByProfile(List<IPlayer> registeredPlayers, string profileId, out Player player)
        {
            player = null;
            if (registeredPlayers == null)
            {
                return false;
            }

            for (int i = 0; i < registeredPlayers.Count; i++)
            {
                if (registeredPlayers[i] is Player candidate &&
                    string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal))
                {
                    player = candidate;
                    return true;
                }
            }

            return false;
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
