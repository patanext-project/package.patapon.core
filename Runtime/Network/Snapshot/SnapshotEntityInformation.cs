using Unity.Entities;

namespace Patapon4TLB.Core.Networking
{
    public struct SnapshotEntityInformation
    {
        public void Deconstruct(out Entity source, out int modelId)
        {
            source  = Source;
            modelId = ModelId;
        }

        public Entity Source;
        public int ModelId;

        public SnapshotEntityInformation(Entity source, int modelId)
        {
            Source = source;
            ModelId = modelId;
        }
    }
}