using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon.Mixed.Systems
{
	public class RhythmEngineRemoveOldCommandsSystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps =
				Entities
					.WithAll<FlowSimulateProcess>()
					.ForEach((ref DynamicBuffer<RhythmEngineCommandProgression> progression, in FlowEngineProcess process, in RhythmEngineSettings settings, in RhythmEngineState state) =>
					{
						var flowBeat = process.GetFlowBeat(settings.BeatInterval);

						for (var j = 0; j != progression.Length; j++)
						{
							var currCommand = progression[j];
							if (flowBeat > currCommand.Data.RenderBeat + 1 + settings.MaxBeats
							    || state.IsRecovery(flowBeat))
							{
								progression.RemoveAt(j);
								j--; // swap back method.
							}
						}
					})
					.Schedule(inputDeps);

			return inputDeps;

		}
	}
}