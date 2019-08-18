using System;
using System.Collections.Generic;
using DefaultNamespace;
using Patapon4TLB.Core.BasicUnitSnapshot;
using Patapon4TLB.GameModes;
using Runtime.Misc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Modules;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Default.Test.Structures
{
	public class StructureWallPresentation : RuntimeAssetPresentation<StructureWallPresentation>
	{
		public MaterialPropertyBlock mpb;
		public List<Renderer>        rendererArray;
		public List<Renderer>        rendererWithTeamColorArray;
		public float                 reintegrationProgress;

		private static readonly int TintPropertyId     = Shader.PropertyToID("_Color");
		private static readonly int ProgressPropertyId = Shader.PropertyToID("_Progress");

		private void OnEnable()
		{
			mpb = new MaterialPropertyBlock();
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		private void Update()
		{
			reintegrationProgress += Time.deltaTime;
			if (Input.GetKeyDown(KeyCode.R))
			{
				GetComponent<Animator>().SetTrigger("Create");
				reintegrationProgress = 0;
			}

			SetReintegrationProgress(reintegrationProgress, true);
			SetTeamColor(Color.Lerp(Color.cyan, Color.black, 0.25f));
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
	}

	public class StructureWallBackend : RuntimeAssetBackend<StructureWallPresentation>
	{
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

					if (backend.Presentation == null)
						return;

					var presentation = backend.Presentation;
					var chunk        = EntityManager.GetChunk(backend.DstEntity);
					var comps        = chunk.Archetype.GetComponentTypes();
					for (var i = 0; i != comps.Length; i++)
					{
						if (comps[i].GetManagedType() == typeof(Relative<TeamDescription>))
						{
							var teamDesc = EntityManager.GetComponentData<Relative<TeamDescription>>(backend.DstEntity);
							if (teamDesc.Target == default || !EntityManager.HasComponent<Relative<ClubDescription>>(teamDesc.Target))
								continue;

							var clubInfo = EntityManager.GetComponentData<ClubInformation>(EntityManager.GetComponentData<Relative<ClubDescription>>(teamDesc.Target).Target);
							presentation.SetTeamColor(clubInfo.PrimaryColor);
						}
					}
				});
			}
		}
	}

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

					backendGameObject.SetActive(true);
					
					EntityManager.AddComponentData(entity, new ToModel {Target = backend.DstEntity});
				}
			});
		}
	}
}