using System.Collections.Generic;
using package.patapon.core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
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
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class UIDrumPressureSystem : GameBaseSystem
	{
		public Dictionary<int, AsyncAssetPool<GameObject>> DrumPresentationPools = new Dictionary<int, AsyncAssetPool<GameObject>>();
		public Dictionary<int, AssetPool<GameObject>>      DrumBackendPools      = new Dictionary<int, AssetPool<GameObject>>();
		
		public Dictionary<int, int> DrumVariantCount = new Dictionary<int, int>();

		private EntityQuery m_DrumCanvasQuery;

		private static readonly int StrHashPlay    = Animator.StringToHash("Play");
		private static readonly int StrHashVariant = Animator.StringToHash("Variant");
		private static readonly int StrHashKey     = Animator.StringToHash("Key");
		private static readonly int StrHashPerfect = Animator.StringToHash("Perfect");

		protected override void OnCreate()
		{
			base.OnCreate();

			for (var i = 1; i <= 4; i++)
			{
				DrumPresentationPools[i] = new AsyncAssetPool<GameObject>("int:RhythmEngine/UI/DrumPressure");
				DrumBackendPools[i]      = new AssetPool<GameObject>(CreateBackendDrumGameObject, World);
				DrumVariantCount[i]      = 0;

				Debug.Log("Created with " + i);
			}

			m_DrumCanvasQuery = GetEntityQuery(typeof(UIDrumCanvas));
		}

		protected override void OnUpdate()
		{
			var internalSystem = GetActiveClientWorld()?.GetExistingSystem<UIDrumPressureSystemClientInternal>();
			if (internalSystem != null)
			{
				var canvas = EntityManager.GetComponentObject<Canvas>(m_DrumCanvasQuery.GetSingletonEntity());

				foreach (var ev in internalSystem.Events)
				{
					var keyPos = new Vector3();
					if (ev.Key <= 2)
					{
						if (ev.Key == 1)
							keyPos.x = -35f;
						else
							keyPos.x = 35f;

						keyPos.x += Random.Range(-2.5f, 2.5f);
						keyPos.y =  Random.Range(-10f, 10f);
					}
					else
					{
						if (ev.Key == 3)
							keyPos.y = -37.5f;
						else
							keyPos.y = 37.5f;

						keyPos.y += Random.Range(-2.5f, 2.5f);
						keyPos.x =  Random.Range(-10f, 10f);
					}

					var beGameObject = DrumBackendPools[ev.Key].Dequeue();
					beGameObject.name = $"BackendPressure (Key: {ev.Key})";
					beGameObject.SetActive(true);
					beGameObject.transform.SetParent(canvas.transform, false);
					beGameObject.transform.localScale    = new Vector3(0.5f, 0.5f, 0.5f);
					beGameObject.transform.localPosition = keyPos;
					beGameObject.transform.rotation      = Quaternion.Euler(0, 0, Random.Range(-12.5f, 12.5f));

					var backend = beGameObject.GetComponent<UIDrumPressureBackend>();
					backend.OnReset();
					backend.SetFromPool(DrumPresentationPools[ev.Key], internalSystem.EntityManager);

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
			var go = new GameObject("(Not Init) BackendPressure", typeof(GameObjectEntity), typeof(UIDrumPressureBackend));
			go.SetActive(false);
			go.GetComponent<UIDrumPressureBackend>().SetRootPool(poolCaller);

			return go;
		}
	}
}