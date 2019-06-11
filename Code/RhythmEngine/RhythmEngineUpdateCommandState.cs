using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class RhythmEngineUpdateCommandState : JobGameBaseSystem
	{
		private struct Job : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, FlowRhythmEngineProcess, FlowCurrentCommand>
		{
			public bool IsServer;

			[ReadOnly]
			public ComponentDataFromEntity<FlowCommandData> CommandDataFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<FlowRhythmEngineSimulateTag> SimulateTagFromEntity;

			public void Execute(Entity entity, int index, ref RhythmEngineSettings settings, ref RhythmEngineState state, ref FlowRhythmEngineProcess process, ref FlowCurrentCommand flow)
			{
				if (state.IsPaused
				    || (!IsServer && settings.UseClientSimulation && !SimulateTagFromEntity.Exists(entity)))
					return;

				if (flow.CommandTarget == default)
					return;

				var commandData = CommandDataFromEntity[flow.CommandTarget];
				var isActive =
					// check start
					(flow.ActiveAtBeat < 0 || flow.ActiveAtBeat <= process.Beat)
					// check end
					&& (flow.CustomEndBeat == -2
					    || (flow.ActiveAtBeat >= 0 && flow.ActiveAtBeat + commandData.BeatLength >= process.Beat)
					    || flow.CustomEndBeat >= process.Beat)
					// if both are set to no effect, then the command is not active
					&& flow.ActiveAtBeat != 1 && flow.CustomEndBeat != 1;

				if (!isActive)
				{
					flow.CommandTarget = default;
					flow.ActiveAtBeat  = -1;
					flow.CustomEndBeat = -1;
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				IsServer              = IsServer,
				CommandDataFromEntity = GetComponentDataFromEntity<FlowCommandData>(),
				SimulateTagFromEntity = GetComponentDataFromEntity<FlowRhythmEngineSimulateTag>(),
			}.Schedule(this, inputDeps);

			return inputDeps;
		}
	}
}