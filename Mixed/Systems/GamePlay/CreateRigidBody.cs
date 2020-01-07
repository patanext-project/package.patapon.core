using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay
{
	public static class CreateRigidBody
	{
		public static unsafe void Execute(ref NativeList<RigidBody>            outputs,               in NativeArray<Entity>                   inputs,
		                                  ComponentDataFromEntity<Translation> translationFromEntity, ComponentDataFromEntity<PhysicsCollider> colliderFromEntity,
		                                  bool                                 sameLength)
		{
			outputs.Clear();
			for (var i = 0; i != inputs.Length; i++)
			{
				if (!colliderFromEntity.TryGet(inputs[i], out var collider)
				    || !translationFromEntity.TryGet(inputs[i], out var translation))
				{
					if (sameLength) outputs.Add(default);
					continue;
				}

				outputs.Add(new RigidBody
				{
					Entity        = inputs[i],
					Collider      = collider.ColliderPtr,
					WorldFromBody = new RigidTransform(quaternion.identity, translation.Value)
				});
			}
		}

		public static unsafe void Execute(ref NativeList<RigidBody>             outputs, in NativeArray<Entity> inputs,
		                                  ComponentDataFromEntity<LocalToWorld> ltwFromEntity,
		                                  ComponentDataFromEntity<Translation>  translationFromEntity, ComponentDataFromEntity<PhysicsCollider> colliderFromEntity,
		                                  bool                                  sameLength = false)
		{
			outputs.Clear();
			for (var i = 0; i != inputs.Length; i++)
			{
				if (!colliderFromEntity.TryGet(inputs[i], out var collider)
				    || !translationFromEntity.TryGet(inputs[i], out var translation))
				{
					if (sameLength) outputs.Add(default);
					continue;
				}

				if (!ltwFromEntity.TryGet(inputs[i], out var ltw))
				{
					ltw = new LocalToWorld {Value = new float4x4(quaternion.identity, translation.Value)};
				}
				else
				{
					// translation is always updated after ltw!
					ltw.Value = new float4x4(ltw.Rotation, translation.Value);
				}

				outputs.Add(new RigidBody
				{
					Entity        = inputs[i],
					Collider      = collider.ColliderPtr,
					WorldFromBody = new RigidTransform(ltw.Value)
				});
			}
		}

		public static unsafe void Execute(ref NativeList<RigidBody>             outputs, in NativeArray<HitShapeContainer> inputs,
		                                  Entity                                owner,
		                                  ComponentDataFromEntity<LocalToWorld> ltwFromEntity,
		                                  ComponentDataFromEntity<Translation>  translationFromEntity, ComponentDataFromEntity<PhysicsCollider> colliderFromEntity,
		                                  bool                                  sameLength = false)
		{
			outputs.Clear();
			for (var i = 0; i != inputs.Length; i++)
			{
				if (!colliderFromEntity.TryGet(inputs[i].Value, out var collider)
				    || !translationFromEntity.TryGet(inputs[i].Value, out var translation))
				{
					if (sameLength) outputs.Add(default);
					continue;
				}

				if (!ltwFromEntity.TryGet(inputs[i].Value, out var ltw))
				{
					ltw = new LocalToWorld {Value = new float4x4(quaternion.identity, translation.Value)};
				}
				else
				{
					// translation is always updated after ltw!
					ltw.Value = new float4x4(ltw.Rotation, translation.Value);
				}

				RigidBody rigidBody = default;
				rigidBody.Entity        = inputs[i].Value;
				rigidBody.Collider      = collider.ColliderPtr;
				rigidBody.WorldFromBody = new RigidTransform(ltw.Value);

				if (inputs[i].AttachedToParent)
				{
					var hasTranslation = translationFromEntity.TryGet(owner, out var ownerTranslation);
					if (!ltwFromEntity.TryGet(owner, out ltw))
					{
						ltw = new LocalToWorld {Value = new float4x4(quaternion.identity, ownerTranslation.Value)};
					}
					else if (hasTranslation)
					{
						ltw.Value = new float4x4(ltw.Rotation, ownerTranslation.Value);
					}

					rigidBody.WorldFromBody.pos += ltw.Position;
				}

				outputs.Add(rigidBody);
			}
		}
	}
}