using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ship : MonoBehaviour {

	private FieldOfView fov;
	[Header("Ship Settings")]
	public float thrust;
	public int health;
	[SerializeField]
	private Weapon[] weapons;
	private float minPowerRatio = 0.2f;

	private enum Stage
	{
		Idle,
		LooseDrag,
		Shoot,
		Move
	};
	private Stage stage = Stage.Idle;

	public int liveProjectiles = 0;

	[Space(50)]
	[Header("Non Editable")]
	public int assignedPlayerNumber;
	public int shipNumber;
	public Player localPlayer;
	public bool selected = false;
	public bool controllable = false;
	public bool actionUnderway = false;
	public bool actionDone = true;
	private bool blinking = false;

	Rigidbody2D rb;
	Transform shipHull;
	Transform shipGhost;

	Camera cam;

	bool forceQued = false;
	Vector2 forceToBeApplied;

	Vector2 dragAnchor;
	Vector2 dragPoint;
	Vector2 dragVector;
	Vector2 dragVectorRefined;

	private float sway = 0f;

	private float radiusScale = 1f;
	float shootRadius = 2f;
	float moveRadius = 5f;
	const float dragRadiusMax = 6f;

	const float velocityToStop = 4f;

	const float shipSelectionZoneY = 80f;

	private Quaternion origionalRot;

	private CircleCollider2D clickTrigger;
	private float selectionSizeRatio = 0.2f;

	private Transform shipBlink;
	private float blinkMin;
	private float blinkMax;
	private float blinkSizeRatio = 1.5f;
	private float blinkRate = 1f;

	void Start ()
	{
		origionalRot = transform.rotation;
		weapons = GetComponentsInChildren<Weapon> ();
		if (weapons.Length == 0) {
			moveRadius = shootRadius;
		}
		clickTrigger = transform.Find("SelectionZone").GetComponent<CircleCollider2D> ();
		fov = GetComponent<FieldOfView>();
		rb = GetComponent<Rigidbody2D> ();
		shipHull = transform.Find ("ShipHull");
		shipGhost = transform.Find ("ShipGhost");
		cam = GameObject.Find ("Main Camera").GetComponent<Camera> ();

		shipBlink = transform.Find ("ShipBlink");
		blinkMin = shipBlink.localScale.x;
		blinkMax = blinkMin * blinkSizeRatio;
	}

	public void TurnStart () {
		actionDone = false;
		blinking = true;
	}
	
	void Update ()
	{
		if (rb.velocity.magnitude <= velocityToStop) {
			if (rb.velocity.magnitude != 0) {
				rb.velocity = Vector2.zero;
			}
		}

		if (controllable) {
			if (rb.velocity.magnitude <= 0) {
				if (localPlayer.actionState == Player.ActionUnderway.MoveAction && actionUnderway) {
					rb.angularVelocity = 0;
					actionUnderway = false;
					localPlayer.ActionOver ();
				}
			}
		}

		if (blinking) {
			Blink ();
		} else {
			shipBlink.gameObject.SetActive (false);
		}

		SetSelectionZone ();
	}

	public IEnumerator Click ()
	{
		if (!controllable || localPlayer.actionState != Player.ActionUnderway.None || GameManager.instance.activePlayer != assignedPlayerNumber)
			yield break;

		ClickDown ();

		while (Input.GetMouseButton(0)) {
			ClickHold ();
			yield return new WaitForEndOfFrame ();
		}

		ClickRelease ();
	}

	void ClickDown ()
	{
		if (!controllable) {
			return;
		}
		if (stage != Stage.Idle || localPlayer.actionState != Player.ActionUnderway.None || GameManager.instance.activePlayer != assignedPlayerNumber || !GameManager.instance.matchStarted || !localPlayer.myTurn || actionDone) {
			return;
		}

		radiusScale = cam.orthographicSize / 25f;

		Vector3 touchPos3d = cam.ScreenToWorldPoint (TouchManager.firstTouchPos);

		origionalRot = transform.rotation;
		dragAnchor = new Vector2 (touchPos3d.x, touchPos3d.y);

		TouchManager.isShipSelected = true;	

		stage = Stage.LooseDrag;

		sway = 0;
	}

	void ClickHold ()
	{
		if (stage == Stage.Idle) {
			return;
		}

		blinking = false;

		float angle = 0f;

		Vector3 dragPoint3d = cam.ScreenToWorldPoint (TouchManager.firstTouchPos);
		dragPoint = new Vector2 (dragPoint3d.x, dragPoint3d.y);

		dragVector = (dragPoint - dragAnchor) / radiusScale;

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

		if (weapons.Length > 0) {
			if (stage == Stage.Shoot) {
				sway = weapons [0].CalculateSway ();
				for (int i = 0; i < weapons.Length; i++) {
					weapons [i].UpdateShootPath (dragVectorRefined, sway, shootRadius, moveRadius);
				}
			} else {
				for (int i = 0; i < weapons.Length; i++) {
					weapons [i].DisablePath ();
				}
			}
		}

		if (stage == Stage.Move) {
			shipGhost.gameObject.SetActive(true);
		} else {
			shipGhost.gameObject.SetActive(false);
		}
	}

	void ClickRelease ()
	{
		if (stage == Stage.Move) {
			shipHull.localPosition = Vector3.zero;

			float powerRatio = dragVectorRefined.magnitude / dragRadiusMax;

			Vector2 force = -dragVectorRefined.normalized * powerRatio * thrust;

			actionUnderway = true;
			origionalRot = transform.rotation;

			localPlayer.HandleShipAction (shipNumber: shipNumber, direction: force, actionType: "Move");
			actionDone = true;
			blinking = false;
		} else if (stage == Stage.Shoot) {
			Vector2 basicDirection = dragVector.normalized;
			origionalRot = transform.rotation;

			actionUnderway = true;

			float powerRatio = (dragVector.magnitude - shootRadius) / ((moveRadius) - (shootRadius));

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
			blinking = false;
			actionDone = true;
		} else if (stage == Stage.LooseDrag) {
			transform.rotation = origionalRot;
			UpdateFOV ();
			blinking = true;
		}

		stage = Stage.Idle;
		TouchManager.isShipSelected = false;	
		for (int i = 0; i < weapons.Length; i++) {
			weapons [i].DisablePath ();
		}
		shipGhost.gameObject.SetActive(false);
	}

	public void OutOfTime () {
		stage = Stage.Idle;
		actionDone = true;
		actionUnderway = false;
		blinking = false;
		transform.rotation = origionalRot;
		for (int i = 0; i < weapons.Length; i++) {
			weapons [i].DisablePath ();
		}
		shipHull.localPosition = Vector3.zero;
		UpdateFOV ();
	}

	public void BulletDespawn ()
	{
		liveProjectiles--;
		if (liveProjectiles <= 0) {
			actionUnderway = false;

			if (localPlayer != null) {
				localPlayer.ActionOver ();
			}
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

	private Vector2 RotateVector2 (Vector2 aPoint, float aDegree)
	{
	    return Quaternion.Euler(0,0, aDegree) * aPoint;
	}

	void LateUpdate ()
	{
		if (rb.velocity != Vector2.zero || rb.angularVelocity != 0 || stage != Stage.Idle) {
			UpdateFOV();
		}
	}

	public void UpdateFOV ()
	{
		if (fov.enabled == true) {
			fov.UpdateFOV ();
		}
	}

	private void SetSelectionZone () {
		float camSize = cam.orthographicSize;
		clickTrigger.radius = camSize * selectionSizeRatio;
	}

	private void Blink () {
		float curBlink = Mathf.Clamp01((Time.time * blinkRate) - Mathf.Floor (Time.time * blinkRate));
		float scale = (curBlink * (blinkMax - blinkMin)) + blinkMin;
		shipBlink.localScale = new Vector3 (scale, scale, 1);
		shipBlink.gameObject.SetActive (true);
	}
}
