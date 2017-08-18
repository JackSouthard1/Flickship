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
	float height;

	void Start () {
		rectTransform = GetComponent<RectTransform>();
		width = rectTransform.rect.width;
		height = rectTransform.rect.height;

		UpdateUI();
	}

	public void TurnStart () {
		actionsLeft = maxActions;
		UpdateUI ();
	}

	public void ActionUsed ()
	{
		actionsLeft = actionsLeft - 1;
		UpdateUI ();
	}

	void UpdateUI () {
		float actionsUsed = maxActions - actionsLeft;
		float newWidth = actionsLeft * (width / maxActions) - width;
		fill.offsetMax = new Vector2 (newWidth, fill.offsetMax.y);
	}
}
