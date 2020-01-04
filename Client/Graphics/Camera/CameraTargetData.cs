using Unity.Entities;
using Unity.Mathematics;

namespace package.patapon.core
{
	public struct CameraTargetAnchor : IComponentData
	{
        /// <summary>
        ///     The type of our anchor to be used
        /// </summary>
        public AnchorType Type;

        /// <summary>
        ///     The anchor value, world (value between -infinity;+infinity) or screen space (values between 0;1)
        /// </summary>
        public float2 Value;

		public CameraTargetAnchor(AnchorType type, float2 value)
		{
			Type  = type;
			Value = value;
		}
	}
}