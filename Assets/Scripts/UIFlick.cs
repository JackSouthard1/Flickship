using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFlick : MonoBehaviour {

	public enum Stage
	{
		Idle,
		LooseDrag,
		Move
	};
	public Stage stage = Stage.Idle;

	public Transform dotted;
	public Transform button;
	public RectTransform serverListUI;
	[Space(20)]
	public float moveRadius;
	public float maxDragDistance;
	[Space(20)]
	public float dragForce;
	public float draggableRadius;

	private float anchorY;
	private float dragDistance;
	private float dragDistanceRefined;

	private bool released = false;
	private Camera cam;
	[Space(20)]
	public float endY;
	public float serverListOffset;
	private float serverListStartY;

	void Start () {
		cam = GameObject.Find("Main Camera").GetComponent<Camera>();
		serverListStartY = serverListUI.rect.y;
//		serverListOffset = transform.position.y - serverList.position.y;
	}
	
	void Update ()
	{
		if (!released) {
			if (Input.GetMouseButtonDown (0)) {
				ClickStart ();
			}

			if (Input.GetMouseButton (0)) {
				ClickDown ();
			}

			if (Input.GetMouseButtonUp (0)) {
				ClickRelease ();
			}
		}

		if (released) {
			if (transform.position.y >= endY) {
				Destroy (GetComponent<Rigidbody2D> ());
			} else {
//				serverListUI.rect.position = new Vector2 (0, serverListStartY - serverListOffset);
			}
		}
	}

	void ClickStart ()
	{
		if (stage != Stage.Idle) {
			return;
		}

		Vector3 mousePos3d = cam.ScreenToWorldPoint (Input.mousePosition);

		Vector3 diff = transform.position - new Vector3 (mousePos3d.x, mousePos3d.y, 0f);

		if (diff.magnitude > draggableRadius) {
			return;
		}

		anchorY = transform.position.y;

		stage = Stage.LooseDrag;
	}

	void ClickDown ()
	{
		if (stage == Stage.Idle) {
			return;
		}

		float mouseY = cam.ScreenToWorldPoint (Input.mousePosition).y;
		dragDistance = anchorY - mouseY;

		if (dragDistance > moveRadius) {
			stage = Stage.Move;
		}

		dragDistanceRefined = 0f;

		if (stage == Stage.Move) {
			dragDistanceRefined = Mathf.Clamp(dragDistance - moveRadius, 0, maxDragDistance);
		}

		button.localPosition = new Vector3 (0, -dragDistanceRefined, 0);
	}

	void ClickRelease ()
	{
		if (stage == Stage.Move && button.localPosition.magnitude > 0.1f) {			
			button.localPosition = Vector3.zero;

//			float powerRatio = dragDistanceRefined / (maxDragDistance - moveRadius);
			GetComponent<Rigidbody2D>().velocity = new Vector2(0, dragForce);

			released = true;

			GameObject.FindObjectOfType<MainMenu> ().StartServerListTransition ();
		} else {
			button.localPosition = Vector3.zero;
			stage = Stage.Idle;
		}
	}
}
