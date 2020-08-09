using package.stormiumteam.shared.ecs;
using PataNext.Client.PoolingSystems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Graphics.Models.InGame.Multiplayer
{
	public class UIPlayerTargetCursorPresentation : RuntimeAssetPresentation<UIPlayerTargetCursorPresentation>
	{
		private bool[] m_ActiveStates;

		public GameObject Controlled;
		public GameObject NotOwned;

		public Renderer[] RendererForColor;

		public MaterialPropertyBlock mpb;
		
		private static readonly int BaseColorId = Shader.PropertyToID("_Color");

		private void OnEnable()
		{
			m_ActiveStates = new bool[2];
			Controlled.SetActive(false);
			NotOwned.SetActive(false);
			
			mpb = new MaterialPropertyBlock();
		}

		private void OnDisable()
		{
			mpb.Clear();
			mpb = null;
		}

		public void SetActive(int index, bool state)
		{
			if (m_ActiveStates[index] == state)
				return;

			m_ActiveStates[index] = state;
			switch (index)
			{
				case 0:
					Controlled.SetActive(state);
					break;
				case 1:
					NotOwned.SetActive(state);
					break;
			}
		}

		public void SetColor(Color target)
		{
			foreach (var r in RendererForColor)
			{
				r.GetPropertyBlock(mpb);
				{
					mpb.SetColor(BaseColorId, target);
				}
				r.SetPropertyBlock(mpb);
			}
		}
	}

	public class UIPlayerTargetCursorBackend : RuntimeAssetBackend<UIPlayerTargetCursorPresentation>
	{
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	[UpdateAfter(typeof(UIPlayerTargetCursorPoolingSystem))]
	public class UIPlayerTargetCursorRenderSystem : BaseRenderSystem<UIPlayerTargetCursorPresentation>
	{
		public Entity LocalPlayer;

		protected override void PrepareValues()
		{
			LocalPlayer = this.GetFirstSelfGamePlayer();
		}

		protected override void Render(UIPlayerTargetCursorPresentation definition)
		{
			var backend      = definition.Backend;
			var targetEntity = definition.Backend.DstEntity;
			backend.transform.position = new Vector3
			{
				x = EntityManager.GetComponentData<Translation>(targetEntity).Value.x,
				y = -0.3f
			};

			if (!EntityManager.TryGetComponentData(targetEntity, out Relative<PlayerDescription> relativePlayer))
				return;

			if (relativePlayer.Target == Entity.Null)
			{
				definition.SetActive(0, false);
				definition.SetActive(1, false);
				return;
			}

			definition.SetActive(0, LocalPlayer == relativePlayer.Target);
			definition.SetActive(1, LocalPlayer != relativePlayer.Target);

			/*EntityManager.TryGetComponentData(targetEntity, out var relativeTeam, new Relative<TeamDescription>(targetEntity));
			EntityManager.TryGetComponentData(relativeTeam.Target, out var relativeClub, new Relative<ClubDescription>(targetEntity));
			EntityManager.TryGetComponentData(relativeClub.Target, out ClubInformation clubInformation);

			definition.SetColor(clubInformation.PrimaryColor);*/
		}

		protected override void ClearValues()
		{
		}
	}
}