using System;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon4TLB.Core.Snapshots;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class EndMapBlock : BlockCollection
	{
		public EndMapBlock(string name) : base(name)
		{
		}

		protected override bool OnRun()
		{
			var queries   = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
			var gmContext = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
			var worldCtx  = Context.GetExternal<WorldContext>();

			queries.GetEntityQueryBuilder().WithAny<HealthDescription, UnitTargetDescription, RhythmEngineDescription>().ForEach(e => worldCtx.EntityMgr.DestroyEntity(e));
			foreach (ref var team in gmContext.Teams.AsSpan())
			{
				team.Flag         = default;
				team.AveragePower = 0;
				team.SpawnPoint   = default;
			}

			return true;
		}
	}
}