using System.Collections.Generic;
using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace FikaBotDeathReconcile
{
    internal static class BotDeathReconcilePackets
    {
        internal const int MaxEntries = 96;

        internal sealed class BotDeathReportEntry
        {
            public int NetId;
            public bool IsAlive;
            public Vector3 Position;
        }

        internal sealed class BotDeathReportPacket : INetSerializable
        {
            public long ReportSequence;
            public readonly List<BotDeathReportEntry> Entries = new List<BotDeathReportEntry>();

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(ReportSequence);
                writer.Put(Entries.Count);

                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];
                    writer.Put(entry.NetId);
                    writer.Put(entry.IsAlive);
                    writer.Put(entry.Position.x);
                    writer.Put(entry.Position.y);
                    writer.Put(entry.Position.z);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                ReportSequence = reader.GetLong();
                Entries.Clear();

                int count = reader.GetInt();
                if (count < 0)
                {
                    count = 0;
                }

                if (count > MaxEntries)
                {
                    count = MaxEntries;
                }

                for (int i = 0; i < count; i++)
                {
                    Entries.Add(new BotDeathReportEntry
                    {
                        NetId = reader.GetInt(),
                        IsAlive = reader.GetBool(),
                        Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat())
                    });
                }
            }
        }
    }
}
