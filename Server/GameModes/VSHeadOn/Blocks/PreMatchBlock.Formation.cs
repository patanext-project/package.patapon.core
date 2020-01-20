using System.Collections.Generic;
using GmMachine;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon.Mixed.Units.Statistics;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using Patapon4TLB.Core.MasterServer.Data;
using Patapon4TLB.Core.MasterServer.P4;
using Patapon4TLB.Core.MasterServer.P4.EntityDescription;
using Patapon4TLB.Default;
using Patapon4TLB.Default.Player;
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
	public partial class PreMatchBlock
	{
		private struct GmRequest : IComponentData
		{
			public Entity Player;
		}
		
		public Entity[]                   FormationEntity;
		public Dictionary<Entity, Entity> PlayerToFormationRequest;

		private void UpdateFormations()
		{
			if (FormationEntity == null)
			{
				FormationEntity = new Entity[2];
				const int formationCount = 2;
				for (var i = 0; i != formationCount; i++)
				{
					FormationEntity[i] = WorldCtx.EntityMgr.CreateEntity(typeof(GameFormationTag), typeof(FormationTeam), typeof(FormationRoot), typeof(GhostEntity));
					WorldCtx.EntityMgr.SetComponentData(FormationEntity[i], new FormationTeam {TeamIndex = i + 1});
				}
			}

			ManageCreationOfRequests();
			ProcessRequests();

			Queries.GetEntityQueryBuilder()
			       .ForEach((DynamicBuffer<UnitDefinedAbilities> abilities,
			                 ref ResponseGetUnitKit              responseUnitKit, ref UnitStatistics statistics, ref UnitDisplayedEquipment displayedEquipment, ref UnitCurrentKit currentKit) =>
			       {
				       abilities.Clear();

				       var kitTarget = UnitKnownTypes.FromEnum(responseUnitKit.KitId);
				       SetClass(kitTarget, ref statistics, abilities, ref displayedEquipment);
				       currentKit.Value = kitTarget;
			       });
		}

		private void ManageCreationOfRequests()
		{
			var removeSet = new NativeHashMap<Entity, byte>(PlayerToFormationRequest.Count, Allocator.Temp);
			foreach (var kvp in PlayerToFormationRequest)
			{
				if (WorldCtx.EntityMgr.Exists(kvp.Key))
					continue;
				removeSet.Add(kvp.Key, 0);
			}

			var keys = removeSet.GetKeyArray(Allocator.Temp);
			foreach (var key in keys)
			{
				PlayerToFormationRequest.Remove(key);
			}

			removeSet.Dispose();

			using (var players = Queries.Player.ToEntityArray(Allocator.TempJob))
			{
				foreach (var playerEntity in players)
				{
					if (PlayerToFormationRequest.ContainsKey(playerEntity))
						continue;

					var request = WorldCtx.EntityMgr.CreateEntity(typeof(RequestGetUserFormationData), typeof(GmRequest));
					WorldCtx.EntityMgr.SetComponentData(request, new GmRequest {Player = playerEntity});
					WorldCtx.EntityMgr.SetComponentData(request, new RequestGetUserFormationData
					{
						UserId    = WorldCtx.EntityMgr.GetComponentData<GamePlayer>(playerEntity).MasterServerId,
						UserLogin = default
					});
					PlayerToFormationRequest[playerEntity] = request;
				}
			}
		}

		private void ProcessRequests()
		{
			Queries.GetEntityQueryBuilder().ForEach((Entity entity, ResultGetUserFormationData result, ref GmRequest data) =>
			{
				if (!WorldCtx.EntityMgr.TryGetComponentData(data.Player, out Relative<TeamDescription> relativeTeam))
					return;

				var teamIndex = -1;
				for (var i = 0; i != GameModeCtx.Teams.Length && teamIndex < 0; i++)
				{
					if (GameModeCtx.Teams[i].Target == relativeTeam.Target)
						teamIndex = i;
				}

				if (teamIndex < 0)
					return; // ???

				var formation = FormationEntity[teamIndex];
				foreach (var army in result.Armies)
				{
					var armyEntity = WorldCtx.EntityMgr.CreateEntity(typeof(ArmyFormation), typeof(FormationParent), typeof(FormationChild), typeof(GhostEntity));
					WorldCtx.EntityMgr.SetComponentData(armyEntity, new FormationParent {Value = formation});
					WorldCtx.EntityMgr.ReplaceOwnerData(armyEntity, data.Player);

					foreach (var unit in army.Units)
					{
						var unitEntity = WorldCtx.EntityMgr.CreateEntity(typeof(UnitFormation), typeof(MasterServerP4UnitMasterServerEntity),
							                          typeof(UnitStatistics), typeof(UnitDefinedAbilities), typeof(UnitDisplayedEquipment), typeof(UnitCurrentKit),
							                          typeof(FormationParent), typeof(GhostEntity));
						WorldCtx.EntityMgr.SetComponentData(unitEntity, new FormationParent {Value                       = armyEntity});
						WorldCtx.EntityMgr.SetComponentData(unitEntity, new MasterServerP4UnitMasterServerEntity {UnitId = unit});
						WorldCtx.EntityMgr.AddComponentData(unitEntity, new RequestGetUnitKit.Automatic());
						WorldCtx.EntityMgr.AddComponentData(unitEntity, new MasterServerGlobalUnitPush());
						WorldCtx.EntityMgr.ReplaceOwnerData(unitEntity, data.Player);
					}
				}

				WorldCtx.EntityMgr.DestroyEntity(entity);
			});
		}

		private void SetClass(NativeString64 kit, ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities, ref UnitDisplayedEquipment display)
		{
			statistics.Health = 1000;
			statistics.Attack = 24;
			statistics.Defense = 7;
			statistics.BaseWalkSpeed = 2f;
			statistics.FeverWalkSpeed = 2.2f;
			statistics.AttackSpeed = 2.0f;
			statistics.MovementAttackSpeed = 3.1f;
			statistics.Weight = 8.5f;
			statistics.AttackSeekRange = 16f;
			
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_march"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_backward"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_retreat"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_jump"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_party"), 0));

			display = default;
			switch (kit)
			{
				case NativeString64 _ when kit.Equals(UnitKnownTypes.Taterazay):
					SetAsTaterazay(ref statistics, definedAbilities, ref display);
					break;
				case NativeString64 _ when kit.Equals(UnitKnownTypes.Yarida):
					SetAsYarida(ref statistics, definedAbilities, ref display);
					break;
			}
		}

		private void SetAsYarida(ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities, ref UnitDisplayedEquipment display)
		{
			statistics.Health           = 175;
			statistics.Attack           = 28;
			statistics.Defense          = 0;
			statistics.Weight           = 6;
			statistics.AttackMeleeRange = 2.6f;

			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("yari/basic_attack"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("yari/jump_attack"), 0, AbilitySelection.Top));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("yari/basic_defend"), 0));

			display.Mask           = new NativeString64("Masks/n_yarida");
			display.RightEquipment = new NativeString64("Spears/default_spear");
		}

		private void SetAsTaterazay(ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities, ref UnitDisplayedEquipment display)
		{
			statistics.Health           = 260;
			statistics.Attack           = 24;
			statistics.Defense          = 7;
			statistics.Weight           = 8.5f;
			statistics.AttackMeleeRange = 2.3f;

			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_attack"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_defend"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_defend_frontal"), 0, AbilitySelection.Bottom));

			display.Mask           = new NativeString64("Masks/n_taterazay");
			display.LeftEquipment  = new NativeString64("Shields/default_shield");
			display.RightEquipment = new NativeString64("Swords/default_sword");
		}
	}
}