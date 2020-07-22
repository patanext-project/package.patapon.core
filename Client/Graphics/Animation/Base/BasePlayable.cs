using System;
using PataNext.Client.Graphics.Animation.Units.Base;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Base
{
	public abstract class BasePlayable<TInit> : PlayableBehaviour
	{
		public PlayableGraph          Graph  { get; private set; }
		public Playable               Self   { get; private set; }
		public AnimationMixerPlayable Mixer  { get; private set; }
		public AnimationMixerPlayable Root   { get; private set; }

		public Type SystemType { get; private set; }

		public void Initialize(BaseAnimationSystem system, PlayableGraph graph, Playable self, int index, AnimationMixerPlayable rootMixer, TInit init)
		{
			SystemType = system.GetType();

			Self  = self;
			Root  = rootMixer;
			Graph = graph;

			Mixer = AnimationMixerPlayable.Create(graph, 4, true);
			Mixer.SetPropagateSetTime(true);
			OnInitialize(init);
			rootMixer.AddInput(self, 0);
			self.AddInput(Mixer, 0, 1);
		}

		protected abstract void OnInitialize(TInit init);
	}

	public abstract class BaseAbilityPlayable<TInit> : BasePlayable<TInit>
	{
		public UnitVisualPlayableBehaviourData Visual { get; set; }
	}
}