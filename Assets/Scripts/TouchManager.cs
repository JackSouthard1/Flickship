using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchManager : MonoBehaviour
{
	public static bool isShipSelected = false;

	Camera cam;

	public static Vector2 firstTouchPos {
		get {
			if (Input.touchCount > 0)
				return Input.GetTouch (0).position;
			else if (Input.GetMouseButton (0))
				return Input.mousePosition;
			else {
				Debug.LogError ("No touch");
				return Vector2.zero;
			}
		}
	}

	void Start () {
		cam = gameObject.GetComponent<Camera> ();
	}
		
	void Update () {
		if (Input.GetMouseButton (0) && !isShipSelected)
			HandleTouchInput ();
		else
			CameraController.instance.ResetMovementState ();
	}

	void HandleTouchInput () {
		//only check for ships if it's the first touch
		if (Input.GetMouseButtonDown (0) && Input.touchCount < 2) {
			Ship currentShip = GetSelectedShip ();
			if (currentShip != null) {
				CameraController.instance.ResetMovementState ();
				StartCoroutine (currentShip.Click ());

				return;
			}
		}

		CameraController.instance.HandleTouchInput ();
	}

	Ship GetSelectedShip () {
		Vector3 mouseWorldPos3d = cam.ScreenToWorldPoint (firstTouchPos);
		Vector2 mouseWorldPos2d = new Vector2 (mouseWorldPos3d.x, mouseWorldPos3d.y);

		RaycastHit2D[] hitObjects = Physics2D.RaycastAll (mouseWorldPos2d, Vector2.zero);

		if (hitObjects.Length == 0)
			return null;

		Ship closestShip = null;
		float closestShipDistance = Mathf.Infinity;

		foreach (RaycastHit2D hitObject in hitObjects) {
			Ship ship;

			if (hitObject.collider.gameObject.name == "SelectionZone")
				ship = hitObject.collider.transform.parent.GetComponent<Ship> ();
			else
				ship = hitObject.collider.gameObject.GetComponent<Ship> ();

			if (ship != null) {
				float shipDistance = Vector3.Distance (mouseWorldPos3d, ship.transform.position);

				if (shipDistance < closestShipDistance) {
					closestShipDistance = shipDistance;
					closestShip = ship;
				}
			}
		}

		return closestShip;
	}
}