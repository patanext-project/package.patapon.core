using Unity.Entities;

namespace Patapon4TLB.Core
{
	public struct UnitControllerState : IComponentData
	{
		public bool ControlOverVelocity;
		public bool PassThroughEnemies;
	}
}