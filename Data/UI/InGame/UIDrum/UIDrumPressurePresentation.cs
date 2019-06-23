using System;
using System.Collections.Generic;
using package.patapon.core;
using Patapon4TLB.Default;
using Patapon4TLB.UI.InGame;
using Runtime.Misc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Patapon4TLB.UI
{
	public class UIDrumPressurePresentation : CustomAsyncAssetPresentation<UIDrumPressurePresentation>
	{
		public Animator animator;

		public Image effectImage, drumImage;

		public Sprite[] sprites;
		public Color[]  colors;

		private void OnEnable()
		{
			//animator.enabled = false; // manual update
		}
	}

	public class UIDrumPressureBackend : CustomAsyncAsset<UIDrumPressurePresentation>
	{
		public int   key, rand;
		public float endTime;

		public bool perfect;

		public bool play;
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIDrumPressureSystemClientInternal : ComponentSystem
	{
		public List<PressureEvent> Events = new List<PressureEvent>();

		protected override void OnUpdate()
		{
			Events.Clear();

			Entities.ForEach((ref PressureEvent ev) =>
			{
				if (!EntityManager.HasComponent<RhythmEngineSimulateTag>(ev.Engine))
					return;

				Events.Add(ev);
			});
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
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_Canvas = World.GetOrCreateSystem<UIClientCanvasSystem>().CreateCanvas(out _, "UIDrumCanvas");
			m_Canvas.renderMode = RenderMode.WorldSpace;
			m_Canvas.transform.localScale = new Vector3() * 0.05f;

			m_CameraQuery = GetEntityQuery(typeof(GameCamera));

			for (var i = 1; i <= 4; i++)
			{
				DrumPresentationPools[i] = new AsyncAssetPool<GameObject>("int:RhythmEngine/UI/DrumPressure");
				DrumBackendPools[i]      = new AssetPool<GameObject>(CreateBackendDrumGameObject, World);
				DrumVariantCount[i]      = 0;

				Debug.Log("Created with " + i);
			}
		}

		protected override void OnUpdate()
		{
			var cameraEntity = m_CameraQuery.GetSingletonEntity();
			var cameraObject = EntityManager.GetComponentObject<Camera>(cameraEntity);
			var cameraPosition = cameraObject.transform.position;
			
			var currentGamePlayer = GetFirstSelfGamePlayer();
			var currentCameraState = GetCurrentCameraState(currentGamePlayer);

			var isWorldSpace = currentCameraState.Target != default;
			if (isWorldSpace)
			{
				var translation = EntityManager.GetComponentData<Translation>(currentCameraState.Target);
				m_Canvas.transform.position = new Vector3(translation.Value.x, translation.Value.y, cameraPosition.z + 10);

				var rectTransform = m_Canvas.GetComponent<RectTransform>();
				rectTransform.sizeDelta = new Vector2(100, 100);
			}
			else
			{
				m_Canvas.transform.position = Vector3.zero;
				m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			}

			var canvasRect = m_Canvas.pixelRect;
			
			var internalSystem = World.GetExistingSystem<UIDrumPressureSystemClientInternal>();
			var pixelRange = new float2(canvasRect.width, canvasRect.height);
			foreach (var ev in internalSystem.Events)
			{
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

				/*if (!isWorldSpace)
				{
					keyPos.x += canvasRect.width * 0.4f * Math.Sign(keyPos.x);
					keyPos.y += canvasRect.height * 0.4f * Math.Sign(keyPos.y);
				}*/
				keyRange += 0.5f;
				
				var width = pixelRange.x * 0.5f;
				var height = pixelRange.y * 0.5f;
				var keyPos = new float2(math.lerp(-width, width, keyRange.x), math.lerp(-height, height, keyRange.y));

				Debug.Log(keyPos + ", " + keyRange + ", " + pixelRange);
				
				var beGameObject = DrumBackendPools[ev.Key].Dequeue();
				var rectTransform = beGameObject.GetComponent<RectTransform>();
				using (new SetTemporaryActiveWorld(World))
				{
					beGameObject.name = $"BackendPressure (Key: {ev.Key})";
					beGameObject.SetActive(true);
					beGameObject.transform.SetParent(m_Canvas.transform, false);
					beGameObject.transform.localScale = m_Canvas.renderMode == RenderMode.WorldSpace
						? Vector3.one * 0.5f
						: 0.75f * (canvasRect.width / canvasRect.height) * Vector3.one;
					beGameObject.transform.position = new Vector3(keyPos.x, keyPos.y, 0);
					beGameObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-12.5f, 12.5f));
				}

				var backend = beGameObject.GetComponent<UIDrumPressureBackend>();
				backend.OnReset();
				backend.SetFromPool(DrumPresentationPools[ev.Key], EntityManager);

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

				Debug.Log("KeyRand: " + backend.rand);
			}

			Entities.ForEach((UIDrumPressureBackend backend) =>
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

					//presentation.animator.Update(Time.deltaTime);
				}

				if (backend.endTime > Time.time)
					return;

				backend.DisableNextUpdate                 = true;
				backend.ReturnToPoolOnDisable             = true;
				backend.ReturnPresentationToPoolNextFrame = true;
			});
		}

		private GameObject CreateBackendDrumGameObject(AssetPool<GameObject> poolCaller)
		{
			var go = new GameObject("(Not Init) BackendPressure", typeof(RectTransform), typeof(UIDrumPressureBackend), typeof(GameObjectEntity));
			go.SetActive(false);
			go.GetComponent<UIDrumPressureBackend>().SetRootPool(poolCaller);

			/*var rectTransform = go.GetComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;*/

			return go;
		}
	}
}