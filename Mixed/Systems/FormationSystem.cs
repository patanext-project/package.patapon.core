using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	public class FormationSystemGroup : ComponentSystemGroup
	{
		private List<JobHandle> m_Dependencies;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Dependencies = new List<JobHandle>();
		}

		protected override void OnUpdate()
		{
			m_Dependencies.Clear();

			base.OnUpdate();

			foreach (var dep in m_Dependencies)
				dep.Complete();
		}

		public void AddDependency(JobHandle inputDeps)
		{
			m_Dependencies.Add(inputDeps);
		}
	}

	[UpdateInGroup(typeof(FormationSystemGroup))]
	public class FormationSystem : SystemBase
	{
		private EntityQuery m_ChildBufferQuery;
		private EntityQuery m_ChildWithParentQuery;

		private FormationSystemGroup m_FormationSystemGroup;

		private EntityQuery m_FormationWithoutBufferQuery;
		private EntityQuery m_FormationWithoutRootQuery;

		private EntityQuery m_RootWithChildQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_FormationSystemGroup = World.GetOrCreateSystem<FormationSystemGroup>();

			m_FormationWithoutBufferQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(FormationRoot)},
				None = new ComponentType[] {typeof(FormationChild)}
			});
			m_FormationWithoutRootQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(FormationParent)},
				None = new ComponentType[] {typeof(InFormation)}
			});

			m_ChildBufferQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(FormationChild)}
			});
			m_ChildWithParentQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(FormationParent)}
			});

			m_RootWithChildQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(FormationRoot), typeof(FormationChild)}
			});
		}

		protected override void OnUpdate()
		{
			if (m_FormationWithoutBufferQuery.CalculateEntityCount() > 0)
			{
				Dependency.Complete();

				using (var entities = m_FormationWithoutBufferQuery.ToEntityArray(Allocator.TempJob))
				{
					EntityManager.AddComponent(entities, typeof(FormationChild));
					for (var ent = 0; ent != entities.Length; ent++)
					{
						var buffer = EntityManager.GetBuffer<FormationChild>(entities[ent]);
						buffer.Reserve(buffer.Capacity + 1);
						buffer.Clear();
					}
				}
			}

			if (m_FormationWithoutRootQuery.CalculateEntityCount() > 0)
			{
				Dependency.Complete();

				EntityManager.AddComponent(m_FormationWithoutRootQuery, typeof(InFormation));
			}

			if (m_ChildBufferQuery.CalculateEntityCount() > 0)
				Entities.ForEach((ref DynamicBuffer<FormationChild> buffer) => buffer.Clear()).Schedule();

			if (m_ChildWithParentQuery.CalculateEntityCount() > 0)
			{
				var childFromEntity = GetBufferFromEntity<FormationChild>();
				Dependency = new AddToParent
				{
					ChildFromEntity = childFromEntity
				}.ScheduleSingle(m_ChildWithParentQuery, Dependency);
			}

			if (m_RootWithChildQuery.CalculateEntityCount() > 0)
			{
				var formationFromEntity = GetComponentDataFromEntity<InFormation>();
				var childFromEntity     = GetBufferFromEntity<FormationChild>(true);
				Entities.WithAll<FormationRoot, FormationChild>()
				        .ForEach((Entity root) => { Recursive(root, root, childFromEntity[root], formationFromEntity, childFromEntity); })
				        // It should be safe to access to this array since all childs got the same parent
				        .WithNativeDisableParallelForRestriction(formationFromEntity)
				        // Read only because it's read only :shrug:
				        .WithReadOnly(childFromEntity)
				        .ScheduleParallel();
			}

			m_FormationSystemGroup.AddDependency(Dependency);
		}

		private static void Recursive(Entity                               root,                Entity                           parent, DynamicBuffer<FormationChild> children,
		                              ComponentDataFromEntity<InFormation> formationFromEntity, BufferFromEntity<FormationChild> childFromEntity)
		{
			for (int ent = 0, length = children.Length; ent < length; ent++)
			{
				var child = children[ent].Value;
				formationFromEntity[child] = new InFormation
				{
					Root = root
				};

				if (childFromEntity.Exists(child))
					Recursive(root, child, childFromEntity[child], formationFromEntity, childFromEntity);
			}
		}

		// can't Entities-fy it because there is no ScheduleSingle for it... 
		[BurstCompile]
		private struct AddToParent : IJobForEachWithEntity<FormationParent>
		{
			public BufferFromEntity<FormationChild> ChildFromEntity;

			public void Execute(Entity entity, int index, ref FormationParent parent)
			{
				if (!ChildFromEntity.Exists(parent.Value))
				{
					parent.Value = default;
					return;
				}

				ChildFromEntity[parent.Value].Add(new FormationChild {Value = entity});
			}
		}
	}
}