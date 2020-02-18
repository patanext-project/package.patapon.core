using System;
using package.stormiumteam.shared.ecs;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public class HeadOnCannonAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public float                    healthModifier;
		public HeadOnStructureAuthoring structureParent;

		public float shootPerSecond = 5;
		public Vector2 shootOffset = new Vector2(0.5f, 0.5f);
		public float gravity = -10;
		public Vector2[] projectileVelocities = {new Vector2(6, 3), new Vector2(10, 5)};

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			// make sure that we have no parent...
			if (dstManager.HasComponent<Parent>(entity))
				dstManager.RemoveComponent<Parent>(entity);

			transform.parent = GetComponentInParent<ConvertToEntity>().transform;

			dstManager.SetOrAddComponentData(entity, new Translation {Value = transform.position});
			dstManager.AddComponentData(entity, new Relative<TeamDescription>(Entity.Null));

			var towerEntity = conversionSystem.TryGetPrimaryEntity(structureParent);
			dstManager.AddComponentData(entity, new Owner {Target = towerEntity});
			dstManager.AddComponentData(entity, new HeadOnCannon
			{
				HealthModifier = healthModifier,
				ShootPerSecond = shootPerSecond,

				Gravity     = new float2(0, gravity),
				ShootOffset = shootOffset
			});

			var launchBuffer = dstManager.AddBuffer<HeadOnCannon.Launch>(entity);
			foreach (var proj in projectileVelocities)
			{
				launchBuffer.Add(new HeadOnCannon.Launch
				{
					velocity = proj
				});
			}

			dstManager.AddComponentData(entity, new LivableHealth {IsDead = true});
			var healthProvider = dstManager.World.GetExistingSystem<DefaultHealthData.InstanceProvider>();
			var healthEntity = healthProvider.SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
			{
				max   = 0,
				value = 0,
				owner = entity
			});
			dstManager.AddComponent(healthEntity, typeof(GhostEntity));
			
			dstManager.AddComponent(entity, typeof(GhostEntity));
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;

			var dt = 0.15f;
			var maxIter = 12;
			foreach (var velocity in projectileVelocities)
			{
				var left = shootOffset;
				left.x *= -1;

				var right = shootOffset;
				
				// left
				var end = (Vector2) new float3(transform.position).xy + left;
				var vel = velocity;
				vel.x *= -1;
				
				for (var i = 0; i != maxIter; i++)
				{
					Gizmos.DrawLine(end, end + ((vel + new Vector2(0, gravity) * dt) * dt));
					vel += new Vector2(0, gravity) * dt;
					end += vel * dt;
				}

				// right
				end = (Vector2) new float3(transform.position).xy + right;
				vel = velocity;
				for (var i = 0; i != maxIter; i++)
				{
					Gizmos.DrawLine(end, end + ((vel + new Vector2(0, gravity) * dt) * dt));
					vel += new Vector2(0, gravity) * dt;
					end += vel * dt;
				}
			}
		}
	}

	public struct HeadOnCannon : IComponentData, IReadWriteComponentSnapshot<HeadOnCannon>
	{
		public bool Active;
		public float HealthModifier;
		
		public float2 Gravity;
		public float2 ShootOffset;
		public float  ShootPerSecond;

		public UTick NextShootTick;
		public int Cycle;

		public struct Launch : IBufferElementData
		{
			public float2 velocity;
		}
		
		public struct Exclude : IComponentData {}

		public class NetSynchronize : MixedComponentSnapshotSystem<HeadOnCannon, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public void WriteTo(DataStreamWriter writer, ref HeadOnCannon baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WriteBitBool(Active);
			writer.WritePackedUIntDelta(NextShootTick.AsUInt, baseline.NextShootTick.AsUInt, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref HeadOnCannon baseline, DeserializeClientData jobData)
		{
			Active = reader.ReadBitBool(ref ctx);
			NextShootTick.Value = reader.ReadPackedUIntDelta(ref ctx, baseline.NextShootTick.AsUInt, jobData.NetworkCompressionModel);
		}
	}
}