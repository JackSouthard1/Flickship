using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;

public class Player : NetworkBehaviour {

	public enum ActionUnderway
	{
		None,
		MoveAction,
		ShootAction
	};
	public ActionUnderway actionState = ActionUnderway.None;
	public bool myTurn = false;
	private bool waitingForUI = false;

	public int playerNumber = -1;

	private bool TurnQued = false;
	private bool TurnEndQued = false;

	public int actionsDone = 0;
	GameManager gm;
	public CameraController camController;

	public List<GameObject> assignedShips;
	public List<int> assignedShipNumbers;

	private const float viewSpacing = 40f;
	private GameObject shipViewPrefab;
	public GameObject zoomButtonPrefab;
	private GameObject zoomButton;
	private List<GameObject> views = new List<GameObject>();

	public List<Transform> visableShips = new List<Transform>();

	private bool spawnedZoomButton = false;

	void Start ()
	{
		camController = GameObject.Find ("Main Camera").GetComponent<CameraController> ();
		gm = GameManager.instance;
		shipViewPrefab = Resources.Load ("ShipView") as GameObject;
		zoomButtonPrefab = Resources.Load ("ZoomButton") as GameObject;

		if (isLocalPlayer) {
			CmdConnect ();
			gm.localPlayer = GetComponent<Player> ();
			camController.localPlayer = GetComponent<Player> ();
			CmdRequestPlayerInformation ();
		}
	}

	[Command]
	public void CmdConnect ()
	{
		gm.CmdPlayerConnected();
	}

	[Command]
	public void CmdRequestPlayerInformation () {
		RpcSetPlayerInformation(gm.playersConnected - 1);
	}

	[ClientRpc]
	public void RpcSetPlayerInformation (int playerNumber)
	{
		this.playerNumber = playerNumber;
	
		if (playerNumber != gm.activePlayer) {
			myTurn = false;
		}

		if (isLocalPlayer) {
			gm.SetupScene();
//			print ("Local Player Number Set");
			camController.SetTarget(assignedShips[0].transform.position);

			if (playerNumber == 1) {
				camController.FlipCamera();
			}
		}
	}

	public void TurnStart ()
	{
		if (playerNumber == -1 || views.Count == 0) {
//			print ("Que Turn Start");
			TurnQued = true;
		} else {
			myTurn = true;
			gm.actionBar.StartCountdown ();
			for (int i = 0; i < assignedShips.Count; i++) {
				assignedShips [i].GetComponent<Ship> ().TurnStart ();
			}
			if (views.Count > 0) {
//				print ("Reset UI");
				ResetUIColor ();
			}
			actionsDone = 0;
		}
	}

	private void TurnEnd ()
	{
		myTurn = false;
		actionsDone = assignedShips.Count;
		gm.actionBar.StopCountdown ();
		camController.EnterActionZoom ();
		CmdTurnOver();
	}

	public void ActionOver ()
	{
		actionState = ActionUnderway.None;
		actionsDone++;
		print ("" + actionsDone + " / " + assignedShips.Count);

		CmdSyncShips();
		if (actionsDone >= assignedShips.Count) {
			TurnEndQued = true;
		}
	}

	[Command]
	public void CmdSyncShips ()
	{
		gm.CmdSyncShips();
	}

	public void OutOfTime () {
		TurnEndQued = true;
	}

	private void TerminateTurn () {
		for (int i = 0; i < assignedShips.Count; i++) {
			assignedShips [i].GetComponent<Ship> ().OutOfTime();
			ShipActionUsed (i);
		}
		TurnEnd ();
	}

	[Command]
	public void CmdTurnOver () {
		gm.CmdTurnOver();
	}

	public void HandleShipAction (int shipNumber, Vector2 direction, string actionType, int sign = 1)
	{
		CmdShipAction (shipNumber: shipNumber, direction: direction, actionType: actionType, sign: sign);

		ShipActionUsed (shipNumber);
	}

	[Command]
	public void CmdShipAction (int shipNumber, Vector2 direction, string actionType, int sign)
	{
		RpcShipAction (shipNumber: shipNumber, direction: direction, actionType: actionType, sign: sign);
	}

	[ClientRpc]
	public void RpcShipAction (int shipNumber, Vector2 direction, string actionType, int sign)
	{
		ExecuteShipAction (shipNumber: shipNumber, direction: direction, actionType: actionType, sign: sign);
	}

	private void ExecuteShipAction (int shipNumber, Vector2 direction, string actionType, int sign)
	{
		GameObject ship = gm.GetShipWithShipNumber (shipNumber);

		var angle = Mathf.Atan2 (direction.y, direction.x) * Mathf.Rad2Deg - 90f;
		Quaternion newRot = Quaternion.AngleAxis (angle, Vector3.forward);
		ship.transform.rotation = newRot;

		if (actionType == "Move") {
			ship.GetComponent<Rigidbody2D> ().velocity = direction;
			actionState = ActionUnderway.MoveAction;
		} else {
			Weapon[] weapons = ship.GetComponentsInChildren<Weapon>();
			for (int i = 0; i < weapons.Length; i++) {
				weapons[i].Fire (direction, sign);
			}

			actionState = ActionUnderway.ShootAction;
		}

		ship.GetComponent<Ship>().UpdateFOV();
	}

	public void HandleBulletHit (GameObject hit, int parentShipNumber, int damage)
	{
		if (isServer) {
			int hitShipNumber = hit.GetComponent<Ship> ().shipNumber;
			RpcBulletHit (hitShipNumber, damage);
			RpcBulletDespawn (parentShipNumber);
		}
	}

