using Patapon4TLB.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Patapon4TLB.Core.Tests
{
    [UpdateInGroup(typeof(UpdateLoop.ReadStates))]
    public class ReadTransformStateSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            m_Group = GetComponentGroup(typeof(Position), typeof(Rotation), typeof(TransformState));
        }

        protected override void OnUpdate()
        {
            var length        = m_Group.CalculateLength();
            var positionArray = m_Group.GetComponentDataArray<Position>();
            var rotationArray = m_Group.GetComponentDataArray<Rotation>();
            var stateArray    = m_Group.GetComponentDataArray<TransformState>();

            for (var i = 0; i != length; i++)
            {
                positionArray[i] = new Position {Value = stateArray[i].Position};
                rotationArray[i] = new Rotation {Value = quaternion.Euler(stateArray[i].Rotation)};
            }
        }
    }
    
    [UpdateInGroup(typeof(UpdateLoop.WriteStates))]
    public class WriteTransformStateSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            m_Group = GetComponentGroup(typeof(Position), typeof(Rotation), typeof(TransformState));
        }

        protected override void OnUpdate()
        {
            var length = m_Group.CalculateLength();
            var positionArray = m_Group.GetComponentDataArray<Position>();
            var rotationArray = m_Group.GetComponentDataArray<Rotation>();
            var stateArray = m_Group.GetComponentDataArray<TransformState>();

            for (var i = 0; i != length; i++)
            {
                stateArray[i] = new TransformState
                {
                    Position = positionArray[i].Value, 
                    Rotation = math.mul(rotationArray[i].Value, math.float3(1))
                };
            }
        }
    }
}