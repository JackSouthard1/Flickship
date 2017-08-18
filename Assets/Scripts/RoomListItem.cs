using UnityEngine;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour {
	private MatchInfoSnapshot matchInfo;

	public delegate void JoinRoomDelegate(MatchInfoSnapshot matchInfo);
	private JoinRoomDelegate joinRoomCallback;

	[SerializeField]
	private Text roomNameText;

	public void Setup (MatchInfoSnapshot _matchInfo, JoinRoomDelegate _joinRoomCallback)
	{
		matchInfo = _matchInfo;
		joinRoomCallback = _joinRoomCallback;

		roomNameText.text = matchInfo.name + " (" + matchInfo.currentSize + "/" + matchInfo.maxSize + ")";
	}

	public void JoinRoom () {
		joinRoomCallback.Invoke(matchInfo);
	}
}
