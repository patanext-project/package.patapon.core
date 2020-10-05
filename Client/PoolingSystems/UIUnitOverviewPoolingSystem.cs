using System;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.PoolingSystems
{
	public class UIUnitOverviewPoolingSystem : PoolingSystem<UIUnitOverviewBackend, UIUnitOverviewPresentation>
	{
		private Canvas m_Canvas;
		
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Menu()
			              .Folder("__Barracks")
			              .GetFile("UnitOverview.prefab");
		
		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			// TODO: Filter it by barracks
			return GetEntityQuery(typeof(CurrentUnitOverview));
		}
		
		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
				// todo: order
				// todo: should we attach it to the Barracks interface canvas?
				m_Canvas = CanvasUtility.Create(World, 0, "UnitOverview", scalerMatchWidthOrHeight: 0.5f);

			base.SpawnBackend(target);

			LastBackend.transform.SetParent(m_Canvas.transform, false);
			CanvasUtility.ExtendRectTransform(LastBackend.GetComponent<RectTransform>());
		}
	}
}