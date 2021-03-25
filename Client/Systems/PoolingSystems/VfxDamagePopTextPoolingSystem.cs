using System;
using GameHost.Revolution.NetCode.Components;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Interface.InGame;
using Replicated.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.GamePlay.Events;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace PataNext.Client.PoolingSystems
{
	public struct FalseIfReplicatedLocal : ICheckValidity
	{
		[ReadOnly] public ComponentDataFromEntity<SnapshotEntity> snapshotEntityFromEntity;

		public int LocalInstigatorId;

		public void OnSetup(ComponentSystemBase system)
		{
			LocalInstigatorId = -1;
			if (system.HasSingleton<LocalInstigatorId>())
				LocalInstigatorId = system.GetSingleton<LocalInstigatorId>().Value;

			snapshotEntityFromEntity = system.GetComponentDataFromEntity<SnapshotEntity>();
		}

		public bool IsValid(Entity target)
		{
			if (snapshotEntityFromEntity.TryGet(target, out var snapshotEntity)
			    && snapshotEntity.InstigatorId == LocalInstigatorId)
				return false;

			return true;
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class VfxDamagePopTextPoolingSystem : PoolingSystem<VfxDamagePopTextBackend, VfxDamagePopTextPresentation, FalseIfReplicatedLocal>
	{
		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};
		
		protected override AssetPath AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("InGame")
			              .Folder("Effects")
			              .Folder("VfxDamage")
			              .GetAsset("VfxDamagePopTextDefault");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(TargetDamageEvent));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			LastBackend.Play(EntityManager.GetComponentData<TargetDamageEvent>(LastBackend.DstEntity));
			LastBackend.setToPoolAt          = Time.ElapsedTime + 2f;
			LastBackend.transform.localScale = Vector3.one * 0.5f;
			
			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "OverlayUI";
		}

		protected override void ReturnBackend(VfxDamagePopTextBackend backend)
		{
			if (backend.setToPoolAt > Time.ElapsedTime)
				return;
			base.ReturnBackend(backend);
		}
	}
}