using Revolution;
using Unity.Entities;
using Unity.Mathematics;

namespace GameModes.VSHeadOn
{
	public struct HeadOnSpectating : IComponentData
	{
		public float Velocity;
		public float Position;
		public Entity CurrentTarget;

		public class NetSynchronizer : ComponentSnapshotSystemTag<HeadOnSpectating> {}
	}
}