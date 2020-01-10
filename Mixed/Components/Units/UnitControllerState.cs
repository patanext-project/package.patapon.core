using Revolution;
using Unity.Entities;
using Unity.Mathematics;

namespace Patapon.Mixed.Units
{
	public struct UnitControllerState : IComponentData
	{
		public bool3 ControlOverVelocity;
		public bool  PassThroughEnemies;

		public bool  OverrideTargetPosition;
		public float TargetPosition;

		internal float3 PreviousPosition;
		
		public class NetSynchronize : ComponentSnapshotSystemEmpty<UnitControllerState>
		{
			public override ComponentType ExcludeComponent => typeof(ExcludeFromTagging);
		}
	}
}