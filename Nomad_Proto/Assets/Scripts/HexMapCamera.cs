using UnityEngine;
using System.Collections;

public class HexMapCamera : MonoBehaviour {

	public float stickMinZoom, stickMaxZoom;

	public float swivelMinZoom, swivelMaxZoom;

	public float moveSpeedMinZoom, moveSpeedMaxZoom;

	public float rotationSpeed;

	public bool useMouse = false;
	public float mouseSpeed = 1f;
	public float panBorderThickness = 10f;
	private float mouseX = 0f;
	private float mouseY = 0f;

	Transform swivel, stick;

	public HexGrid grid;

	float zoom = 1f;

	float rotationAngle;

	[SerializeField] private float _focusSpeed = 0.25f;

	static HexMapCamera instance;

	public static bool Locked {
		set {
			instance.enabled = !value;
		}
	}

	public static void ValidatePosition () {
		if(instance)instance.AdjustPosition(0f, 0f);
	}

	void Awake () {
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}

	void OnEnable () {
		instance = this;
		ValidatePosition();

		//Focus on a unit
			HexUnit firstUnit = FindObjectOfType<HexGrid> ().GetUnits () [0];
			SetFollowedUnit (firstUnit);
	}

	void Update () {
		float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (zoomDelta != 0f) {
			AdjustZoom(zoomDelta);
		}

		float rotationDelta = Input.GetAxis("Rotation");
		if (rotationDelta != 0f) {
			AdjustRotation(rotationDelta);
		}

		float xDelta = Input.GetAxis("Horizontal");
		float zDelta = Input.GetAxis("Vertical");

		if (xDelta != 0f || zDelta != 0f) {
			AdjustPosition(xDelta, zDelta);
		} else //use mouse instead
		{
			if (useMouse) {
				if (Input.mousePosition.y >= Screen.height - panBorderThickness) {
					//move up
					mouseY += mouseSpeed * Time.deltaTime;
				} else if (Input.mousePosition.y < panBorderThickness) {
					//move down
					mouseY -= mouseSpeed * Time.deltaTime;
				} else {
					//damping
					if (mouseY != 0f) {
						if (mouseY > 0f)
							mouseY = Mathf.Clamp (mouseY - 2 * mouseSpeed * Time.deltaTime, 0f, 1f);
						else
							mouseY = Mathf.Clamp (mouseY + 2 * mouseSpeed * Time.deltaTime, -1f, 0f);
					}
				}
				if (Input.mousePosition.x >= Screen.width - panBorderThickness) {
					//move right
					mouseX += mouseSpeed * Time.deltaTime;
				} else if (Input.mousePosition.x < panBorderThickness) {
					//move left
					mouseX -= mouseSpeed * Time.deltaTime;
				} else {
					//damping
					if (mouseX != 0f) {
						if (mouseX > 0f)
							mouseX = Mathf.Clamp (mouseX - 2 * mouseSpeed * Time.deltaTime, 0f, 1f);
						else
							mouseX = Mathf.Clamp (mouseX + 2 * mouseSpeed * Time.deltaTime, -1f, 0f);
					}
				}
				if (mouseX != 0f || mouseY != 0f)
					AdjustPosition (mouseX, mouseY);
			}
		}
	}

	public void SetFollowedUnit(HexUnit unit)
	{
		StartCoroutine (FocusOnUnit (unit.transform.position));
	}

	IEnumerator FocusOnUnit(Vector3 unitPos)
	{
		float elaspedTime = 0f;
		Vector3 camPos = transform.localPosition;
		while (elaspedTime < _focusSpeed)
		{
			transform.localPosition = Vector3.Lerp (camPos, unitPos, elaspedTime / _focusSpeed);
			elaspedTime += Time.deltaTime;
			yield return null;
		}
		transform.localPosition = unitPos;
	}

	void AdjustZoom (float delta) {
		zoom = Mathf.Clamp01(zoom + delta);

		float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		stick.localPosition = new Vector3(0f, 0f, distance);

		float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

	void AdjustRotation (float delta) {
		rotationAngle += delta * rotationSpeed * Time.deltaTime;
		if (rotationAngle < 0f) {
			rotationAngle += 360f;
		}
		else if (rotationAngle >= 360f) {
			rotationAngle -= 360f;
		}
		transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
	}

	void AdjustPosition (float xDelta, float zDelta) {
		Vector3 direction =
			transform.localRotation *
			new Vector3(xDelta, 0f, zDelta).normalized;
		float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		float distance =
			Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
			damping * Time.deltaTime;

		Vector3 position = transform.localPosition;
		position += direction * distance;
		transform.localPosition =
			grid.wrapping ? WrapPosition(position) : ClampPosition(position);
	}

	void AdjustMousePosition (bool vertical)
	{
		if(vertical)	//damp vertical movement
		{
			
		}
		else 			//damp horizontal movement
		{
			
		}
		Vector3 direction =
			transform.localRotation *
			new Vector3(mouseX, 0f, mouseY).normalized;
		float damping = Mathf.Max(Mathf.Abs(mouseX), Mathf.Abs(mouseY));
		float distance =
			Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) *
			damping * Time.deltaTime;

		Vector3 position = transform.localPosition;
		position += direction * distance;
		transform.localPosition =
			grid.wrapping ? WrapPosition(position) : ClampPosition(position);		
	}

	Vector3 ClampPosition (Vector3 position) {
		float xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
		position.x = Mathf.Clamp(position.x, 0f, xMax);

		float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		return position;
	}

	Vector3 WrapPosition (Vector3 position) {
		float width = grid.cellCountX * HexMetrics.innerDiameter;
		while (position.x < 0f) {
			position.x += width;
		}
		while (position.x > width) {
			position.x -= width;
		}

		float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		grid.CenterMap(position.x);
		return position;
	}
}