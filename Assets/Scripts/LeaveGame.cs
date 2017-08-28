using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LeaveGame : MonoBehaviour {

	private NetworkManager networkManager;
	private Button button;

	void Start () {
		networkManager = NetworkManager.singleton;
		button = GetComponent<Button> ();
		button.onClick.AddListener(() => Disconnect());
	}
	
	public void Disconnect () {
		print ("Disconnect");
		networkManager.StopHost ();
	}
}
