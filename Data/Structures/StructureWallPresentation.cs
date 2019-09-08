using System.Collections.Generic;
using MonoComponents;
using Patapon4TLB.GameModes;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Misc;
using StormiumTeam.GameBase.Modules;
using Unity.Entities;
using Unity.Mathematics;
using Revolution.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Default.Test.Structures
{
	public class StructureWallPresentation : RuntimeAssetPresentation<StructureWallPresentation>
	{
		public MaterialPropertyBlock mpb;
		public List<Renderer>        rendererArray;
		public List<Renderer>        rendererWithTeamColorArray;
		public float                 reintegrationProgress;

		public Animator animator;

		private static readonly int TintPropertyId     = Shader.PropertyToID("_Color");
		private static readonly int ProgressPropertyId = Shader.PropertyToID("_Progress");
		private static readonly int DeadPropertyId = Animator.StringToHash("Dead");

		private void OnEnable()
		{
			mpb = new MaterialPropertyBlock();
			reintegrationProgress = float.NegativeInfinity;
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		private float m_Reintegration;

		internal void OnUpdate(StructureWallBackend backend, bool teamUpdate, bool hasTeam, bool healthUpdate, bool isDead)
		{
			if (teamUpdate)
			{
				if (hasTeam)
				{
					OnCreate();
					m_Reintegration = 0.0f;
				}
			}

			if (healthUpdate && isDead)
			{
				m_Reintegration = 0.0f;
			}
			
			animator.SetBool(DeadPropertyId, isDead);

			m_Reintegration += Time.deltaTime;
			
			if (!hasTeam)
			{
				SetReintegrationProgress(0, true);
			}
			else
			{
				if (isDead)
				{
					SetReintegrationProgress(math.clamp(1 - m_Reintegration, 0, 1), true);
				}
				else
				{
					SetReintegrationProgress(m_Reintegration * 3f, true);	
				}
			}
		}

		public void SetTeamColor(Color color)
		{
			for (var i = 0; i != rendererWithTeamColorArray.Count; i++)
			{
				rendererWithTeamColorArray[i].GetPropertyBlock(mpb);
				{
					mpb.SetColor(TintPropertyId, color);
				}
				rendererWithTeamColorArray[i].SetPropertyBlock(mpb);
			}
		}

		public void SetReintegrationProgress(float progress, bool force = false)
		{
			if (progress.Equals(reintegrationProgress) && !force)
				return;

			reintegrationProgress = progress;
			for (var i = 0; i != rendererArray.Count; i++)
			{
				rendererArray[i].GetPropertyBlock(mpb);
				{
					mpb.SetFloat(ProgressPropertyId, progress * 2.5f);
				}
				rendererArray[i].SetPropertyBlock(mpb);
			}
		}

		public void OnCreate()
		{
			animator.SetTrigger("Create");
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class StructureWallBackend : RuntimeAssetBackend<StructureWallPresentation>
	{
		public bool IsDead;
		public bool HasTeam;

		public class Process : GameBaseSystem
		{
			private ModuleGetAssetFromGuid m_ModuleGetAssetFromGuid;

			protected override void OnCreate()
			{
				base.OnCreate();

				GetModule(out m_ModuleGetAssetFromGuid);
			}

			protected override void OnUpdate()
			{
				Entities.ForEach((StructureWallBackend backend) =>
				{
					if (!EntityManager.Exists(backend.DstEntity))
					{
						backend.Return(true, true);
						return;
					}

					if (!backend.HasIncomingPresentation)
					{
						var targetPool = StaticSceneResourceHolder.GetPool("versus:wall/wood");
						if (targetPool != null)
						{
							backend.SetPresentationFromPool(targetPool);
						}

						return;
					}

					if (backend.Presentation == null)
						return;

					var hadTeam   = backend.HasTeam;
					var wasDead   = backend.IsDead;
					var direction = 1;

					var presentation = backend.Presentation;
					var chunk        = EntityManager.GetChunk(backend.DstEntity);
					var comps        = chunk.Archetype.GetComponentTypes();
					for (var i = 0; i != comps.Length; i++)
					{
						if (comps[i].GetManagedType() == typeof(Relative<TeamDescription>))
						{
							var teamDesc = EntityManager.GetComponentData<Relative<TeamDescription>>(backend.DstEntity);
							if (teamDesc.Target == default || !EntityManager.HasComponent<Relative<ClubDescription>>(teamDesc.Target))
							{
								backend.HasTeam = false;
								continue;
							}

							var clubInfo = EntityManager.GetComponentData<ClubInformation>(EntityManager.GetComponentData<Relative<ClubDescription>>(teamDesc.Target).Target);
							presentation.SetTeamColor(clubInfo.PrimaryColor);

							if (EntityManager.HasComponent<TeamDirection>(teamDesc.Target))
							{
								var teamDir = EntityManager.GetComponentData<TeamDirection>(teamDesc.Target);
								direction = teamDir.Value;
							}

							backend.HasTeam = true;
						}
					}

					var health = EntityManager.GetComponentData<LivableHealth>(backend.DstEntity);
					presentation.OnUpdate(backend, hadTeam != backend.HasTeam, backend.HasTeam, wasDead != health.ShouldBeDead(), health.ShouldBeDead());

					var pos = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
					pos.z += 200;

					backend.transform.position   = pos;
					backend.transform.localScale = new Vector3(direction, 1, 1);
				});
			}
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class StructureWallGenerateSystem : GameBaseSystem
	{
		public struct ToModel : IComponentData
		{
			public Entity Target;
		}

		private EntityQuery           m_StructureWithoutModelQuery;
		private AssetPool<GameObject> m_BackendPool;

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
				var gameObject = new GameObject("Wall Backend", typeof(StructureWallBackend), typeof(GameObjectEntity));
				gameObject.SetActive(false);
				
				return gameObject;
			}, World);
		}

		protected override void OnUpdate()
		{
			Entities.With(m_StructureWithoutModelQuery).ForEach((Entity entity, ref HeadOnStructure structure) =>
			{
				if (structure.Type != HeadOnStructure.EType.Wall)
					return;

				using (new SetTemporaryActiveWorld(World))
				{
					var backendGameObject = m_BackendPool.Dequeue();
					var backend           = backendGameObject.GetComponent<StructureWallBackend>();

					backend.SetTarget(EntityManager, entity);
					backendGameObject.SetActive(true);
					
					EntityManager.AddComponentData(entity, new ToModel {Target = backend.DstEntity});
				}
			});
		}
	}
}