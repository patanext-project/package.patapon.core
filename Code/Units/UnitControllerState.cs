using Unity.Entities;
using Unity.Mathematics;

namespace Patapon4TLB.Core
{
	public struct UnitControllerState : IComponentData
	{
		public bool ControlOverVelocity;
		public bool PassThroughEnemies;

		public bool   OverrideTargetPosition;
		public float3 TargetPosition;
	}
}