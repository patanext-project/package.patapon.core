using System.Collections.Generic;
using DefaultNamespace.Utility.DOTS;
using GameHost.ShareSimuWorldFeature.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.OrderSystems;
using PataNext.Module.Simulation.Components;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine.Structures;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Game.RhythmEngine;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Systems;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.DOTS;
using StormiumTeam.GameBase.Utility.DOTS.xMonoBehaviour;
using StormiumTeam.GameBase.Utility.Pooling;
using StormiumTeam.GameBase.Utility.uGUI.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Entity = Unity.Entities.Entity;
using EntityQuery = Unity.Entities.EntityQuery;
using Random = UnityEngine.Random;

namespace PataNext.Client.Graphics.Models.InGame.UIDrum
{
	public class UIDrumPressurePresentation : RuntimeAssetPresentation
	{
		public Animator animator;

		public Unity.VectorGraphics.SVGImage   drumImage;
		public Unity.VectorGraphics.SVGImage[] effectImages;

		public Sprite[] sprites;
		public Color[]  colors;

		private void Awake()
		{
			animator.enabled = false;
		}
	}

	public class UIDrumPressureBackend : RuntimeAssetBackend<UIDrumPressurePresentation>
	{
		public int    key, rand;
		public double endTime;

		public bool perfect;

		public bool play;
	}

	[AlwaysUpdateSystem]
	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	[AlwaysSynchronizeSystem]
	public class UIDrumPressureSystem : AbsGameBaseSystem
	{
		public Dictionary<int, AsyncAssetPool<GameObject>> DrumPresentationPools = new Dictionary<int, AsyncAssetPool<GameObject>>();
		public Dictionary<int, AssetPool<GameObject>>      DrumBackendPools      = new Dictionary<int, AssetPool<GameObject>>();

		public Dictionary<int, int> DrumVariantCount = new Dictionary<int, int>();

		private static readonly int StrHashPlay    = Animator.StringToHash("Play");
		private static readonly int StrHashVariant = Animator.StringToHash("Variant");
		private static readonly int StrHashKey     = Animator.StringToHash("Key");
		private static readonly int StrHashPerfect = Animator.StringToHash("Perfect");

		private Canvas      m_Canvas;
		private EntityQuery m_CameraQuery;
		private EntityQuery m_EngineQuery;

		private TimeSystem timeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Canvas                      = World.GetOrCreateSystem<ClientCanvasSystem>().CreateCanvas(out _, "UIDrumCanvas", defaultAddRaycaster: false);
			m_Canvas.renderMode           = RenderMode.WorldSpace;
			m_Canvas.transform.localScale = new Vector3() * 0.05f;

			m_CameraQuery = GetEntityQuery(typeof(GameCamera));

			var address = AddressBuilder.Client()
			                            .Folder("Models")
			                            .Folder("InGame")
			                            .Folder("UIDrum")
			                            .GetAsset("UIDrumPressure");
			for (var i = 1; i <= 4; i++)
			{
				DrumPresentationPools[i] = new AsyncAssetPool<GameObject>(address);
				DrumBackendPools[i]      = new AssetPool<GameObject>(CreateBackendDrumGameObject, World);
				DrumVariantCount[i]      = 0;
			}

			timeSystem = World.GetExistingSystem<TimeSystem>();

			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
		}

