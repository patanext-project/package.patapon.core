using Unity.Entities;
using Unity.Mathematics;

namespace package.patapon.core
{
    public struct CameraTargetData : IComponentData
    {
        /// <summary>
        /// The camera target
        /// </summary>
        public Entity CameraId;
        /// <summary>
        /// The priority of our work
        /// </summary>
        public int Priority;

        public CameraTargetData(Entity cameraId, int priority)
        {
            CameraId = cameraId;
            Priority = priority;
        }
    }
    
    public struct CameraTargetPosition : IComponentData
    {
        /// <summary>
        /// The target position
        /// </summary>
        public float2 Value;

        public CameraTargetPosition(float2 position)
        {
            Value = position;
        }
    }

    public struct CameraTargetAnchor : IComponentData
    {
        /// <summary>
        /// The type of our anchor to be used
        /// </summary>
        public AnchorType Type;
        /// <summary>
        /// The anchor value, world (value between -infinity;+infinity) or screen space (values between 0;1)
        /// </summary>
        public float2 Value;

        public CameraTargetAnchor(AnchorType type, float2 value)
        {
            Type = type;
            Value = value;
        }
    }
}