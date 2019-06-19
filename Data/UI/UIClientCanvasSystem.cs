using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.UI
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIClientCanvasSystem : ComponentSystem
	{
		public Canvas Current;

		protected override void OnCreate()
		{
			base.OnCreate();

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
		}

		protected override void OnUpdate()
		{
			
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			Object.Destroy(Current.gameObject);
			Current = null;
		}
	}
}