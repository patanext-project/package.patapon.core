using System;
using package.stormiumteam.shared.ecs;
using Patapon.Client.PoolingSystems;
using Patapon.Mixed.GameModes.VSHeadOn;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Entity = Unity.Entities.Entity;

namespace Patapon4TLB.GameModes.Interface
{
	public enum DrawerAlignment
	{
		Top,
		Center,
		Bottom
	}
	
	public class UIHeadOnPresentation : RuntimeAssetPresentation<UIHeadOnPresentation>
	{
		public UIHeadOnScoreFrame[] ScoreFrames;
		public UIHeadOnDrawerFrame  DrawerFrame;

		public TextMeshProUGUI ChronoLabel;

		[NonSerialized]
		public float[] FlagPositions;

		[NonSerialized]
		private int m_PreviousTime;

		private ClubInformation[] m_ClubInformationArray;

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			Backend.DstEntityManager.World.GetExistingSystem<UIHeadOnUnitStatusPoolingSystem>()
			       .Reorder(DrawerFrame.UnitStatusFrame);
		}

		private void OnEnable()
		{
			Debug.Assert(ScoreFrames.Length == 2, "ScoreFrames.Length == 2");

			m_ClubInformationArray = new ClubInformation[2];
			FlagPositions          = new float[2];
			foreach (var frame in ScoreFrames)
			{
				foreach (var category in frame.Categories)
				{
					category.Label.text = "0";
				}
			}
		}

		public void SetTime(int seconds)
		{
			if (m_PreviousTime.Equals(seconds))
				return;
			m_PreviousTime = seconds;

			if (seconds < 0)
			{
				ChronoLabel.text = string.Empty;
				return;
			}

			var timespan = TimeSpan.FromSeconds(seconds);
			ChronoLabel.text = $"<mspace=0.46em>{timespan:mm}</mspace>:<mspace=0.46em>{timespan:ss}</mspace>";
		}

		public void SetScore(int category, int team, int score)
		{
			Debug.Assert(team >= 0 && team < ScoreFrames.Length, "team >= 0 && team < ScoreFrames.Length");
			var frame = ScoreFrames[team];
			frame.SetScore(category, score);
		}

		public void SetFlagPosition(float3 flag0Pos, float3 flag1Pos)
		{
			FlagPositions[0] = flag0Pos.x;
			FlagPositions[1] = flag1Pos.x;
		}

		public float3 GetPositionOnDrawer(float3 position, DrawerAlignment alignment, bool limit = true)
		{
			var t = math.unlerp(FlagPositions[0], FlagPositions[1], position.x);
			if (limit)
				t = math.clamp(t, 0, 1);

			return DrawerFrame.GetPosition(t, alignment);
		}

		public void UpdateClubInformation(ClubInformation team1, ClubInformation team2)
		{
			m_ClubInformationArray[0] = team1;
			m_ClubInformationArray[1] = team2;

			for (var index = 0; index < DrawerFrame.FlagSides.Length; index++)
			{
				var side = DrawerFrame.FlagSides[index];
				var team = m_ClubInformationArray[index];

				side.circleGraphic.color = team.PrimaryColor;
			}

			for (var index = 0; index < ScoreFrames.Length; index++)
			{
				var frame = ScoreFrames[index];
				var team  = m_ClubInformationArray[index];

				foreach (var cat in frame.Categories)
				{
					cat.Image.color = team.PrimaryColor;
				}
			}
		}
	}

	public class UIHeadOnBackend : RuntimeAssetBackend<UIHeadOnPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class UIHeadOnRenderSystem : BaseRenderSystem<UIHeadOnPresentation>
	{
		public int               ArenaTimeMs;
		public Vector3[]         FlagPositions;
		public ClubInformation[] Clubs;
		public int[]             TeamPoints;
		public int[]             TeamEliminations;

		private EntityQuery m_GameModeQuery;
		private EntityQuery m_FlagQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			FlagPositions    = new Vector3[2];
			Clubs            = new ClubInformation[2];
			TeamPoints       = new int[2];
			TeamEliminations = new int[2];

			m_GameModeQuery = GetEntityQuery(typeof(ReplicatedEntity), typeof(MpVersusHeadOn));
			m_FlagQuery     = GetEntityQuery(typeof(HeadOnFlag), typeof(Translation), typeof(Relative<TeamDescription>));

			RequireForUpdate(m_GameModeQuery);
			RequireForUpdate(m_FlagQuery);
		}

		protected override void PrepareValues()
		{
			var gameMode = EntityManager.GetComponentData<MpVersusHeadOn>(m_GameModeQuery.GetSingletonEntity());
			using (var entities = m_FlagQuery.ToEntityArray(Allocator.TempJob))
			using (var teamArray = m_FlagQuery.ToComponentDataArray<Relative<TeamDescription>>(Allocator.TempJob))
			{
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var teamTarget                                          = -1;
					if (teamArray[ent].Target == gameMode.Team0) teamTarget = 0;
					if (teamArray[ent].Target == gameMode.Team1) teamTarget = 1;

					if (teamTarget < 0)
						continue;

					FlagPositions[teamTarget] = EntityManager.GetComponentData<Translation>(entities[ent]).Value;
				}
			}

			for (var i = 0; i != 2; i++)
			{
				var team = i == 0 ? gameMode.Team0 : gameMode.Team1;

				EntityManager.TryGetComponentData(team, out var relativeClub, new Relative<ClubDescription>(team));
				EntityManager.TryGetComponentData(relativeClub.Target, out Clubs[i], new ClubInformation
				{
					Name           = new NativeString64("No Team"),
					PrimaryColor   = Color.cyan,
					SecondaryColor = Color.yellow
				});
				
				TeamPoints[i]       = gameMode.GetPoints(i);
				TeamEliminations[i] = gameMode.GetEliminations(i);
			}

			if (gameMode.EndTime > 0)
			{
				var endTimeSeconds = gameMode.EndTime / 1000;
				ArenaTimeMs = endTimeSeconds - (int) GetTick(false).Seconds;
			}
			else
			{
				ArenaTimeMs = -1;
			}
		}

		protected override void Render(UIHeadOnPresentation definition)
		{
			definition.SetFlagPosition(FlagPositions[0], FlagPositions[1]);
			definition.SetTime(ArenaTimeMs);
			definition.UpdateClubInformation(Clubs[0], Clubs[1]);
			for (var i = 0; i != 2; i++)
			{
				definition.SetScore(0, i, TeamPoints[i]);
				definition.SetScore(1, i, TeamEliminations[i]);
			}
		}

		protected override void ClearValues()
		{

		}
	}
}