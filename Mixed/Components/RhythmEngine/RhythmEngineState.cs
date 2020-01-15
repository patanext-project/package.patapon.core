using System;
using System.Diagnostics.Contracts;
using package.stormiumteam.shared.ecs;
using Revolution;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon.Mixed.RhythmEngine
{
	public struct RhythmEngineState : IComponentData
	{
		public bool IsPaused;
		public bool IsNewBeat;
		public bool IsNewPressure;

		/// <summary>
		///     If a user do a f**k-up (doing pressure in an active command, waited a beat too much,...), he will need to wait a
		///     beat before starting to do pressures.
		/// </summary>
		public int NextBeatRecovery;

		/// <summary>
		///     Used on client side since a client could have set a custom recovery.
		/// </summary>
		public int RecoveryTick;

		public bool ApplyCommandNextBeat;
		public bool VerifyCommand;
		public int  LastPressureBeat;

		[Pure]
		public bool IsRecovery(int processBeat)
		{
			return NextBeatRecovery > processBeat;
		}

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<RhythmEngineState, DefaultSetup>
		{
			public bool IsPaused;
			public int  NextBeatRecovery;
			public int  RecoveryTick;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WriteBitBool(IsPaused);
				writer.WritePackedIntDelta(NextBeatRecovery, baseline.NextBeatRecovery, compressionModel);
				writer.WritePackedIntDelta(RecoveryTick, baseline.RecoveryTick, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				IsPaused         = reader.ReadBitBool(ref ctx);
				NextBeatRecovery = reader.ReadPackedIntDelta(ref ctx, baseline.NextBeatRecovery, compressionModel);
				RecoveryTick     = reader.ReadPackedIntDelta(ref ctx, baseline.RecoveryTick, compressionModel);
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				return IsPaused != baseline.IsPaused
				       || NextBeatRecovery != baseline.NextBeatRecovery
				       || RecoveryTick != baseline.RecoveryTick;
			}

			public void SynchronizeFrom(in RhythmEngineState component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				IsPaused         = component.IsPaused;
				NextBeatRecovery = component.NextBeatRecovery;
				RecoveryTick     = component.RecoveryTick;
			}

			public void SynchronizeTo(ref RhythmEngineState component, in DeserializeClientData deserializeData)
			{
				throw new NotImplementedException();
			}
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<RhythmEngineState, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		[UpdateInGroup(typeof(GhostUpdateSystemGroup))]
		public class UpdateSystem : JobComponentSystem
		{
			private LazySystem<ClientSimulationSystemGroup> m_ClientGroup;

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				inputDeps = Entities.ForEach((ref RhythmEngineState component, in DynamicBuffer<Snapshot> snapshots) =>
				{
					if (snapshots.Length == 0)
						return;
					var last = snapshots.GetLastBaseline();
					component.IsPaused = last.IsPaused;
					if (component.RecoveryTick < last.RecoveryTick || component.NextBeatRecovery < last.NextBeatRecovery)
					{
						component.RecoveryTick     = last.RecoveryTick;
						component.NextBeatRecovery = last.NextBeatRecovery;
					}
				}).Schedule(inputDeps);

				return inputDeps;
			}
		}
	}
}