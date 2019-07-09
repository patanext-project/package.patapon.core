using package.patapon.core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI
{
	public class UIBeatFrame : MonoBehaviour, IConvertGameObjectToEntity
	{
		public enum Phase
		{
			NoCommand,
			Fever,
			Command,
		}

		private int[] m_LinesState;

		public GameObject[] Lines = new GameObject[3];
		public Material     Material;

		public Phase CurrPhase;
		public Color Color;

		private void OnEnable()
		{
			m_LinesState = new int[Lines.Length];
			for (var i = 0; i != m_LinesState.Length; i++)
			{
				m_LinesState[i] = -1;
			}
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentObject(entity, transform);
			dstManager.AddComponentObject(entity, this);
		}

		public void SetEnabled(int index, bool state)
		{
			ref var currState = ref m_LinesState[index];
			if (currState == -1)
			{
				currState = state ? 1 : 0;
				Lines[index].SetActive(state);
				return;
			}

			var stateAsInt = state ? 1 : 0;
			if (currState == stateAsInt)
				return;

			currState = stateAsInt;
			Lines[index].SetActive(state);
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class ClientSystem : GameBaseSystem
		{
			public int   ActivationBeat;
			public bool  IsCommandServer;
			public bool  IsCommandClient;
			public float LastBeatTime;

			public GameComboState ComboState;

			private EntityQueryBuilder.F_EDDDDDD<RhythmEngineProcess, GameCommandState, RhythmCurrentCommand, GamePredictedCommandState, GameComboState, GameComboPredictedClient> m_Delegate;

			private void ForEach(Entity entity, ref RhythmEngineProcess process, ref GameCommandState gameCommandState, ref RhythmCurrentCommand currentCommand, ref GamePredictedCommandState predictedCommand, ref GameComboState comboState, ref GameComboPredictedClient predictedCombo)
			{
				if (gameCommandState.StartTime >= currentCommand.ActiveAtTime)
				{
					ComboState = comboState;
				}
				else
				{
					ComboState = predictedCombo.State;
				}

				var activationBeat = process.GetActivationBeat(EntityManager.GetComponentData<RhythmEngineSettings>(entity).BeatInterval);
				if (activationBeat != ActivationBeat)
				{
					ActivationBeat = activationBeat;
					LastBeatTime   = Time.time;
				}

				IsCommandServer = gameCommandState.StartTime <= process.TimeTick && gameCommandState.EndTime > process.TimeTick;
				if (EntityManager.HasComponent<RhythmEngineSimulateTag>(entity))
				{
					IsCommandClient = currentCommand.ActiveAtTime <= process.TimeTick && predictedCommand.State.EndTime > process.TimeTick;
				}
			}

			protected override void OnCreate()
			{
				base.OnCreate();

				m_Delegate = ForEach;
			}

			protected override void OnUpdate()
			{
				// for now, we only do the simulated engine (in future, when we want spectator mode, we need to find a different approach)
				Entities.WithAll<RhythmEngineSimulateTag>().ForEach(m_Delegate);
			}
		}

		[UpdateInGroup(typeof(PresentationSystemGroup))]
		[UpdateAfter(typeof(TickClientPresentationSystem))]
		public class System : GameBaseSystem
		{
			private float m_BeatTime;
			private float m_BeatTimeExp;
			private int   m_PreviousBeat;
			private int   m_CurrentHue;

			private static void SetGrayScale(ref Color c, float v)
			{
				c.r = v;
				c.g = v;
				c.b = v;
			}

			private void UpdateAll(UIBeatFrame uiBeatFrame)
			{
				if (m_ClientSystem == null)
				{
					for (var i = 0; i < uiBeatFrame.Lines.Length; i++)
					{
						uiBeatFrame.SetEnabled(i, false);
					}

					return;
				}

				var newBeat = false;
				if (m_PreviousBeat != m_ClientSystem.ActivationBeat)
				{
					m_BeatTime     = 0;
					m_BeatTimeExp  = 0;
					m_PreviousBeat = m_ClientSystem.ActivationBeat;

					newBeat = true;
				}
				else
				{
					if (uiBeatFrame.CurrPhase != Phase.Fever)
					{
						m_BeatTime += Time.deltaTime * 2f + (m_BeatTimeExp * 0.5f);
					}
					else
					{
						m_BeatTime += Time.deltaTime * 2f;
					}

					m_BeatTimeExp += Time.deltaTime;
				}

				uiBeatFrame.Color.a = 1;
				if (!m_ClientSystem.ComboState.IsFever && newBeat)
				{
					uiBeatFrame.CurrPhase = Phase.NoCommand;
				}

				if (m_ClientSystem.ComboState.IsFever && newBeat)
				{
					uiBeatFrame.CurrPhase =  Phase.Fever;
					m_CurrentHue          += 1;
				}

				if ((m_ClientSystem.IsCommandServer || m_ClientSystem.IsCommandClient) && newBeat)
				{
					uiBeatFrame.CurrPhase = Phase.Command;
				}

				switch (uiBeatFrame.CurrPhase)
				{
					case Phase.NoCommand:
					{
						SetGrayScale(ref uiBeatFrame.Color, 0.75f);

						uiBeatFrame.SetEnabled(0, false);
						uiBeatFrame.SetEnabled(1, true);
						uiBeatFrame.SetEnabled(2, false);

						break;
					}

					case Phase.Command:
					{
						SetGrayScale(ref uiBeatFrame.Color, 0.5f);

						uiBeatFrame.SetEnabled(0, true);
						uiBeatFrame.SetEnabled(1, false);
						uiBeatFrame.SetEnabled(2, true);

						break;
					}

					case Phase.Fever:
					{
						// goooo crazy
						for (var i = 0; i != 3; i++)
						{
							uiBeatFrame.Color[i] = Mathf.Lerp(uiBeatFrame.Color[i], Random.Range(0f, 1f), Time.deltaTime * 25f);
						}

						uiBeatFrame.Color[m_CurrentHue % 3] = 1;

						uiBeatFrame.SetEnabled(0, true);
						uiBeatFrame.SetEnabled(1, true);
						uiBeatFrame.SetEnabled(2, true);

						break;
					}
				}

				var noAlphaColor = uiBeatFrame.Color;
				noAlphaColor.a = 0.0f;

				var lerpResult = Color.Lerp(uiBeatFrame.Color, noAlphaColor, m_BeatTime);

				uiBeatFrame.Material.SetColor(m_ColorId, lerpResult);
			}

			private EntityQueryBuilder.F_C<UIBeatFrame> m_QueryUpdateAll;
			private ClientSystem                        m_ClientSystem;

			private Material m_BeatMaterial;
			private int      m_ColorId;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_QueryUpdateAll = UpdateAll;
				m_ColorId        = Shader.PropertyToID("_Color");

				RequireForUpdate(GetEntityQuery(typeof(UIBeatFrame)));
			}

			protected override void OnUpdate()
			{
				var clientWorld = GetActiveClientWorld();
				m_ClientSystem = clientWorld?.GetOrCreateSystem<ClientSystem>();

				Entities.ForEach(m_QueryUpdateAll);
			}
		}
	}
}