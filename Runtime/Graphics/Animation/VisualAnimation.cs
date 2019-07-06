using System;
using System.Collections.Generic;
using Patapon4TLB.Core;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace package.patapon.core.Animation
{
	public class VisualAnimationPlayable : PlayableBehaviour
	{
		private PlayableGraph graph => Playable.GetGraph();

		public Playable               Playable;
		public AnimationMixerPlayable RootMixer;

		public override void OnPlayableCreate(Playable playable)
		{
			Playable = playable;
			Playable.SetInputCount(1);

			RootMixer = AnimationMixerPlayable.Create(graph);
			RootMixer.SetTraversalMode(PlayableTraversalMode.Mix);
			graph.Connect(RootMixer, 0, Playable, 0);

			Playable.SetInputWeight(0, 1);
		}
	}

	public class VisualAnimation : MonoBehaviour
	{
		protected abstract class SystemDataBase
		{
			public Type Type;
			public int  Index;
		}

		protected class SystemData<T> : SystemDataBase
			where T : struct
		{
			public T               Data;
			public RemoveSystem<T> RemoveDelegate;
		}

		public struct ManageData
		{
			public VisualAnimation         Handle;
			public PlayableGraph           Graph;
			public VisualAnimationPlayable Behavior;
			public int                     Index;
		}

		public delegate void AddSystem<T>(ref ManageData data, ref T systemData) where T : struct;

		public delegate void RemoveSystem<in T>(ManageData data, T systemData) where T : struct;

		protected Dictionary<Type, SystemDataBase> m_SystemData = new Dictionary<Type, SystemDataBase>();
		protected VisualAnimationPlayable          m_Playable;
		protected AnimationMixerPlayable rootMixer => m_Playable.RootMixer;
		protected PlayableGraph                    m_PlayableGraph;


		public void DestroyPlayableGraph()
		{
			if (m_PlayableGraph.IsValid())
				m_PlayableGraph.Destroy();
		}

		private void OnDestroy()
		{
			DestroyPlayableGraph();
		}

		public void CreatePlayableGraph(string name)
		{
			m_PlayableGraph = PlayableGraph.Create($"{GetType()}.{name}");
		}

		public void CreatePlayable()
		{
			m_PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			m_PlayableGraph.Play();
			m_Playable = ScriptPlayable<VisualAnimationPlayable>.Create(m_PlayableGraph).GetBehaviour();
		}

		public void SetAnimatorOutput(string outputName, Animator animator)
		{
			var output = AnimationPlayableOutput.Create(m_PlayableGraph, "Standard Output", animator);
			output.SetSourcePlayable(m_Playable.Playable);
		}

		public bool ContainsSystem(Type type)
		{
			return m_SystemData.ContainsKey(type);
		}

		public void InsertSystem<T>(Type type, AddSystem<T> addDelegate, RemoveSystem<T> removeDelegate)
			where T : struct
		{
			var data = new ManageData
			{
				Handle   = this,
				Behavior = m_Playable,
				Index    = m_SystemData.Count,
				Graph    = m_PlayableGraph
			};
			var systemData = new T();

			addDelegate(ref data, ref systemData);

			m_SystemData[type] = new SystemData<T>
			{
				Data           = systemData,
				Index          = m_SystemData.Count,
				Type           = type,
				RemoveDelegate = removeDelegate
			};
		}

		public unsafe ref T GetSystemData<T>(Type type)
			where T : struct
		{
			return ref ((SystemData<T>) m_SystemData[type]).Data;
		}

		public static int GetIndexFrom(Playable parent, Playable child)
		{
			var rootInputCount = parent.GetInputCount();
			for (var i = 0; i != rootInputCount; i++)
			{
				if (parent.GetInput(i).Equals(child))
					return i;
			}

			return -1;
		}

		public static float GetWeightFixed(double time, double start, double end)
		{
			if (start < 0 || end < 0)
				return 0;
			if (time > end)
				return 0;
			if (time < start)
				return 1;
			return (float) (1 - math.unlerp(start, end, time));
		}
	}
}