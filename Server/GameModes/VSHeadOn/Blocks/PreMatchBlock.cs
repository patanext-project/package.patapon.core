using System.Collections.Generic;
using GmMachine;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon4TLB.Default;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Entity = Unity.Entities.Entity;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public partial class PreMatchBlock : Block
	{
		public MpVersusHeadOnGameMode.ModeContext GameModeCtx;

		public WorldContext                          WorldCtx;
		public MpVersusHeadOnGameMode.QueriesContext Queries;

		public int[] TeamCount;
		public int[] TeamReady;

		public PreMatchBlock(string name) : base(name)
		{
			TeamCount = new int[2];
			TeamReady = new int[2];
			
			PlayerToFormationRequest = new Dictionary<Entity, Entity>();
		}

		protected override bool OnRun()
		{
			if (!GameModeCtx.RunPreMatch)
				return true;

			GameModeCtx.HudSettings.EnableGameModeInterface = false;
			GameModeCtx.HudSettings.EnablePreMatchInterface = true;

			for (var i = 0; i != 2; i++)
			{
				TeamCount[i] = 0;
				TeamReady[i] = 0;
			}

			using (var players = Queries.Player.ToEntityArray(Allocator.TempJob))
			{
				foreach (var entity in players)
				{
					if (!WorldCtx.EntityMgr.TryGetComponentData(entity, out Relative<TeamDescription> relativeTeam))
						continue;

					var index = relativeTeam.Target == GameModeCtx.Teams[0].Target ? 0 : 1;

					TeamCount[index]++;
					if (WorldCtx.EntityMgr.HasComponent(entity, typeof(PreMatchPlayerIsReady)))
						TeamReady[index]++;
				}
			}

			Queries.GetEntityQueryBuilder()
			       .WithNone<Relative<TeamDescription>>()
			       .WithAll<GamePlayer>().ForEach((Entity entity) =>
			       {
				       var selectedTeam = TeamCount[0] > TeamCount[1] ? 1 : 0;
				       WorldCtx.EntityMgr.AddComponentData(entity, new Relative<TeamDescription>(GameModeCtx.Teams[selectedTeam].Target));
			       });

			Queries.GetEntityQueryBuilder().ForEach((Entity entity, ref HeadOnChangeTeamRpc rpc, ref ReceiveRpcCommandRequestComponent receive) =>
			{
				WorldCtx.EntityMgr.DestroyEntity(entity);

				var commandTarget = WorldCtx.EntityMgr.GetComponentData<CommandTargetComponent>(receive.SourceConnection);
				if (commandTarget.targetEntity == default || WorldCtx.EntityMgr.HasComponent<PreMatchPlayerIsReady>(commandTarget.targetEntity)
				                                          || !WorldCtx.EntityMgr.HasComponent<OwnerChild>(commandTarget.targetEntity))
					return;

				if (rpc.Team >= 0 && rpc.Team <= 1)
				{
					WorldCtx.EntityMgr.SetOrAddComponentData(commandTarget.targetEntity, new Relative<TeamDescription>(GameModeCtx.Teams[rpc.Team].Target));
					
					var children = WorldCtx.EntityMgr.GetBuffer<OwnerChild>(commandTarget.targetEntity);
					for (var i = 0; i != children.Length; i++)
					{
						if (!WorldCtx.EntityMgr.HasComponent<ArmyFormation>(children[i].Child))
							continue;
						WorldCtx.EntityMgr.SetComponentData(children[i].Child, new FormationParent {Value = FormationEntity[rpc.Team]});
					}
				}
				else
				{
					
				}
			});

			UpdateFormations();

			/*for (var i = 0; i != 2; i++)
				if (TeamCount[i] == 1 && TeamReady[i] == 1 && TeamCount[1 - i] == 0)
					return true;*/
			if (Input.GetKeyDown(KeyCode.R))
				return true;
			
			return TeamCount[0] > 0 && TeamReady[0] == TeamCount[0] && TeamReady[1] == TeamCount[1];
		}

		protected override void OnReset()
		{
			base.OnReset();

			// -------- -------- -------- -------- //
			// : Retrieve contexts
			// -------- -------- -------- -------- //
			GameModeCtx = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
			WorldCtx    = Context.GetExternal<WorldContext>();
			Queries     = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
			
			Queries.GetEntityQueryBuilder().WithAll<PreMatchPlayerIsReady>().ForEach(e => WorldCtx.EntityMgr.RemoveComponent<PreMatchPlayerIsReady>(e));
		}
	}
}