using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestGetUserArmyData : IMasterServerRequest, IComponentData
	{
		public struct Indices : IBufferElementData
		{
			public int Value;
		}

		public bool error => ErrorCode != 0;

		public int ErrorCode;
	}

	public struct ResultGetUserArmyData : IComponentData
	{
		public struct DArmy
		{
			public int            Index;
			public BlobArray<int> UnitIds;
		}

		public struct DBlob
		{
			public BlobArray<DArmy> Armies;
		}

		public DBlob Result;

		public static BlobAssetReference<DBlob> ConstructBlob(Allocator allocator, int[][] unitIds)
		{
			using (var builder = new BlobBuilder(Allocator.TempJob))
			{
				ref var root          = ref builder.ConstructRoot<DBlob>();
				var     armiesBuilder = builder.Allocate(unitIds.Length, ref root.Armies);
				for (var i = 0; i != unitIds.Length; i++)
				{
					var     unitArray                                           = unitIds[i];
					ref var army                                                = ref armiesBuilder[i];
					var     unitsBuilder                                        = builder.Allocate(unitArray.Length, ref army.UnitIds);
					for (var u = 0; u != unitArray.Length; u++) unitsBuilder[u] = unitArray[u];
				}

				return builder.CreateBlobAssetReference<DBlob>(allocator);
			}
		}
	}
}