using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ship : MonoBehaviour {

	[Header("Ship Settings")]
	public float thrust;
	public int health;
	public Weapon weapon;
	private float shootPathResolution = 5f;
	private float minPowerRatio = 0.2f;

	private enum Stage
	{
		Idle,
		LooseDrag,
		Shoot,
		Move
	};
	private Stage stage = Stage.Idle;

	[Space(50)]
	public int assignedPlayerNumber;
	public int shipNumber;
	public Player localPlayer;
	public bool selected = false;
	public bool controllable = false;
	public bool actionUnderway = false;

	GameManager gm;

	Rigidbody2D rb;
	Transform shipHull;
	Transform shipGhost;

	LineRenderer shootPath;

	Camera cam;

	bool forceQued = false;
	Vector2 forceToBeApplied;

	Vector2 dragAnchor;
	Vector2 dragPoint;
	Vector2 dragVector;
	Vector2 dragVectorRefined;

	private float sway = 0f;
	private float swayStartTime;

	const float shootRadius = 2f;
	const float moveRadius = 5f;
	const float dragRadiusMax = 6f;

	const float velocityToStop = 4f;

	const float shipSelectionZoneY = 80f;

	private Quaternion origionalRot;

	void Start ()
	{
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();
		rb = GetComponent<Rigidbody2D> ();
		shipHull = transform.FindChild ("ShipHull");
		shipGhost = transform.FindChild ("ShipGhost");
		shootPath = transform.FindChild("ShootPath").GetComponent<LineRenderer>();
		cam = GameObject.Find ("Main Camera").GetComponent<Camera> ();
	}
	
	void Update ()
	{
		if (rb.velocity.magnitude <= velocityToStop) {
			if (rb.velocity.magnitude != 0) {
				rb.velocity = Vector2.zero;
			}
		}

		if (controllable) {
			if (localPlayer.actionState == Player.ActionUnderway.None) {
				if (gm.activePlayer == assignedPlayerNumber) {
					if (Input.GetMouseButton (0)) {
						ClickDown ();
					}

					if (Input.GetMouseButtonUp (0)) {
						ClickRelease ();
					}
				}
			} else if (rb.velocity.magnitude <= 0) {
				if (localPlayer.actionState == Player.ActionUnderway.MoveAction && actionUnderway) {
					rb.angularVelocity = 0;
					actionUnderway = false;
					localPlayer.ActionOver ();
				}
			}
		}
	}

	private void OnMouseDown ()
	{
		if (stage != Stage.Idle || Input.mousePosition.y < shipSelectionZoneY || localPlayer.actionState != Player.ActionUnderway.None || gm.activePlayer != assignedPlayerNumber || !gm.matchStarted || !localPlayer.myTurn) {
			return;
		}

		Vector3 mousePos3d = cam.ScreenToWorldPoint (Input.mousePosition);

		origionalRot = transform.rotation;
		dragAnchor = new Vector2 (mousePos3d.x, mousePos3d.y);

		localPlayer.camController.shipSelected = true;	

		stage = Stage.LooseDrag;

		sway = 0;
	}

	void ClickDown ()
	{
		if (stage == Stage.Idle) {
			return;
		}

		float angle = 0f;

		Vector3 dragPoint3d = cam.ScreenToWorldPoint (Input.mousePosition);
		dragPoint = new Vector2 (dragPoint3d.x, dragPoint3d.y);

		dragVector = dragPoint - dragAnchor;

		if (dragVector.magnitude > shootRadius) {
			stage = Stage.Shoot;

			if (dragVector.magnitude > moveRadius) {
				stage = Stage.Move;
			}
		} else {
			stage = Stage.LooseDrag;
		}

		Vector2 thumbOffset = dragVector.normalized * moveRadius;

		if (stage == Stage.Move) {
			dragVectorRefined = Vector2.ClampMagnitude (dragVector - thumbOffset, dragRadiusMax);
		} else {
			dragVectorRefined = Vector2.zero;
		}

		shipHull.localPosition = new Vector3 (0f, -dragVectorRefined.magnitude, 0f);
		angle = Mathf.Atan2 (dragVector.y, dragVector.x) * Mathf.Rad2Deg + 90f;

		Quaternion newRot = Quaternion.AngleAxis (angle, Vector3.forward);
		transform.rotation = newRot;

		if (stage == Stage.Shoot) {
			CalculateSway();
			shipHull.localRotation = Quaternion.Euler(new Vector3(0, 0, sway));
			UpdateShootPath();
			shootPath.gameObject.SetActive (true);
		} else {
			shootPath.gameObject.SetActive (false);
		}

		if (stage == Stage.Move) {
			shipGhost.gameObject.SetActive(true);
			shipHull.localRotation = Quaternion.Euler(Vector3.zero);
		} else {
			shipGhost.gameObject.SetActive(false);
		}
	}

	private void ClickRelease ()
	{
		if (stage == Stage.Move) {
			shipHull.localPosition = Vector3.zero;

			float powerRatio = dragVectorRefined.magnitude / dragRadiusMax;

			Vector2 force = -dragVectorRefined.normalized * powerRatio * thrust;

			actionUnderway = true;
			origionalRot = transform.rotation;

			localPlayer.HandleShipAction (shipNumber: shipNumber, direction: force, actionType: "Move");
		} else if (stage == Stage.Shoot) {
			Vector2 basicDirection = dragVector.normalized;
			origionalRot = transform.rotation;

			actionUnderway = true;

			float powerRatio = (dragVector.magnitude - shootRadius) / (moveRadius - shootRadius);

			int sign;
			if (powerRatio < 0.5f) {
				sign = 1;
			} else {
				sign = -1;
			}

			powerRatio = (-4f * powerRatio) * (powerRatio - 1f);
			powerRatio = Mathf.Clamp(powerRatio, minPowerRatio, 1f);

			Vector2 finalDirection = -basicDirection * powerRatio;
			finalDirection = RotateVector2(finalDirection, sway);

			localPlayer.HandleShipAction (shipNumber: shipNumber, direction: finalDirection, actionType: "Shoot", sign: sign);
		} else if (stage == Stage.LooseDrag) {
			transform.rotation = origionalRot;
		}

		stage = Stage.Idle;
		localPlayer.camController.shipSelected = false;	
		shootPath.gameObject.SetActive(false);
		shipGhost.gameObject.SetActive(false);
	}

	public void ShootActionDone ()
	{
		actionUnderway = false;

		if (localPlayer != null) {
			localPlayer.ActionOver ();
		}
	}

	public void QueForce (Vector2 force)
	{
		forceToBeApplied = force;
		forceQued = true;
	}

	void FixedUpdate ()
	{
		if (forceQued) {
			rb.AddForce(forceToBeApplied, ForceMode2D.Impulse);
			forceQued = false;
		}
	}

	private void UpdateShootPath ()
	{
		float basicRatio = (dragVector.magnitude - shootRadius) / (moveRadius - shootRadius);
		float projectileRange = weapon.projectileRange;

		float modifierRatio = 1;
		int nodeCount = 2;

		shootPath.transform.localRotation = Quaternion.Euler(new Vector3(0,0, sway));

		Vector3[] pathVerts;
		if (weapon.trajectory == Weapon.Trajectory.Wave) {
			modifierRatio = (-4f * basicRatio) * (basicRatio - 1f);
			modifierRatio = Mathf.Clamp (modifierRatio, minPowerRatio, 1f);

			nodeCount = Mathf.RoundToInt (shootPathResolution * projectileRange);
			shootPath.numPositions = nodeCount;

			float nodeIncriment = 1f / nodeCount;
			float[] nodePercentageY = new float[nodeCount];
			for (int i = 0; i < nodePercentageY.Length; i++) {
				nodePercentageY [i] = nodeIncriment * i;
			}

			pathVerts = new Vector3[nodeCount];
			for (int i = 0; i < pathVerts.Length; i++) {
				float x = 3 * Mathf.Sin (4f * nodePercentageY [i] / modifierRatio);
				Vector3 localPos = new Vector3 (x, (projectileRange * nodePercentageY [i] + shootPath.transform.localPosition.y), 0);
				pathVerts [i] = shootPath.transform.TransformPoint (localPos);
			}
		} else if (weapon.trajectory == Weapon.Trajectory.Straight) {
			shootPath.numPositions = 2;
			pathVerts = new Vector3[2];
			pathVerts [0] = shootPath.transform.TransformPoint(shootPath.transform.localPosition);
			pathVerts [1] = shootPath.transform.TransformPoint ((Vector3.up * projectileRange * modifierRatio) + shootPath.transform.localPosition);
		} else {
			modifierRatio = (-4f * basicRatio) * (basicRatio - 1f);
			modifierRatio = Mathf.Clamp (modifierRatio, minPowerRatio, 1f);

			int sign;
			if (basicRatio < 0.5f) {
				sign = 1;
			} else {
				sign = -1;
			}

			modifierRatio = modifierRatio * sign;

			nodeCount = Mathf.RoundToInt (shootPathResolution * projectileRange);
			shootPath.numPositions = nodeCount;

			float nodeIncriment = 1f / nodeCount;
			float[] nodePercentageY = new float[nodeCount];
			for (int i = 0; i < nodePercentageY.Length; i++) {
				nodePercentageY [i] = nodeIncriment * i;
			}

			pathVerts = new Vector3[nodeCount];
			for (int i = 0; i < pathVerts.Length; i++) {
				float x = (100 * nodePercentageY[i] * nodePercentageY[i]) / (4 * modifierRatio);
				Vector3 localPos = new Vector3 (x, (projectileRange * nodePercentageY [i] + shootPath.transform.localPosition.y), 0);
				pathVerts [i] = shootPath.transform.TransformPoint (localPos);
			}
		}
		shootPath.SetPositions (pathVerts);
	}

	private void CalculateSway ()
	{
		float timeRatio = (Time.time) / weapon.swayTime;
		float refinedRatio = Mathf.Sin(2f * Mathf.PI * timeRatio);
		sway = refinedRatio * weapon.maxSway;
	}

	private Vector2 RotateVector2 (Vector2 aPoint, float aDegree)
	{
	    return Quaternion.Euler(0,0, aDegree) * aPoint;
	}
}
