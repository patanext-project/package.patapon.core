using System;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StormiumShared.Core.Networking
{
    public class TransformStateDataStreamer : SnapshotEntityDataStreamer<TransformState>
    {
    }

    public struct TransformState : IStateData, IComponentData
    {
        public half3 Position;
        public half3 Rotation;
    }
}