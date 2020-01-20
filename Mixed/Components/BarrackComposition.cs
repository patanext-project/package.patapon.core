using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct FormationRoot : IComponentData
	{
		public class NetSynchronize : ComponentSnapshotSystemTag<FormationRoot> {}
	}

	public struct InFormation : IComponentData
	{
		public Entity Root;
	}

	public struct FormationChild : IBufferElementData
	{
		public Entity Value;
	}

	public struct FormationParent : IReadWriteComponentSnapshot<FormationParent, GhostSetup>, ISnapshotDelta<FormationParent>
	{
		public Entity Value;

		public void WriteTo(DataStreamWriter writer, ref FormationParent baseline, GhostSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(setup[Value], jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref FormationParent baseline, DeserializeClientData jobData)
		{
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Value);
		}

		public bool DidChange(FormationParent baseline)
		{
			return Value != baseline.Value;
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<FormationParent, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(ExcludeFromTagging);
		}
	}

	public struct ArmyFormation : IComponentData
	{
		public class NetSynchronize : ComponentSnapshotSystemTag<ArmyFormation> {}
	}

	public struct UnitFormation : IComponentData
	{
		public class NetSynchronize : ComponentSnapshotSystemTag<UnitFormation> {}
	}

	// indicate if this formation is used in-game
	public struct GameFormationTag : IComponentData
	{
		public class NetSynchronize : ComponentSnapshotSystemTag<GameFormationTag> {}
	}

	public struct FormationTeam : IComponentData
	{
		public int TeamIndex;
	}
}