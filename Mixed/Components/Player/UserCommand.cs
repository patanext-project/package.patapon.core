using System;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Default.Player
{
	public unsafe struct UserCommand : ICommandData<UserCommand>
	{
		public uint Tick { get; set; }

		public struct RhythmAction
		{
			public byte flags;

			public bool IsActive
			{
				get => MainBit.GetBitAt(flags, 0) == 1;
				set => MainBit.SetBitAt(ref flags, 0, value);
			}

			public bool FrameUpdate
			{
				get => MainBit.GetBitAt(flags, 1) == 1;
				set => MainBit.SetBitAt(ref flags, 1, value);
			}

			public bool WasPressed  => IsActive && FrameUpdate;
			public bool WasReleased => !IsActive && FrameUpdate;
		}

		public const int MaxActionCount = 4;
		
		private fixed byte m_RhythmActions[sizeof(byte) * 4];

		public Span<RhythmAction> GetRhythmActions()
		{
			fixed (byte* fx = m_RhythmActions)
			{
				return new Span<RhythmAction>(fx, 4);
			}
		}

		public void ReadFrom(DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			ReadFrom(reader, ref ctx, default, compressionModel);
		}

		public void WriteTo(DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			WriteTo(writer, default, compressionModel);
		}

		public void ReadFrom(DataStreamReader reader, ref DataStreamReader.Context ctx, UserCommand baseline, NetworkCompressionModel compressionModel)
		{
			var baselineActions = baseline.GetRhythmActions();
			var i               = 0;
			foreach (ref var action in GetRhythmActions())
			{
				action.flags = (byte) reader.ReadPackedUIntDelta(ref ctx, baselineActions[i++].flags, compressionModel);
			}
		}

		public void WriteTo(DataStreamWriter writer, UserCommand baseline, NetworkCompressionModel compressionModel)
		{
			var baselineActions = baseline.GetRhythmActions();
			var i               = 0;
			foreach (ref readonly var action in GetRhythmActions())
			{
				writer.WritePackedUIntDelta(action.flags, baselineActions[i++].flags, compressionModel);
			}
		}
	}

	[UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
	public class SpawnUserCommand : ComponentSystem
	{
		protected override void OnUpdate()
		{
			EntityManager.AddComponent(Entities.WithNone<UserCommand>().WithAll<GamePlayer>().ToEntityQuery(), typeof(UserCommand));
		}
	}
}