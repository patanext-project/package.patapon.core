using System;
using Components.GamePlay.Projectiles;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.Projectiles;
using Patapon.Mixed.Units;
using Patapon4TLB.Core.Snapshots;
using Revolution;
using Stormium.Core.Projectiles;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems.GamePlay.CYari
{
	public struct CannonProjectile : IComponentData
	{
		public class NetSynchronize : ComponentSnapshotSystemTag<CannonProjectile> {}
		
		public float  DetectionRadius;
		public float3 Gravity;

		public struct Create
		{
			public Entity Owner;

			public float3 Position;
			public float3 Velocity;
			public float3 Gravity;

			public int StartDamage;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(ProjectileDescription),
					typeof(CannonProjectile),
					typeof(SVelocity),
					typeof(Translation),
					typeof(LocalToWorld),

					typeof(DamageFrame),

					typeof(ProjectileDefaultExplosion),
					typeof(ProjectileAgeTime),
					typeof(DistanceDamageFallOf),
					typeof(DistanceImpulseFallOf),
					typeof(GhostEntity)
				};
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				if (data.Owner == default)
					throw new ArgumentException(nameof(data.Owner));
				if (entity == default)
					throw new ArgumentException(nameof(entity));

				var tick = GetTick(true);

				EntityManager.ReplaceOwnerData(entity, data.Owner);
				EntityManager.SetComponentData(entity, new Translation {Value                = data.Position});
				EntityManager.SetComponentData(entity, new SVelocity {Value                   = data.Velocity});
				EntityManager.SetComponentData(entity, new CannonProjectile {DetectionRadius = 0.25f, Gravity  = data.Gravity});
				EntityManager.SetComponentData(entity, new ProjectileAgeTime {StartMs        = tick.Ms, EndMs = tick.Ms + 3000});

				EntityManager.SetComponentData(entity, new DamageFrame {Damage = data.StartDamage});

				EntityManager.SetComponentData(entity, new ProjectileDefaultExplosion
				{
					DamageRadius = 0.75f,
					BumpRadius   = 0.01f,

					MinDamage = -1,
					MaxDamage = -1,

					HorizontalImpulseMin = 2.5f,
					HorizontalImpulseMax = 10f,

					VerticalImpulseMin = 2.5f,
					VerticalImpulseMax = 16f,

					SelfImpulseFactor = 0.5f
				});

				var dmgFallOf = EntityManager.GetBuffer<DistanceDamageFallOf>(entity);
				dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(1.0f, 1.0f));

				var impFallOf = EntityManager.GetBuffer<DistanceImpulseFallOf>(entity);
				impFallOf.Add(DistanceImpulseFallOf.FromPercentage(1.0f, 1.0f));
			}
		}
	}
}