using System.Collections;
using System.Collections.Generic;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

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
		
		public GameObject[] Lines = new GameObject[3];
		public Material Material;

		public Phase CurrPhase;
		public Color Color;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentObject(entity, transform);
			dstManager.AddComponentObject(entity, this);
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class ClientSystem : GameBaseSystem
		{
			public int   Beat;
			public bool  IsCommandServer;
			public bool  IsCommandClient;
			public float LastBeatTime;

			public GameComboState ComboState;
			
			private EntityQueryBuilder.F_EDDDDDD<RhythmEngineProcess, GameCommandState, RhythmCurrentCommand, GamePredictedCommandState, GameComboState, GameComboPredictedClient> m_Delegate;

			private void ForEach(Entity entity, ref RhythmEngineProcess process, ref GameCommandState gameCommandState, ref RhythmCurrentCommand currentCommand, ref GamePredictedCommandState predictedCommand, ref GameComboState comboState, ref GameComboPredictedClient predictedCombo)
			{
				if (gameCommandState.StartBeat >= currentCommand.ActiveAtBeat)
				{
					ComboState = comboState;
				}
				else
				{
					ComboState         = predictedCombo.State;
				}
				
				if (process.Beat != Beat)
				{
					Beat         = process.Beat;
					LastBeatTime = Time.time;
				}

				IsCommandServer = gameCommandState.StartBeat <= Beat && gameCommandState.EndBeat > Beat;
				if (EntityManager.HasComponent<RhythmEngineSimulateTag>(entity))
				{
					IsCommandClient = currentCommand.ActiveAtBeat <= Beat && predictedCommand.EndBeat > Beat;
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
			private int m_PreviousBeat; 
			private int m_CurrentHue;
			
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
					foreach (var line in uiBeatFrame.Lines)
						line.SetActive(false);
					return;
				}

				var newBeat = false;
				if (m_PreviousBeat != m_ClientSystem.Beat)
				{
					m_BeatTime = 0;
					m_BeatTimeExp = 0;
					m_PreviousBeat = m_ClientSystem.Beat;

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
					uiBeatFrame.CurrPhase = Phase.Fever;
					m_CurrentHue += 1;
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

						uiBeatFrame.Lines[0].gameObject.SetActive(false);
						uiBeatFrame.Lines[1].gameObject.SetActive(true);
						uiBeatFrame.Lines[2].gameObject.SetActive(false);

						break;
					}
					case Phase.Command:
					{
						SetGrayScale(ref uiBeatFrame.Color, 0.5f);

						uiBeatFrame.Lines[0].gameObject.SetActive(true);
						uiBeatFrame.Lines[1].gameObject.SetActive(false);
						uiBeatFrame.Lines[2].gameObject.SetActive(true);

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

						uiBeatFrame.Lines[0].gameObject.SetActive(true);
						uiBeatFrame.Lines[1].gameObject.SetActive(true);
						uiBeatFrame.Lines[2].gameObject.SetActive(true);

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
			private int m_ColorId;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_QueryUpdateAll = UpdateAll;
				m_ColorId = Shader.PropertyToID("_Color");
				
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