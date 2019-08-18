using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Karambolo.Common;
using StormiumTeam.GameBase;
using UnityEngine;

namespace DefaultNamespace
{
	public class AddRelativeRpcToCollection
	{
		public static void GetRpc<TDescription>(List<Type> types) where TDescription : struct, IEntityDescription
		{
			types.Add(typeof(SynchronizeRelativeSystem<TDescription>.SendAllRpc));
			types.Add(typeof(SynchronizeRelativeSystem<TDescription>.SendUpdateRpc));
			types.Add(typeof(SynchronizeRelativeSystem<TDescription>.SendDeltaRpc));
		}
		
		[GenerateAdditionalRpc]
		public static void Add(List<Type> types)
		{
			var getRpcMethod = typeof(AddRelativeRpcToCollection).GetMethod("GetRpc", BindingFlags.Public | BindingFlags.Static);
			if (getRpcMethod == null)
				throw new Exception("getRpcMethod is null");

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var descriptions = assembly.GetTypes().Where(t => t.HasInterface(typeof(IEntityDescription)));
				foreach (var desc in descriptions)
				{
					getRpcMethod.MakeGenericMethod(desc).Invoke(null, new object[] {types});
				}
			}
		}
	}
}