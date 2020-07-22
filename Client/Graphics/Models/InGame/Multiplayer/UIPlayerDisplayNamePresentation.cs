using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Rules;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Graphics.Models.InGame.Multiplayer
{
	public class UIPlayerDisplayNamePresentation : RuntimeAssetPresentation<UIPlayerDisplayNamePresentation>
	{
		public TextMeshPro[] NameLabels;

		private NativeString512 m_LastName;
		private int m_LastIndex;

		public void SetName(int index, NativeString512 str)
		{
			if (m_LastName.Equals(str) && m_LastIndex != index)
				return;

			m_LastName = str;
			m_LastIndex = index;
			foreach (var label in NameLabels)
			{
				label.SetText(index + ". " + str.ToString());
			}
		}

		public void SetColor(Color color)
		{
			foreach (var label in NameLabels)
			{
				label.color = color;
			}
		}
	}

	public class UIPlayerDisplayNameBackend : RuntimeAssetBackend<UIPlayerDisplayNamePresentation>
	{

	}

	public class UIPlayerDisplayNameRenderSystem : BaseRenderSystem<UIPlayerDisplayNamePresentation>
	{
		public Color  DefaultColor;
		public Color  OwnedColor;
		public Entity LocalPlayer;

		private int m_Index;

		protected override void PrepareValues()
		{
			DefaultColor = GetSingleton<P4ColorRules.Data>().UnitNoTeamColor;
			OwnedColor   = GetSingleton<P4ColorRules.Data>().UnitOwnedColor;
			LocalPlayer  = this.GetFirstSelfGamePlayer();
			
			m_Index = 1;
		}

		protected override void Render(UIPlayerDisplayNamePresentation definition)
		{
			var targetEntity = definition.Backend.DstEntity;
			var targetPosition = EntityManager.GetComponentData<Translation>(targetEntity);
			definition.Backend.transform.position = new Vector3
			{
				x = targetPosition.Value.x,
				y = -0.3f
			};
			
			if (!EntityManager.TryGetComponentData(targetEntity, out Relative<PlayerDescription> relativePlayer))
				return;

			if (LocalPlayer == relativePlayer.Target)
				definition.SetColor(OwnedColor);
			else
			{
				// we don't get the team from the player, but from the entity (in case there is a gamemode where you control entities with multiple team)
				EntityManager.TryGetComponentData(targetEntity, out Relative<TeamDescription> relativeTeam);
				EntityManager.TryGetComponentData(relativeTeam.Target, out var relativeClub, new Relative<ClubDescription>(relativePlayer.Target));
				EntityManager.TryGetComponentData(relativeClub.Target, out var clubInfo, new ClubInformation
				{
					PrimaryColor = DefaultColor
				});

				definition.SetColor(clubInfo.PrimaryColor);
			}
			
			if (EntityManager.TryGetComponent(relativePlayer.Target, out PlayerName pn))
				definition.SetName(m_Index++, pn.Value);
			else
				definition.SetName(m_Index++, m_NoName);
		}
		
		private NativeString512 m_NoName = new NativeString512("NoName");

		protected override void ClearValues()
		{

		}
	}
}