using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Starfield : MonoBehaviour {

	private Camera cam;
	private float detectionRange;
	private float detectionRangeScale = 5f;

	public GameObject starfieldPrefab;
	private float scale = 2f;
	private float offset;

	private Dictionary<Vector2, FieldData> starfields = new Dictionary<Vector2, FieldData>();
	private Dictionary<FieldData, Vector2> starfieldsInverse = new Dictionary<FieldData, Vector2>();

	void Start () {
		cam = Camera.main;
		offset = scale * 10f;
	}
	
	void Update () {
		detectionRange = cam.orthographicSize * detectionRangeScale;

		Vector2[] cords = GetFieldCordinatesInRange ();

		for (int i = 0; i < cords.Length; i++) {

			FieldData field;
			starfields.TryGetValue (cords [i], out field);

			if (field.GO == null) {
				GameObject newField = (GameObject)Instantiate (starfieldPrefab, Vector3.zero, Quaternion.Euler(new Vector3(-90, 0, 0)), transform);
				newField.transform.position = new Vector3 (cords [i].x * offset, cords [i].y * offset, 10);
				newField.transform.localScale = new Vector3 (scale, 1, scale);
				starfields.Add (cords [i], new FieldData(cords[i], newField));
				starfieldsInverse.Add (new FieldData (cords [i], newField), cords [i]); 
			}
		}

		List<FieldData> fieldDatas = starfieldsInverse.Keys.ToList ();
		for (int i = 0; i < fieldDatas.Count; i++) {
			if (!cords.Contains (fieldDatas [i].cord)) {
				Destroy (fieldDatas [i].GO);
				starfields.Remove (fieldDatas [i].cord);
				starfieldsInverse.Remove (fieldDatas [i]);
			}
		}
	}

	private Vector2[] GetFieldCordinatesInRange () {
		int cordsOnSide = Mathf.RoundToInt(detectionRange / offset);

		Vector2[] cords = new Vector2[cordsOnSide * cordsOnSide];

		Vector3 refinedCamPos = cam.transform.position / offset;
		Vector2 centerCord = new Vector2 (Mathf.RoundToInt(refinedCamPos.x), Mathf.RoundToInt(refinedCamPos.y));
		Vector2 bottomLeft = new Vector2 (centerCord.x - Mathf.RoundToInt(cordsOnSide / 2), centerCord.y - Mathf.RoundToInt(cordsOnSide / 2));

		int index = 0;
		for (int y = 0; y < cordsOnSide; y++) {
			for (int x = 0; x < cordsOnSide; x++) {
				cords [index] = new Vector2 (x + bottomLeft.x, y + bottomLeft.y);

				index++;
			}
		}

		return cords;
	}

	public struct FieldData {
		public Vector2 cord;
		public GameObject GO;

		public FieldData (Vector2 cord, GameObject GO) {
			this.cord = cord;
			this.GO = GO;
		}
	}
}
