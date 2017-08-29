using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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

	public int playerNumber;

	public int actionsDone = 0;
	GameManager gm;
	public CameraController camController;

	public List<GameObject> assignedShips;
	public List<int> assignedShipNumbers;

	private const float viewSpacing = 40f;
	private GameObject shipViewPrefab;
	public GameObject zoomButtonPrefab;
	private GameObject zoomButton;
	private List<GameObject> views;

	void Start ()
	{
		camController = GameObject.Find ("Main Camera").GetComponent<CameraController> ();
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();
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
	
		if (playerNumber == gm.activePlayer) {
			TurnStart ();
		} else {
			myTurn = false;
		}

		if (isLocalPlayer) {
			gm.SetupScene();
			camController.SetTarget(assignedShips[0].transform.position);

			if (playerNumber == 1) {
				camController.FlipCamera();
			}
		}
	}

	public void TurnStart ()
	{
		myTurn = true;
	}

	private void TurnEnd ()
	{
		myTurn = false;
		CmdTurnOver();
	}

	public void ActionOver ()
	{
		actionState = ActionUnderway.None;
		actionsDone++;

		CmdSyncShips();

		if (actionsDone >= 2) {
			TurnEnd();
			actionsDone = 0;
		}
	}

	[Command]
	public void CmdSyncShips ()
	{
		gm.CmdSyncShips();
	}

	[Command]
	public void CmdTurnOver () {
		gm.CmdTurnOver();
	}

	public void HandleShipAction (int shipNumber, Vector2 direction, string actionType, int sign = 1)
	{
		CmdShipAction (shipNumber: shipNumber, direction: direction, actionType: actionType, sign: sign);
		gm.ActionUsed();
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
			GameObject newBullet = GameObject.Instantiate (ship.GetComponent<Ship>().weapon.projectilePrefab);
			Bullet bulletScript = newBullet.GetComponent<Bullet> ();
			bulletScript.localPlayer = GetComponent<Player> ();
			bulletScript.parentShipNumber = shipNumber;
			Weapon weapon = ship.GetComponent<Ship>().weapon;
			bulletScript.SetupBullet(range: weapon.projectileRange, damage: weapon.projectileDamage, trajectory: weapon.trajectory, powerRatio: direction.magnitude, sign: sign);

			newBullet.transform.rotation = newRot;
			newBullet.transform.position = ship.transform.Find ("BulletSpawn").position;
			newBullet.GetComponent<Rigidbody2D> ().velocity = direction.normalized * ship.GetComponent<Ship>().weapon.projectileSpeed;

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
		gm.localPlayer.PrintWinner(winningPlayerNumber);
	}

	void PrintWinner (int winningPlayerNumber)
	{
		if (winningPlayerNumber == playerNumber) {
			print ("You Win!");
		} else {
			print ("You lose!");
		}
	}

	public void HandleBulletMiss (int parentShipNumber)
	{
		if (isServer) {
			RpcBulletDespawn(parentShipNumber);
		}
	}

	[ClientRpc]
	public void RpcBulletDespawn (int parentShipNumber)
	{
		GameObject parentShip = gm.GetShipWithShipNumber (parentShipNumber);
		parentShip.GetComponent<Ship> ().ShootActionDone ();

		actionState = ActionUnderway.None;
	}

	public void SetupUI ()
	{
		waitingForUI = true;
	}

	void Update () {
		if (waitingForUI && assignedShips.Count > 0) {
			UpdateUI ();
			PositionViews ();
			waitingForUI = false;
		}
	}

	private void UpdateUI ()
	{
		if (GameObject.Find ("Canvas").transform.Find ("ZoomButton") == null) {
			zoomButton = (GameObject)Instantiate (zoomButtonPrefab, Vector3.zero, Quaternion.identity, GameObject.Find ("Canvas").transform);
			zoomButton.GetComponent<Button>().onClick.AddListener(() => camController.ActionZoom());
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
			int assignedShipNumber = i;
			int shipPrefabIndex = gm.mapData.shipSpawnDatas[i].shipTypeIndex;
			newShipView.GetComponent<Image>().sprite = gm.shipPrefabs[shipPrefabIndex].icon;
			newShipView.GetComponent<Button>().onClick.AddListener(() => ShipViewClicked(assignedShipNumber));
		}

		views.Add (zoomButton);

		PositionViews();
	}

	private void PositionViews ()
	{
		for (int i = 0; i < views.Count; i++) {
			float spacingOffset = 0f;
			if (assignedShips.Count % 2 == 0) {
				spacingOffset = viewSpacing / 2;
			}
			float posX = ((i - Mathf.RoundToInt (assignedShips.Count / 2)) * viewSpacing) + spacingOffset;
			if (playerNumber == 1) {
				posX = -posX;
			}
			Vector3 anchoredPos = new Vector3 (posX, 50f, 0f);

			views[i].GetComponent<RectTransform>().anchoredPosition = anchoredPos;
		}

	}

	private void UpdateViewStates () {
//		for (int i = 0; i < shipViews.Count; i++) {
//			shipViews[i].transform.rotation = assignedShips[i].transform.FindChild("ShipHull").rotation;
//		}
	}

	public void ShipViewClicked (int buttonShipNumber)
	{
		camController.SetTarget(assignedShips[buttonShipNumber].transform.position);
	}
}
