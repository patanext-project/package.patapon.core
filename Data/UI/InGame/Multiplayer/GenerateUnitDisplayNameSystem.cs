using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class GenerateUnitDisplayNameSystem : UIGameSystemBase
	{
		private AssetPool<GameObject>      m_BackendPool;
		private AsyncAssetPool<GameObject> m_PresentationPool;

		private EntityQuery m_PlayerQuery;
		
		private const string KeyBase = "int:UI/InGame/UnitDisplay/";

		protected override void OnCreate()
		{
			base.OnCreate();
			
			m_PresentationPool = new AsyncAssetPool<GameObject>(KeyBase + "/MpUnitDisplayName.prefab");
			m_BackendPool = new AssetPool<GameObject>((pool) =>
			{
				var gameObject = new GameObject("UnitDisplayName Backend", typeof(RectTransform), typeof(UnitDisplayNameBackend), typeof(GameObjectEntity));
				var backend    = gameObject.GetComponent<UIStatusBackend>();
				backend.SetRootPool(pool);

				return gameObject;
			});
		}

		protected override void OnUpdate()
		{
			
		}
	}
}