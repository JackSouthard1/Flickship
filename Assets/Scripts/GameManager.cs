using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour {
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

	ActionBar actionBar;

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

	[Space(10)]
	public float astroidResolution;
	public float maxVertexHeightDifference;

	void Start ()
	{
		actionBar = GameObject.Find ("Canvas").transform.FindChild ("ActionBar").GetComponent<ActionBar> ();

		menu.SetActive (false);
		if (isServer) {
			actionBar.TurnStart ();
		}

		if (allowDebugMatches) {
			matchStarted = true;
		}
	}

	public override void OnStartServer ()
	{
		base.OnStartServer();
	}

	[Command]
	public void CmdTurnOver ()
	{
		if (activePlayer >= playersConnected - 1) {
			activePlayer = 0;
		} else {
			activePlayer++;
		}

		RpcTurnOver (activePlayer);
	}

	[ClientRpc]
	public void RpcTurnOver (int newActivePlayer)
	{
		activePlayer = newActivePlayer;
		if (localPlayer.playerNumber == newActivePlayer) {
			localPlayer.TurnStart();
			actionBar.TurnStart();
		}
	}

	public void ActionUsed ()
	{
		actionBar.ActionUsed();
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
		Random.InitState(seed);
		GenerateAstroids();
	}

	[ClientRpc]
	public void RpcSetShipColor (int shipNumber, int colorIndex)
	{	
		ships[shipNumber].transform.FindChild("ShipHull").GetComponent<SpriteRenderer>().color = playerColors[colorIndex];
		ships[shipNumber].transform.FindChild("ShipGhost").GetComponent<SpriteRenderer>().color = playerColors[colorIndex];
	}

	[ClientRpc]
	private void RpcSetActionBarColor (int colorIndex)
	{
		if (!isServer) {
			SetActionBarColor(colorIndex);
		}
	}

	private void SetActionBarColor (int colorIndex) {
		actionBar.transform.FindChild("Fill").GetComponent<Image>().color = playerColors[colorIndex];
		actionBar.transform.FindChild("Background").GetComponent<Image>().color = playerColors[colorIndex];
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
		GameObject topBorder 	= borderContainer.FindChild("TopBorder")	.gameObject;
		GameObject bottomBorder = borderContainer.FindChild("BottomBorder")	.gameObject;
		GameObject leftBorder 	= borderContainer.FindChild("LeftBorder")	.gameObject;
		GameObject rightBorder	= borderContainer.FindChild("RightBorder")	.gameObject;

		topBorder.transform.position = new Vector3(0, mapData.mapHeight / 2, 0f);
		topBorder.GetComponent<BoxCollider2D>().size = new Vector2(mapData.mapWidth, 1);

		bottomBorder.transform.position = new Vector3(0, -mapData.mapHeight / 2, 0f);
		bottomBorder.GetComponent<BoxCollider2D>().size = new Vector2(mapData.mapWidth, 1);

		leftBorder.transform.position = new Vector3(-mapData.mapWidth / 2, 0, 0);
		leftBorder.GetComponent<BoxCollider2D>().size = new Vector2(1, mapData.mapHeight);

		rightBorder.transform.position = new Vector3(mapData.mapWidth / 2, 0, 0);
		rightBorder.GetComponent<BoxCollider2D>().size = new Vector2(1, mapData.mapHeight);
	}

	private void GenerateAstroids () {
		for (int i = 0; i < astroidCount; i++) {
			float x = Random.Range(-mapData.mapWidth, mapData.mapWidth);
			float y = Random.Range(-astroidMaxY, astroidMaxY);
			Vector3 spawnPos = new Vector3 (x, y, 0);
			float size = Random.Range(astroidSizeMin, astroidSizeMax);
			SpawnAstroid(spawnPos, size);
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
//		Vector3[] lrPath = new Vector3[vertexCount + 1];
//		for (int i = 0; i < lrPath.Length; i++) {
//			if (i == vertexCount) {
//				lrPath[i] = new Vector3 (colPath [0].x, colPath [0].y, 0);
//			} else {
//				lrPath [i] = new Vector3 (colPath [i].x, colPath [i].y, 0);
//			}
//
//		}
//		astroid.GetComponent<LineRenderer>().numPositions = vertexCount + 1;
//		astroid.GetComponent<LineRenderer>().SetPositions(lrPath);
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


