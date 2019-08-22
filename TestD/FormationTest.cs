using NUnit.Framework;
using Patapon4TLB.Default;
using Unity.Entities;
using Unity.Entities.Tests;
using UnityEngine;

namespace Patapon4TLBCore.Tests
{
	[TestFixture]
	[Category("P4")]
	public class FormationTest : ECSTestsFixture
	{
		private void CreateSystems()
		{
			var group  = World.GetOrCreateSystem<FormationSystemGroup>();
			var system = World.GetOrCreateSystem<FormationSystem>();

			group.AddSystemToUpdateList(system);
		}

		[Test]
		public void Full_CreateFormationWithArmiesAndOneEntityInFirstArmy()
		{
			CreateSystems();

			var formationSystemGroup = World.GetExistingSystem<FormationSystemGroup>();

			var root = m_Manager.CreateEntity(typeof(FormationRoot));
			for (var a = 0; a != 4; a++)
			{
				var armyEntity = m_Manager.CreateEntity(typeof(ArmyFormation), typeof(FormationParent), typeof(FormationChild));
				m_Manager.SetComponentData(armyEntity, new FormationParent {Value = root});
			}

			formationSystemGroup.Update();

			var rootChildren = m_Manager.GetBuffer<FormationChild>(root);
			Assert.AreEqual(4, rootChildren.Length);
			for (var i = 0; i != rootChildren.Length; i++)
			{
				Assert.AreNotEqual(Entity.Null, rootChildren[i].Value);
			}

			var firstChild = rootChildren[0].Value;
			var unitEntity = m_Manager.CreateEntity(typeof(UnitFormation), typeof(FormationParent), typeof(InFormation));
			m_Manager.SetComponentData(unitEntity, new FormationParent {Value = firstChild});

			formationSystemGroup.Update();

			rootChildren = m_Manager.GetBuffer<FormationChild>(firstChild);
			Assert.AreEqual(rootChildren[0].Value, unitEntity);
			Assert.IsTrue(m_Manager.HasComponent<InFormation>(unitEntity));
			if (m_Manager.HasComponent<InFormation>(unitEntity))
			{
				Assert.AreEqual(root, m_Manager.GetComponentData<InFormation>(unitEntity).Root);
			}
		}
	}
}