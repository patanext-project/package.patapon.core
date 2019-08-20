using System.Collections.Generic;
using Patapon4TLB.GameModes;
using Patapon4TLB.UI.InGame;
using Runtime.Misc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Default.Test.Structures
{
	public class StructureSpawnPresentation : RuntimeAssetPresentation<StructureSpawnPresentation>
	{
		private static readonly int TintPropertyId = Shader.PropertyToID("_Color");

		public Animator animator;

		public MaterialPropertyBlock mpb;
		public List<Renderer>        rendererPrimaryColor;
		public List<Renderer>        rendererSecondaryColor;
		public List<ParticleSystem> particleSystems;

		private void OnEnable()
		{
			mpb = new MaterialPropertyBlock();
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		public void SetColors(Color primary, Color secondary)
		{
			for (var i = 0; i != rendererPrimaryColor.Count; i++)
			{
				rendererPrimaryColor[i].GetPropertyBlock(mpb);
				{
					mpb.SetColor(TintPropertyId, primary);
				}
				rendererPrimaryColor[i].SetPropertyBlock(mpb);
			}

			for (var i = 0; i != rendererSecondaryColor.Count; i++)
			{
				rendererSecondaryColor[i].GetPropertyBlock(mpb);
				{
					mpb.SetColor(TintPropertyId, secondary);
				}
				rendererSecondaryColor[i].SetPropertyBlock(mpb);
			}

			for (var i = 0; i != particleSystems.Count; i++)
			{
				var module = particleSystems[i].main;
				module.startColor = secondary;
			}
		}
	}

	public class StructureSpawnBackend : RuntimeAssetBackend<StructureSpawnPresentation>
	{
		private bool m_HasTeam;
		
		private static readonly int AnimatorKeyIsSet = Animator.StringToHash("IsSet");
		private static readonly int AnimatorKeyOnCreate = Animator.StringToHash("OnCreate");

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class ProcessSystem : UIGameSystemBase
		{
			private Entity m_SpectatedEntity;
			
			protected override void OnUpdate()
			{
				m_SpectatedEntity = default;
				
				var self = GetFirstSelfGamePlayer();
				if (self != default)
				{
					var cameraState = GetCurrentCameraState(self);
					if (cameraState.Target != default)
					{
						m_SpectatedEntity = cameraState.Target;
					}
				}
					
				Entities.ForEach((StructureSpawnBackend backend) =>
				{
					if (!EntityManager.Exists(backend.DstEntity))
					{
						backend.Return(true, true);
						return;
					}

					if (backend.Presentation == null)
						return;

					var presentation = backend.Presentation;
					var pos = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
					pos.z -= 10;
					pos.y += 2f;
					
					backend.transform.position = pos;

					Entity spectatedTeam = default;
					var direction = 1;
					var primaryColor = Color.white;
					var secondaryColor = Color.gray;
					if (m_SpectatedEntity != default)
					{
						if (TryGetRelative<TeamDescription>(m_SpectatedEntity, out spectatedTeam))
						{
							if (TryGetRelative<ClubDescription>(spectatedTeam, out var club))
							{
								var clubInfo = EntityManager.GetComponentData<ClubInformation>(club);
								primaryColor   = clubInfo.PrimaryColor;
								secondaryColor = clubInfo.SecondaryColor;
							}

							if (EntityManager.HasComponent<TeamDirection>(spectatedTeam))
							{
								direction = EntityManager.GetComponentData<TeamDirection>(spectatedTeam).Value;
							}
						}
					}

					var isSameTeam = false;
					var chunk = EntityManager.GetChunk(backend.DstEntity);
					var comps = chunk.Archetype.GetComponentTypes();
					for (var i = 0; i != comps.Length; i++)
					{
						if (comps[i].GetManagedType() == typeof(Relative<TeamDescription>))
						{
							var teamDesc = EntityManager.GetComponentData<Relative<TeamDescription>>(backend.DstEntity);
							if (teamDesc.Target == default)
							{
								continue;
							}

							isSameTeam = teamDesc.Target == spectatedTeam;

							if (!backend.m_HasTeam)
							{
								presentation.animator.SetTrigger(AnimatorKeyOnCreate);
							}
							backend.m_HasTeam = true;
						}
					}

					backend.transform.localScale = new Vector3(direction, 1, 1);
					
					presentation.SetColors(primaryColor, secondaryColor);
					presentation.animator.SetBool(AnimatorKeyIsSet, backend.m_HasTeam);
				});
			}
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class GenerateSystem : GameBaseSystem
		{
			public struct ToModel : IComponentData
			{
				public Entity Target;
			}
			
			private EntityQuery m_StructureWithoutModelQuery;
			private AssetPool<GameObject> m_BackendPool;
			private AsyncAssetPool<GameObject> m_PresentationPool;
			
			protected override void OnCreate()
			{
				base.OnCreate();
				
				m_StructureWithoutModelQuery = GetEntityQuery(new EntityQueryDesc
				{
					All  = new ComponentType[] {typeof(HeadOnStructure)},
					None = new ComponentType[] {typeof(ToModel)}
				});
				m_BackendPool = new AssetPool<GameObject>((pool) =>
				{
					var gameObject = new GameObject("StructureSpawn Backend", typeof(StructureSpawnBackend), typeof(GameObjectEntity));
					gameObject.SetActive(false);
				
					return gameObject;
				}, World);
				m_PresentationPool = new AsyncAssetPool<GameObject>("int:Structures/StructureSpawn/SpawnFakeUI.prefab");
			}

			protected override void OnUpdate()
			{
				Entities.With(m_StructureWithoutModelQuery).ForEach((Entity entity) =>
				{
					using (new SetTemporaryActiveWorld(World))
					{
						var backendGameObject = m_BackendPool.Dequeue();
						var backend           = backendGameObject.GetComponent<StructureSpawnBackend>();

						backend.SetTarget(EntityManager, entity);
						backend.SetPresentationFromPool(m_PresentationPool);
						backendGameObject.SetActive(true);

						EntityManager.AddComponentData(entity, new ToModel {Target = backend.DstEntity});
					}
				});
			}
		}
	}
}