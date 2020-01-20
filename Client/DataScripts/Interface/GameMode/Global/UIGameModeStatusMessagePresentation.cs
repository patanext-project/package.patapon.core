using System;
using DefaultNamespace;
using Misc.Extensions;
using Patapon.Client.PoolingSystems;
using Patapon.Mixed.GameModes;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace DataScripts.Interface.GameMode.Global
{
	public class UIGameModeStatusMessagePresentation : RuntimeAssetPresentation<UIGameModeStatusMessagePresentation>
	{
		public TextMeshProUGUI label;
		public Animator        animator;

		private void OnEnable()
		{
			animator = animator == null ? GetComponent<Animator>() : animator;
		}
	}

	public class UIGameModeStatusMessageBackend : RuntimeAssetBackend<UIGameModeStatusMessagePresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIGameModeStatusMessagePoolingSystem : PoolingSystem<UIGameModeStatusMessageBackend, UIGameModeStatusMessagePresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("GameMode")
			              .Folder("Global")
			              .Folder("StatusMessage")
			              .GetFile("UIStatusMessage.prefab");

		protected override Type[] AdditionalBackendComponents => new [] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(GameModeHudSettings));
		}

		private Canvas m_Canvas;
		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
				m_Canvas = CanvasUtility.Create(World, 100, "StatusMessage");
			base.SpawnBackend(target);

			var rt = LastBackend.GetComponent<RectTransform>();
			CanvasUtility.ExtendRectTransform(rt);
			
			rt.SetParent(m_Canvas.transform, false);
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class UIGameModeStatusMessageRenderSystem : BaseRenderSystem<UIGameModeStatusMessagePresentation>
	{
		private EntityQuery m_QueryUpdate;

		public bool                 Trigger;
		public GameModeStatusUpdate Status;

		protected override void PrepareValues()
		{
			if (m_QueryUpdate == null)
				m_QueryUpdate = GetEntityQuery(typeof(GameModeStatusUpdate));

			if (!m_QueryUpdate.IsEmptyIgnoreFilter)
			{
				Status  = m_QueryUpdate.GetSingleton<GameModeStatusUpdate>();
				Trigger = true;
			}
		}

		protected override void Render(UIGameModeStatusMessagePresentation definition)
		{
			if (!Trigger)
				return;

			var backend = (UIGameModeStatusMessageBackend) definition.Backend;
			if (Status.Hud.StatusMessage.LengthInBytes > 0)
			{
				definition.label.text = Status.Hud.StatusMessage.ToString();
				definition.animator.SetTrigger("OnShow");
			}
		}

		protected override void ClearValues()
		{
			Trigger = false;
		}
	}
}