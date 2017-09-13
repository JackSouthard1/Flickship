using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : Projectile {
	public enum Shape {
		Straight,
		Semicircle,
		Circle
	};
	public Shape shape;
	public float size;

	public int liveTimeInTurns;
	public int health;

	private int turnsRemaining;
	private bool deployed = false;
	private bool drawing = false;

	private LineRenderer line;
	private float resolution = 3f;
	private EdgeCollider2D edge;

	public float drawTime;
	private float curDrawExtent;
	private float drawTimeRatio = 0;
	private float startTime;

	private Vector3[] finalPositions;

	public override void StartChild () {
		line = GetComponentInChildren<LineRenderer> ();
		edge = GetComponentInChildren<EdgeCollider2D> ();
	}		

	public override void RangeReached () {
		if (!deployed) {
			deployed = true;
		
			transform.GetChild(0).GetComponent<SpriteRenderer> ().enabled = false;
			GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
			turnsRemaining = liveTimeInTurns;

			Deploy ();
		}
	}

	public void TurnOver () {
		turnsRemaining--;
		if (turnsRemaining <= 0) {
			Destroy (gameObject);
		}
	}

	private void Deploy () {

		startTime = Time.time;

		int finalPositionsPerSide = Mathf.RoundToInt (resolution * size);
		int totalPositionCount = Mathf.RoundToInt(finalPositionsPerSide * 2 + 1);
		float incriment = 2 * size / totalPositionCount;

		finalPositions = new Vector3[totalPositionCount];
		float x = -size;
		for (int i = 0; i < finalPositions.Length; i++) {
			float y = CalculateSemicircle (x);
			finalPositions [i] = transform.TransformPoint(new Vector3 (x, y, 3f));

			x += incriment;
		}

		drawing = true;
	}

	void LateUpdate () {
		if (drawing) {
			DrawShield ();
		}
	}

	void DrawShield () {
		float passedTime = Time.time - startTime;
		drawTimeRatio = Mathf.Clamp01(passedTime / drawTime);

		int revealedPosCount = Mathf.RoundToInt (finalPositions.Length * drawTimeRatio);
		Vector3[] revealedPositions = new Vector3[revealedPosCount];

		int startingIndexCenter = Mathf.FloorToInt (finalPositions.Length / 2f);
		int startingIndex = startingIndexCenter - Mathf.FloorToInt (revealedPosCount / 2f);
		for (int i = 0; i < revealedPosCount; i++) {
			revealedPositions [i] = finalPositions [startingIndex + i];
		}

		line.positionCount = revealedPosCount;
		line.SetPositions (revealedPositions);

		if (drawTimeRatio >= 1) {
			drawing = false;
			DrawEdgeCollider ();
			if (master) {
				localPlayer.HandleProjectileActionComplete (parentShipNumber);
			}
		}
	}

	private float CalculateSemicircle (float x) {
		return Mathf.Sqrt (-(x * x) + (size * size)) - size;
	}

	void DrawEdgeCollider () {
		Vector2[] colPoints = new Vector2[finalPositions.Length];
		for (int i = 0; i < colPoints.Length; i++) {
			Vector3 localPoint = transform.InverseTransformPoint (finalPositions[i]);
			colPoints [i] =  new Vector2 (localPoint.x, localPoint.y);
		}

		edge.points = colPoints;
	}

	public void TakeDamage (int damage) {
		health -= damage;
		if (health <= 0) {
			Destroy (gameObject);
		}
	}

	public override void HitAstroid () {
		return;
	}

	public override void HitShip (Collider2D col) {
		return;
	}

	public override void HitShield (Collider2D col) {
		return;
	}
}