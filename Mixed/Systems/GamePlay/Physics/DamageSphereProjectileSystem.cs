using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Mixed.GamePlay.Physics
{
	[UpdateInGroup(typeof(ProjectilePhysicIterationSystemGroup))]
	public class DamageSphereProjectileSystem : AbsGameBaseSystem
	{
		protected override void OnUpdate()
		{
			var dt = Time.DeltaTime;
			Entities.ForEach((ref Translation translation, ref Velocity velocity, in DamageSphereProjectile projectile) =>
			{
				Debug.DrawRay(translation.Value, velocity.Value * dt, Color.green, 0.25f);
				velocity.Value    += projectile.Gravity * dt;
				translation.Value += velocity.Value * dt;
			}).Schedule();
		}
	}

	public struct DamageSphereProjectile : IComponentData
	{
		public float3 Gravity;
	}

	public class DamageSphereProjectileProvider : BaseProviderBatch<DamageSphereProjectileProvider.Create>
	{
		public struct Create
		{
			public float3 Position;
			public float3 Velocity;
			public float3 Gravity;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ProjectileDescription),
				typeof(Translation),
				typeof(LocalToWorld),
				typeof(Velocity),
				typeof(DamageSphereProjectile),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new Translation {Value        = data.Position});
			EntityManager.SetComponentData(entity, new Velocity {Value           = data.Velocity});
			EntityManager.SetComponentData(entity, new DamageSphereProjectile {Gravity = data.Gravity});
		}
	}
}