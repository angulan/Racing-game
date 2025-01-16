using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
	public static GameManager Instance;

	public Transform[] Checkpoints;
	public Transform Finish;
	PlayerMovement[] players = new PlayerMovement[0];
	public Button[] EmoteButtons;
	public GameObject EmoteWindow;

	private void Start()
	{
		if (Instance == null) Instance = this;
		else Destroy(this);

		NetworkManager.Singleton.OnClientConnectedCallback += UpdatePlayerCount;
	}

	private void Update()
	{
		CalculatePlayerPosition();
	}

	private void UpdatePlayerCount(ulong clientId)
	{
		players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
	}

	public void CalculatePlayerPosition()
	{
		if (players.Length == 0) return;
		System.Collections.Generic.List<PlayerMovement> playersToCheck = new System.Collections.Generic.List<PlayerMovement>();

		foreach (var plr in players)
		{
			playersToCheck.Add(plr);
		}

		int place = 1;

		while (playersToCheck.Count > 0)
		{
			PlayerMovement top = null;

			foreach (var plr in playersToCheck)
			{
				if (top == null)
				{
					top = plr;
				}
				else
				{
					if (plr.Laps.Value > top.Laps.Value)
					{
						top = plr;
					}
					else if (plr.Laps.Value == top.Laps.Value)
					{
						if (plr.CheckpointsSinceLapStart.Value > top.CheckpointsSinceLapStart.Value)
						{
							top = plr;
						}
						else if (plr.CheckpointsSinceLapStart.Value == top.CheckpointsSinceLapStart.Value)
						{
							int ch = plr.CheckpointsSinceLapStart.Value;

							if (ch == Checkpoints.Length)
							{
								if (Vector2.Distance(plr.transform.position, Finish.position) < Vector2.Distance(top.transform.position, Finish.position))
								{
									top = plr;
								}
							}
							else if(Vector2.Distance(plr.transform.position, Checkpoints[ch].position) < Vector2.Distance(top.transform.position, Checkpoints[ch].position))
							{
								top = plr;
							}


						}
					}
				}
			}

			top.SetPlace(place);
			place++;
			playersToCheck.Remove(top);

		}
	}
}
