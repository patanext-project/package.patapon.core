using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.UI
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class UIClientCanvasManageStateSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			if (ClientServerBootstrap.clientWorld == null)
				return;

			foreach (var clientW in ClientServerBootstrap.clientWorld)
			{
				var system = clientW.GetExistingSystem<UIClientCanvasSystem>();
				system.InternalSetActive(clientW.GetExistingSystem<ClientPresentationSystemGroup>().Enabled);
			}
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIClientCanvasSystem : ComponentSystem
	{
		public List<Canvas> Canvas { get; private set; }

		public  Canvas Current { get; private set; }
		private bool   m_State;

		protected override void OnCreate()
		{
			base.OnCreate();

			Canvas = new List<Canvas>();

			var gameObject = new GameObject($"(World: {World.Name}) UICanvas",
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster));

			Current            = gameObject.GetComponent<Canvas>();
			Current.renderMode = RenderMode.ScreenSpaceOverlay;

			var canvasScaler = gameObject.GetComponent<CanvasScaler>();
			canvasScaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution    = new Vector2(1920, 1080);
			canvasScaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight     = 0;
			canvasScaler.referencePixelsPerUnit = 100;

			var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
			graphicRaycaster.ignoreReversedGraphics = true;
			graphicRaycaster.blockingObjects        = GraphicRaycaster.BlockingObjects.None;

			m_State = true;
		}

		protected override void OnUpdate()
		{

		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			foreach (var canvas in Canvas)
			{
				if (canvas != null)
					Object.Destroy(canvas.gameObject);
			}

			Canvas.Clear();

			if (Current != null)
				Object.Destroy(Current.gameObject);

			Current = null;
			Canvas  = null;
		}

		internal void InternalSetActive(bool state)
		{
			if (state == m_State)
				return;

			m_State = state;
			Current.gameObject.SetActive(state);
		}

		public Canvas CreateCanvas(out int listIndex, string name = "UICustomCanvas", bool defaultInitialization = false)
		{
			var gameObject = new GameObject($"(World: {World.Name}) {name}#{Canvas.Count}",
				typeof(Canvas),
				typeof(CanvasScaler),
				typeof(GraphicRaycaster));
			var canvas = gameObject.GetComponent<Canvas>();

			listIndex = Canvas.Count;
			if (!defaultInitialization)
				return canvas;

			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var canvasScaler = gameObject.GetComponent<CanvasScaler>();
			canvasScaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution    = new Vector2(1920, 1080);
			canvasScaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			canvasScaler.matchWidthOrHeight     = 0;
			canvasScaler.referencePixelsPerUnit = 100;

			var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
			graphicRaycaster.ignoreReversedGraphics = true;
			graphicRaycaster.blockingObjects        = GraphicRaycaster.BlockingObjects.None;

			return canvas;
		}
	}
}