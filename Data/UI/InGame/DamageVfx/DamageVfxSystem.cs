using System.Collections.Generic;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Misc;
using StormiumTeam.Shared.Gen;
using Unity.Collections;
using Unity.Entities;
using Revolution.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI.InGame.DamageVfx
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIDamageVfxSystem : ComponentSystem
	{
		[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
		public class Recover : GameBaseSystem
		{
			public  List<(uint, TargetDamageEvent)> DamageEvents = new List<(uint, TargetDamageEvent)>();
			private EntityQuery                     m_DamageEventQuery;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_DamageEventQuery = GetEntityQuery(typeof(GameEvent), typeof(TargetDamageEvent));
			}

			protected override void OnUpdate()
			{
				if (!IsPresentationActive)
					return;

				TargetDamageEvent damageEvent = default;
				GameEvent         gameEvent   = default;
				foreach (var _ in this.ToEnumerator_DD(m_DamageEventQuery, ref damageEvent, ref gameEvent))
				{
					DamageEvents.Add((gameEvent.SnapshotTick, damageEvent));
				}
			}
		}

		private struct Pool
		{
			public AsyncAssetPool<GameObject> Presentation;
			public AssetPool<GameObject>      Backend;
		}

		private const string KeyBase = "int:UI/InGame/Effects/Damage/";

		private Pool m_PopTextPool;
		private Pool m_EffectPool;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PopTextPool = new Pool
			{
				Presentation = new AsyncAssetPool<GameObject>(KeyBase + "DamageTextPopup/DamageTextPop.prefab"),
				Backend = new AssetPool<GameObject>(pool =>
				{
					var gameObject = new GameObject("DamageTextPop Backend", typeof(DamagePopTextVfxBackend), typeof(GameObjectEntity));
					gameObject.SetActive(false);

					var backend = gameObject.GetComponent<DamagePopTextVfxBackend>();
					backend.SetRootPool(pool);

					return gameObject;
				}, World)
			};
		}

		protected override void OnUpdate()
		{
			var currentTick = World.GetExistingSystem<NetworkTimeSystem>().interpolateTargetTick;
			var recover     = World.GetExistingSystem<Recover>();

			for (var i = 0; i != recover.DamageEvents.Count; i++)
			{
				var (tick, ev) = recover.DamageEvents[i];
				if (currentTick < tick)
					continue;

				var textPopBackendGameObject = m_PopTextPool.Backend.Dequeue();
				using (new SetTemporaryActiveWorld(World))
				{
					textPopBackendGameObject.SetActive(true);
				}

				var textPopBackend = textPopBackendGameObject.GetComponent<DamagePopTextVfxBackend>();

				textPopBackend.SetTarget(EntityManager);
				textPopBackend.SetPresentationFromPool(m_PopTextPool.Presentation);
				textPopBackend.Play(ev);
				textPopBackend.SetToPoolAt          = Time.time + 2f;
				textPopBackend.transform.localScale = Vector3.one * 0.5f;

				EntityManager.AddComponentData(textPopBackend.BackendEntity, new RuntimeAssetDisable());

				recover.DamageEvents.RemoveAtSwapBack(i);
				i--;
			}
		}
	}
}