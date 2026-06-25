using UnityEngine;

namespace FikaBotDeathReconcile
{
    internal sealed class BotDeathEntry
    {
        public int NetId;
        public bool IsAlive;
        public Vector3 Position;
        public string ProfileId;
        public string Nickname;
    }

    internal sealed class BotDeathSnapshot
    {
        public readonly System.Collections.Generic.List<BotDeathEntry> Entries = new System.Collections.Generic.List<BotDeathEntry>();
    }
}
