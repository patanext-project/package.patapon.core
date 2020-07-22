using System;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace DataScripts.Models.Effects.EnergyField
{
	public class VfxEnergyFieldPresentation : RuntimeAssetPresentation<VfxEnergyFieldPresentation>
	{
		public Animator animator;
		public string   triggerActive = "Activate";
	}

	public class VfxEnergyFieldBackend : RuntimeAssetBackend<VfxEnergyFieldPresentation>
	{
		public float3 position;
		public double setToPoolAt;

		public bool hasBeenActive;

		public override void OnReset()
		{
			base.OnReset();
			hasBeenActive = false;
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class VfxEnergyFieldRenderSystem : BaseRenderSystem<VfxEnergyFieldPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(VfxEnergyFieldPresentation definition)
		{
			var backend = (VfxEnergyFieldBackend) definition.Backend;
			if (!backend.hasBeenActive)
			{
				definition.animator.SetTrigger(definition.triggerActive);
				
				backend.hasBeenActive = true;
			}
				
			backend.transform.position = backend.position;
		}

		protected override void ClearValues()
		{
			
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
	public class VfxEnergyFieldPoolingSystem : PoolingSystem<VfxEnergyFieldBackend, VfxEnergyFieldPresentation, VfxEnergyFieldPoolingSystem.Validation>
	{
		public struct Validation : ICheckValidity
		{
			[ReadOnly]
			public ComponentDataFromEntity<EnergyFieldBuff.HasBonusTag> HasBonusFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<TargetDamageEvent> DamageEventFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<UnitPlayState> PlayStateFromEntity;

			public void OnSetup(ComponentSystemBase system)
			{
				HasBonusFromEntity    = system.GetComponentDataFromEntity<EnergyFieldBuff.HasBonusTag>(true);
				DamageEventFromEntity = system.GetComponentDataFromEntity<TargetDamageEvent>(true);
				PlayStateFromEntity = system.GetComponentDataFromEntity<UnitPlayState>(true);
			}

			public bool IsValid(Entity target)
			{
				return HasBonusFromEntity.Exists(DamageEventFromEntity[target].Destination)
					&& PlayStateFromEntity.TryGet(DamageEventFromEntity[target].Destination, out var ps) && ps.ReceiveDamagePercentage < 0.75f;
			}
		}

		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Effects")
			              .Folder("EnergyField")
			              .GetFile("EnergyFieldHalved.prefab");
		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(TargetDamageEvent), typeof(Translation));
		}

		protected override void ReturnBackend(VfxEnergyFieldBackend backend)
		{
			if (backend.setToPoolAt >= Time.ElapsedTime)
				return;
			base.ReturnBackend(backend);
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			LastBackend.position    = EntityManager.GetComponentData<Translation>(target).Value;
			LastBackend.position.z  = 0;
			LastBackend.setToPoolAt = Time.ElapsedTime + 1;
			
			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "BattlegroundEffects";
			sortingGroup.sortingOrder     = 0;
		}
	}
}