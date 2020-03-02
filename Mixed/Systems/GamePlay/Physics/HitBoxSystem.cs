using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Units;
using Patapon.Mixed.Utilities;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace Patapon.Mixed.GamePlay.Physics
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[AlwaysSynchronizeSystem]
	public class DisableHitBoxSystem : AbsGameBaseSystem
	{
		private EndSimulationEntityCommandBufferSystem m_EndBuffer;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_EndBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			var ecb  = m_EndBuffer.CreateCommandBuffer();
			var tick = ServerTick;

			Entities.ForEach((Entity entity, ref HitBox hitBox, ref DynamicBuffer<HitBoxHistory> history, in PhysicsCollider collider) =>
			{
				if (hitBox.DisableAt.Value > 0 && hitBox.DisableAt <= tick)
				{
					history.Clear();
					EntityManager.AddComponent<Disabled>(entity);
				}
			}).WithStructuralChanges().Run();
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[UpdateAfter(typeof(DisableHitBoxSystem))]
	public unsafe class HitBoxAgainstEnemiesSystem : SystemBase
	{
		private TargetDamageEvent.Provider m_DamageEventProvider;

		public struct Payload
		{
			[ReadOnly] public BufferFromEntity<TeamEnemies>         TeamEnemies;
			[ReadOnly] public BufferFromEntity<TeamEntityContainer> EntityContainer;

			[ReadOnly] public BufferFromEntity<HitShapeContainer>      HitShapeContainer;
			[ReadOnly] public ComponentDataFromEntity<LocalToWorld>    LocalToWorld;
			[ReadOnly] public ComponentDataFromEntity<Translation>     Translation;
			[ReadOnly] public ComponentDataFromEntity<PhysicsCollider> PhysicsCollider;

			[ReadOnly] public ComponentDataFromEntity<DamageFromStatisticFrame> DamageFromStatistic;
			[ReadOnly] public ComponentDataFromEntity<UnitPlayState>            PlayState;
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			m_DamageEventProvider = World.GetOrCreateSystem<TargetDamageEvent.Provider>();
		}

		protected override void OnUpdate()
		{
			var payload = new Payload
			{
				TeamEnemies     = GetBufferFromEntity<TeamEnemies>(true),
				EntityContainer = GetBufferFromEntity<TeamEntityContainer>(true),

				HitShapeContainer = GetBufferFromEntity<HitShapeContainer>(true),
				LocalToWorld      = GetComponentDataFromEntity<LocalToWorld>(true),
				Translation       = GetComponentDataFromEntity<Translation>(true),
				PhysicsCollider   = GetComponentDataFromEntity<PhysicsCollider>(true),

				DamageFromStatistic = GetComponentDataFromEntity<DamageFromStatisticFrame>(true),
				PlayState           = GetComponentDataFromEntity<UnitPlayState>(true)
			};

			var damageEvArchetype = m_DamageEventProvider.EntityArchetype;
			var ecb               = m_DamageEventProvider.CreateEntityCommandBuffer();

			Entities.ForEach((Entity entity, ref HitBox hitBox, ref DynamicBuffer<HitBoxHistory> history, in HitBoxAgainstEnemies against, in Translation translation, in PhysicsCollider collider) =>
			{
				if (!payload.TeamEnemies.Exists(against.AllyBufferSource))
					return;

				if (hitBox.MaxHits > 0 && history.Length >= hitBox.MaxHits)
					return;

				var distanceInput = CreateDistanceFlatInput.ColliderWithOffset(collider.ColliderPtr, translation.Value.xy, float2.zero);
				var hasStatistic  = payload.DamageFromStatistic.TryGet(entity, out var frame);
				var resultStat    = frame.Value;
				if (hasStatistic && payload.PlayState.TryGet(frame.UseValueFrom, out resultStat))
						frame.Modifier.Multiply(ref resultStat);

				var teamEnemyBuffer = payload.TeamEnemies[against.AllyBufferSource];
				for (var i = 0; i != teamEnemyBuffer.Length; i++)
				{
					var entityContainer = payload.EntityContainer[teamEnemyBuffer[i].Target].AsNativeArray();
					var rigidBodies     = new NativeList<RigidBody>(entityContainer.Length, Allocator.Temp);
					for (var ent = 0; ent != entityContainer.Length; ent++)
					{
						if (history.Reinterpret<Entity>()
						           .AsNativeArray()
						           .Contains(entityContainer[ent].Value)
						    || !payload.HitShapeContainer.Exists(entityContainer[ent].Value))
							continue;

						rigidBodies.Clear();
						CreateRigidBody.Execute(ref rigidBodies, payload.HitShapeContainer[entityContainer[ent].Value].AsNativeArray(),
							entityContainer[ent].Value,
							payload.LocalToWorld, payload.Translation, payload.PhysicsCollider);

						for (var rb = 0; rb != rigidBodies.Length; rb++)
						{
							var cc = new CustomCollide(rigidBodies[rb]) {WorldFromMotion = {pos = {z = 0}}};
							if (!new CustomCollideCollection(ref cc).CalculateDistance(distanceInput, out var closestHit))
								continue;

							history.Add(new HitBoxHistory {Entity = entityContainer[ent].Value});

							var evEnt = ecb.CreateEntity(damageEvArchetype);
							ecb.SetComponent(evEnt, new TargetDamageEvent
							{
								Origin = hitBox.Source, Destination = entityContainer[ent].Value, Damage = -resultStat.Attack
							});
							ecb.AddComponent(evEnt, new Translation {Value = closestHit.Position});
							if (hasStatistic)
								ecb.AddComponent(evEnt, new DamageResultFrame {PlayState = resultStat});
							Debug.Log($"atk={resultStat.Attack}");
							
							break;
						}

						if (hitBox.MaxHits > 0 && history.Length >= hitBox.MaxHits)
							break;
					}
				}
			}).Schedule();

			m_DamageEventProvider.AddJobHandleForProducer(Dependency);
		}
	}

	public struct HitBox : IComponentData
	{
		public Entity Source;
		public int    MaxHits;
		public UTick  DisableAt;

		public struct Create
		{
			public Entity                       Source;
			public BlobAssetReference<Collider> Collider;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(HitBox),
					typeof(HitBoxHistory),
					typeof(PhysicsCollider),
					typeof(Translation),
					typeof(LocalToWorld)
				};
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				Debug.Assert(data.Collider.IsCreated, "data.Collider.IsValid");

				EntityManager.SetComponentData(entity, new HitBox {Source = data.Source});
				EntityManager.SetComponentData(entity, new PhysicsCollider {Value = data.Collider});
			}
		}
	}

	[InternalBufferCapacity(32)]
	public struct HitBoxHistory : IBufferElementData
	{
		public Entity Entity;
	}

	public struct HitBoxAgainstEnemies : IComponentData
	{
		public Entity AllyBufferSource;
	}
}