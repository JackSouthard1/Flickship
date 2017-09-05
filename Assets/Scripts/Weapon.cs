using System.Collections;
using UnityEngine;

[System.Serializable]
public class Weapon : MonoBehaviour {
	public Ship parentShip;
	[Header("Sway")]
	public float maxSway;
	public float swayTime;
	protected float baseRotZ;

	[Header("Shoot Path")]
	protected LineRenderer shootPath;
	public float minPowerRatio = 0.2f;
	public float resolution = 5f;

	private void Start () {
		parentShip = GetComponentInParent<Ship> ();
		shootPath = GetComponent<LineRenderer> ();
		baseRotZ = transform.localEulerAngles.z;
	}
	public virtual void Fire (Vector2 direction, int sign) {
		Debug.Log ("Fire");
	}

	public virtual void UpdateShootPath (Vector2 dragVector, float sway, float shootRadius, float moveRadius) {
		Debug.Log ("Update Path");
	}

	public float CalculateSway ()
	{
		float timeRatio = (Time.time) / swayTime;
		float refinedRatio = Mathf.Sin(2f * Mathf.PI * timeRatio);
		return refinedRatio * maxSway;
	}

	public void DisablePath () {
		shootPath.enabled = false;
	}
}