	[ClientRpc]
	public void RpcBulletHit (int hitShipNumber, int damage)
	{
		Ship shipScript = gm.GetShipWithShipNumber (hitShipNumber).GetComponent<Ship> ();

		shipScript.health -= damage;
		if (shipScript.health <= 0) {
			gm.localPlayer.ShipDestroyed (hitShipNumber);
		}
	}

	public void ShipDestroyed (int destroyedShipNumber)
	{
		gm.GetShipWithShipNumber (destroyedShipNumber).SetActive (false);
		 
		for (int i = 0; i < assignedShips.Count; i++) {
			Ship curShip = assignedShips [i].GetComponent<Ship> ();
//			print ("Hit Ship: " + hitShipNumber + ", Checking with ship: " + curShip.shipNumber);
			if (curShip.shipNumber == destroyedShipNumber) {
				assignedShips.Remove (assignedShips [i]);
//				print ("Removed ship number: " + curShip.shipNumber);
				UpdateUI ();

				if (assignedShips.Count <= 0) {
					CmdAllPlayerShipsDestroyed(playerNumber);
				}
			}
		}
	}

	[Command]
	void CmdAllPlayerShipsDestroyed (int playerNumber)
	{
		if (playerNumber == 0) {
			RpcGameOver (1);	
		} else {
			RpcGameOver (0);	
		}
	}

	[ClientRpc]
	void RpcGameOver (int winningPlayerNumber) {
		gm.actionBar.StopCountdown ();
		gm.PrintWinner(winningPlayerNumber);
	}

	public void HandleBulletMiss (int parentShipNumber)
	{
		if (isServer) {
			RpcBulletDespawn (parentShipNumber);
		}
	}

	[ClientRpc]
	public void RpcBulletDespawn (int parentShipNumber)
	{
		GameObject parentShip = gm.GetShipWithShipNumber (parentShipNumber);
		parentShip.GetComponent<Ship> ().BulletDespawn ();

		actionState = ActionUnderway.None;
	}

	public void SetupUI ()
	{
		waitingForUI = true;
	}

	void Update () {
		if (TurnQued) {
			if (playerNumber != -1 && views.Count > 0) {
				TurnQued = false;
				TurnStart ();
			}
		}
		if (TurnEndQued) {
			if (actionState == ActionUnderway.None) {
				TurnEndQued = false;
				TerminateTurn ();
			}
		}
		if (waitingForUI && assignedShips.Count > 0) {
			UpdateUI ();
			PositionViews ();
			waitingForUI = false;
		}

		RequestVisableShips ();
	}

	void ShipActionUsed (int index) {
		if (index > gm.mapData.shipSpawnDatas.Count - 1) {
			index -= gm.mapData.shipSpawnDatas.Count;
		}
		views [index].GetComponent<Image> ().color = new Color (1, 1, 1, 0.25f);
	}

	void ResetUIColor () {
		for (int i = 0; i < views.Count; i++) {
			views [i].GetComponent<Image> ().color = Color.white;
		}
	}

	private void UpdateUI ()
	{
		if (!spawnedZoomButton) {
			zoomButton = (GameObject)Instantiate (zoomButtonPrefab, Vector3.zero, Quaternion.identity, GameObject.Find ("Canvas").transform);
			zoomButton.GetComponent<Button>().onClick.AddListener(() => camController.EnterActionZoom());

			spawnedZoomButton = true;
		}

		if (views != null) {
			for (int i = 0; i < views.Count - 1; i++) {
				Destroy (views [i]);
			}
		}

		views = new List<GameObject>();

		for (int i = 0; i < assignedShips.Count; i++) {
			GameObject newShipView = (GameObject)Instantiate (shipViewPrefab, Vector3.zero, Quaternion.identity, GameObject.Find ("Canvas").transform);
			views.Add(newShipView);

			int shipNumber = assignedShips[i].GetComponent<Ship>().shipNumber;
			if (shipNumber > gm.mapData.shipSpawnDatas.Count - 1) {
				shipNumber -= gm.mapData.shipSpawnDatas.Count;
			}
			int index = i;
			int shipPrefabIndex = gm.mapData.shipSpawnDatas[shipNumber].shipTypeIndex;
			newShipView.GetComponent<Image>().sprite = gm.shipPrefabs[shipPrefabIndex].icon;
			newShipView.GetComponent<Button>().onClick.AddListener(() => ShipViewClicked(index));
		}

		views.Add (zoomButton);

		PositionViews();
	}

	private void PositionViews ()
	{
		for (int i = 0; i < views.Count; i++) {
			float spacingOffset = 0f;
			if (views.Count % 2 == 0) {
				spacingOffset = viewSpacing / 2;
			}
			float posX = ((i - Mathf.RoundToInt (views.Count / 2)) * viewSpacing) + spacingOffset;
			if (playerNumber == 1) {
				posX = -posX;
			}
			Vector3 anchoredPos = new Vector3 (posX, 50f, 0f);

			views[i].GetComponent<RectTransform>().anchoredPosition = anchoredPos;
		}

	}
		
	public void ShipViewClicked (int buttonShipNumber)
	{
		camController.SetTarget(assignedShips[buttonShipNumber].transform.position);
	}

	private void RequestVisableShips () {
		visableShips.Clear ();

		for (int i = 0; i < assignedShips.Count; i++) {
			visableShips.AddRange (assignedShips [i].GetComponent<FieldOfView> ().visibleShips);
		}

		// Remove duplicants
		visableShips = visableShips.Distinct().ToList();
	}
}
