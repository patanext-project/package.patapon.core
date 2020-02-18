using System;
using DataScripts.Interface.Popup;
using DefaultNamespace;
using package.stormiumteam.shared.ecs;
using Patapon.Client.PoolingSystems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[assembly: RegisterGenericComponentType(typeof(Relative<PopupDescription>))]

namespace DataScripts.Interface.Popup
{
	public class UIPopup : IComponentData
	{
		public Canvas CustomCanvas;

		public RectTransform Board;
		
		public string Title;
		public string Content;
	}
	
	public struct PopupDescription : IEntityDescription {}

	public class PopupPresentation : RuntimeAssetPresentation<PopupPresentation>
	{
		public TextMeshProUGUI titleLabel;
		public TextMeshProUGUI contentLabel;

		public RectTransform board;

		[SerializeReference]
		public GameObject[] layouts;
	}

	public class PopupBackend : RuntimeAssetBackend<PopupPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class PopupPoolingSystem : PoolingSystem<PopupBackend, PopupPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Popup")
			              .GetFile("DarkStyle.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UIPopup));
		}

		private Canvas m_Canvas;

		protected override void ReturnBackend(PopupBackend backend)
		{
			base.ReturnBackend(backend);
		}

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
				m_Canvas = CanvasUtility.Create(World, 100, "Popups");
			base.SpawnBackend(target);

			var targetCanvas = m_Canvas;
			if (EntityManager.TryGetComponent(target, out UIPopup uiPopup))
			{
				targetCanvas = uiPopup.CustomCanvas ? uiPopup.CustomCanvas : m_Canvas;
			}
			
			EntityManager.SetComponentData(target, uiPopup);
			
			LastBackend.transform.SetParent(targetCanvas.transform, false);
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class PopupRenderSystem : BaseRenderSystem<PopupPresentation>
	{
		protected override void PrepareValues()
		{

		}

		protected override void Render(PopupPresentation definition)
		{
			var backend = (PopupBackend) definition.Backend;
			var entity  = backend.DstEntity;

			if (!EntityManager.TryGetComponent(entity, out UIPopup uiPopup))
				return;

			definition.titleLabel.text   = uiPopup.Title;
			definition.contentLabel.text = uiPopup.Content;

			var rt         = backend.GetComponent<RectTransform>();
			var targetSize = default(Vector2);
			foreach (var layoutGo in definition.layouts)
			{
				var layout = layoutGo.GetComponent<ILayoutElement>();
				targetSize.x = math.max(layout.preferredWidth, targetSize.x);
				targetSize.y = math.max(layout.preferredHeight, targetSize.y);
			}

			rt.sizeDelta = new Vector2(targetSize.x + 50, targetSize.y + 60);

			if (uiPopup.Board != definition.board)
			{
				uiPopup.Board = definition.board;
				EntityManager.SetComponentData(entity, uiPopup);
			}
		}

		protected override void ClearValues()
		{

		}
	}
}