using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Unity.Entities;
using Unity.Mathematics;

namespace Patapon.Mixed.GamePlay.Abilities.CYari
{
	public struct YaridaFearSpearAbility : IComponentData
	{
		public const uint DelayThrowMs = 350;
		
		public uint  AttackStartTick;
		public float NextAttackDelay;
		public bool  HasThrown;

		public float2 ThrowVec;
		
		public class Provider : BaseRhythmAbilityProvider<YaridaFearSpearAbility>
		{
			public override    string MasterServerId          => nameof(P4OfficialAbilities.YariFearSpear);
			public override    Type   ChainingCommand         => typeof(AttackCommand);
			public override    Type[] HeroModeAllowedCommands => new[] {typeof(MarchCommand)};
			protected override string file_path_prefix        => "yari";

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

				EntityManager.SetComponentData(entity, new YaridaFearSpearAbility {ThrowVec = new float2(20f, -1f)});
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemTag<YaridaFearSpearAbility>
		{
		}
	}
}