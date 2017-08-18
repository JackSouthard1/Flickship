using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public class HostGame : MonoBehaviour {
	private uint roomSize = 2;
	private string roomName;

//	private GameObject menu;

	void Start ()
	{
//		menu = GameObject.Find("Canvas").transform.FindChild("Menu").gameObject;
		roomName = Random.Range (0, 999).ToString ();

		if (NetworkManager.singleton.matchMaker == null) {
			NetworkManager.singleton.StartMatchMaker(); 
		}
	}

	public void CreateRoom ()
	{
		if (roomName != null) {
			 print ("Creating Room: " + roomName);
			 NetworkManager.singleton.matchMaker.CreateMatch(roomName, roomSize, true, "", "", "", 0, 0, NetworkManager.singleton.OnMatchCreate);
		}
	}

//	public void OnMatchCreate (bool success, string extendedInfo, MatchInfo matchInfo) {
//		menu.SetActive(false);
//	}
}
