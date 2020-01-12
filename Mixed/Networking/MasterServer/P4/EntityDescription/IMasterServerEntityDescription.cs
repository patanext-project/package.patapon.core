using System;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Core.MasterServer.P4.EntityDescription
{
	public interface IMasterServerEntityDescription<TDescription> : IComponentData, IEquatable<TDescription>
		where TDescription : IMasterServerEntityDescription<TDescription>
	{

	}

	public class MasterServerGetEntityWithDescriptionModule<TEntityDescription> : BaseSystemModule
		where TEntityDescription : struct, IMasterServerEntityDescription<TEntityDescription>
	{
		public EntityQuery                     DescriptionQuery;
		public NativeArray<Entity>             Entities;
		public NativeArray<TEntityDescription> Descriptions;

		private NativeList<Entity> m_MatchEntities;

		protected override void OnEnable()
		{
			DescriptionQuery = EntityManager.CreateEntityQuery(typeof(TEntityDescription));
			m_MatchEntities = new NativeList<Entity>(Allocator.Persistent);
		}

		protected override void OnUpdate(ref JobHandle jobHandle)
		{
			if (Entities.IsCreated)
			{
				Entities.Dispose();
				Descriptions.Dispose();
			}

			if (DescriptionQuery.IsEmptyIgnoreFilter)
				return;

			Entities     = DescriptionQuery.ToEntityArray(Allocator.TempJob);
			Descriptions = DescriptionQuery.ToComponentDataArray<TEntityDescription>(Allocator.TempJob);
		}

		protected override void OnDisable()
		{
			if (Entities.IsCreated)
			{
				Entities.Dispose();
				Descriptions.Dispose();
			}

			m_MatchEntities.Dispose();
		}

		public NativeList<Entity> GetMatch(NativeArray<TEntityDescription> descriptionArray)
		{
			m_MatchEntities.Clear();
			
			var entityLength = Entities.Length;
			for (var ent = 0; ent != entityLength; ent++)
			{
				for (var i = 0; i != descriptionArray.Length; i++)
				{
					if (descriptionArray[i].Equals(Descriptions[ent]))
					{
						m_MatchEntities.Add(Entities[ent]);
						break;
					}
				}		
			}

			return m_MatchEntities;
		}
	}
}