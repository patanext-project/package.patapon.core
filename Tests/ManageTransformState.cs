using Patapon4TLB.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Patapon4TLB.Core.Tests
{
    [UpdateInGroup(typeof(UpdateLoop.ReadStates))]
    public class ReadTransformStateSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref TransformState state, ref Position position, ref Rotation rotation) =>
            {
                position.Value = state.Position;
                rotation.Value = quaternion.Euler(state.Rotation);
            });
        }
    }
    
    [UpdateInGroup(typeof(UpdateLoop.WriteStates))]
    public class WriteTransformStateSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            ForEach((ref TransformState state, ref Position position, ref Rotation rotation) =>
            {
                state.Position = position.Value;
                state.Rotation = math.mul(rotation.Value, math.float3(1));
            });
        }
    }
}