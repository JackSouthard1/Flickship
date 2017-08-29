using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour {
	public enum MovementState
	{
		None,
		Moving,
		Zooming
	};

	public bool shipSelected = false;

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
	}

	void CheckForTouches () {
		if (Input.touchCount == 2)
			CheckForPinchZoom ();
		else if (Input.touchCount == 1 || Input.GetMouseButton(0))
			CheckForScroll ();
		else
			currentState = MovementState.None;
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
}
