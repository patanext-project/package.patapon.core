using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Systems;
using Revolution;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct DefaultRetreatAbility : IReadWriteComponentSnapshot<DefaultRetreatAbility>
	{
		public const float StopTime      = 1.5f;
		public const float MaxActiveTime = StopTime + 0.5f;

		public int LastActiveId;

		public float  AccelerationFactor;
		public float3 StartPosition;
		public float  BackVelocity;
		public bool   IsRetreating;
		public float  ActiveTime;

		public void WriteTo(DataStreamWriter writer, ref DefaultRetreatAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref DefaultRetreatAbility baseline, DeserializeClientData jobData)
		{
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystem<DefaultRetreatAbility, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
		[UpdateAfter(typeof(RhythmEngineGroup))]
		[UpdateAfter(typeof(GhostSimulationSystemGroup))]
		[UpdateAfter(typeof(UpdateAbilityRhythmStateSystem))]
		[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
		public class LocalUpdate : JobComponentSystem
		{
			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				var engineProcessFromEntity = GetComponentDataFromEntity<FlowEngineProcess>(true);

				return Entities.ForEach((ref DefaultRetreatAbility ability, in AbilityState controller, in AbilityEngineSet engineSet) =>
				{
					if ((controller.Phase & EAbilityPhase.ActiveOrChaining) != 0)
					{
						ability.ActiveTime   = (engineProcessFromEntity[engineSet.Engine].Milliseconds - engineSet.CommandState.StartTime) * 0.001f;
						ability.IsRetreating = ability.ActiveTime <= MaxActiveTime;
					}
					else
					{
						ability.ActiveTime   = 0.0f;
						ability.IsRetreating = false;
					}
				}).WithReadOnly(engineProcessFromEntity).Schedule(inputDeps);
			}
		}
	}

	public class RetreatAbilityProvider : BaseRhythmAbilityProvider<DefaultRetreatAbility>
	{
		public const string MapPath = "retreat_data";

		public override string MasterServerId  => nameof(P4OfficialAbilities.BasicRetreat);
		public override Type   ChainingCommand => typeof(RetreatCommand);

		public override void SetEntityData(Entity entity, CreateAbility data)
		{
			base.SetEntityData(entity, data);
			EntityManager.SetComponentData(entity, GetValue(MapPath, new DefaultRetreatAbility {AccelerationFactor = 1}));
		}
	}
}