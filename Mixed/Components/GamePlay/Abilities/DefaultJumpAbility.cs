using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Systems;
using Revolution;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct DefaultJumpAbility : IComponentData
	{
		public int LastActiveId;

		public bool  IsJumping;
		public float ActiveTime;

		public struct Exclude : IComponentData
		{
		}

		// all of the things here are already predicted from the client...
		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<DefaultJumpAbility>
		{
			public int foo;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				return false;
			}

			public void SynchronizeFrom(in DefaultJumpAbility component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
			}

			public void SynchronizeTo(ref DefaultJumpAbility component, in DeserializeClientData deserializeData)
			{
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemBasic<DefaultJumpAbility, Snapshot>
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

				return Entities.ForEach((ref DefaultJumpAbility ability, in RhythmAbilityState state) =>
				{
					if (state.IsActive || state.IsStillChaining)
					{
						ability.ActiveTime = (engineProcessFromEntity[state.Engine].Milliseconds - state.StartTime) * 0.001f;
						ability.IsJumping  = ability.ActiveTime <= 0.5f;
					}
					else
					{
						ability.ActiveTime = 0.0f;
						ability.IsJumping  = false;
					}
				}).WithReadOnly(engineProcessFromEntity).Schedule(inputDeps);
			}
		}
	}

	public class DefaultJumpAbilityProvider : BaseRhythmAbilityProvider<DefaultJumpAbility>
	{
	}
}