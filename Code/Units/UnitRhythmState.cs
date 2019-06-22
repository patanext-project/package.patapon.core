using package.patapon.core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon4TLB.Core
{
	public struct UnitRhythmState : IComponentData
	{
		public GameComboState Combo;
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class UnitRhythmStateUpdateSystem : JobComponentSystem
	{
		private struct Job : IJobForEach<UnitRhythmState, Relative<RhythmEngineDescription>>
		{
			[ReadOnly]
			public ComponentDataFromEntity<GameComboState> ComboStateFromEntity;

			public void Execute(ref UnitRhythmState rhythmState, [ReadOnly] ref Relative<RhythmEngineDescription> rhythmEngineRelative)
			{
				rhythmState.Combo = ComboStateFromEntity[rhythmEngineRelative.Target];
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				ComboStateFromEntity = GetComponentDataFromEntity<GameComboState>(true),
			}.Schedule(this, inputDeps);
		}
	}
}