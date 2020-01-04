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
		[UpdateAfter(typeof(UpdateRhythmAbilityState))]
		[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
		public class LocalUpdate : JobComponentSystem
		{
			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				var engineProcessFromEntity = GetComponentDataFromEntity<FlowEngineProcess>(true);

				return Entities.ForEach((ref DefaultRetreatAbility ability, in RhythmAbilityState state) =>
				{
					if (state.IsActive || state.IsStillChaining)
					{
						ability.ActiveTime   = (engineProcessFromEntity[state.Engine].Milliseconds - state.StartTime) * 0.001f;
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
	}
}