using PataNext.Client.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace PataNext.Client.Components
{
	public struct ShooterProjectilePrediction : IComponentData
	{
		public RigidTransform Transform;
	}

	public struct ShooterProjectileVisualTarget : IComponentData
	{
		public EntityVisualDefinition Definition;
	}
}