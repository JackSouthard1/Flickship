﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionBar : MonoBehaviour {

	[SerializeField]
	private RectTransform background;
	[SerializeField]
	private RectTransform fill;

	private RectTransform rectTransform;

	int maxActions = 2;
	int actionsLeft = 0;

	float width;

	void Start () {
		rectTransform = GetComponent<RectTransform>();
		width = rectTransform.rect.width;
	}

	public void Enable ()
	{
		background.GetComponent<Image>().enabled = true;
		fill.GetComponent<Image>().enabled = true;
		UpdateUI();
	}

	public void TurnStart () {
		actionsLeft = maxActions;
		fill.gameObject.SetActive(true);
		background.gameObject.SetActive(true);		
		UpdateUI ();
	}

	public void ActionUsed ()
	{
		actionsLeft = actionsLeft - 1;
		if (actionsLeft <= 0) {
			fill.gameObject.SetActive(false);
			background.gameObject.SetActive(false);		
		} else {
			UpdateUI ();
		}
	}

	void UpdateUI () {
		float newWidth = actionsLeft * (width / maxActions) - width;
		fill.offsetMax = new Vector2 (newWidth, fill.offsetMax.y);
	}
}
