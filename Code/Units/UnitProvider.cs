using package.StormiumTeam.GameBase;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace Patapon4TLB.Core
{
	public class UnitProvider : BaseProviderBatch<UnitProvider.Create>
	{
		public struct Create
		{
			public BlobAssetReference<Collider> MovableCollider;
			public UnitBaseSettings?            Settings;
			public PhysicsMass?                 Mass;
			public UnitDirection                Direction;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
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
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(GroundState),

				typeof(ActionContainer),
				
				typeof(PlayEntityTag),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			Debug.Assert(data.MovableCollider != null, "data.MovableCollider != null");
			Debug.Assert(data.Settings != null, "data.Settings != null");

			EntityManager.SetComponentData(entity, new PhysicsCollider {Value = data.MovableCollider});
			EntityManager.SetComponentData(entity, data.Mass ?? PhysicsMass.CreateKinematic(data.MovableCollider.Value.MassProperties));
			EntityManager.SetComponentData(entity, data.Settings.Value);
			EntityManager.SetComponentData(entity, data.Direction);
			EntityManager.SetComponentData(entity, new GroundState(true));
		}
	}
}