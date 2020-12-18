using System;
using PataNext.Client.DataScripts.Interface.Bubble;
using PataNext.Module.Simulation.Components.GameModes;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.GameModes.Training
{
	public class YaridaTrainingGameModeSystem : AbsGameBaseSystem
	{
		private Entity checkpointBubble;
		private Entity startBubble;

		protected override void OnStartRunning()
		{
			base.OnStartRunning();

			startBubble = EntityManager.CreateEntity();
			EntityManager.AddComponentData(startBubble, new SpeechBubble {IsEnabled = false});
			EntityManager.AddComponentData(startBubble, new SpeechBubbleText(@"Welcome to the: 
<size=16>64 Yarida Challenge!</size>

To start this challenge, walk up to the baobab;
and so the timer will start.

<align=""center"">RULES</align><size=9>
You'll need to reach the destination;
the last Yarida, as fast as possible.
Without <color=""red"">overtaking</color> any of them <size=8>(except the last one)</size>
</size>
Good luck!"));
			EntityManager.AddComponentData(startBubble, new Translation {Value = new float3(0, 2f, 0)});

			checkpointBubble = EntityManager.CreateEntity();
			EntityManager.AddComponentData(checkpointBubble, new SpeechBubble {IsEnabled = false});
			EntityManager.AddComponentData(checkpointBubble, new SpeechBubbleText(@"Checkpoint!
Score=75%
Time=00:32"));
			EntityManager.AddComponentData(checkpointBubble, new Translation {Value = new float3(5, 2f, 0)});
		}

		private YaridaTrainingGameModeData previousData;

		protected override void OnUpdate()
		{
			Entities.ForEach((YaridaTrainingGameModeData gameMode) =>
			{
				if (previousData.YaridaOvertakeCount != gameMode.YaridaOvertakeCount)
				{
					if (gameMode.YaridaOvertakeCount >= 0)
					{
						SetComponent(checkpointBubble, new SpeechBubble {IsEnabled = true});
						SetComponent(checkpointBubble, new Translation {Value      = {x = 20 + gameMode.YaridaOvertakeCount * 10, y = 2}});

						EntityManager.GetComponentData<SpeechBubbleText>(checkpointBubble).Value = $@"Checkpoint!
Score={(int) (gameMode.LastCheckpointScore * 100)}%
Time={gameMode.LastCheckpointTime:F1}s";
					}
					else
						SetComponent(checkpointBubble, new SpeechBubble {IsEnabled = false});
				}

				switch (gameMode.Phase)
				{
					case YaridaTrainingGameModeData.EPhase.Waiting:
						SetComponent(startBubble, new SpeechBubble {IsEnabled = true});
						SetComponent(checkpointBubble, new SpeechBubble {IsEnabled = false});
						break;
					case YaridaTrainingGameModeData.EPhase.March:
						SetComponent(startBubble, new SpeechBubble {IsEnabled      = false});
						break;
					case YaridaTrainingGameModeData.EPhase.Backward:
						SetComponent(startBubble, new SpeechBubble {IsEnabled      = false});
						SetComponent(checkpointBubble, new SpeechBubble {IsEnabled = true});
						
						SetComponent(checkpointBubble, new Translation {Value = {x = gameMode.CurrUberHeroPos, y = 2}});
						EntityManager.GetComponentData<SpeechBubbleText>(checkpointBubble).Value = $@"You did it!
Now as a bonus, go back to zero!";
						
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				
				SetComponent(startBubble, new SpeechBubble {IsEnabled = false});

				previousData = gameMode;
			}).WithoutBurst().Run();
		}
	}
}