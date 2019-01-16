using Patapon4TLB.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace Patapon4TLB.Core.Tests
{
    public class PlayerInputDataStreamer : SnapshotEntityDataStreamer<PlayerInput>
    {
        
    }
    
    public struct PlayerInput : IStateData, IComponentData
    {
        public float2 Value;
    }
}