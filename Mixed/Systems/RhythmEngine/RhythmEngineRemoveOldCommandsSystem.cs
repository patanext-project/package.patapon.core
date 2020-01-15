using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon.Mixed.Systems
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(RhythmEngineCheckCommandValidity))]
	public class RhythmEngineRemoveOldCommandsSystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var isServer = IsServer;
			
			inputDeps =
				Entities
					.WithAll<FlowSimulateProcess>()
					.ForEach((ref DynamicBuffer<RhythmEngineCommandProgression> progression, in FlowEngineProcess process, in RhythmEngineSettings settings, in RhythmEngineState state) =>
					{
						var flowBeat = process.GetFlowBeat(settings.BeatInterval);
						var mercy = isServer ? 1 : 0;
						
						for (var j = 0; j != progression.Length; j++)
						{
							var currCommand = progression[j];
							if (flowBeat >= currCommand.Data.RenderBeat + mercy + settings.MaxBeats
							    || state.IsRecovery(flowBeat))
							{
								//Debug.Log("Removed");
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