using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
	public GameObject serverList;

	public float transitionTime;

	static bool isMainMenu = true;

	void Start() {
		if (!isMainMenu)
			ExitMainMenu ();
		else
			serverList.SetActive(false);
	}

	public void StartServerListTransition() {
		StartCoroutine (ServerListTransition ());
	}

	IEnumerator ServerListTransition() {
		serverList.SetActive (true);
		serverList.GetComponent<Animator> ().SetTrigger("ExitMenu");

		yield return new WaitForSeconds (transitionTime);

		ExitMainMenu ();
	}

	void ExitMainMenu () {
		isMainMenu = false;
		gameObject.SetActive (false);
		GameObject.FindObjectOfType<JoinGame> ().StartMatchMaker ();
	}
}