using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using Revolution;
using Scripts.Utilities;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Mixed.GamePlay.Abilities.CTate
{
	public struct EnergyFieldBuff : /*IReadWriteComponentSnapshot<EnergyFieldBuff>, ISnapshotDelta<EnergyFieldBuff>*/ IComponentData
	{
		public float DamageReduction;
		public int   Defense;

		// not useful to be serialized for now...
		/*public void WriteTo(DataStreamWriter writer, ref EnergyFieldBuff baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedInt(Direction, jobData.NetworkCompressionModel);
			writer.WritePackedFloat(Position.x, jobData.NetworkCompressionModel);

			writer.WritePackedFloat(MinDistance, jobData.NetworkCompressionModel);
			writer.WritePackedFloat(MaxDistance, jobData.NetworkCompressionModel);

			writer.WritePackedFloat(DamageReduction, jobData.NetworkCompressionModel);
			writer.WritePackedInt(Defense, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref EnergyFieldBuff baseline, DeserializeClientData jobData)
		{
			Direction  = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			Position.x = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);

			MinDistance = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			MaxDistance = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);

			DamageReduction = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			Defense         = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(EnergyFieldBuff baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<EnergyFieldBuff>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}*/

		// used to recognize if this damage was halved by the bonus... (useful for displaying vfx on client)
		public struct DamageWasHalvedTag : IComponentData
		{
			public class TagSynchronize : ComponentSnapshotSystemTag<DamageWasHalvedTag>
			{
			}
		}

		// used to recognize if an entity currently possess this bonus or not...
		public struct HasBonusTag : IComponentData
		{
			public class TagSynchronize : ComponentSnapshotSystemTag<HasBonusTag>
			{
			}
		}
	}

	public struct TaterazayEnergyFieldAbility : IComponentData
	{
		public float MinDistance;
		public float MaxDistance;

		public float GivenDamageReduction;
		public float GivenDefenseReal;

		public Entity BuffEntity;

		public class Provider : BaseRhythmAbilityProvider<TaterazayEnergyFieldAbility>
		{
			public override    string MasterServerId          => nameof(P4OfficialAbilities.TateEnergyField);
			public override    Type   ChainingCommand         => typeof(DefendCommand);
			public override    Type[] HeroModeAllowedCommands => new[] {typeof(MarchCommand)};
			protected override string file_path_prefix        => "tate";

			public override void SetEntityData(Entity entity, CreateAbility data)
			{
				base.SetEntityData(entity, data);

				var activation = EntityManager.GetComponentData<AbilityActivation>(entity);
				activation.Type = EActivationType.HeroMode;

				EntityManager.SetComponentData(entity, activation);

				EntityManager.AddComponentData(entity, new DefaultSubsetMarch
				{
					SubSet             = DefaultSubsetMarch.ESubSet.Cursor,
					AccelerationFactor = 1
				});

				var buff = EntityManager.CreateEntity(typeof(BuffModifierDescription), typeof(Translation), typeof(UnitDirection), typeof(BuffDistance), typeof(BuffForTarget), typeof(BuffSource), typeof(EnergyFieldBuff));
				EntityManager.ReplaceOwnerData(buff, entity);
				EntityManager.SetEnabled(buff, false);

				EntityManager.SetComponentData(entity, new TaterazayEnergyFieldAbility
				{
					BuffEntity = buff,

					// safe minimal distance
					MinDistance = 2.5f,
					// a bit larger than the attack seek range
					MaxDistance = 20f,
					// reduce by two the damage for our buffed entities
					GivenDamageReduction = 0.5f,
					// give all of our defense
					GivenDefenseReal = 1f
				});
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemTag<TaterazayEnergyFieldAbility>
		{
		}
	}
}