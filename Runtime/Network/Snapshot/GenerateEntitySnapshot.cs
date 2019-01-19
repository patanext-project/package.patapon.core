using Unity.Entities;

namespace Patapon4TLB.Core.Networking
{
    public struct ModelIdent : IComponentData
    {
        public int Id;

        public ModelIdent(int id)
        {
            Id = id;
        }
    }

    public struct GenerateEntitySnapshot : IComponentData
    {
        
    }
}