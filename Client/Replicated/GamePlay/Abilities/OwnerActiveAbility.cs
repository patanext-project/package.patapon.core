using GameHost;
using GameHost.Native;
using GameHost.Native.Fixed;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
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
			public int                         LastCommandActiveTime, LastActivationTime;
			public GhGameEntity                Active,                Incoming;
			public FixedBuffer32<GhGameEntity> CurrentCombo;
		}

		public int LastCommandActiveTime;
		public int LastActivationTime;

		public Entity Active;
		public Entity Incoming;

		/// <summary>
		///     Current combo of the entity...
		/// </summary>
		public FixedBuffer32<Entity> CurrentCombo; //< 32 bytes should suffice, it would be 4 combo commands...

		public void AddCombo(Entity ent)
		{
			while (CurrentCombo.GetLength() >= CurrentCombo.GetCapacity())
				CurrentCombo.RemoveAt(0);
			CurrentCombo.Add(ent);
		}

		public bool RemoveCombo(Entity ent)
		{
			var index = CurrentCombo.IndexOf(ent);
			if (index < 0)
				return false;
			CurrentCombo.RemoveAt(index);
			return true;
		}

		public class Register : RegisterGameHostComponentData<OwnerActiveAbility>
		{
			protected override ICustomComponentDeserializer CustomDeserializer { get; }
		}

		public int Size => UnsafeUtility.SizeOf<Replica>();

		public void Deserialize(EntityManager em, NativeHashMap<GhGameEntity, Entity> ghEntityToUEntity, ref OwnerActiveAbility component, ref DataBufferReader reader)
		{
			var replica = reader.ReadValue<Replica>();
			LastActivationTime    = replica.LastActivationTime;
			LastCommandActiveTime = replica.LastCommandActiveTime;
			ghEntityToUEntity.TryGetValue(replica.Active, out Active);
			ghEntityToUEntity.TryGetValue(replica.Incoming, out Incoming);
			CurrentCombo.SetLength(replica.CurrentCombo.GetLength());

			for (var i = 0; i != replica.CurrentCombo.Span.Length; i++) 
				ghEntityToUEntity.TryGetValue(replica.CurrentCombo.Span[i], out CurrentCombo.Span[i]);
		}
	}
}