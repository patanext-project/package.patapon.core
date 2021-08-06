using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameHost.Core;
using Newtonsoft.Json;
using PataNext.UnityCore.DOTS;
using PataNext.UnityCore.Rpc;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	public class DentBank : SystemBase
	{
		protected override void OnUpdate()
		{
		}
		
		private Dictionary<DentEntity, Task<Entity>> taskMap = new Dictionary<DentEntity, Task<Entity>>();
		private Dictionary<DentEntity, Entity>       map     = new Dictionary<DentEntity, Entity>();

		public Task<Entity> CallAndStoreLater(DentEntity dent, bool forceUpdate = false)
		{
			if (!forceUpdate && map.TryGetValue(dent, out var entity))
			{
				taskMap.Remove(dent);
				return Task.FromResult(entity);
			}

			if (taskMap.TryGetValue(dent, out var existing))
				return existing;

			var task = World.GetExistingSystem<GameHostConnector>()
			                .RpcClient.SendRequest<GetDentComponentsRpc, GetDentComponentsRpc.Response>(new GetDentComponentsRpc()
			                {
				                Dent = dent
			                });
			
			return taskMap[dent] = storeTask(task, dent);
		}

		private async Task<Entity> storeTask(Task<GetDentComponentsRpc.Response> parent, DentEntity dent)
		{
			await UniTask.SwitchToMainThread();
			
			var response = await parent;

			var output = EntityManager.CreateEntity();
			foreach (var kvp in response.ComponentTypeToJson)
			{
				var type = kvp.Key;
				var json = kvp.Value;
				Debug.LogError(type + ", " + json);
				switch (type)
				{
					case "PataNext.Core.ItemDetails":
						EntityManager.AddSharedComponentData(output, new ItemTargetAssetIdComponent { Value = JsonConvert.DeserializeObject<ItemDetails>(json).Asset });
						break;
					case "PataNext.Core.MissionDetails":
					{
						var missionDetails = JsonConvert.DeserializeObject<MissionDetails>(json);

						MissionDetailsComponent component;
						component.Path   = missionDetails.Path;
						component.Scenar = missionDetails.Scenar;
						component.Name   = missionDetails.Name;

						component.Path.Compute();
						component.Scenar.Compute();

						EntityManager.AddSharedComponentData(output, component);
						break;
					}
				}
			}

			StoreOutput(dent, output);
			return output;
		}

		public void StoreOutput(DentEntity itemEntity, Entity output)
		{
			map[itemEntity] = output;
		}

		public bool TryGetOutput(DentEntity itemEntity, out Entity output)
		{
			return map.TryGetValue(itemEntity, out output);
		}
	}
	
	[StructLayout(LayoutKind.Explicit)]
	public struct DentEntity : IEquatable<DentEntity>
	{
		[FieldOffset(0)]
		[JsonProperty]
		public short Version;

		[FieldOffset(2)]
		[JsonProperty]
		public short WorldId;

		[FieldOffset(4)]
		[JsonProperty]
		public int EntityId;

		public bool Equals(DentEntity other)
		{
			return Version == other.Version && WorldId == other.WorldId && EntityId == other.EntityId;
		}

		public override bool Equals(object obj)
		{
			return obj is DentEntity other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Version.GetHashCode();
				hashCode = (hashCode * 397) ^ WorldId.GetHashCode();
				hashCode = (hashCode * 397) ^ EntityId;
				return hashCode;
			}
		}

		public override string ToString()
		{
			return $"DentEntity({WorldId}:{EntityId}:{Version})";
		}
	}
}