using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionSquare : MonoBehaviour {
	private GameObject target;
	private Image image;

	private bool lerping = false;
	private const float lerpTime = 0.3f;
	float elapsedTime;
	Vector3 startingPos;
	Vector3 targetPos;

	void Start () {
		image = GetComponent<Image>();
		image.enabled = false;
	}

	void FixedUpdate ()
	{
		if (lerping) {
			transform.position = Vector3.Lerp (startingPos, target.transform.position, elapsedTime / lerpTime);
			elapsedTime += Time.deltaTime;
			if (elapsedTime >= lerpTime) {
				lerping = false;
			}
		}
	}
	
	public void SetTarget (GameObject target)
	{
		if (image.enabled == false) {
			transform.position = target.transform.position;
		} else {
			elapsedTime = 0;
			startingPos = transform.position;
			this.target = target;
			lerping = true;
		}
		image.enabled = true;
	}

	public void UpdatePosition ()
	{
		if (target) {
			transform.position = target.transform.position;
		} else {
			image.enabled = false;
		}
	}

	public void Deselect () {
		image.enabled = false;
	}
}
