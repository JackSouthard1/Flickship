using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour {

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

	void Start () 
	{
		cam = gameObject.GetComponent<Camera>();
		targetPos = transform.position;
	}

	void Update ()
	{
		if (!EventSystem.current.IsPointerOverGameObject () && !shipSelected) {
			if (Input.GetMouseButtonDown (0)) {
				momentum = Vector3.zero;
				target = Vector3.up;
			}

			if (Input.GetMouseButton (0)) {
				if (lastPosition != null) {
					Vector3 delta = (Input.mousePosition - lastPosition);
					momentum = delta * speed;
				}
			}

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
}
