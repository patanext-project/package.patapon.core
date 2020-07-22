using System;
using Patapon.Client.OrderSystems;
using Patapon4TLB.GameModes.Interface;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIHeadOnInterfacePoolingSystem : PoolingSystem<UIHeadOnBackend, UIHeadOnPresentation>
	{
		private Canvas m_Canvas;

		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .GameMode("VSHeadOn")
			              .GetFile("VSHeadOnInterface.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			RequireForUpdate(GetEntityQuery(typeof(HeadOnFlag)));
			
			return GetEntityQuery(typeof(MpVersusHeadOn));
		}

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
				m_Canvas = CanvasUtility.Create(World, World.GetExistingSystem<UIHeadOnInterfaceOrderSystem>().Order, "VSHeadOn", scalerMatchWidthOrHeight: 1f);

			base.SpawnBackend(target);

			LastBackend.transform.SetParent(m_Canvas.transform, false);
			CanvasUtility.ExtendRectTransform(LastBackend.GetComponent<RectTransform>());
		}
	}
}