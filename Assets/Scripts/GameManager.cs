using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour {
	public static GameManager instance;

	public List<ShipPrefab> shipPrefabs;
	public MapData mapData;

	[HideInInspector]
	public int activePlayer = 0;
	public int playersConnected = 0;
	[HideInInspector]
	public Player localPlayer;
	private List<GameObject> ships;
	[Space(20)]
	public GameObject menu;

	public ActionBar actionBar;
	GameObject waitingText;
	Text gameOverText;

	[SerializeField]
	public Color[] playerColors;

	public bool allowDebugMatches;
	public bool matchStarted = false;

	[Header("Astroids")]
	public GameObject astroidPrefab;
	public int astroidCount;
	public int astroidSizeMin;
	public int astroidSizeMax;
	public int astroidMaxY;
	public float minAstroidDst;

	[Space(10)]
	public float astroidResolution;
	public float maxVertexHeightDifference;

	void Awake ()
	{
		instance = this;
	}

	void Start ()
	{
		actionBar = GameObject.Find ("Canvas").transform.Find ("ActionBar").GetComponent<ActionBar> ();
		waitingText = GameObject.Find ("Canvas").transform.Find ("WaitingText").gameObject;
		gameOverText = GameObject.Find ("Canvas").transform.Find ("GameOverText").gameObject.GetComponent<Text>();
		gameOverText.gameObject.SetActive (false);

		menu.SetActive (false);

		if (allowDebugMatches) {
			matchStarted = true;
		}

//		GenerateAstroids();
	}

	public override void OnStartServer ()
	{
		base.OnStartServer();
	}

	[Command]
	public void CmdTurnOver ()
	{
		if (activePlayer >= 1) {
			activePlayer = 0;
		} else {
			activePlayer = 1;
		}

		RpcTurnOver (activePlayer);
	}

	[ClientRpc]
	public void RpcTurnOver (int newActivePlayer)
	{
//		print ("Rpc. New Active Player: " + newActivePlayer);
		activePlayer = newActivePlayer;
		if (localPlayer.playerNumber == newActivePlayer) {
			localPlayer.TurnStart();
		}
	}

	[Command]
	public void CmdPlayerConnected ()
	{
		playersConnected++;
		if (playersConnected >= 2) {
			RpcStartGame(Random.Range(0, 100000));
		}
	}

	[ClientRpc]
	public void RpcStartGame (int seed)
	{
		matchStarted = true;

	
		if (isServer) {
			int[] colorIndexes = new int[2];
			colorIndexes [0] = Random.Range (0, 4);
			colorIndexes [1] = Random.Range (0, 4);
			while (colorIndexes [0] == colorIndexes [1]) {
				colorIndexes[1] = Random.Range (0, 4);
			}

			for (int i = 0; i < playersConnected; i++) {
				for (int j = 0; j < ships.Count/2; j++) {
					RpcSetShipColor(j + (i * ships.Count/2), colorIndexes[i]);
				}
			}

			SetActionBarColor (colorIndexes[0]);
			RpcSetActionBarColor(colorIndexes[1]);
		}

		waitingText.SetActive(false);
		localPlayer.SetupUI();
		if (localPlayer.playerNumber == activePlayer) {
			localPlayer.TurnStart ();
		}
		CameraController.instance.EnterActionZoom ();
		GenerateAstroids(seed);
	}

	[ClientRpc]
	public void RpcSetShipColor (int shipNumber, int colorIndex)
	{	
		ships[shipNumber].transform.Find("ShipHull").GetComponent<SpriteRenderer>().color = playerColors[colorIndex];
		ships[shipNumber].transform.Find("ShipGhost").GetComponent<SpriteRenderer>().color = playerColors[colorIndex];
		ships[shipNumber].transform.Find("ShipBlink").GetComponent<SpriteRenderer>().color = playerColors[colorIndex];
	}

	[ClientRpc]
	private void RpcSetActionBarColor (int colorIndex)
	{
		if (!isServer) {
			SetActionBarColor(colorIndex);
		}
	}

	private void SetActionBarColor (int colorIndex) {
		actionBar.transform.Find("Fill").GetComponent<Image>().color = playerColors[colorIndex];
		actionBar.transform.Find("Background").GetComponent<Image>().color = playerColors[colorIndex];
	}

	[Command]
	public void CmdSyncShips ()
	{
		Vector3[] newPositions = new Vector3[ships.Count];
		Quaternion[] newRotations = new Quaternion[ships.Count];

		for (int i = 0; i < newPositions.Length; i++) {
			newPositions[i] = ships[i].transform.position;
			newRotations[i] = ships[i].transform.rotation;
		}
		RpcSyncShips(newPositions, newRotations);
	}

	[ClientRpc]
	public void RpcSyncShips (Vector3[] newPositions, Quaternion[] newRotations)
	{
		if (!isServer) {
			for (int i = 0; i < newPositions.Length; i++) {
				CorrectShip(i, newPositions[i], newRotations[i]);
			}
		}
	}

	private void CorrectShip (int shipNumber, Vector3 newPos, Quaternion newRot)
	{
		ships[shipNumber].transform.position = newPos;
		ships[shipNumber].transform.rotation = newRot;
		ships[shipNumber].GetComponent<Rigidbody2D>().angularVelocity = 0;
		ships[shipNumber].GetComponent<Rigidbody2D>().velocity = Vector2.zero;
		ships[shipNumber].GetComponent<Ship>().UpdateFOV();
	}

	public GameObject GetShipWithShipNumber (int shipNumber)
	{
		for (int i = 0; i < ships.Count; i++) {
			if (ships [i].GetComponent<Ship> ().shipNumber == shipNumber) {
				return ships[i];
			}
		}

		return ships[0];
	}

	public void SetupScene ()
	{
		SetupBorders();
		SetupShips();
	}

	private void SetupShips ()
	{
		ships = new List<GameObject> ();
		int shipIndex = 0;

		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < mapData.shipSpawnDatas.Count; j++) {
				// Calculate X and Y spacing
				float spacingOffset = 0f;
				if (mapData.shipSpawnDatas.Count % 2 == 0) {
					spacingOffset = mapData.shipSpacing / 2;
				}
				float spawnPosX = ((j - Mathf.RoundToInt (mapData.shipSpawnDatas.Count / 2)) * mapData.shipSpacing) + spacingOffset;
				float spawnPosY = mapData.verticalSpawnOffset * ((i * 2) - 1);
				Vector3 shipSpawnPos = new Vector3 (spawnPosX, spawnPosY, 0);

				Quaternion shipSpawnRot = Quaternion.identity;
				if (i == 1) {
					shipSpawnRot = Quaternion.Euler(new Vector3(0f,0f, 180f));
				}

				GameObject ship = Instantiate (shipPrefabs [mapData.shipSpawnDatas [j].shipTypeIndex].prefab, shipSpawnPos, shipSpawnRot);
				ships.Add (ship);
				ship.GetComponent<Ship> ().assignedPlayerNumber = i;

				ship.GetComponent<Ship> ().shipNumber = shipIndex;
				shipIndex++;

				if (i == localPlayer.playerNumber) {
					ship.GetComponent<Ship> ().controllable = true;
					ship.GetComponent<Ship> ().localPlayer = localPlayer;
					localPlayer.assignedShipNumbers.Add (ship.GetComponent<Ship> ().shipNumber);
					localPlayer.assignedShips.Add (ship);

					ship.GetComponent<FieldOfView>().enabled = true;
				}
			}
		}
	}

	private void SetupBorders () {
		Transform borderContainer = GameObject.Find("Borders").transform;
		GameObject topBorder 	= borderContainer.Find("TopBorder")	.gameObject;
		GameObject bottomBorder = borderContainer.Find("BottomBorder")	.gameObject;
		GameObject leftBorder 	= borderContainer.Find("LeftBorder")	.gameObject;
		GameObject rightBorder	= borderContainer.Find("RightBorder")	.gameObject;

		topBorder.transform.position = new Vector3(0, mapData.mapHeight / 2, 0f);
		topBorder.GetComponent<BoxCollider2D>().size = new Vector2(mapData.mapWidth, 1);

		bottomBorder.transform.position = new Vector3(0, -mapData.mapHeight / 2, 0f);
		bottomBorder.GetComponent<BoxCollider2D>().size = new Vector2(mapData.mapWidth, 1);

		leftBorder.transform.position = new Vector3(-mapData.mapWidth / 2, 0, 0);
		leftBorder.GetComponent<BoxCollider2D>().size = new Vector2(1, mapData.mapHeight);

		rightBorder.transform.position = new Vector3(mapData.mapWidth / 2, 0, 0);
		rightBorder.GetComponent<BoxCollider2D>().size = new Vector2(1, mapData.mapHeight);
	}

	private void GenerateAstroids (int seed)
	{
		Random.InitState(seed);
		float sizeIncriments = (astroidSizeMax - astroidSizeMin) / astroidCount;
		for (int i = 0; i < astroidCount; i++) {
			float x = Random.Range (-mapData.mapWidth / 2, mapData.mapWidth / 2);
			float y = Random.Range (-astroidMaxY, astroidMaxY);
			Vector3 spawnPos = new Vector3 (x, y, 0);
			float size = astroidSizeMax - (i * sizeIncriments);
			SpawnAstroid (spawnPos, size);
//			if (SafeSpawnPos(spawnPos, size)) {
//				SpawnAstroid (spawnPos, size);
//			} else {
//				bool safeSpawn = false;
//				while (!safeSpawn) {
//					x = Random.Range(-mapData.mapWidth / 2, mapData.mapWidth / 2);
//					y = Random.Range(-astroidMaxY, astroidMaxY);
//					spawnPos = new Vector3 (x, y, 0);
//
//					safeSpawn = SafeSpawnPos(spawnPos, size);
//					if (safeSpawn) {
//						SpawnAstroid (spawnPos, size);
//					}
//				}
//			}
		}
	}

	private void SpawnAstroid (Vector3 spawnPos, float size)
	{
		// Find vertex angles from center
		int vertexCount = Mathf.RoundToInt (astroidResolution * size);
		float[] vertexAngles = new float[vertexCount];
		float angleDifference = 360f / vertexCount;
		for (int i = 0; i < vertexAngles.Length; i++) {
			vertexAngles [i] = i * angleDifference;
		}

		// Generate vertex points
		List<Vector3> verticies = new List<Vector3> ();
		verticies.Add (Vector3.zero);
		float previousRadius = size;
		for (int i = 1; i < vertexCount + 1; i++) {
			float angleInRad = Mathf.Deg2Rad * (vertexAngles [i - 1]);

			float vertexRadius = previousRadius + Random.Range (-maxVertexHeightDifference * size, maxVertexHeightDifference * size);
			vertexRadius = Mathf.Clamp(vertexRadius, astroidSizeMin, size + (minAstroidDst / 2));

			float angleVectorX = Mathf.Cos (angleInRad);
			float angleVectorY = Mathf.Sin (angleInRad);
			Vector3 angleVector = new Vector3 (angleVectorX, angleVectorY, 0);

			verticies.Add (angleVector * vertexRadius);

			previousRadius = vertexRadius;
		}

		// Generate Triangles Array
		List<int> triangles = new List<int> ();
		for (int i = 0; i < vertexCount; i++) {
			triangles.Add (0);
			triangles.Add (i + 1);

			if (i + 2 != vertexCount + 1) {
				triangles.Add (i + 2);
			} else {
				triangles.Add (1);
			}
		}

		// Generate collider path
		Vector2[] colPath = new Vector2[vertexCount];
		for (int i = 0; i < colPath.Length; i++) {
			Vector3 point = verticies [i + 1];
			colPath [i] = new Vector2 (point.x, point.y);
		}

		// Generate Mesh
		Mesh mesh = new Mesh ();
		mesh.SetVertices (verticies);
		mesh.SetTriangles (triangles, 0);
		mesh.RecalculateBounds ();

		// Create GO
		GameObject astroid = GameObject.Instantiate (astroidPrefab);
		astroid.transform.position = spawnPos;
		astroid.transform.Rotate (new Vector3 (180, 0, Random.Range (0, 360)));
		// Set mesh
		astroid.GetComponent<MeshFilter> ().mesh = mesh;

		// Set collider
		astroid.GetComponent<PolygonCollider2D> ().SetPath (0, colPath);

		// Set border
		Vector3[] lrPath = new Vector3[vertexCount + 1];
		for (int i = 0; i < lrPath.Length; i++) {
			if (i == vertexCount) {
				lrPath[i] = new Vector3 (colPath [0].x, colPath [0].y, -1);
			} else {
				lrPath [i] = new Vector3 (colPath [i].x, colPath [i].y, -1);
			}

		}
		astroid.GetComponent<LineRenderer>().positionCount = vertexCount + 1;
		astroid.GetComponent<LineRenderer>().SetPositions(lrPath);
	}

	private bool SafeSpawnPos (Vector3 pos, float size)
	{
		Vector2 pos2d = new Vector2 (pos.x, pos.y);
		Collider2D[] cols = Physics2D.OverlapCircleAll (pos2d, size);

		for (int i = 0; i < cols.Length; i++) {
			if (cols [i].gameObject.tag == "Ship") {
				return false;
			}
		}

		return true;
	}

	public void PrintWinner (int winningPlayerNumber)
	{
		if (winningPlayerNumber == localPlayer.playerNumber) {
			gameOverText.text = "You win!";
		} else {
			gameOverText.text = "You lose!";
		}

		gameOverText.gameObject.SetActive (true);
	}
}

[System.Serializable]
public struct MapData {
	public List<ShipSpawnData> shipSpawnDatas;
	public float verticalSpawnOffset;
	public float shipSpacing;
	public float mapWidth;
	public float mapHeight;

	public MapData (List<ShipSpawnData> shipSpawnData, float verticalSpawnOffset, float shipSpacing, float mapWidth, float mapHeight)
	{
		this.shipSpawnDatas = shipSpawnData;
		this.verticalSpawnOffset = verticalSpawnOffset;
		this.shipSpacing = shipSpacing;
		this.mapWidth = mapWidth;
		this.mapHeight = mapHeight;
	}
}

[System.Serializable]
public struct ShipSpawnData {
	public int spawnIndex;
	public int shipTypeIndex;
}

[System.Serializable]
public struct ShipPrefab {
	public GameObject prefab;
	public Sprite icon;
}


