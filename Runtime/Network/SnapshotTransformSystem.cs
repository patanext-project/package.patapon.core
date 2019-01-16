using System;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Patapon4TLB.Core.Networking
{
    public class SnapshotEntityDataTransformSystem : SnapshotEntityDataStreamer<SnapshotEntityDataTransformSystem.State>
    {
        public struct State : IStateData, IComponentData
        {
            public half3 Position;
            public half3 Rotation;
        }
    }
}