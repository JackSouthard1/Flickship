using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class HostGame : MonoBehaviour {
	private uint roomSize = 2;
	private string roomName = "000";

//	private GameObject menu;

	void Start ()
	{
		if (NetworkManager.singleton.matchMaker == null) {
			NetworkManager.singleton.StartMatchMaker(); 
		}

		roomName = Random.Range (0, 999).ToString ();
		GetComponent<Button>().onClick.AddListener(() => CreateRoom());
	}

	public void CreateRoom ()
	{
		if (roomName != null) {
			print ("Creating Room: " + roomName);
			NetworkManager.singleton.matchMaker.CreateMatch(roomName, roomSize, true, "", "", "", 0, 0, NetworkManager.singleton.OnMatchCreate);
		}
	}
}
