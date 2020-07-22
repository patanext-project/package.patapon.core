using Patapon.Client.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
	public struct ShooterProjectilePrediction : IComponentData
	{
		public RigidTransform Transform;
	}

	public struct ShooterProjectileVisualTarget : IComponentData
	{
		public VisualThrowableDefinition Definition;
	}
}