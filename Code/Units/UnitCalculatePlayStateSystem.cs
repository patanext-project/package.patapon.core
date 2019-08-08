using package.patapon.core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Core
{
	[UpdateAfter(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(ActionSystemGroup))]
	public class UnitInitStateSystemGroup : ComponentSystemGroup
	{}
	
	[UpdateInGroup(typeof(UnitInitStateSystemGroup))]
	public class UnitCalculatePlayStateSystem : JobComponentSystem
	{
		private struct JobUpdate : IJobForEach<UnitStatistics, UnitPlayState, Relative<RhythmEngineDescription>>
		{
			[ReadOnly] public ComponentDataFromEntity<GameComboState> ComboStateFromEntity;

			public void Execute(ref UnitStatistics settings, ref UnitPlayState state, ref Relative<RhythmEngineDescription> rhythmEngineRelative)
			{
				var comboState = ComboStateFromEntity[rhythmEngineRelative.Target];

				state.MovementSpeed = comboState.IsFever ? settings.FeverWalkSpeed : settings.BaseWalkSpeed;
				if (comboState.IsFever && comboState.Score >= 50)
				{
					state.MovementSpeed += state.MovementSpeed * 0.2f;
				}

				state.MovementAttackSpeed = settings.MovementAttackSpeed;
				if (comboState.IsFever)
				{
					state.MovementAttackSpeed += state.MovementAttackSpeed * 0.8f;
					if (comboState.Score >= 50)
						state.MovementAttackSpeed += state.MovementAttackSpeed * 0.2f;
				}
				
				state.Weight              = settings.Weight;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new JobUpdate
			{
				ComboStateFromEntity = GetComponentDataFromEntity<GameComboState>(true)
			}.Schedule(this, inputDeps);
		}
	}
}