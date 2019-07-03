using package.patapon.core;
using package.StormiumTeam.GameBase;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
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
			
			Debug.Log("------------------- Create UnitSystem");

			m_UnitArchetype = EntityManager.CreateArchetype
			(
				typeof(GhostComponent),
				
				typeof(EntityAuthority),
				typeof(UnitDescription),

				typeof(UnitBaseSettings),
				typeof(UnitControllerState),
				typeof(UnitDirection),
				typeof(UnitTargetPosition),
				typeof(UnitRhythmState),

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

		protected override void OnUpdate()
		{
			var gameMode = World.GetExistingSystem<BasicGameModeSystem>();
			for (var pl = 0; pl != gameMode.NewPlayers.Length; pl++)
			{
				var playerEntity = gameMode.NewPlayers[pl];
				var playerData   = EntityManager.GetComponentData<BasicGameModePlayer>(playerEntity);

				// Create a sample unit...
				var unit     = EntityManager.CreateEntity(m_UnitArchetype);
				var collider = CapsuleCollider.Create(new float3(0), new float3(0, 2, 0), 0.5f);

				EntityManager.SetComponentData(unit, new UnitBaseSettings
				{
					MovementAttackSpeed = 2.25f,
					BaseWalkSpeed       = 2f,
					FeverWalkSpeed      = 2.2f,
					Weight              = 6
				});
				EntityManager.SetComponentData(unit, UnitDirection.Right);
				EntityManager.SetComponentData(unit, new UnitRhythmState());
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
				EntityManager.SetComponentData(unit, new DestroyChainReaction(playerEntity));

				// We should instead search for entities with 'MarchCommand' component tag...
				var marchCommand = World.GetExistingSystem<RhythmCommandBuilder>().GetOrCreate(new NativeArray<RhythmCommandSequence>(4, Allocator.TempJob)
				{
					[0] = new RhythmCommandSequence(0, RhythmKeys.Left),
					[1] = new RhythmCommandSequence(1, RhythmKeys.Left),
					[2] = new RhythmCommandSequence(2, RhythmKeys.Left),
					[3] = new RhythmCommandSequence(3, RhythmKeys.Right),
				});
				var retreatCommand = World.GetExistingSystem<RhythmCommandBuilder>().GetOrCreate(new NativeArray<RhythmCommandSequence>(4, Allocator.TempJob)
				{
					[0] = new RhythmCommandSequence(0, RhythmKeys.Right),
					[1] = new RhythmCommandSequence(1, RhythmKeys.Left),
					[2] = new RhythmCommandSequence(2, RhythmKeys.Right),
					[3] = new RhythmCommandSequence(3, RhythmKeys.Left),
				});
				var backwardCommand = World.GetExistingSystem<RhythmCommandBuilder>().GetOrCreate(new NativeArray<RhythmCommandSequence>(4, Allocator.TempJob)
				{
					[0] = new RhythmCommandSequence(0, RhythmKeys.Up),
					[1] = new RhythmCommandSequence(1, RhythmKeys.Left),
					[2] = new RhythmCommandSequence(2, RhythmKeys.Up),
					[3] = new RhythmCommandSequence(3, RhythmKeys.Left),
				});
				var jumpCommand = World.GetExistingSystem<RhythmCommandBuilder>().GetOrCreate(new NativeArray<RhythmCommandSequence>(4, Allocator.TempJob)
				{
					[0] = new RhythmCommandSequence(0, RhythmKeys.Down),
					[1] = new RhythmCommandSequence(1, RhythmKeys.Down),
					[2] = new RhythmCommandSequence(2, RhythmKeys.Up),
					[3] = new RhythmCommandSequence(3, RhythmKeys.Up),
				});

				using (var createList = new NativeList<Entity>(1, Allocator.TempJob))
				{
					World.GetOrCreateSystem<MarchAbilityProvider>().SpawnLocalEntityWithArguments(new MarchAbilityProvider.Create
					{
						Command            = marchCommand,
						AccelerationFactor = 1,
						Owner              = unit
					}, createList);
				}

				using (var createList = new NativeList<Entity>(1, Allocator.TempJob))
				{
					World.GetOrCreateSystem<MarchWithTargetAbilityProvider>().SpawnLocalEntityWithArguments(new MarchWithTargetAbilityProvider.Create
					{
						Command            = marchCommand,
						AccelerationFactor = 1,
						Owner              = unit
					}, createList);
				}

				using (var createList = new NativeList<Entity>(1, Allocator.TempJob))
				{
					World.GetOrCreateSystem<RetreatAbilityProvider>().SpawnLocalEntityWithArguments(new RetreatAbilityProvider.Create
					{
						Command            = retreatCommand,
						AccelerationFactor = 1,
						Owner              = unit
					}, createList);
				}

				using (var createList = new NativeList<Entity>(1, Allocator.TempJob))
				{
					World.GetOrCreateSystem<BackwardAbilityProvider>().SpawnLocalEntityWithArguments(new BackwardAbilityProvider.Create
					{
						Command            = backwardCommand,
						AccelerationFactor = 1,
						Owner              = unit
					}, createList);
				}

				using (var createList = new NativeList<Entity>(1, Allocator.TempJob))
				{
					World.GetOrCreateSystem<BackwardWithTargetAbilityProvider>().SpawnLocalEntityWithArguments(new BackwardWithTargetAbilityProvider.Create
					{
						Command            = backwardCommand,
						AccelerationFactor = 1,
						Owner              = unit
					}, createList);
				}
				
				using (var createList = new NativeList<Entity>(1, Allocator.TempJob))
				{
					World.GetOrCreateSystem<JumpAbilityProvider>().SpawnLocalEntityWithArguments(new JumpAbilityProvider.Create
					{
						Command            = jumpCommand,
						AccelerationFactor = 1,
						Owner              = unit
					}, createList);
				}

				playerData.Unit = unit;
				EntityManager.SetComponentData(playerEntity, playerData);

				var cameraState = EntityManager.GetComponentData<ServerCameraState>(playerEntity);
				cameraState.Data.Mode = CameraMode.Forced;
				cameraState.Data.Target = unit;
				
				EntityManager.SetComponentData(playerEntity, cameraState);

				Debug.Log($"------------------ Create entity with {unit} {playerData.RhythmEngine}");
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
		}
	}
}