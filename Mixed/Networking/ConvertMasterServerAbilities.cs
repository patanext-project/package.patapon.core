using System;
using System.Collections.Generic;
using P4TLB.MasterServer.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core
{
	public static class MasterServerAbilities
	{
		private const string InternalFormat = "p4:{0}";

		private static void _c(ComponentSystemBase system, Entity entity, string typeId)
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

					using (var entities = query.ToEntityArray(Allocator.TempJob))
					{
						return entities[0];
					}
				}
			}

			void CreateAbility<TProvider, TActionCreate>(TActionCreate create)
				where TProvider : BaseProviderBatch<TActionCreate>
				where TActionCreate : struct
			{
				using (var entities = new NativeList<Entity>(1, Allocator.TempJob))
				{
					var provider = system.World.GetOrCreateSystem<TProvider>();
					provider.SpawnLocalEntityWithArguments(create, entities);

					system.EntityManager.AddComponent(entities[0], typeof(GhostEntity));
				}
			}

			switch (typeId)
			{
				case string _ when string.IsNullOrEmpty(typeId):
					throw new InvalidOperationException();
				case string _ when typeId == GetInternal("basic_party"):
					CreateAbility<DefaultPartyAbilityProvider, DefaultPartyAbilityProvider.Create>(new DefaultPartyAbilityProvider.Create
					{
						Owner   = entity,
						Command = FindCommand(typeof(PartyCommand)),
						Data = new DefaultPartyAbility()
						{
							TickPerSecond = 100,
							EnergyPerTick = 1,
							EnergyOnActivation = 30
						}
					});
					break;
				case string _ when typeId == GetInternal("tate/basic_march"):
				case string _ when typeId == GetInternal("basic_march"):
					CreateAbility<DefaultMarchAbilityProvider, DefaultMarchAbilityProvider.Create>(new DefaultMarchAbilityProvider.Create
					{
						Owner = entity,
						Command = FindCommand(typeof(MarchCommand)),
						Data = new DefaultMarchAbility
						{
							AccelerationFactor = 1.0f
						}
					});
					break;
				/*case string _ when typeId == GetInternal("basic_backward"):
					CreateAbility<BackwardAbilityProvider, BackwardAbilityProvider.Create>(new BackwardAbilityProvider.Create
					{
						Owner              = entity,
						AccelerationFactor = 1,
						Command            = FindCommand(typeof(BackwardCommand))
					});
					break;
				case string _ when typeId == GetInternal("basic_jump"):
					CreateAbility<JumpAbilityProvider, JumpAbilityProvider.Create>(new JumpAbilityProvider.Create
					{
						Owner              = entity,
						AccelerationFactor = 1,
						Command            = FindCommand(typeof(JumpCommand))
					});
					break;
				case string _ when typeId == GetInternal("basic_retreat"):
					CreateAbility<RetreatAbilityProvider, RetreatAbilityProvider.Create>(new RetreatAbilityProvider.Create
					{
						Owner              = entity,
						AccelerationFactor = 1,
						Command            = FindCommand(typeof(RetreatCommand))
					});
					break;
				case string _ when typeId == GetInternal("tate/basic_attack"):
					CreateAbility<BasicTaterazayAttackAbility.Provider, BasicTaterazayAttackAbility.Create>(new BasicTaterazayAttackAbility.Create
					{
						Owner   = entity,
						Command = FindCommand(typeof(AttackCommand))
					});
					break;
				default:
					Debug.LogError("No ability found with type: " + typeId);
					break;*/
			}
		}

		public static void Convert(ComponentSystemBase system, Entity entity, DynamicBuffer<UnitDefinedAbilities> abilities)
		{
			var array = abilities.ToNativeArray(Allocator.TempJob);
			foreach (var ab in array)
			{
				_c(system, entity, ab.Type.ToString());
			}

			array.Dispose();
		}

		public static void Convert(ComponentSystemBase system, Entity entity, List<Ability> abilities)
		{
			foreach (var ab in abilities)
			{
				_c(system, entity, ab.Type);
			}
		}

		public static string GetInternal(string ability)	
		{
			return string.Format(InternalFormat, ability);
		}
	}
}