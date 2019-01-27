using System;
using Unity.Entities;

namespace StormiumShared.Core.Networking
{
    [Flags]
    public enum SnapshotReceiverFlags
    {
        None = 0,
        FullData = 1,
        Local = 2,
        FullDataAndLocal = FullData | Local
    }
    
    public struct SnapshotReceiver
    {
        public Entity Client;
        public SnapshotReceiverFlags Flags;

        public SnapshotReceiver(Entity client, SnapshotReceiverFlags flags)
        {
            Client = client;
            Flags = flags;
        }
    }

    public struct SnapshotSender
    {
        public Entity Client;
    }
}