using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionBar : MonoBehaviour {

	[SerializeField]
	private RectTransform background;
	[SerializeField]
	private RectTransform fill;

	private RectTransform rectTransform;

	bool timerActive = false;
	public float turnTime = 10;
	float timeLeft = 0;

	float width;

	void Start () {
		rectTransform = GetComponent<RectTransform>();
		width = rectTransform.rect.width;
	}

	public void StartCountdown () {
		timeLeft = turnTime;
		timerActive = true;
		fill.gameObject.SetActive(true);
		background.gameObject.SetActive(true);		
		UpdateUI ();
	}

	public void StopCountdown () {
		timerActive = false;
		fill.gameObject.SetActive (false);
		background.gameObject.SetActive (false);
	}

	void Update ()
	{
		if (timerActive) {
			if (timeLeft > 0) {
				timeLeft -= Time.deltaTime;
				UpdateUI ();
			} else {
				GameManager.instance.localPlayer.OutOfTime ();
				StopCountdown ();
			}
		}
	}

	void UpdateUI () {
		float newWidth = timeLeft * (width / turnTime) - width;
		fill.offsetMax = new Vector2 (newWidth, fill.offsetMax.y);
	}
}
