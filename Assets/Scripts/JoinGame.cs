using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Collections.Generic;
using UnityEngine.UI;

public class JoinGame : MonoBehaviour {

	private NetworkManager networkManager;

	private List<GameObject> roomList = new List<GameObject>();

	[SerializeField]
	private Text status;

	[SerializeField]
	private GameObject roomListItemPrefab;

	[SerializeField]
	private Transform roomListParent;

	[SerializeField]
	private Animator refreshAnim;

	void Start ()
	{
		networkManager = NetworkManager.singleton;

		if (networkManager.matchMaker == null) {
			networkManager.StartMatchMaker();
		}

		GetRoomList();
	}

	public void RefreshRoomList () {
		refreshAnim.SetBool ("isSpinning", true);
		GetRoomList ();
	}

	void GetRoomList () {
		ClearRoomList ();
		networkManager.matchMaker.ListMatches(0, 20, "", false, 0, 0, OnMatchList);
		status.text = "Loading...";
	}

	public void OnMatchList (bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
	{
		status.text = "";

		if (matches == null) {
			status.text = "Error";
			return;
		}

		foreach (MatchInfoSnapshot matchInfo in matches) {
			GameObject roomListItemGO = Instantiate (roomListItemPrefab, roomListParent);

			RoomListItem _roomListItem = roomListItemGO.GetComponent<RoomListItem> ();
			if (_roomListItem != null) {
				_roomListItem.Setup(matchInfo, JoinRoom);
			}

			roomList.Add (roomListItemGO);
		}

		if (roomList.Count == 0) {
			status.text = "No rooms";
		}

		refreshAnim.SetBool ("isSpinning", false);
    }

    void ClearRoomList () {
		for (int i = 0; i < roomList.Count; i++) {
    		Destroy(roomList[i]);
		}

		roomList.Clear();
    }

    public void JoinRoom (MatchInfoSnapshot _matchInfo) {
    	networkManager.matchMaker.JoinMatch(_matchInfo.networkId, "", "", "", 0, 0, networkManager.OnMatchJoined);
    	ClearRoomList();
    	status.text = "Joining";
    }
}
