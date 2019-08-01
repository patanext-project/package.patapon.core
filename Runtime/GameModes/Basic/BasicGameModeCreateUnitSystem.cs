using System;
using System.Collections.Generic;
using P4TLB.MasterServer.GamePlay;
using package.patapon.core;
using package.StormiumTeam.GameBase;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using Patapon4TLB.Default.Attack;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

namespace Patapon4TLB.GameModes.Basic
{
	[DisableAutoCreation]
	public class BasicGameModeCreateUnitSystem : GameBaseSystem
	{
		private EntityArchetype m_UnitArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_UnitArchetype = EntityManager.CreateArchetype
			(
				typeof(GhostComponent),

				typeof(EntityAuthority),
				typeof(LivableDescription),
				typeof(MovableDescription),
				typeof(UnitDescription),

				typeof(UnitBaseSettings),
				typeof(UnitPlayState),
				typeof(UnitControllerState),
				typeof(UnitDirection),
				typeof(UnitTargetPosition),

				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),

				typeof(PhysicsCollider),
				typeof(PhysicsDamping),
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(GroundState),

				typeof(Relative<PlayerDescription>),
				typeof(Relative<RhythmEngineDescription>),
				typeof(Relative<TeamDescription>),

				typeof(ActionContainer),

				typeof(DestroyChainReaction)
			);
		}

		// static list...
		private List<Ability> GetAbilities(uint ct) // ct = class type (0: tate, 1: yari, 2: yumi)
		{
			var list = new List<Ability>
			{
				new Ability {Type = ct == 0 ? AbilityType.TateBasicMarch : AbilityType.BasicMarch},
				new Ability {Type = AbilityType.BasicBackward},
				new Ability {Type = AbilityType.BasicJump},
				new Ability {Type = AbilityType.BasicRetreat}
			};

			switch (ct)
			{
				case 0:
					list.Add(new Ability {Type = AbilityType.TateBasicAttack});
					list.Add(new Ability {Type = AbilityType.TateBasicDefense});
					break;
				case 1:
					list.Add(new Ability {Type = AbilityType.YariBasicAttack});
					list.Add(new Ability {Type = AbilityType.YariBasicDefense});
					break;
				case 2:
					list.Add(new Ability {Type = AbilityType.YumiBasicAttack});
					list.Add(new Ability {Type = AbilityType.YumiBasicDefense});
					break;
				default:
					throw new NotImplementedException();
			}

			return list;
		}

		private void CreateAbilityForEntity(List<Ability> abilities, Entity entity)
		{
			Entity FindCommand(Type type)
			{
				var query = GetEntityQuery(type);
				if (query.CalculateEntityCount() == 0)
					return Entity.Null;
				using (var entities = query.ToEntityArray(Allocator.TempJob))
					return entities[0];
			}

			void CreateAbility<TProvider, TActionCreate>(TActionCreate create)
				where TProvider : BaseProviderBatch<TActionCreate>
				where TActionCreate : struct
			{
				using (var entities = new NativeList<Entity>(1, Allocator.TempJob))
				{
					var provider = World.GetOrCreateSystem<TProvider>();
					provider.SpawnLocalEntityWithArguments(create, entities);
				}
			}

			foreach (var ab in abilities)
			{
				switch (ab.Type)
				{
					case AbilityType.Unknown:
						throw new InvalidOperationException();
					case AbilityType.TateBasicMarch:
					case AbilityType.BasicMarch:
						CreateAbility<MarchAbilityProvider, MarchAbilityProvider.Create>(new MarchAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(MarchCommand))
						});
						break;
					case AbilityType.BasicBackward:
						CreateAbility<BackwardAbilityProvider, BackwardAbilityProvider.Create>(new BackwardAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(BackwardAbility))
						});
						break;
					case AbilityType.BasicJump:
						CreateAbility<JumpAbilityProvider, JumpAbilityProvider.Create>(new JumpAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(JumpAbility))
						});
						break;
					case AbilityType.BasicRetreat:
						CreateAbility<RetreatAbilityProvider, RetreatAbilityProvider.Create>(new RetreatAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(RetreatAbility))
						});
						break;
					case AbilityType.TateBasicAttack:
						CreateAbility<BasicTaterazayAttackAbility.Provider, BasicTaterazayAttackAbility.Create>(new BasicTaterazayAttackAbility.Create
						{
							Owner   = entity,
							Command = FindCommand(typeof(AttackCommand))
						});
						break;
					case AbilityType.TateBasicDefense:
						break;
					case AbilityType.YariBasicAttack:
						break;
					case AbilityType.YariBasicDefense:
						break;
					case AbilityType.YumiBasicAttack:
						break;
					case AbilityType.YumiBasicDefense:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
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
					var unit     = EntityManager.CreateEntity(m_UnitArchetype);
					var collider = CapsuleCollider.Create(new float3(0), new float3(0, 2, 0), 0.5f);

					EntityManager.SetComponentData(unit, new UnitBaseSettings
					{
						AttackSpeed = 1.75f,

						MovementAttackSpeed = 2.25f,
						BaseWalkSpeed       = 2f,
						FeverWalkSpeed      = 2.2f,
						Weight              = 6
					});
					EntityManager.SetComponentData(unit, UnitDirection.Left);
					EntityManager.SetComponentData(unit, PhysicsMass.CreateDynamic(collider.Value.MassProperties, 1));
					EntityManager.SetComponentData(unit, new PhysicsCollider
					{
						Value = collider
					});
					EntityManager.SetComponentData(unit, new PhysicsDamping
					{
						Linear = 0.1f
					});
					EntityManager.SetComponentData(unit, new GroundState(true));
					EntityManager.SetComponentData(unit, new Relative<PlayerDescription> {Target       = playerEntity});
					EntityManager.SetComponentData(unit, new Relative<RhythmEngineDescription> {Target = playerData.RhythmEngine});
					EntityManager.SetComponentData(unit, new Relative<TeamDescription> {Target         = gameMode.GameModeData.PlayerTeam});
					EntityManager.SetComponentData(unit, new DestroyChainReaction(playerEntity));

					var abilities = GetAbilities(0);
					CreateAbilityForEntity(abilities, unit);

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