		// Set canvas order
		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			m_Canvas.sortingLayerID = SortingLayer.NameToID("OverlayUI");
			m_Canvas.sortingOrder   = (int) World.GetOrCreateSystem<UIDrumPressureOrderingSystem>().Order;
		}

		private void Destroy()
		{
			Entities.ForEach((UIDrumPressureBackend backend) => { backend.Return(true, true); }).WithStructuralChanges().Run();
		}

		protected override void OnUpdate()
		{
			Entity cameraEntity   = default;
			float3 cameraPosition = default;
			if (m_CameraQuery.CalculateEntityCount() > 0)
			{
				cameraEntity = m_CameraQuery.GetSingletonEntity();
				var cameraObject = EntityManager.GetComponentObject<UnityEngine.Camera>(cameraEntity);
				cameraPosition = cameraObject.transform.position;
			}

			var player = this.GetFirstSelfGamePlayer();
			if (!EntityManager.TryGetComponentData(player, out GameRhythmInputComponent playerCommand))
			{
				Destroy();
				return;
			}

			var cameraState = this.GetComputedCameraState();

			var isWorldSpace = cameraState.StateData.Target != default;
			if (!EntityManager.HasComponent<UnitDescription>(cameraState.StateData.Target))
				isWorldSpace = false;

			var canvasRect = m_Canvas.pixelRect;
			if (isWorldSpace && cameraEntity != default)
			{
				var translation = EntityManager.GetComponentData<Translation>(cameraState.StateData.Target);
				m_Canvas.renderMode           = RenderMode.WorldSpace;
				m_Canvas.transform.position   = new Vector3(translation.Value.x, translation.Value.y + 25 * 0.05f, cameraPosition.z + 10);
				m_Canvas.transform.localScale = Vector3.one * 0.05f;

				var rectTransform = m_Canvas.GetComponent<RectTransform>();
				rectTransform.sizeDelta = new Vector2(100, 100);

				canvasRect.width  = 90;
				canvasRect.height = 105;
			}
			else
			{
				m_Canvas.transform.position = Vector3.zero;
				m_Canvas.renderMode         = RenderMode.ScreenSpaceCamera;
				m_Canvas.worldCamera        = EntityManager.GetComponentObject<UnityEngine.Camera>(cameraEntity);
			}

			var pixelRange = new float2(canvasRect.width, canvasRect.height);

			Entity engine = default;
			if (cameraState.StateData.Target != default)
				engine = PlayerComponentFinder.GetRelativeChild<RhythmEngineDescription>(EntityManager, m_EngineQuery, cameraState.StateData.Target, player);
			else
				engine = PlayerComponentFinder.FromQueryFindPlayerChild(m_EngineQuery, player);

			if (engine == default)
			{
				Destroy();
				return;
			}

			var process  = EntityManager.GetComponentData<RhythmEngineLocalState>(engine);
			var settings = EntityManager.GetComponentData<RhythmEngineSettings>(engine);

			if (!EntityManager.HasComponent<RhythmEngineIsPlaying>(engine) || RhythmEngineUtility.GetFlowBeat(process.Elapsed, settings.BeatInterval) < 0)
			{
				Destroy();
				return;
			}

			var key = 1;
			foreach (var ac in playerCommand.Actions)
			{
				if (!ac.InterFrame.HasBeenPressed(timeSystem.GetReport(player).Active))
				{
					key++;
					continue;
				}

				var keyRange = new float2();
				if (key <= 2)
				{
					if (key == 1)
						keyRange.x = -0.35f;
					else
						keyRange.x = 0.35f;

					keyRange.x += Random.Range(-0.025f, 0.025f);
					keyRange.y =  Random.Range(-0.1f, 0.1f);
				}
				else
				{
					if (key == 3)
						keyRange.y = -0.375f;
					else
						keyRange.y = 0.375f;

					keyRange.y += Random.Range(-0.025f, 0.025f);
					keyRange.x =  Random.Range(-0.1f, 0.1f);
				}

				keyRange += 0.5f;

				var width  = pixelRange.x * 0.5f;
				var height = pixelRange.y * 0.5f;
				var keyPos = new float2(math.lerp(-width, width, keyRange.x), math.lerp(-height, height, keyRange.y));

				var beGameObject = DrumBackendPools[key].Dequeue();
				using (new SetTemporaryInjectionWorld(World))
				{
					beGameObject.name = $"BackendPressure (Key: {key})";
					beGameObject.SetActive(true);
					beGameObject.transform.SetParent(m_Canvas.transform, false);
					beGameObject.transform.localScale = m_Canvas.renderMode == RenderMode.WorldSpace
						? Vector3.one * 0.7f
						: 0.008f * math.min(width, height) * Vector3.one;
					beGameObject.transform.localPosition = new Vector3(keyPos.x, keyPos.y, 0);
					beGameObject.transform.rotation      = Quaternion.Euler(0, 0, Random.Range(-12.5f, 12.5f));
				}

				var backend = beGameObject.GetComponent<UIDrumPressureBackend>();
				backend.OnReset();
				backend.SetTarget(EntityManager);
				backend.SetPresentationFromPool(DrumPresentationPools[key]);

				var prevRand     = backend.rand;
				var absRealScore = math.abs(RhythmEngineUtility.GetScore(process.Elapsed, settings.BeatInterval));

				backend.play    = true;
				backend.key     = key;
				backend.rand    = DrumVariantCount[key];
				backend.perfect = absRealScore <= FlowPressure.Perfect;
				backend.endTime = Time.ElapsedTime + 1;

				var i = 0;
				while (prevRand == DrumVariantCount[key] && i < 3)
				{
					DrumVariantCount[key] = Random.Range(0, 2);
					i++;
				}
				
				key++;
			}

			Entities.ForEach((UIDrumPressureBackend backend) =>
			{
				var presentation = backend.Presentation;
				if (presentation != null)
				{
					if (backend.play)
					{
						var color = presentation.colors[backend.key - 1];
						color.a = 1;
						foreach (var effectImage in presentation.effectImages)
						{
							effectImage.color = color;
						}

						presentation.drumImage.sprite = presentation.sprites[backend.key - 1];

						backend.play = false;

						presentation.animator.SetBool(StrHashPerfect, backend.perfect);
						presentation.animator.SetInteger(StrHashKey, backend.key);
						presentation.animator.SetFloat(StrHashVariant, backend.rand);
						presentation.animator.SetTrigger(StrHashPlay);
					}

					presentation.animator.Update(Time.DeltaTime);
				}

				if (backend.endTime > Time.ElapsedTime)
					return;

				backend.Return(true, true);
			}).WithStructuralChanges().Run();

			return;
		}

		private GameObject CreateBackendDrumGameObject(AssetPool<GameObject> poolCaller)
		{
			var go = new GameObject("(Not Init) BackendPressure", typeof(RectTransform), typeof(UIDrumPressureBackend), typeof(GameObjectEntity));
			go.SetActive(false);
			go.GetComponent<UIDrumPressureBackend>().SetRootPool(poolCaller);

			return go;
		}
	}
}