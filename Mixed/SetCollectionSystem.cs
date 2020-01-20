using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.Common;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DefaultNamespace
{
	public class SetCollectionSystem : ComponentSystem
	{
		public enum SystemType
		{
			Dynamic,
			Description,
			Command
		}

		public static  Dictionary<SystemType, Type[]> DynamicTypes;
		private static Type[]                         s_CurrentTreatedArray;

		private static List<Type> m_AssembliesTypes;

		private static void SetSystems(SystemType type, Type interfaceType, Type subclass)
		{
			Type[] result;

			var directoryPath = $"{Application.streamingAssetsPath}/collections/";
			var filePath      = $"{directoryPath}systems_{type.ToString().ToLower()}.json";
			if (!File.Exists(filePath))
			{
				result = GetTypes(interfaceType, subclass)
					.ToArray();

				Directory.CreateDirectory(directoryPath);
				File.Create(filePath).Dispose();

				var strTypes = new string[result.Length];
				s_CurrentTreatedArray = result;
				Parallel.ForEach(strTypes, (source, state, i) => { strTypes[i] = s_CurrentTreatedArray[i].AssemblyQualifiedName; });

				File.WriteAllText(filePath, JsonUtility.ToJson(new FileData
				{
					systemTypes = strTypes
				}, true));
			}
			else
			{
				var strTypes = JsonUtility.FromJson<FileData>(File.ReadAllText(filePath)).systemTypes;
				result = new Type[strTypes.Length];

				s_CurrentTreatedArray = result;

				Parallel.ForEach(strTypes, (source, state, i) =>
				{
					s_CurrentTreatedArray[i] = Type.GetType(source);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (s_CurrentTreatedArray[i] == null)
						throw new NullReferenceException();
#endif
				});
			}

			DynamicTypes[type] = result;
		}

		protected override void OnCreate()
		{
			if (DynamicTypes == null)
			{
				var stopwatch = new Stopwatch();
				stopwatch.Start();

				DynamicTypes = new Dictionary<SystemType, Type[]>(4);
				SetSystems(SystemType.Dynamic, typeof(IDynamicSnapshotSystem), typeof(ComponentSystemBase));
				SetSystems(SystemType.Description, typeof(IEntityDescription), null);
				SetSystems(SystemType.Command, typeof(ICommandData), null);

				stopwatch.Stop();
				Debug.LogError("Time took for searching systems for builders = " + stopwatch.ElapsedMilliseconds);
			}

			World.GetOrCreateSystem<SnapshotManager>().SetFixedSystemsFromBuilder((world, builder) =>
			{
				var i = 1;
				foreach (var type in DynamicTypes[SystemType.Dynamic])
				{
					builder.Add(world.GetOrCreateSystem(type));
					i++;
				}

				foreach (var type in DynamicTypes[SystemType.Description])
				{
					builder.Add(world.GetOrCreateSystem(typeof(ComponentSnapshotSystemTag<>).MakeGenericType(type)));
					i++;
				}
			});
			World.GetOrCreateSystem<CommandCollectionSystem>().SetFixedCollection((world, builder) =>
			{
				foreach (var type in DynamicTypes[SystemType.Command]) builder.Add((CommandProcessSystemBase) world.GetOrCreateSystem(typeof(DefaultCommandProcessSystem<>).MakeGenericType(type)));
			});
		}

		protected override void OnUpdate()
		{
		}

		private static IEnumerable<Type> GetTypes(Type interfaceType, Type subclass)
		{
			if (m_AssembliesTypes == null)
			{
				m_AssembliesTypes = new List<Type>(1024);

				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var asm in assemblies)
					try
					{
						var types = asm.GetTypes();
						m_AssembliesTypes.AddRange(types);
					}
					catch (Exception e)
					{
						Debug.LogError($"Error from assembly: {asm.FullName}");
					}

				m_AssembliesTypes = m_AssembliesTypes.AsParallel()
				                                     .WithDegreeOfParallelism(4)
				                                     .OrderBy(t => t.FullName)
				                                     .ToList();
			}

			foreach (var type in m_AssembliesTypes)
				if (type.HasInterface(interfaceType) && (subclass == null || type.IsSubclassOf(subclass)) && !type.IsAbstract && !type.ContainsGenericParameters)
					yield return type;
		}

		[Serializable]
		private class FileData
		{
			public string[] systemTypes;
		}
	}

	public class SetVersionSystem : ComponentSystem
	{
		protected override void OnCreate()
		{
			// Year_Month_Day_SubVersion
			GameStatic.Version = 2020_01_20_01;
		}

		protected override void OnUpdate()
		{
		}
	}
}