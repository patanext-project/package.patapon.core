using package.patapon.core;
using Patapon4TLB.Default;
using Revolution;
using Unity.Entities;

namespace Patapon4TLBCore.Snapshots
{
	public class RhythmEngineSnapshotSystem
	{
		public struct Exclude : IComponentData
		{
		}

		public class SyncSettings : MixedComponentSnapshotSystem<RhythmEngineSettings, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class SyncProcess : MixedComponentSnapshotSystem<RhythmEngineProcess, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class SyncState : MixedComponentSnapshotSystem<RhythmEngineState, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class SyncCurrCmd : MixedComponentSnapshotSystem<RhythmCurrentCommand, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class SyncCombo : MixedComponentSnapshotSystem<GameComboState, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
		
		public class SyncCmd : MixedComponentSnapshotSystem<GameCommandState, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}