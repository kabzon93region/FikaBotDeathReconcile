using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;

namespace FikaBotDeathReconcile
{
    internal sealed class BotDeathReconcileNetwork
    {
        private static BotDeathReconcileNetwork _instance;

        private ManualLogSource _logger;
        private IFikaNetworkManager _networkManager;
        private bool _packetsRegistered;
        private long _reportSequence;
        private readonly Dictionary<int, BotDeathReconcilePackets.BotDeathReportEntry> _clientReports =
            new Dictionary<int, BotDeathReconcilePackets.BotDeathReportEntry>();

        public static BotDeathReconcileNetwork Instance => _instance ?? (_instance = new BotDeathReconcileNetwork());

        public void Initialize(ManualLogSource logger)
        {
            _logger = logger;
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerDestroyedEvent>(OnNetworkManagerDestroyed);
            TryRefreshAvailability();
        }

        public void Shutdown()
        {
            FikaEventDispatcher.UnsubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            FikaEventDispatcher.UnsubscribeEvent<FikaNetworkManagerDestroyedEvent>(OnNetworkManagerDestroyed);
            _networkManager = null;
            _packetsRegistered = false;
            _clientReports.Clear();
        }

        public bool TryRefreshAvailability()
        {
            if (_networkManager == null)
            {
                _networkManager = GetNetworkManagerFromSingletons();
            }

            if (_networkManager == null)
            {
                return false;
            }

            if (!_packetsRegistered)
            {
                RegisterPackets();
            }

            return _packetsRegistered;
        }

        public bool IsCoopActive()
        {
            try
            {
                return TryRefreshAvailability();
            }
            catch
            {
                return false;
            }
        }

        public bool IsHost()
        {
            return IsCoopActive() && FikaBackendUtils.IsServer;
        }

        public bool IsRemoteClient()
        {
            return IsCoopActive() && FikaBackendUtils.IsClient;
        }

        public void SendClientReport(BotDeathSnapshot snapshot)
        {
            if (!IsRemoteClient() || _networkManager == null || snapshot == null || snapshot.Entries.Count == 0)
            {
                return;
            }

            var packet = new BotDeathReconcilePackets.BotDeathReportPacket
            {
                ReportSequence = ++_reportSequence
            };

            for (int i = 0; i < snapshot.Entries.Count && packet.Entries.Count < BotDeathReconcilePackets.MaxEntries; i++)
            {
                var entry = snapshot.Entries[i];
                packet.Entries.Add(new BotDeathReconcilePackets.BotDeathReportEntry
                {
                    NetId = entry.NetId,
                    IsAlive = entry.IsAlive,
                    Position = entry.Position
                });
            }

            SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public IEnumerable<BotDeathReconcilePackets.BotDeathReportEntry> ConsumeClientReports()
        {
            if (_clientReports.Count == 0)
            {
                yield break;
            }

            foreach (var pair in _clientReports)
            {
                yield return pair.Value;
            }

            _clientReports.Clear();
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent evt)
        {
            _networkManager = evt.Manager;
            RegisterPackets();
        }

        private void OnNetworkManagerDestroyed(FikaNetworkManagerDestroyedEvent evt)
        {
            _networkManager = null;
            _packetsRegistered = false;
            _clientReports.Clear();
            BotDeathAudioSilencer.Reset();
        }

        private IFikaNetworkManager GetNetworkManagerFromSingletons()
        {
            var fikaServer = Singleton<FikaServer>.Instance;
            if (fikaServer != null)
            {
                return fikaServer;
            }

            return Singleton<FikaClient>.Instance;
        }

        private void RegisterPackets()
        {
            if (_packetsRegistered || _networkManager == null)
            {
                return;
            }

            _networkManager.RegisterPacket<BotDeathReconcilePackets.BotDeathReportPacket>(OnReportPacketReceived);
            _packetsRegistered = true;
            _logger?.LogInfo("[BOT_RECONCILE] Fika packets registered");
        }

        private void OnReportPacketReceived(BotDeathReconcilePackets.BotDeathReportPacket packet)
        {
            if (!IsHost() || packet?.Entries == null)
            {
                return;
            }

            for (int i = 0; i < packet.Entries.Count; i++)
            {
                var entry = packet.Entries[i];
                if (entry == null || entry.IsAlive)
                {
                    continue;
                }

                _clientReports[entry.NetId] = entry;
            }
        }

        private void SendData<T>(ref T packet, DeliveryMethod deliveryMethod)
            where T : Fika.Core.Networking.LiteNetLib.Utils.INetSerializable
        {
            var fikaClient = Singleton<FikaClient>.Instance;
            fikaClient?.SendData(ref packet, deliveryMethod);
        }
    }
}
