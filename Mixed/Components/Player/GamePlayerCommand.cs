using System;
using Revolution;
using Scripts.Utilities;
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
		public UserCommand Base;

		// there seems to be a bug on mono backend with Span on outer struct.
		//public Span<UserCommand.RhythmAction> RhythmActions => Base.GetRhythmActions();

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<GamePlayerCommand>
		{
			public UserCommand Base;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedUInt(Base.Tick, compressionModel);
				Base.WriteTo(writer, baseline.Base, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Base.Tick = reader.ReadPackedUInt(ref ctx, compressionModel);
				Base.ReadFrom(reader, ref ctx, baseline.Base, compressionModel);
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				baseline.Tick      = Tick;
				baseline.Base.Tick = Base.Tick;
				return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
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

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<GamePlayerCommand, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
	public class SynchronizeGamePlayerCommandSystem : JobGameBaseSystem
	{
		private uint m_PreviousClientTick;

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var currentTick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;

			var isSameTick      = m_PreviousClientTick == currentTick;
			var localFromEntity = GetComponentDataFromEntity<GamePlayerLocalTag>(true);

			m_PreviousClientTick = currentTick;

			inputDeps = Entities.ForEach((Entity entity, ref GamePlayerCommand command, in DynamicBuffer<GamePlayerCommand.Snapshot> snapshots) =>
			{
				if (localFromEntity.Exists(entity))
					return;

				var previousCommand = command.Base;
				command.Base = snapshots.GetLastBaseline().Base;
				if (isSameTick)
				{
					var prevSpan = previousCommand.GetRhythmActions();
					var currSpan = command.Base.GetRhythmActions();
					for (var i = 0; i != prevSpan.Length; i++)
					{
						var     prev = prevSpan[i];
						ref var curr = ref currSpan.AsRef(i);

						if (!curr.FrameUpdate && prev.FrameUpdate)
							curr.FrameUpdate = true;
					}
				}
			}).WithReadOnly(localFromEntity).Schedule(inputDeps);

			return inputDeps;
		}
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateAfter(typeof(CommandReceiveSystem))]
	[UpdateBefore(typeof(SnapshotSendSystem))]
	public class ServerCopyUserCommandToPlayer : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var targetTick = World.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick;
			return Entities.ForEach((DynamicBuffer<UserCommand> buffer, ref GamePlayerCommand command) => { buffer.GetDataAtTick(targetTick, out command.Base); }).Schedule(inputDeps);
		}
	}
}