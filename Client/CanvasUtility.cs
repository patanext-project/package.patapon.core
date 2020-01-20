using System;
using Systems;
using DataScripts.Interface.Popup;
using Patapon.Client.OrderSystems;
using StormiumTeam.GameBase.Misc;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon.Client.PoolingSystems
{
	public static class CanvasUtility
	{
		public static Canvas Create(World world, int order, string name, float scalerMatchWidthOrHeight = 0.5f)
		{
			if (world.GetExistingSystem<ClientSimulationSystemGroup>() == null)
				throw new Exception("CanvasUtility.Create was called in a non client world, world caller: " + world.Name);

			var canvasSystem = world.GetOrCreateSystem<ClientCanvasSystem>();

			var canvas = canvasSystem.CreateCanvas(out _, name + " Canvas", defaultAddRaycaster: false);
			canvas.renderMode     = RenderMode.ScreenSpaceCamera;
			canvas.worldCamera    = world.GetOrCreateSystem<ClientCreateCameraSystem>().Camera;
			canvas.planeDistance  = 1;
			canvas.sortingLayerID = SortingLayer.NameToID("OverlayUI");
			canvas.sortingOrder   = order;
			// svg need them additional channels 👌
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
			                                  | AdditionalCanvasShaderChannels.TexCoord2
			                                  | AdditionalCanvasShaderChannels.TexCoord3;

			var scaler = canvas.GetComponent<CanvasScaler>();
			scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
			scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight  = 1;

			return canvas;
		}

		public static void DisableInteractionOnActivePopup(World world, Canvas canvas, EntityQuery customQuery = null)
		{
			if (canvas.GetComponent<CanvasGroup>() == null)
				canvas.gameObject.AddComponent<CanvasGroup>();
			if (canvas.GetComponent<DisableInteractionOnPopup>() == null)
			{
				var comp = canvas.gameObject.AddComponent<DisableInteractionOnPopup>();
				comp.World = world;
				comp.Query = customQuery ?? world.EntityManager.CreateEntityQuery(typeof(UIPopup));
			}
		}

		public static void ExtendRectTransform(RectTransform rt)
		{
			rt.anchorMin        = new Vector2(0, 0);
			rt.anchorMax        = new Vector2(1, 1);
			rt.sizeDelta        = Vector2.zero;
			rt.anchoredPosition = Vector2.zero;
			rt.pivot            = new Vector2(0.5f, 0.5f);
		}

		public class DisableInteractionOnPopup : MonoBehaviour
		{
			public World       World { get; set; }
			public EntityQuery Query { get; set; }

			private CanvasGroup m_CanvasGroup;

			private void OnEnable()
			{
				m_CanvasGroup = GetComponent<CanvasGroup>();
			}

			private void LateUpdate()
			{
				m_CanvasGroup.interactable = Query.IsEmptyIgnoreFilter;
			}
		}
	}
}