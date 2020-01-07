using System;
using Scripts.Utilities;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

namespace Patapon.Mixed.GamePlay
{
	public interface IRayCastTarget<TCast> where TCast : struct, IRayCastTarget<TCast>
	{
		FunctionPointer<CastAction> Function { get; }

		void Initialize(ComponentSystemBase system);
	}

	public static unsafe class RayCastTarget
	{
		public static void Invoke<TCast>(this TCast cast, ref NativeList<Entity> hitEntities)
			where TCast : struct, IRayCastTarget<TCast>
		{
			cast.Function.Invoke(ref hitEntities, UnsafeUtility.AddressOf(ref cast), UnsafeUtility.SizeOf<TCast>());
		}
	}

	public unsafe delegate void CastAction(ref NativeList<Entity> hitEntities, void* data, int dataSize);

	[BurstCompile]
	public unsafe struct CastEnemies : IRayCastTarget<CastEnemies>
	{
		private static FunctionPointer<CastAction> s_Function;

		public FunctionPointer<CastAction> Function
		{
			get
			{
				var d = default(FunctionPointer<CastAction>);
				if (UnsafeUtilityOp.AreEquals(ref s_Function, ref d))
					s_Function = BurstCompiler.CompileFunctionPointer<CastAction>(InvokeCast);
				return s_Function;
			}
		}

		public Entity                       Target;
		public BlobAssetReference<Collider> BlobCollider;

		public void Initialize(ComponentSystemBase system)
		{
		}

		[BurstCompile]
		private static void InvokeCast(ref NativeList<Entity> hitEntities, void* dataPtr, int dataSize)
		{
			if (dataSize != UnsafeUtility.SizeOf<CastEnemies>())
				throw new Exception();

			/*var data = UnsafeUtilityEx.AsRef<CastEnemies>(dataPtr);
			var distanceInput = new ColliderDistanceInput
			{
				Collider    = (Collider*) ,
				MaxDistance = 0f,
				// remove z depth
				Transform = new RigidTransform(quaternion.identity, TranslationFromEntity[origin].Value * new float3(1, 1, 0))
			};

			var damage = 0;
			if (UnitSettingsFromEntity.Exists(origin))
			{
				var unitStatistics = UnitSettingsFromEntity[origin];
				damage = unitStatistics.Attack;

				float dmgF = damage;
				if (comboState.IsFever)
				{
					dmgF *= 1.2f;
					if (comboState.Score >= 50)
						dmgF *= 1.2f;

					damage += (int) dmgF - damage;
				}
			}

			for (var team = 0; team != teamEnemies.Length; team++)
			{
				var entities = SeekEnemies.EntitiesFromTeam[teamEnemies[team].Target];
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var entity = entities[ent].Value;
					if (LivableHealthFromEntity.Exists(entity) && LivableHealthFromEntity[entity].IsDead)
						continue;
					if (!SeekEnemies.HitShapeContainerFromEntity.Exists(entity))
						continue;

					var hitShapeBuffer = SeekEnemies.HitShapeContainerFromEntity[entity];
					for (int i = 0, length = hitShapeBuffer.Length; i != length; i++)
					{
						var hitShape  = hitShapeBuffer[i];
						var transform = SeekEnemies.LocalToWorldFromEntity[hitShape.Value];
						var collider  = PhysicsColliderFromEntity[hitShape.Value];

						var cc = new CustomCollide(collider, transform);
						if (hitShape.AttachedToParent)
							cc.WorldFromMotion.pos += SeekEnemies.LocalToWorldFromEntity[entity].Position;
						// remove z depth
						cc.WorldFromMotion.pos.z = 0;

						var collection = new CustomCollideCollection(cc);
						var collector  = new ClosestHitCollector<DistanceHit>(1.0f);

						if (!collection.CalculateDistance(distanceInput, ref collector))
							continue;

						DamageEventList.Add(new TargetDamageEvent
						{
							Position    = cc.WorldFromMotion.pos + collider.ColliderPtr->CalculateAabb().Center,
							Origin      = origin,
							Destination = entity,
							Damage      = -damage
						});
						break;
					}
				}
			}*/
		}
	}
}