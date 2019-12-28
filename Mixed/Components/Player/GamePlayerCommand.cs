using System;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default.Player
{
	// similar to UserCommand except it's synced between all clients
	public unsafe struct GamePlayerCommand : IComponentData
	{
		public UserCommand Base { get; set; }

		public Span<UserCommand.RhythmAction> RhythmActions => Base.GetRhythmActions();

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<GamePlayerCommand>
		{
			public UserCommand Base;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Base.Tick = Tick;
				Base.WriteTo(writer, baseline.Base, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Base.Tick = Tick;
				Base.ReadFrom(reader, ref ctx, baseline.Base, compressionModel);
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				return UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref this), &baseline, sizeof(Snapshot)) != 0;
			}

			public void SynchronizeFrom(in GamePlayerCommand component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Base = component.Base;
			}

			public void SynchronizeTo(ref GamePlayerCommand component, in DeserializeClientData deserializeData)
			{
				throw new NotImplementedException("Should not be used here");
			}
		}
		
		public struct Exclude {}

		public class NetSynchronize : ComponentSnapshotSystemDelta<GamePlayerCommand, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
	public class SynchronizeGamePlayerCommandSystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var localFromEntity = GetComponentDataFromEntity<GamePlayerLocalTag>(true);

			inputDeps = Entities.ForEach((Entity entity, ref GamePlayerCommand command, in DynamicBuffer<GamePlayerCommand.Snapshot> snapshots) =>
			{
				if (localFromEntity.Exists(entity))
					return;

				command.Base = snapshots.GetLastBaseline().Base;
			}).WithReadOnly(localFromEntity).Schedule(inputDeps);

			return inputDeps;
		}
	}
}