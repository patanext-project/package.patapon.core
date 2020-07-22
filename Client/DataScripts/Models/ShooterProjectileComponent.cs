using package.stormiumteam.shared.ecs;
using PataNext.Client.Components;
using PataNext.Client.Components.Archetypes;
using PataNext.Client.DataScripts.Models.Equipments;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Systems;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.DataScripts.Models
{
	public class ShooterProjectileComponent : MonoBehaviour, IBackendReceiver
	{
		public bool      canPredict;
		public int targetRoot = -1;

		// this is a very ugly solution, but it's currently the best one for animation :|
		public Transform[] availableRoots;

		public RuntimeAssetBackendBase Backend { get; set; }

		private EquipmentRootData m_Root;

		public void OnBackendSet()
		{
			Debug.LogError($"On Set Data for {Backend.DstEntity}");
			if (!Backend.DstEntityManager.HasComponent<ShooterProjectileVisualTarget>(Backend.DstEntity))
			{
				Backend.DstEntityManager.AddComponentData(Backend.DstEntity, new ShooterProjectileVisualTarget());
			}

			m_Root = null;
		}

		public void OnPresentationSystemUpdate()
		{
			var entityMgr = Backend.DstEntityManager;
			var dstEntity = Backend.DstEntity;

			if (targetRoot < 0 || targetRoot >= availableRoots.Length)
				return;
			
			if (canPredict)
			{
				var matrix = availableRoots[targetRoot].localToWorldMatrix;
				entityMgr.SetOrAddComponentData(dstEntity, new ShooterProjectilePrediction {Transform = new RigidTransform(matrix)});
			}
			else if (entityMgr.HasComponent<ShooterProjectilePrediction>(dstEntity))
				entityMgr.RemoveComponent<ShooterProjectilePrediction>(dstEntity);

			if (!(Backend is UnitVisualBackend backend)
			    || Backend.GetPresentationBoxed() == null
			    || !(Backend.GetPresentationBoxed() is IEquipmentRoot equipRoot))
				return;

			var previousRoot = m_Root;
			m_Root = equipRoot.GetRoot(availableRoots[targetRoot]);

			ThrowableProjectileComponent throwable;
			if (previousRoot == m_Root || (throwable = m_Root.UnitEquipmentBackend.GetComponentInChildren<ThrowableProjectileComponent>()) == null)
				return;

			VisualThrowableDefinition projectileDefinition = default;
			if (throwable.assetReference != null)
			{
				projectileDefinition = entityMgr.World.GetExistingSystem<VisualThrowableProjectileManager>()
				                                .Register((string) throwable.assetReference.RuntimeKey);
			}

			entityMgr.SetComponentData(dstEntity, new ShooterProjectileVisualTarget {Definition = projectileDefinition});
		}
	}
}