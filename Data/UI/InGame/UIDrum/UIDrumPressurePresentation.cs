using System.Collections.Generic;
using package.patapon.core;
using Patapon4TLB.Default;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Misc;
using StormiumTeam.Shared.Gen;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Patapon4TLB.UI
{
	public class UIDrumPressurePresentation : RuntimeAssetPresentation<UIDrumPressurePresentation>
	{
		public Animator animator;

		public Image effectImage, drumImage;

		public Sprite[] sprites;
		public Color[]  colors;
	}

	public class UIDrumPressureBackend : RuntimeAssetBackend<UIDrumPressurePresentation>
	{
		public int   key, rand;
		public float endTime;

		public bool perfect;

		public bool play;

		public override void OnTargetUpdate()
		{
			DstEntityManager.AddComponent(BackendEntity, typeof(RuntimeAssetDisable));
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIDrumPressureSystemClientInternal : ComponentSystem
	{
		public List<PressureEvent> Events = new List<PressureEvent>();
		private EntityQuery m_Query;

		protected override void OnCreate() => m_Query = GetEntityQuery(ComponentType.ReadWrite<PressureEvent>());

		protected override void OnUpdate()
		{
			Events.Clear();

			PressureEvent ev = default;
			foreach (var _ in this.ToEnumerator_D(m_Query, ref ev))
			{
				if (!EntityManager.HasComponent<RhythmEngineSimulateTag>(ev.Engine))
					continue;

				Events.Add(ev);			
			}
		}

		protected override void OnStopRunning()
		{
			base.OnStopRunning();
			Events.Clear();
		}
	}

	[AlwaysUpdateSystem]
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIDrumPressureSystem : UIGameSystemBase
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
		private EntityQuery m_BackendQuery;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_Canvas = World.GetOrCreateSystem<UIClientCanvasSystem>().CreateCanvas(out _, "UIDrumCanvas");
			m_Canvas.renderMode = RenderMode.WorldSpace;
			m_Canvas.sortingOrder = (int) UICanvasOrder.Drums;
			m_Canvas.sortingLayerName = "OverlayUI";
			m_Canvas.transform.localScale = new Vector3() * 0.05f;

			m_CameraQuery = GetEntityQuery(typeof(GameCamera));

			for (var i = 1; i <= 4; i++)
			{
				DrumPresentationPools[i] = new AsyncAssetPool<GameObject>("int:RhythmEngine/UI/DrumPressure");
				DrumBackendPools[i]      = new AssetPool<GameObject>(CreateBackendDrumGameObject, World);
				DrumVariantCount[i]      = 0;

				Debug.Log("Created with " + i);
			}

			m_BackendQuery = GetEntityQuery(typeof(UIDrumPressureBackend), typeof(RuntimeAssetDisable));
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

			var currentGamePlayer  = GetFirstSelfGamePlayer();
			var currentCameraState = GetCurrentCameraState(currentGamePlayer);

			var isWorldSpace = currentCameraState.Target != default;
			var canvasRect   = m_Canvas.pixelRect;
			if (isWorldSpace && cameraEntity != default)
			{
				var translation = EntityManager.GetComponentData<Translation>(currentCameraState.Target);
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

			var internalSystem = World.GetExistingSystem<UIDrumPressureSystemClientInternal>();
			var pixelRange     = new float2(canvasRect.width, canvasRect.height);

			UIDrumPressureBackend backend = null;
			foreach (var ev in internalSystem.Events)
			{
				Debug.Log("event!");
				
				var keyRange = new float2();
				if (ev.Key <= 2)
				{
					if (ev.Key == 1)
						keyRange.x = -0.35f;
					else
						keyRange.x = 0.35f;

					keyRange.x += Random.Range(-0.025f, 0.025f);
					keyRange.y =  Random.Range(-0.1f, 0.1f);
				}
				else
				{
					if (ev.Key == 3)
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

				var beGameObject = DrumBackendPools[ev.Key].Dequeue();
				using (new SetTemporaryActiveWorld(World))
				{
					beGameObject.name = $"BackendPressure (Key: {ev.Key})";
					beGameObject.SetActive(true);
					beGameObject.transform.SetParent(m_Canvas.transform, false);
					beGameObject.transform.localScale = m_Canvas.renderMode == RenderMode.WorldSpace
						? Vector3.one * 0.7f
						: 0.008f * math.min(width, height) * Vector3.one;
					beGameObject.transform.localPosition = new Vector3(keyPos.x, keyPos.y, 0);
					beGameObject.transform.rotation      = Quaternion.Euler(0, 0, Random.Range(-12.5f, 12.5f));
				}

				backend = beGameObject.GetComponent<UIDrumPressureBackend>();
				backend.OnReset();
				backend.SetTarget(EntityManager);
				backend.SetPresentationFromPool(DrumPresentationPools[ev.Key]);

				var prevRand = backend.rand;

				backend.play    = true;
				backend.key     = ev.Key;
				backend.rand    = DrumVariantCount[ev.Key];
				backend.perfect = math.abs(ev.Score) <= 0.15f;
				backend.endTime = Time.time + 1f;

				var i = 0;
				while (prevRand == DrumVariantCount[ev.Key] && i < 3)
				{
					DrumVariantCount[ev.Key] = Random.Range(0, 2);
					i++;
				}
			}

			RuntimeAssetDisable disable = default;
			foreach (var _ in this.ToEnumerator_DC(m_BackendQuery, ref disable, ref backend))
			{
				var presentation = backend.Presentation;
				if (presentation != null)
				{
					if (backend.play)
					{
						var color = presentation.colors[backend.key - 1];
						color.a                               = 0;
						presentation.effectImage.color        = color;
						presentation.drumImage.overrideSprite = presentation.sprites[backend.key - 1];

						backend.play = false;

						presentation.animator.SetBool(StrHashPerfect, backend.perfect);
						presentation.animator.SetInteger(StrHashKey, backend.key);
						presentation.animator.SetFloat(StrHashVariant, backend.rand);
						presentation.animator.SetTrigger(StrHashPlay);
					}
				}

				if (backend.endTime > Time.time)
					continue;
				
				disable.IgnoreParent = true;
				disable.ReturnPresentation = true;
				disable.DisableGameObject = true;
				disable.ReturnToPool = true;
			}
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