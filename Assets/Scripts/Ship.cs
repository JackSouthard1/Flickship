using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ship : MonoBehaviour {

	[Header("Ship Settings")]
	public float thrust;
	public int health;
	public Weapon weapon;

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
		if (stage != Stage.Idle || Input.mousePosition.y < shipSelectionZoneY || localPlayer.actionState != Player.ActionUnderway.None || gm.activePlayer != assignedPlayerNumber || !gm.matchStarted) {
			return;
		}

		Vector3 mousePos3d = cam.ScreenToWorldPoint (Input.mousePosition);

		origionalRot = transform.rotation;
		dragAnchor = new Vector2 (mousePos3d.x, mousePos3d.y);

		localPlayer.camController.shipSelected = true;	

		stage = Stage.LooseDrag;
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
			UpdateShootPath();
			shootPath.gameObject.SetActive (true);
		} else {
			shootPath.gameObject.SetActive (false);
		}

		if (stage == Stage.Move) {
			shipGhost.gameObject.SetActive(true);
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

			localPlayer.HandleShipAction (shipNumber, force, "Move");
		} else if (stage == Stage.Shoot) {
			Vector2 direction = dragVector.normalized;
			origionalRot = transform.rotation;

			actionUnderway = true;
			localPlayer.HandleShipAction (shipNumber, -direction, "Shoot");
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

	private void UpdateShootPath () {
		float basicRatio = (dragVector.magnitude - shootRadius) / (moveRadius - shootRadius);
//		shootPathLengthRatio = (-4f * basicRatio) * (basicRatio - 1f);

		Vector3[] pathVerts = new Vector3[2];
		pathVerts [0] = shootPath.transform.position;
		pathVerts [1] = transform.TransformPoint ((Vector3.up * weapon.projectileRange) + shootPath.transform.localPosition);
		shootPath.SetPositions (pathVerts);
	}
}
