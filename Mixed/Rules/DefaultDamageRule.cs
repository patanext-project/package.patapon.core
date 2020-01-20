using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace StormiumTeam.GameBase
{
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	[AlwaysSynchronizeSystem]
	public class DefaultDamageRule : RuleBaseSystem
	{
		public RuleProperties<Data>                CustomProperties;
		public RuleProperties<Data>.Property<bool> DisableEventForNoDamageProperty;

		private EntityQuery                          m_EntityQuery;
		private EntityArchetype                      m_ModifyHealthArchetype;
		public  RuleProperties<Data>.Property<float> SelfDamageFactorProperty;
		public  RuleProperties<Data>.Property<float> TeamDamageFactorProperty;

		public override string Name        => "Default Damage Rule";
		public override string Description => "Automatically manage the damage events.";

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EntityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(GameEvent), typeof(TargetDamageEvent)}
			});
			m_ModifyHealthArchetype = EntityManager.CreateArchetype(typeof(ModifyHealthEvent));

			CustomProperties = AddRule<Data>(out var data);

			SelfDamageFactorProperty        = CustomProperties.Add("Self damage factor", ref data, ref data.SelfDamageFactor);
			TeamDamageFactorProperty        = CustomProperties.Add("Team damage factor", ref data, ref data.SameTeamDamageFactor);
			DisableEventForNoDamageProperty = CustomProperties.Add("Disable event if no damage were dealt", ref data, ref data.DisableEventForNoDamage);

			SelfDamageFactorProperty.Value = 0.25f;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			new JobCreateEvents
			{
				Data                    = GetSingleton<Data>(),
				Tick                    = GetTick(true),
				Ecb                     = GetCommandBuffer().ToConcurrent(),
				ModifyHealthArchetype   = m_ModifyHealthArchetype,
				TeamOwnerFromEntity     = GetComponentDataFromEntity<Relative<TeamDescription>>(),
				HealthHistoryFromEntity = GetBufferFromEntity<HealthModifyingHistory>()
			}.Run(m_EntityQuery);

			AddJobHandleForProducer(inputDeps);

			return default;
		}

		public struct Data : IComponentData
		{
			public float SelfDamageFactor;
			public float SameTeamDamageFactor;
			public bool  DisableEventForNoDamage;
		}

		private struct JobCreateEvents : IJobForEachWithEntity<TargetDamageEvent>
		{
			public Data  Data;
			public UTick Tick;

			public EntityCommandBuffer.Concurrent Ecb;
			public EntityArchetype                ModifyHealthArchetype;

			[ReadOnly]
			public ComponentDataFromEntity<Relative<TeamDescription>> TeamOwnerFromEntity;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<HealthModifyingHistory> HealthHistoryFromEntity;

			public void Execute(Entity entity, int index, ref TargetDamageEvent damageEvent)
			{
				var shooterTeam = TeamOwnerFromEntity.Exists(damageEvent.Origin) ? TeamOwnerFromEntity[damageEvent.Origin].Target : default;
				var victimTeam  = TeamOwnerFromEntity.Exists(damageEvent.Destination) ? TeamOwnerFromEntity[damageEvent.Destination].Target : default;

				if (damageEvent.Origin == damageEvent.Destination && math.abs(Data.SelfDamageFactor) > math.FLT_MIN_NORMAL && damageEvent.Damage < 0)
					damageEvent.Damage = (int) math.round(damageEvent.Damage * Data.SelfDamageFactor);
				else if (shooterTeam != default && victimTeam != default && shooterTeam == victimTeam && math.abs(Data.SameTeamDamageFactor) > math.FLT_MIN_NORMAL && damageEvent.Damage < 0)
					damageEvent.Damage = (int) math.round(damageEvent.Damage * Data.SameTeamDamageFactor);
				
				if (damageEvent.Damage == 0 && Data.DisableEventForNoDamage)
				{
					Ecb.DestroyEntity(index, entity);
					return;
				}

				if (HealthHistoryFromEntity.Exists(damageEvent.Destination))
					HealthHistoryFromEntity[damageEvent.Destination].Add(new HealthModifyingHistory
					{
						Instigator = damageEvent.Origin,
						Value      = damageEvent.Damage,
						Tick       = Tick
					});

				var healthEvent = Ecb.CreateEntity(index, ModifyHealthArchetype);
				Ecb.SetComponent(index, healthEvent, new ModifyHealthEvent(ModifyHealthType.Add, damageEvent.Damage, damageEvent.Destination));
			}
		}
	}
}