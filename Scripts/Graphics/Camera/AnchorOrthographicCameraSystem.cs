using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;

namespace package.patapon.core
{
    [UpdateAfter(typeof(PreLateUpdate))]
    public class AnchorOrthographicCameraSystem : ComponentSystem
    {
        public struct Group
        {
            public ComponentDataArray<AnchorOrthographicCameraData> Data;
            public ComponentDataArray<AnchorOrthographicCameraTarget> Targets;
            public ComponentDataArray<Position> Positions;
            public TransformAccessArray Transforms;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        protected override void OnUpdate()
        {
            new CalculatePositionJob().Run(this);
            for (int i = 0; i != m_Group.Length; i++)
            {
                m_Group.Transforms[i].position = m_Group.Positions[i].Value;
            }
        }

        [BurstCompile]
        public struct CalculatePositionJob : IJobProcessComponentData
            <AnchorOrthographicCameraData, AnchorOrthographicCameraTarget, Position>
        {
            public void Execute
            (
                [ReadOnly] ref  AnchorOrthographicCameraData   data,
                [ReadOnly] ref  AnchorOrthographicCameraTarget target,
                [WriteOnly] ref Position                       position
            )
            {
                var up   = math.float3(0, 1, 0) * (data.Anchor.y * data.Height);
                var left = math.float3(1, 0, 0) * (data.Anchor.x * data.Width);

                position.Value = target.Target + left + up;
            }
        }
    }
}