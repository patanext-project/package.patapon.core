using System;
using Systems;
using DefaultNamespace;
using package.patapon.core.FeverWorm;
using Patapon.Client.OrderSystems;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon4TLB.GameModes.Interface;
using StormiumTeam.GameBase.Misc;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

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