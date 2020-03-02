using System;
using Revolution;
using Scripts.Utilities;
using StormiumTeam.GameBase;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

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

	[InternalBufferCapacity(16)]
	public struct CommandInterFrame : IBufferElementData
	{
		public UserCommand Base;
	}

	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public class ResetGamePlayerCommandInterFrameSystem : AbsGameBaseSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithNone<CommandInterFrame>().WithAll<GamePlayerCommand.Snapshot>().ForEach((Entity ent) =>
			{
				// commands interframe can be useful
				// todo: say why it's useful
				EntityManager.AddBuffer<CommandInterFrame>(ent);
			}).WithStructuralChanges().Run();

			Entities.ForEach((ref DynamicBuffer<CommandInterFrame> cmds, in DynamicBuffer<GamePlayerCommand.Snapshot> snapshots) => { cmds.Clear(); }).Run();
		}
	}

	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
	public class SynchronizeGamePlayerCommandSystem : AbsGameBaseSystem
	{
		protected override void OnUpdate()
		{
			var localFromEntity = GetComponentDataFromEntity<GamePlayerLocalTag>(true);
			Entities.ForEach((Entity entity, ref GamePlayerCommand command, in DynamicBuffer<GamePlayerCommand.Snapshot> snapshots) =>
			{
				if (localFromEntity.Exists(entity))
					return;

				command.Base = snapshots.GetLastBaselineReadOnly().Base;
			}).WithReadOnly(localFromEntity).ScheduleParallel();
		}
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateAfter(typeof(CommandReceiveSystem))]
	[UpdateBefore(typeof(SnapshotSendSystem))]
	public class ServerCopyUserCommandToPlayer : SystemBase
	{
		protected override void OnUpdate()
		{
			var targetTick = World.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick;
			Entities.ForEach((DynamicBuffer<UserCommand> buffer, ref GamePlayerCommand command) => { buffer.GetDataAtTick(targetTick, out command.Base); }).Schedule();
		}
	}
}