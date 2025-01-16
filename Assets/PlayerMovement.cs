using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] float acceleration = 5;
    [SerializeField] float speedLoss = 3;
    [SerializeField] float turnRotation = 5;
    float currentSpeed;
	bool onGrass = false;
	[SerializeField] Rigidbody2D rb;
	Transform cam;
	[SerializeField] SpriteRenderer sr;
	[SerializeField] SpriteRenderer emoteSR;
	[SerializeField] Sprite[] emotes;
	bool emoteDebounce = false;

	public PlayerMovement LocalPlayer;

	public NetworkVariable<int> Laps = new NetworkVariable<int>(0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner);
	public NetworkVariable<int> CheckpointsSinceLapStart = new NetworkVariable<int>(0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner);
	int place;

	GameManager gm;

	Text lapsText;
	Text placeText;
	GameObject emotesWindow;

	public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>();

	string[] positionNames = new string[] { "1st", "2nd", "3rd", "4th" };

	public override void OnNetworkSpawn()
	{
		cam = Camera.main.transform;
		gm = GameManager.Instance;

		if (IsServer)
		{
			PlayerColor.Value = Random.ColorHSV();
		}
		sr.color = new Color(PlayerColor.Value.r, PlayerColor.Value.g, PlayerColor.Value.b, 1);

		lapsText = GameObject.Find("Laps").GetComponent<Text>();
		placeText = GameObject.Find("Place").GetComponent<Text>();
		emotesWindow = gm.EmoteWindow;

		if (IsLocalPlayer)
		{
			LocalPlayer = this;

			for (int i = 0; i < gm.EmoteButtons.Length; i++)
			{
				int index = i;
				gm.EmoteButtons[i].onClick.AddListener(() => EmoteRpc(index));
			}
		}
	}
	[Rpc(SendTo.Everyone)]
	public void EmoteRpc(int emoteId)
	{
		if (!emoteDebounce)
		{
			print(emoteId);
			print("of " + emotes.Length);

			emoteSR.sprite = emotes[emoteId];

			StartCoroutine(AnimateEmote());
		}
	}

	IEnumerator AnimateEmote()
	{
		emoteDebounce = true;
		emotesWindow.SetActive(false);

		for (float i = 0; i < 0.3f; i += Time.deltaTime / 2)
		{
			emoteSR.transform.localScale = new Vector3(i, i, i);
			yield return null;
		}

		yield return new WaitForSeconds(0.5f);

		for (float i = 0; i < 0.3f; i += Time.deltaTime / 2)
		{
			emoteSR.transform.localScale = new Vector3(0.3f - i, 0.3f - i, 0.3f - i);
			yield return null;
		}

		emoteSR.transform.localScale = new Vector3(0, 0, 0);

		emoteDebounce = false;
	}

	public void SetPlace(int pl)
	{
		place = pl;

		if (IsOwner) placeText.text = positionNames[pl - 1];
	}

	private void Update()
	{
		if (IsOwner)
		{
			currentSpeed += Input.GetAxis("Vertical") * Time.deltaTime * acceleration;

			if (Mathf.Round(currentSpeed) != 0)
			{
				int dir = currentSpeed < 0 ? -1 : 1;


				if (!onGrass)
					rb.rotation += -dir * Input.GetAxis("Horizontal") * Time.deltaTime * turnRotation / (Mathf.Clamp(currentSpeed - 5, 1, 10) / 5);
			}

			if (currentSpeed != 0)
			{
				if (currentSpeed < 0)
					currentSpeed += speedLoss * Time.deltaTime;
				else if (currentSpeed > 0)
					currentSpeed -= speedLoss * Time.deltaTime;
			}

			if (Input.GetAxis("Horizontal") != 0)
			{
				rb.angularVelocity = 0;
			}

			cam.position = rb.transform.position - new Vector3(0, 0, 100);

			lapsText.text = "#" + (Laps.Value + 1).ToString();

			if (Input.GetKeyDown(KeyCode.E))
			{
				emotesWindow.SetActive(!emotesWindow.activeInHierarchy);
			}

		}

		rb.linearVelocity = Mathf.Abs(currentSpeed) > 0.1f ? new Vector2(currentSpeed * rb.transform.up.x, currentSpeed * rb.transform.up.y) : Vector2.zero;
		emoteSR.transform.rotation = Quaternion.Euler(Vector3.zero);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (IsOwner)
		{
			if (collision.CompareTag("Grass")) onGrass = true;

			else if (collision.CompareTag("Checkpoint") && collision.transform == gm.Checkpoints[CheckpointsSinceLapStart.Value])
			{
				CheckpointsSinceLapStart.Value++;
			}

			else if (collision.CompareTag("Finish"))
			{
				if (CheckpointsSinceLapStart.Value == gm.Checkpoints.Length)
				{
					Laps.Value++;
				}

				CheckpointsSinceLapStart.Value = 0;
			}
		}
	}
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.CompareTag("Grass")) onGrass = false;
	}
}
