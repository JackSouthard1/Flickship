using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour {
	private Ship ship;
	private Rigidbody2D rb;

	private bool initalFOVSet = false;
	
	public float viewRadius;
	[Range(0,363)]
	public float viewAngle;

	public LayerMask targetMask;
	public LayerMask obstacleMask;

	public List<Transform> visibleShips = new List<Transform>();

	public float meshResolution;
	public int edgeResolveIterations;
	public float edgeDstThreshold;

	public float maskCutawayDst = 0.1f;

	public MeshFilter viewMeshFilter;
	Mesh viewMesh;

	void Awake () {
		rb = gameObject.GetComponent<Rigidbody2D>();
		viewMesh = new Mesh();
		viewMesh.name = "View Mesh";
		viewMeshFilter.mesh = viewMesh;
	}

	void Start ()
	{
		ship = GetComponent<Ship> ();
		StartCoroutine("FindTargetsWithDelay", 0.2f);
	}

	void LateUpdate ()
	{
		if (!initalFOVSet) {
			if (GameObject.FindGameObjectWithTag ("Astroid") != null) {
				DrawFieldOfView();
				initalFOVSet = true;
			}
		}
	}

	IEnumerator FindTargetsWithDelay (float delay)
	{
		while (true) {
			yield return new WaitForSeconds (delay);
			FindVisibleShips();
		}
	}

	public void UpdateFOV() {
		DrawFieldOfView ();
	}

	void FindVisibleShips ()
	{
		visibleShips.Clear();

		Collider2D[] shipsInViewRadius = Physics2D.OverlapCircleAll (new Vector2 (transform.position.x, transform.position.y), viewRadius, targetMask);

		for (int i = 0; i < shipsInViewRadius.Length; i++) {
			Transform target = shipsInViewRadius [i].transform;
			Vector2 targetPos2d = new Vector2(target.transform.position.x, target.transform.position.y);
			Vector2 pos2d = new Vector2 (transform.position.x, transform.position.y);

			Vector2 dirToTarget = (targetPos2d - pos2d).normalized;
			if (Vector2.Angle (new Vector2(transform.up.x, transform.up.y), dirToTarget) < viewAngle / 2) {
				float disToTarget = Vector2.Distance(pos2d, targetPos2d);

				if (!Physics2D.Raycast(pos2d, dirToTarget, disToTarget, obstacleMask)) {
					visibleShips.Add(target);
				}
			}
		}
	}

	void DrawFieldOfView ()
	{
		int stepCount = Mathf.RoundToInt (viewAngle * meshResolution);
		float stepAngleSize = viewAngle / stepCount;
		List<Vector3> viewPoints = new List<Vector3> ();
		ViewCastInfo oldViewCast = new ViewCastInfo ();
		for (int i = 0; i < stepCount; i++) {
			float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
			ViewCastInfo newViewCast = ViewCast (angle);

			if (i > 0) {
				bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
				if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit  && edgeDstThresholdExceeded)) {
					EdgeInfo edge = FindEdge (oldViewCast, newViewCast);
					if (edge.pointA != Vector2.zero) {
						viewPoints.Add(edge.pointA);
					}
					if (edge.pointB != Vector2.zero) {
						viewPoints.Add(edge.pointB);
					}
				}
			}

			viewPoints.Add (new Vector3 (newViewCast.point.x, newViewCast.point.y, 0));
			oldViewCast = newViewCast;
		}

		int vertexCount = viewPoints.Count + 1;
		Vector3[] verticies = new Vector3[vertexCount];
		int[] triangles = new int[(vertexCount - 2) * 3];

		verticies [0] = Vector3.zero;
		for (int i = 0; i < vertexCount - 1; i++) {
//			float cutawayMultiplier = (viewPoints[i].magnitude + maskCutawayDst) / viewPoints[i].magnitude;
			verticies [i + 1] = transform.InverseTransformPoint (viewPoints [i]);

			if (i < vertexCount - 2) {
				triangles [i * 3] = 0;
				triangles [i * 3 + 1] = i + 1;
				triangles [i * 3 + 2] = i + 2;
			}
		}

		viewMesh.Clear();
		viewMesh.vertices = verticies;
		viewMesh.triangles = triangles;
		viewMesh.RecalculateNormals();
	}

	EdgeInfo FindEdge (ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
	{
		float minAngle = minViewCast.angle;
		float maxAngle = maxViewCast.angle;
		Vector2 minPoint = Vector2.zero;
		Vector2 maxPoint = Vector2.zero;

		for (int i = 0; i < edgeResolveIterations; i++) {
			float angle = (minAngle + maxAngle) / 2;
			ViewCastInfo newViewCast = ViewCast (angle);

			bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
			if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded) {
				minAngle = angle;
				minPoint = newViewCast.point;
			} else {
				maxAngle = angle;
				maxPoint = newViewCast.point;
			}
		}

		return new EdgeInfo (minPoint, maxPoint);
	}

	ViewCastInfo ViewCast (float globalAngle)
	{
		Vector2 dir = DirFromAngle (globalAngle, true);
		RaycastHit2D hit = Physics2D.Raycast (new Vector2(transform.position.x, transform.position.y), dir, viewRadius, obstacleMask);

		if (hit) {
			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
		} else {
			return new ViewCastInfo(false, new Vector2(transform.position.x, transform.position.y) + dir * viewRadius, viewRadius, globalAngle);
		}
	}

	public Vector2 DirFromAngle (float angleInDegrees, bool angleIsGlobal)
	{
		if (!angleIsGlobal) {
			angleInDegrees += -transform.eulerAngles.z + 180;
		}
		return new Vector2(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
	}

	public struct ViewCastInfo {
		public bool hit;
		public Vector2 point;
		public float dst;
		public float angle;

		public ViewCastInfo(bool _hit, Vector2 _point, float _dst, float _angle) {
			hit = _hit;
			point = _point;
			dst = _dst;
			angle = _angle;
		}
	}

	public struct EdgeInfo {
		public Vector2 pointA;
		public Vector2 pointB;

		public EdgeInfo (Vector2 _pointA, Vector2 _pointB) {
			pointA = _pointA;
			pointB = _pointB;
		}
	}
}
