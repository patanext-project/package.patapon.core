using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CTate
{
	public struct BasicTaterazayDefendFrontalAbility : IReadWriteComponentSnapshot<BasicTaterazayDefendFrontalAbility>, ISnapshotDelta<BasicTaterazayDefendFrontalAbility>
	{
		// 10 is a good range
		public float Range;

		public class Provider : BaseRhythmAbilityProvider<BasicTaterazayDefendFrontalAbility>
		{
			public const string MapPath = "tate_frontal_def";

			public override string MasterServerId  => nameof(P4OfficialAbilities.TateBasicDefendFrontal);
			public override Type   ChainingCommand => typeof(DefendCommand);
			protected override string file_path_prefix => "tate";

			public override void SetEntityData(Entity entity, CreateAbility data)
			{
				base.SetEntityData(entity, data);
				EntityManager.SetComponentData(entity, GetValue(MapPath, new BasicTaterazayDefendFrontalAbility {Range = 10}));
			}
		}

		public void WriteTo(DataStreamWriter writer, ref BasicTaterazayDefendFrontalAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref BasicTaterazayDefendFrontalAbility baseline, DeserializeClientData jobData)
		{
		}

		public bool DidChange(BasicTaterazayDefendFrontalAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<BasicTaterazayDefendFrontalAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}