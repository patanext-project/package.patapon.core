using System;
using System.Collections.Generic;
using P4TLB.MasterServer.GamePlay;
using Patapon4TLB.Default;
using Patapon4TLB.Default.Attack;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Core
{
	public static class MasterServerAbilities
	{
		private const string InternalFormat = "p4:{0}";

		public static void Convert(ComponentSystemBase system, Entity entity, List<Ability> abilities)
		{
			Entity FindCommand(Type type)
			{
				using (var query = system.EntityManager.CreateEntityQuery(type))
				{
					if (query.CalculateEntityCount() == 0)
					{
						Debug.Log("nay " + type);
						return Entity.Null;
					}

					Debug.Log("ye " + type);
					using (var entities = query.ToEntityArray(Allocator.TempJob))
					{
						return entities[0];
					}
				}
			}

			var collection = new GhostSerializerCollection();
			collection.BeginSerialize(system);

			void CreateAbility<TProvider, TActionCreate>(TActionCreate create)
				where TProvider : BaseProviderBatch<TActionCreate>
				where TActionCreate : struct
			{
				using (var entities = new NativeList<Entity>(1, Allocator.TempJob))
				{
					var provider = system.World.GetOrCreateSystem<TProvider>();
					provider.SpawnLocalEntityWithArguments(create, entities);

					// Temporary. For now, we check if the entity can be serialized or not.
					// TODO: In future, all abilities should be able to be serialized.
					var success = false;
					try
					{
						collection.FindSerializer(system.EntityManager.GetChunk(entities[0]).Archetype);
						success = true;
					}
					catch
					{
						success = false;
					}

					if (success)
					{
						system.EntityManager.AddComponent(entities[0], typeof(GhostComponent));
					}
				}
			}

			foreach (var ab in abilities)
			{
				switch (ab.Type)
				{
					case string _ when string.IsNullOrEmpty(ab.Type):
						throw new InvalidOperationException();
					case string _ when ab.Type == GetInternal("tate/basic_march"):
					case string _ when ab.Type == GetInternal("basic_march"):
						CreateAbility<MarchAbilityProvider, MarchAbilityProvider.Create>(new MarchAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(MarchCommand))
						});
						break;
					case string _ when ab.Type == GetInternal("basic_backward"):
						CreateAbility<BackwardAbilityProvider, BackwardAbilityProvider.Create>(new BackwardAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(BackwardCommand))
						});
						break;
					case string _ when ab.Type == GetInternal("basic_jump"):
						CreateAbility<JumpAbilityProvider, JumpAbilityProvider.Create>(new JumpAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(JumpCommand))
						});
						break;
					case string _ when ab.Type == GetInternal("basic_retreat"):
						CreateAbility<RetreatAbilityProvider, RetreatAbilityProvider.Create>(new RetreatAbilityProvider.Create
						{
							Owner              = entity,
							AccelerationFactor = 1,
							Command            = FindCommand(typeof(RetreatCommand))
						});
						break;
					case string _ when ab.Type == GetInternal("tate/basic_attack"):
						CreateAbility<BasicTaterazayAttackAbility.Provider, BasicTaterazayAttackAbility.Create>(new BasicTaterazayAttackAbility.Create
						{
							Owner   = entity,
							Command = FindCommand(typeof(AttackCommand))
						});
						break;
					default:
						Debug.LogError("No ability found with type: " + ab.Type);
						break;
				}
			}
		}

		public static string GetInternal(string ability)
		{
			return string.Format(InternalFormat, ability);
		}
	}
}