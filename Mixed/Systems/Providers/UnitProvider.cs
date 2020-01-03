using Patapon.Mixed.GamePlay.Team;
using Patapon4TLB.Core.Snapshots;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace Patapon.Mixed.Units
{
	public class UnitProvider : BaseProviderBatch<UnitProvider.Create>
	{
		public struct Create
		{
			public BlobAssetReference<Collider> MovableCollider;
			public UnitStatistics?              Settings;
			public PhysicsMass?                 Mass;
			public UnitDirection                Direction;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(EntityDescription),

				typeof(LivableDescription),
				typeof(MovableDescription),
				typeof(UnitDescription),

				typeof(UnitStatistics),
				typeof(UnitPlayState),
				typeof(UnitControllerState),
				typeof(UnitDirection),
				typeof(UnitTargetOffset),

				typeof(TeamAgainstMovable),

				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),

				typeof(PhysicsCollider),
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(GroundState),

				typeof(LivableHealth),
				typeof(ActionContainer),
				typeof(HealthContainer),
				typeof(HitShapeContainer),

				typeof(PlayEntityTag),

				typeof(TranslationSnapshot.Exclude),
				typeof(InterpolatedTranslationSnapshot.Use)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			Debug.Assert(data.MovableCollider != null, "data.MovableCollider != null");
			Debug.Assert(data.Settings != null, "data.Settings != null");

			EntityManager.SetComponentData(entity, EntityDescription.New<UnitDescription>());

			EntityManager.SetComponentData(entity, new PhysicsCollider {Value = data.MovableCollider});
			EntityManager.SetComponentData(entity, data.Mass ?? PhysicsMass.CreateKinematic(data.MovableCollider.Value.MassProperties));
			EntityManager.SetComponentData(entity, data.Settings.Value);
			EntityManager.SetComponentData(entity, data.Direction);
			EntityManager.SetComponentData(entity, new GroundState(true));
			EntityManager.SetComponentData(entity, new TeamAgainstMovable {Size = data.MovableCollider.Value.CalculateAabb().Extents.x});

			// Create a temporary hitshape
			var hitShape = EntityManager.CreateEntity(typeof(LocalToWorld), typeof(Translation), typeof(PhysicsCollider), typeof(HitShapeDescription), typeof(HitShapeFollowParentTag));
			EntityManager.SetComponentData(hitShape, new PhysicsCollider {Value = data.MovableCollider});
			EntityManager.AddComponentData(hitShape, new Owner {Target          = entity});
		}
	}
}