using System;
using GameHost;
using GameHost.Native;
using GameHost.Native.Fixed;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using PataNext.Module.Simulation.Resources;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.Abilities
{
	public struct OwnerActiveAbility : IComponentData, IValueDeserializer<OwnerActiveAbility>
	{
		private struct Replica
		{
			public int LastCommandActiveTime;
			public int LastActivationTime;

			public GhGameEntity Previous;
			public GhGameEntity Active;
			public GhGameEntity Incoming;

			/// <summary>
			///     Current combo of the entity...
			/// </summary>
			public FixedBuffer32<GameResource<RhythmCommandResource>> CurrentCombo; //< 32 bytes should suffice, it would be 4 combo commands...
		}

		public int LastCommandActiveTime;
		public int LastActivationTime;

		public Entity PreviousActive;
		public Entity Active;
		public Entity Incoming;

		/// <summary>
		///     Current combo of the entity...
		/// </summary>
		public FixedBuffer32<GameResource<RhythmCommandResource>> CurrentCombo; //< 32 bytes should suffice, it would be 4 combo commands...

		public class Register : RegisterGameHostComponentData<OwnerActiveAbility>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<OwnerActiveAbility, OwnerActiveAbility>();
		}

		public int Size => UnsafeUtility.SizeOf<Replica>();

		public void Deserialize(EntityManager em, NativeHashMap<GhGameEntity, Entity> ghEntityToUEntity, ref OwnerActiveAbility component, ref DataBufferReader reader)
		{
			var replica = reader.ReadValue<Replica>();
			LastActivationTime    = replica.LastActivationTime;
			LastCommandActiveTime = replica.LastCommandActiveTime;
			ghEntityToUEntity.TryGetValue(replica.Previous, out PreviousActive);
			ghEntityToUEntity.TryGetValue(replica.Active, out Active);
			ghEntityToUEntity.TryGetValue(replica.Incoming, out Incoming);

			CurrentCombo = replica.CurrentCombo;

			component = this;
		}
	}
}