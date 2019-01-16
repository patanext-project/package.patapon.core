using Unity.Entities;

namespace Patapon4TLB.Core.Networking
{
    public struct GenerateEntitySnapshot : IComponentData
    {
        public int ModelId;
    }
}