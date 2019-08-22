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
	public class FormationSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct JobClear : IJobForEach_B<FormationChild>
		{
			public void Execute(DynamicBuffer<FormationChild> buffer) => buffer.Clear();
		}

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

		[RequireComponentTag(typeof(FormationRoot))]
		[BurstCompile]
		private struct RecursiveSetFormationRoot : IJobForEachWithEntity_EB<FormationChild>
		{
			[NativeDisableParallelForRestriction]
			// It should be safe to access to this array since all childs got the same parent
			public ComponentDataFromEntity<InFormation> FormationFromEntity;

			[ReadOnly]
			public BufferFromEntity<FormationChild> ChildFromEntiy;

			private void Recursive(Entity root, Entity parent, DynamicBuffer<FormationChild> children)
			{
				for (int ent = 0, length = children.Length; ent < length; ent++)
				{
					var child = children[ent].Value;
					FormationFromEntity[child] = new InFormation
					{
						Root = root
					};

					if (ChildFromEntiy.Exists(child))
						Recursive(root, child, ChildFromEntiy[child]);
				}
			}

			public void Execute(Entity root, int index, DynamicBuffer<FormationChild> children)
			{
				Recursive(root, root, children);
			}
		}

		private FormationSystemGroup m_FormationSystemGroup;

		private EntityQuery m_FormationWithoutBufferQuery;
		private EntityQuery m_FormationWithoutRootQuery;

		private EntityQuery m_ChildBufferQuery;
		private EntityQuery m_ChildWithParentQuery;

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

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (m_FormationWithoutBufferQuery.CalculateEntityCount() > 0)
			{
				inputDeps.Complete();

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
				inputDeps.Complete();

				EntityManager.AddComponent(m_FormationWithoutRootQuery, typeof(InFormation));
			}

			if (m_ChildBufferQuery.CalculateEntityCount() > 0)
			{
				inputDeps = new JobClear().Schedule(m_ChildBufferQuery, inputDeps);
			}

			if (m_ChildWithParentQuery.CalculateEntityCount() > 0)
			{
				inputDeps = new AddToParent
				{
					ChildFromEntity = GetBufferFromEntity<FormationChild>()
				}.ScheduleSingle(m_ChildWithParentQuery, inputDeps);
			}

			if (m_RootWithChildQuery.CalculateEntityCount() > 0)
			{
				inputDeps = new RecursiveSetFormationRoot
				{
					FormationFromEntity = GetComponentDataFromEntity<InFormation>(),
					ChildFromEntiy      = GetBufferFromEntity<FormationChild>(true)
				}.Schedule(m_RootWithChildQuery, inputDeps);
			}

			m_FormationSystemGroup.AddDependency(inputDeps);

			return inputDeps;
		}
	}
}