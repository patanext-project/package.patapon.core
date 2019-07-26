using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using Runtime.Misc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.Shared.Gen;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI.InGame.DamageVfx
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UIDamageVfxSystem : ComponentSystem
	{
		[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
		public class Recover : GameBaseSystem
		{
			public  List<TargetDamageEvent> DamageEvents = new List<TargetDamageEvent>();
			private EntityQuery             m_DamageEventQuery;

			protected override void OnCreateManager()
			{
				base.OnCreateManager();

				m_DamageEventQuery = GetEntityQuery(typeof(GameEvent), typeof(TargetDamageEvent));
			}

			protected override void OnUpdate()
			{
				if (!IsPresentationActive)
					return;

				TargetDamageEvent damageEvent = default;
				foreach (var _ in this.ToEnumerator_D(m_DamageEventQuery, ref damageEvent))
				{
					DamageEvents.Add(damageEvent);
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
			var recover = World.GetExistingSystem<Recover>();
			foreach (var damageEvent in recover.DamageEvents)
			{
				var textPopBackendGameObject = m_PopTextPool.Backend.Dequeue();
				using (new SetTemporaryActiveWorld(World))
				{
					textPopBackendGameObject.SetActive(true);
				}

				var textPopBackend = textPopBackendGameObject.GetComponent<DamagePopTextVfxBackend>();

				textPopBackend.SetTarget(EntityManager);
				textPopBackend.SetPresentation(m_PopTextPool.Presentation);
				textPopBackend.Play(damageEvent);
				textPopBackend.SetToPoolAt = Time.time + 2f;
				textPopBackend.transform.localScale = Vector3.one * 0.5f;

				EntityManager.AddComponentData(textPopBackend.BackendEntity, new RuntimeAssetDisable());
			}

			recover.DamageEvents.Clear();
		}
	}
}