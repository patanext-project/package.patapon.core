using Unity.Entities;

namespace StormiumShared.Core.Networking
{
    public struct NetworkClient : IComponentData
    {
        public long Id;
    }

    public struct NetworkLocalTag : IComponentData
    {
    }
} 