using System;
using DataScripts.Interface.GameMode.Global;
using Patapon.Client.OrderSystems;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIGameModeIntroPoolingSystem : PoolingSystem<UIGameModeIntroBackend, UIGameModeIntroPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("GameMode")
			              .Folder("Global")
			              .GetFile("UIGameModeIntro.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ExecutingGameMode));
		}

		private Canvas m_Canvas;

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				var interfaceOrder = World.GetExistingSystem<UIGameModeOrderingSystem>().Order;
				var canvasSystem   = World.GetExistingSystem<ClientCanvasSystem>();

				m_Canvas                = canvasSystem.CreateCanvas(out _, "GameModeIntro Canvas", defaultAddRaycaster: false);
				m_Canvas.renderMode     = RenderMode.ScreenSpaceCamera;
				m_Canvas.worldCamera    = World.GetExistingSystem<ClientCreateCameraSystem>().Camera;
				m_Canvas.planeDistance  = 1;
				m_Canvas.sortingLayerID = SortingLayer.NameToID("OverlayUI");
				m_Canvas.sortingOrder   = interfaceOrder;
				// svg need them additional channels 👌
				m_Canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
				                                    | AdditionalCanvasShaderChannels.TexCoord2
				                                    | AdditionalCanvasShaderChannels.TexCoord3;

				var scaler = m_Canvas.GetComponent<CanvasScaler>();
				scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight  = 1;
			}

			base.SpawnBackend(target);

			var backend = LastBackend;
			backend.transform.SetParent(m_Canvas.transform, false);

			var rt = backend.GetComponent<RectTransform>();
			rt.anchorMin        = new Vector2(0, 0);
			rt.anchorMax        = new Vector2(1, 1);
			rt.sizeDelta        = Vector2.zero;
			rt.anchoredPosition = Vector2.zero;
			rt.pivot            = new Vector2(0.5f, 0.5f);
		}
	}
}