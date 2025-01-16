using Unity.Netcode;
using UnityEngine;

public class Menu : MonoBehaviour
{
	[SerializeField] GameObject menuUi;

	public void StartClient()
	{
		NetworkManager.Singleton.StartClient();
		menuUi.SetActive(false);
	}

	public void StartHost()
	{
		NetworkManager.Singleton.StartHost();
		menuUi.SetActive(false);
	}
}
