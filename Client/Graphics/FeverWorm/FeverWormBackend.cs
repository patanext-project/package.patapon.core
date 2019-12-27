using Systems;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Misc;
using StormiumTeam.GameBase.Systems;
using StormiumTeam.Shared;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace package.patapon.core.FeverWorm
{
	public class FeverWormBackend : RuntimeAssetBackend<FeverWormPresentation>
	{
	}

	public struct CreateFeverWormData : IComponentData
	{
	}

	[UpdateInGroup(typeof(OrderInterfaceSystemGroup))]
	public class FeverWormOrderingSystem : OrderingSystem
	{
	}

	public class FeverWormRenderSystem : BaseRenderSystem<FeverWormPresentation>
	{
		private Localization m_LocalTextDb;

		public string ComboString;

		public float SummonEnergyReal;
		public float ComboScoreReal;

		public int ComboCount;

		public bool IsFever;

		private float real(int v, int m)
		{
			if (m == 0 || v == m)
				return 1f;
			if (v == 0)
				return 0f;
			return (float) v / m;
		}

		protected override void PrepareValues()
		{
			if (m_LocalTextDb == null)
			{
				m_LocalTextDb = World.GetOrCreateSystem<LocalizationSystem>()
				                     .LoadLocal("ingame");
			}

			ComboString = m_LocalTextDb["ComboText", "FWorm"];

			var gamePlayer = this.GetFirstSelfGamePlayer();
			if (gamePlayer == default)
				return;

			if (!this.TryGetCurrentCameraState(gamePlayer, out var cameraState))
				return;

			if (EntityManager.TryGetComponentData(cameraState.Target, out Relative<RhythmEngineDescription> rhythmEngineRelative))
			{
				var comboState = EntityManager.GetComponentData<GameComboState>(rhythmEngineRelative.Target);
				SummonEnergyReal = real(comboState.JinnEnergy, comboState.JinnEnergyMax);
				ComboScoreReal   = real(comboState.Score, 50); // todo: the magic number need to be removed!
				ComboCount       = comboState.Chain;
				IsFever          = comboState.IsFever;
			}
		}

		protected override void Render(FeverWormPresentation definition)
		{
			definition.SetComboString(ComboString);
			definition.SetProgression(ComboScoreReal, ComboCount, SummonEnergyReal, IsFever);
		}

		protected override void ClearValues()
		{
			// not used
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			EntityManager.CreateEntity(typeof(CreateFeverWormData));
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class FeverWormCreate : PoolingSystem<FeverWormBackend, FeverWormPresentation>
	{
		protected override string AddressableAsset => "core://Client/Interface/InGame/FeverWorm/FeverWorm.prefab";

		private Canvas m_Canvas;

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(CreateFeverWormData));
		}

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				var interfaceOrder = World.GetExistingSystem<FeverWormOrderingSystem>().Order;
				var canvasSystem   = World.GetExistingSystem<ClientCanvasSystem>();

				m_Canvas               = canvasSystem.CreateCanvas(out _, "FeverWormCanvas");
				m_Canvas.renderMode    = RenderMode.ScreenSpaceCamera;
				m_Canvas.sortingOrder  = interfaceOrder;
				m_Canvas.worldCamera   = World.GetExistingSystem<ClientCreateCameraSystem>().Camera;
				m_Canvas.planeDistance = 1;

				var scaler = m_Canvas.GetComponent<CanvasScaler>();
				scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight  = 1;
			}

			base.SpawnBackend(target);

			var backend = LastBackend;
			backend.transform.SetParent(m_Canvas.transform, false);
			if (!backend.TryGetComponent(out RectTransform rt))
			{
				rt = backend.gameObject.AddComponent<RectTransform>();
			}

			rt.localScale       = new Vector3(70, 70, 1);
			rt.anchorMin        = new Vector2(0, 0.5f);
			rt.anchorMax        = new Vector2(0, 0.5f);
			rt.anchoredPosition = new Vector2(0, 250);
			rt.sizeDelta        = new Vector2(100, 100);
			rt.pivot            = new Vector2(0.5f, 0.5f);
		}
	}
}