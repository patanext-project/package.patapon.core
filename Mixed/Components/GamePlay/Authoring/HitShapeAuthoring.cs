using System.Reflection;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Mixed.GamePlay.Authoring
{
	[UpdateBefore(typeof(PhysicsShapeConversionSystem))]
	public class HitShapeConverter : GameObjectConversionSystem
	{
		internal static void AddOrSetComponent<T>(EntityManager manager, Entity entity, T value)
			where T : struct, IComponentData
		{
			if (!manager.HasComponent<T>(entity))
				manager.AddComponentData(entity, value);
			else if (!TypeManager.IsZeroSized(TypeManager.GetTypeIndex<T>()))
				manager.SetComponentData(entity, value);
		}

		internal static void RemoveParentAndSetWorldTranslationAndRotation(EntityManager manager, Entity entity, Transform worldTransform)
		{
			manager.RemoveComponent<Parent>(entity);
			manager.RemoveComponent<LocalToParent>(entity);
			AddOrSetComponent(manager, entity, new Translation {Value = worldTransform.position});
			AddOrSetComponent(manager, entity, new Rotation {Value    = worldTransform.rotation});
			if (math.lengthsq((float3) worldTransform.lossyScale - new float3(1f)) > 0f)
			{
				// bake in composite scale
				var compositeScale = math.mul(
					math.inverse(float4x4.TRS(worldTransform.position, worldTransform.rotation, 1f)),
					worldTransform.localToWorldMatrix
				);
				AddOrSetComponent(manager, entity, new CompositeScale {Value = compositeScale});
			}

			// TODO: revisit whether or not NonUniformScale/Scale should be preserved along with ParentScaleInverse instead
			manager.RemoveComponent<NonUniformScale>(entity);
			manager.RemoveComponent<Scale>(entity);
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, HitShapeAuthoring authoring) =>
			{
				if (authoring.keepParent)
					return;

				RemoveParentAndSetWorldTranslationAndRotation(DstEntityManager, GetPrimaryEntity(authoring), authoring.transform);

				var parentWithoutCollider = authoring.transform.parent;
				while (parentWithoutCollider != null)
				{
					if (parentWithoutCollider.GetComponent<PhysicsShapeAuthoring>() == null)
						break;
					parentWithoutCollider = parentWithoutCollider.parent;
				}

				authoring.transform.SetParent(parentWithoutCollider, false);
				authoring.transform.SetAsLastSibling();
			});
		}
	}

	public class HitShapeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public bool       followParent = true;
		public bool       keepParent;
		public GameObject owner;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			var ownerEntity = conversionSystem.TryGetPrimaryEntity(owner);
			if (ownerEntity == default)
			{
				Debug.LogError("The owner doesn't have an entity yet!");
				return;
			}

			if (!dstManager.HasComponent(ownerEntity, typeof(HitShapeContainer)))
				dstManager.AddComponent(ownerEntity, typeof(HitShapeContainer));

			dstManager.AddComponent(entity, typeof(HitShapeDescription));
			dstManager.AddComponent(entity, typeof(Owner));
			dstManager.SetComponentData(entity, new Owner {Target = ownerEntity});

			if (followParent)
			{
				// could we call it a hack? I doubt it work if the parent is scaled
				transform.position = transform.localPosition;
				dstManager.AddComponent(entity, typeof(HitShapeFollowParentTag));
			}
		}

		public static T GetCopyOf<T>(Component comp, T other) where T : Component
		{
			var type = comp.GetType();
			if (type != other.GetType()) return null; // type mis-match
			var flags  = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
			var pinfos = type.GetProperties(flags);
			foreach (var pinfo in pinfos)
				if (pinfo.CanWrite)
					try
					{
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					}
					catch
					{
					} // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.

			var finfos = type.GetFields(flags);
			foreach (var finfo in finfos) finfo.SetValue(comp, finfo.GetValue(other));
			return comp as T;
		}
	}
}