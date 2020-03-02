using System.Collections.Generic;
using Systems;
using DefaultNamespace;
using Misc;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Client.OrderSystems;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Misc;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Entity = Unity.Entities.Entity;
using EntityQuery = Unity.Entities.EntityQuery;
using Random = UnityEngine.Random;

namespace package.patapon.core.Models.InGame.UIDrum
{
	public class UIDrumPressurePresentation : RuntimeAssetPresentation<UIDrumPressurePresentation>
	{
		public Animator animator;

		public SVGImage drumImage;
		public SVGImage[] effectImages;

		public Sprite[] sprites;
		public Color[]  colors;

		private void Awake()
		{
			animator.enabled = false;
		}
	}

	public class UIDrumPressureBackend : RuntimeAssetBackend<UIDrumPressurePresentation>
	{
		public int   key, rand;
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

		private Canvas m_Canvas;
		private EntityQuery m_CameraQuery;
		private EntityQuery m_EngineQuery;
		
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
			                            .GetFile("UIDrumPressure.prefab");
			for (var i = 1; i <= 4; i++)
			{
				DrumPresentationPools[i] = new AsyncAssetPool<GameObject>(address);
				DrumBackendPools[i]      = new AssetPool<GameObject>(CreateBackendDrumGameObject, World);
				DrumVariantCount[i]      = 0;
			}

			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
		}

		// Set canvas order
		protected override void OnStartRunning()
		{
			base.OnStartRunning();
			
			m_Canvas.sortingLayerID = SortingLayer.NameToID("OverlayUI");
			m_Canvas.sortingOrder = (int) World.GetOrCreateSystem<UIDrumPressureOrderingSystem>().Order;
		}

		protected override void OnUpdate()
		{
			Entity cameraEntity   = default;
			float3 cameraPosition = default;
			if (m_CameraQuery.CalculateEntityCount() > 0)
			{
				cameraEntity = m_CameraQuery.GetSingletonEntity();
				var cameraObject = EntityManager.GetComponentObject<Camera>(cameraEntity);
				cameraPosition = cameraObject.transform.position;
			}

			var player  = this.GetFirstSelfGamePlayer();
			if (!EntityManager.TryGetComponentData(player, out GamePlayerCommand playerCommand))
				return;

			var cameraState = this.GetComputedCameraState();

			var isWorldSpace = cameraState.StateData.Target != default;
			if (!EntityManager.HasComponent<UnitDescription>(cameraState.StateData.Target))
				isWorldSpace = false;
			
			var canvasRect   = m_Canvas.pixelRect;
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
				m_Canvas.worldCamera        = EntityManager.GetComponentObject<Camera>(cameraEntity);
			}
			
			var pixelRange     = new float2(canvasRect.width, canvasRect.height);

			Entity engine = default;
			if (cameraState.StateData.Target != default)
				engine = PlayerComponentFinder.GetComponentFromPlayer<RhythmEngineDescription>(EntityManager, m_EngineQuery, cameraState.StateData.Target, player);
			else
				engine = PlayerComponentFinder.FindPlayerComponent(m_EngineQuery, player);

			if (engine == default)
				return;
			
			var process  = EntityManager.GetComponentData<FlowEngineProcess>(engine);
			var settings = EntityManager.GetComponentData<RhythmEngineSettings>(engine);
			var state = EntityManager.GetComponentData<RhythmEngineState>(engine);

			if (state.IsPaused || process.GetFlowBeat(settings.BeatInterval) < 0)
			{
				// destroy everything!
				Entities.ForEach((UIDrumPressureBackend backend) =>
				{
					backend.Return(true, true);
				}).WithStructuralChanges().Run();
				return;
			}

			var key = 1;
			foreach (var ac in playerCommand.Base.GetRhythmActions())
			{
				if (!ac.WasPressed)
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
				using (new SetTemporaryActiveWorld(World))
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
				var absRealScore = math.abs(FlowEngineProcess.GetScore(process.Milliseconds, settings.BeatInterval));

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
			}

			Entities.ForEach((UIDrumPressureBackend backend) =>
			{
				var presentation = backend.Presentation;
				if (presentation != null)
				{
					if (backend.play)
					{
						var color = presentation.colors[backend.key - 1];
						color.a                               = 1;
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