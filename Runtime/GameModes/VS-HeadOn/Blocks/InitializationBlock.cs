using GmMachine;
using Misc.GmMachine.Contexts;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Revolution.NetCode;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public partial class MpVersusHeadOnGameMode
	{
		public class InitializationBlock : Block
		{
			public const int TeamCount = 2;

			public WorldContext        WorldCtx;
			public VersusHeadOnContext GameModeCtx;

			public InitializationBlock(string name) : base(name)
			{
			}

			protected override bool OnRun()
			{
				// -- get world systems
				var teamProvider = WorldCtx.GetExistingSystem<GameModeTeamProvider>();
				var clubProvider = WorldCtx.GetExistingSystem<ClubProvider>();

				// -- Create teams
				GameModeCtx.Teams = new Team[TeamCount];
				for (var t = 0; t != TeamCount; t++)
				{
					ref var team = ref GameModeCtx.Teams[t];
					team.Target = teamProvider.SpawnLocalEntityWithArguments(new GameModeTeamProvider.Create());

					// Add club...
					var club = clubProvider.SpawnLocalEntityWithArguments(new ClubProvider.Create
					{
						name           = new NativeString64(t == 0 ? "Blue" : "Red"),
						primaryColor   = Color.Lerp(t == 0 ? Color.blue : Color.red, Color.white, 0.33f),
						secondaryColor = Color.Lerp(Color.Lerp(t == 0 ? Color.blue : Color.red, Color.white, 0.15f), Color.black, 0.15f)
					});
					WorldCtx.EntityMgr.AddComponentData(team.Target, new Relative<ClubDescription> {Target = club});
					WorldCtx.EntityMgr.AddComponent(club, typeof(GhostComponent));
				}

				// -- Set enemies of each team
				for (var t = 0; t != TeamCount; t++)
				{
					var enemies = WorldCtx.EntityMgr.GetBuffer<TeamEnemies>(GameModeCtx.Teams[t].Target);
					enemies.Add(new TeamEnemies {Target = GameModeCtx.Teams[1 - t].Target});
				}

				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				// -------- -------- -------- -------- //
				// : Retrieve contexts
				// -------- -------- -------- -------- //
				GameModeCtx = Context.GetExternal<VersusHeadOnContext>();
				WorldCtx    = Context.GetExternal<WorldContext>();
			}
		}
	}
}