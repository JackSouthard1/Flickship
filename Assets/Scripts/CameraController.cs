using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;

public class CameraController : MonoBehaviour {
	public enum MovementState
	{
		None,
		Moving,
		Zooming
	};

	public bool shipSelected = false;

	private bool actionZoom = false;

	public float interpVelocity;
	public float minDistance;
	public float followDistance;

	private Camera cam;

	public GameObject center;
	public Vector3 target;

	public Player localPlayer;

	public Vector3 offset;
	Vector3 targetPos;

	private Vector3 lastPosition;
	private float speed = -0.06f;
	private Vector3 momentum = Vector3.zero;
	private float friction = 0.01f;

	private float zoomIntervals = 10f;


	private float zoomSpeed = 0.5f;

	MovementState currentState;

	private float padding = 10f;

	void Start () 
	{
		cam = gameObject.GetComponent<Camera>();
		targetPos = transform.position;
		currentState = MovementState.None;
	}

	void Update ()
	{
		if (!EventSystem.current.IsPointerOverGameObject () && !shipSelected) {

			CheckForTouches ();

			if (momentum != Vector3.zero) {
				transform.position += momentum;
				momentum *= Mathf.Pow(friction, Time.deltaTime);
			}
		}

		if (Input.GetKeyDown (KeyCode.Equals)) {
			cam.orthographicSize -= zoomIntervals;
		}

		if (Input.GetKeyDown (KeyCode.Minus)) {
			cam.orthographicSize += zoomIntervals;
		}

		if (actionZoom) {
			CalculateActionZoom ();
		}
	}

	void CheckForTouches () {
		if (Input.touchCount == 2) {
			actionZoom = false;
			CheckForPinchZoom ();
		} else if (Input.touchCount == 1 || Input.GetMouseButton (0)) {
			actionZoom = false;
			CheckForScroll ();
		} else {
			currentState = MovementState.None;
		}
	}

	void CheckForScroll () {
		if (currentState != MovementState.Moving) {
			currentState = MovementState.Moving;
			momentum = Vector3.zero;
			target = Vector3.up;
			lastPosition = Input.mousePosition;
		}

		if (currentState == MovementState.Moving) {
			if (lastPosition != null) {
				Vector3 delta = (Input.mousePosition - lastPosition);
				momentum = delta * speed * (cam.orthographicSize / 30);
				lastPosition = Input.mousePosition;
			}
		}
	}

	void CheckForPinchZoom () {
		currentState = MovementState.Zooming;

		Touch touchZero = Input.GetTouch(0);
		Touch touchOne = Input.GetTouch(1);

		Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
		Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

		float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
		float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

		float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

		cam.orthographicSize += deltaMagnitudeDiff * zoomSpeed;

		cam.orthographicSize = Mathf.Max(cam.orthographicSize, 0.1f);

		lastPosition = Input.mousePosition;
	}

	private bool mouseOverGameObject ()
	{
		RaycastHit2D hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (Input.mousePosition), Vector2.zero);
 
		if (hit.collider != null) {
			return true;
		} else {
			return false;
		}
	}

	public void FlipCamera () {
		transform.Rotate(new Vector3(0f, 0f, 180f));
		speed = -speed;
	}
	
	void FixedUpdate () 
	{
		if (target != Vector3.up)
		{
			Vector3 posNoZ = transform.position;
			posNoZ.z = target.z;

			Vector3 targetDirection = (target - posNoZ);

			interpVelocity = targetDirection.magnitude * 5f;

			targetPos = transform.position + (targetDirection.normalized * interpVelocity * Time.deltaTime); 

			transform.position = Vector3.Lerp( transform.position, targetPos + offset, 0.5f);
		}
	}

	public void SetTarget (Vector3 target)
	{
		this.target = target;
		momentum = Vector3.zero;
	}

	public IEnumerator LerpCamSize (float newSize, float time)
	{
		float elapsedTime = 0;
		float startingSize = cam.orthographicSize;
		float sizeDifference = newSize - startingSize;

		while (elapsedTime < time) {
			float timeRatio = elapsedTime / time;
			cam.orthographicSize = startingSize + sizeDifference * timeRatio;
			elapsedTime += Time.deltaTime;
			yield return new WaitForEndOfFrame ();
		}
	}

	public void EnterActionZoom () {
		actionZoom = true;
	}

	private void CalculateActionZoom () {
		List<Transform> visableShips = localPlayer.visableShips;

		if (visableShips.Count == 0) {
			return;
		}

		float minX = Mathf.Infinity;
		float maxX = -Mathf.Infinity;
		float minY = Mathf.Infinity;
		float maxY = -Mathf.Infinity;

		for (int i = 0; i < visableShips.Count; i++) {
			Vector2 pos2d = new Vector2 (visableShips [i].position.x, visableShips [i].position.y);
			if (pos2d.x < minX) {
				minX = pos2d.x;
			}
			if (pos2d.y < minY) {
				minY = pos2d.y;
			}
			if (pos2d.x > maxX) {
				maxX = pos2d.x;
			}
			if (pos2d.y > maxY) {
				maxY = pos2d.y;
			}
		}
		minX -= padding;
		maxX += padding;
		minY -= padding;
		maxY += padding;

		float xDiff = maxX - minX;
		float yDiff = maxY - minY;

		// Calculate Center
		Vector3 center = new Vector3(xDiff/2 + minX, yDiff/2 + minY, 0);
		SetTarget (center);

		float screenAspect = cam.aspect;
		float shipAspect = xDiff / yDiff;

		float halfHeight;

		if (shipAspect > screenAspect) {
			halfHeight = (xDiff / screenAspect) / 2;
		} else {
			halfHeight = yDiff / 2;
		}

		cam.orthographicSize = halfHeight;
	}
}
