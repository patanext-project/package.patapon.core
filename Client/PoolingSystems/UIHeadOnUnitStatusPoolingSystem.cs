using DefaultNamespace;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.GameModes.Interface;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIHeadOnUnitStatusPoolingSystem : PoolingSystem<UIHeadOnUnitStatusBackend, UIHeadOnUnitStatusPresentation>
	{
		private RectTransform m_LastParent;

		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .GameMode("VSHeadOn")
			              .GetFile("VSHeadOn_UnitStatus.prefab");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UnitDescription), typeof(VersusHeadOnUnit));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			Parent(LastBackend.transform);
		}

		/// <summary>
		/// Called by <see cref="UIHeadOnInterfacePoolingSystem"/>
		/// </summary>
		public void Reorder(RectTransform parent)
		{
			m_LastParent = parent;
			foreach (var entityWithBackend in Module.AttachedBackendEntities)
			{
				var backend = EntityManager.GetComponentObject<UIHeadOnUnitStatusBackend>(entityWithBackend);
				Parent(backend.transform);
			}
		}

		private void Parent(Transform backendTransform)
		{
			backendTransform.SetParent(m_LastParent, false);
		}
	}
}