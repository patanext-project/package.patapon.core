using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	public class RhythmEngineClientSimulateLocalSystem : JobGameBaseSystem
	{
		private struct SimulateJob : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, FlowRhythmEngineProcess>
		{
			public void Execute(Entity entity, int index, ref RhythmEngineSettings settings, ref RhythmEngineState state, ref FlowRhythmEngineProcess process)
			{
				process.Beat = (int)(process.Time % settings.BeatInterval);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new SimulateJob
			{

			}.Schedule(this, inputDeps);
		}
	}
}