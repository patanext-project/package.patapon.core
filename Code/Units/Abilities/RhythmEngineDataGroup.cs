using package.patapon.core;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct RhythmEngineDataGroup
	{
		public struct Result
		{
			public Entity               Entity;
			public RhythmCurrentCommand CurrentCommand;
			public GameCommandState     CommandState;
			public GameComboState       ComboState;
			public RhythmEngineProcess  EngineProcess;
		}

		[ReadOnly] public ComponentDataFromEntity<RhythmCurrentCommand> CurrentCommandFromEntity;
		[ReadOnly] public ComponentDataFromEntity<GameCommandState>     CommandStateFromEntity;
		[ReadOnly] public ComponentDataFromEntity<GameComboState>       ComboStateFromEntity;
		[ReadOnly] public ComponentDataFromEntity<RhythmEngineProcess>  EngineProcessFromEntity;

		public RhythmEngineDataGroup(ComponentSystemBase system)
		{
			CurrentCommandFromEntity = system.GetComponentDataFromEntity<RhythmCurrentCommand>(true);
			CommandStateFromEntity   = system.GetComponentDataFromEntity<GameCommandState>(true);
			ComboStateFromEntity     = system.GetComponentDataFromEntity<GameComboState>(true);
			EngineProcessFromEntity  = system.GetComponentDataFromEntity<RhythmEngineProcess>(true);
		}

		public Result GetResult(Entity e)
		{
			return new Result
			{
				Entity         = e,
				CurrentCommand = CurrentCommandFromEntity[e],
				CommandState   = CommandStateFromEntity[e],
				ComboState     = ComboStateFromEntity[e],
				EngineProcess  = EngineProcessFromEntity[e],
			};
		}
	}
}