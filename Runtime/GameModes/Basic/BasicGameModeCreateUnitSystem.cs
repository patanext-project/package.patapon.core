using System;
using System.Collections.Generic;
using P4TLB.MasterServer.GamePlay;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

namespace Patapon4TLB.GameModes.Basic
{
	[DisableAutoCreation]
	[AlwaysUpdateSystem]
	public class BasicGameModeCreateUnitSystem : GameBaseSystem
	{
		// static list...
		private List<Ability> GetAbilities(uint ct) // ct = class type (0: tate, 1: yari, 2: yumi)
		{
			var list = new List<Ability>
			{
				new Ability {Type = ct == 0 ? MasterServerAbilities.GetInternal("tate/basic_march") : MasterServerAbilities.GetInternal("basic_march")},
				new Ability {Type = MasterServerAbilities.GetInternal("basic_backward")},
				new Ability {Type = MasterServerAbilities.GetInternal("basic_jump")},
				new Ability {Type = MasterServerAbilities.GetInternal("basic_retreat")}
			};

			switch (ct)
			{
				case 0:
					list.Add(new Ability {Type = MasterServerAbilities.GetInternal("tate/basic_attack")});
					list.Add(new Ability {Type = MasterServerAbilities.GetInternal("tate/basic_defense")});
					break;
				case 1:
					list.Add(new Ability {Type = MasterServerAbilities.GetInternal("yari/basic_attack")});
					list.Add(new Ability {Type = MasterServerAbilities.GetInternal("yari/basic_attack")});
					break;
				case 2:
					list.Add(new Ability {Type = MasterServerAbilities.GetInternal("yumi/basic_attack")});
					list.Add(new Ability {Type = MasterServerAbilities.GetInternal("yumi/basic_attack")});
					break;
				default:
					throw new NotImplementedException();
			}

			return list;
		}

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			// Create an enemy bot
			var gameMode = GetSingleton<BasicGameModeData>();

			var unitProvider = World.GetOrCreateSystem<UnitProvider>();
			Entity unit;
			using (var entities = new NativeList<Entity>(Allocator.TempJob))
			{
				var capsuleColl = CapsuleCollider.Create(0, math.up() * 2, 0.5f);
				unitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
				{
					MovableCollider = capsuleColl,
					Direction       = UnitDirection.Left,
					Settings = new UnitBaseSettings
					{
						AttackSpeed = 1.75f,

						MovementAttackSpeed = 2.25f,
						BaseWalkSpeed       = 2f,
						FeverWalkSpeed      = 2.2f,
						Weight              = 6
					},
					Mass = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties)
				}, entities);
				unit = entities[0];
			}

			EntityManager.AddComponent(unit, typeof(GhostComponent));
			EntityManager.AddComponentData(unit, new Relative<TeamDescription> {Target = gameMode.EnemyTeam});
			EntityManager.SetComponentData(unit, new Translation {Value                = new float3(7.5f, 50, 0)});
		}

		protected override void OnUpdate()
		{
			var gameMode = World.GetExistingSystem<BasicGameModeSystem>();
			for (var pl = 0; pl != gameMode.NewPlayers.Length; pl++)
			{
				for (var x = 0; x != 1; x++)
				{
					var playerEntity = gameMode.NewPlayers[pl];
					var playerData   = EntityManager.GetComponentData<BasicGameModePlayer>(playerEntity);

					// Create a sample unit...
					var unitProvider = World.GetOrCreateSystem<UnitProvider>();
					var unit         = Entity.Null;
					using (var entities = new NativeList<Entity>(Allocator.TempJob))
					{
						var capsuleColl = CapsuleCollider.Create(0, math.up() * 2, 0.5f);
						unitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
						{
							MovableCollider = capsuleColl,
							Direction       = UnitDirection.Right,
							Settings = new UnitBaseSettings
							{
								AttackSpeed = 1.75f,

								MovementAttackSpeed = 2.25f,
								BaseWalkSpeed       = 2f,
								FeverWalkSpeed      = 2.2f,
								Weight              = 6
							},
							Mass = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties)
						}, entities);
						unit = entities[0];
					}

					EntityManager.AddComponent(unit, typeof(GhostComponent));
					EntityManager.AddComponentData(unit, new Relative<PlayerDescription> {Target       = playerEntity});
					EntityManager.AddComponentData(unit, new Relative<RhythmEngineDescription> {Target = playerData.RhythmEngine});
					EntityManager.AddComponentData(unit, new Relative<TeamDescription> {Target         = gameMode.GameModeData.PlayerTeam});
					EntityManager.AddComponentData(unit, new DestroyChainReaction(playerEntity));

					MasterServerAbilities.Convert(this, unit, GetAbilities(0));

					playerData.Unit = unit;
					EntityManager.SetComponentData(playerEntity, playerData);

					var cameraState = EntityManager.GetComponentData<ServerCameraState>(playerEntity);
					cameraState.Data.Mode   = CameraMode.Forced;
					cameraState.Data.Target = unit;

					EntityManager.SetComponentData(playerEntity, cameraState);
				}
			}

			Entities.ForEach((ref Translation translation, ref UnitTargetPosition target) =>
			{
				Debug.DrawRay(translation.Value, Vector3.up, Color.blue);
				Debug.DrawRay(target.Value, Vector3.up, Color.red);

				if (Input.GetKey(KeyCode.RightArrow))
					target.Value.x += 0.1f;
				else if (Input.GetKey(KeyCode.LeftArrow))
					target.Value.x -= 0.1f;
			});

			Entities.ForEach((ref GameEvent gameEvent, ref TargetImpulseEvent impulse) =>
			{
				if (EntityManager.HasComponent<Velocity>(impulse.Destination))
				{
					var vel = EntityManager.GetComponentData<Velocity>(impulse.Destination);
					vel.Value *= impulse.Momentum;
					vel.Value += impulse.Force;

					EntityManager.SetComponentData(impulse.Destination, vel);
				}
			});
		}
	}
}