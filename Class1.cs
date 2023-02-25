using System;
using Audio;
using EZCameraShake;
using UnityEngine;

// Token: 0x02000020 RID: 32
public class PlayerMovement : MonoBehaviour
{
	// Token: 0x17000008 RID: 8
	// (get) Token: 0x060000D4 RID: 212 RVA: 0x0000668D File Offset: 0x0000488D
	// (set) Token: 0x060000D5 RID: 213 RVA: 0x00006694 File Offset: 0x00004894
	public static PlayerMovement Instance { get; private set; }

	// Token: 0x060000D6 RID: 214 RVA: 0x0000669C File Offset: 0x0000489C
	private void Awake()
	{
		PlayerMovement.Instance = this;
		this.rb = base.GetComponent<Rigidbody>();
	}

	// Token: 0x060000D7 RID: 215 RVA: 0x000066B0 File Offset: 0x000048B0
	private void Start()
	{
		this.psEmission = this.ps.emission;
		this.playerCollider = base.GetComponent<Collider>();
		this.detectWeapons = (DetectWeapons)base.GetComponentInChildren(typeof(DetectWeapons));
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		this.readyToJump = true;
		this.wallNormalVector = Vector3.up;
		this.CameraShake();
		if (this.spawnWeapon != null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.spawnWeapon, base.transform.position, Quaternion.identity);
			this.detectWeapons.ForcePickup(gameObject);
		}
		this.UpdateSensitivity();
	}

	// Token: 0x060000D8 RID: 216 RVA: 0x00006755 File Offset: 0x00004955
	public void UpdateSensitivity()
	{
		if (!GameState.Instance)
		{
			return;
		}
		this.sensMultiplier = GameState.Instance.GetSensitivity();
	}

	// Token: 0x060000D9 RID: 217 RVA: 0x00006774 File Offset: 0x00004974
	private void LateUpdate()
	{
		if (this.dead || this.paused)
		{
			return;
		}
		this.DrawGrapple();
		this.DrawGrabbing();
		this.WallRunning();
	}

	// Token: 0x060000DA RID: 218 RVA: 0x00006799 File Offset: 0x00004999
	private void FixedUpdate()
	{
		if (this.dead || Game.Instance.done || this.paused)
		{
			return;
		}
		this.Movement();
	}

	// Token: 0x060000DB RID: 219 RVA: 0x000067C0 File Offset: 0x000049C0
	private void Update()
	{
		this.UpdateActionMeter();
		this.MyInput();
		if (this.dead || Game.Instance.done || this.paused)
		{
			return;
		}
		this.Look();
		this.DrawGrabbing();
		this.UpdateTimescale();
		if (base.transform.position.y < -200f)
		{
			this.KillPlayer();
		}
	}

	// Token: 0x060000DC RID: 220 RVA: 0x00006828 File Offset: 0x00004A28
	private void MyInput()
	{
		if (this.dead || Game.Instance.done)
		{
			return;
		}
		this.x = Input.GetAxisRaw("Horizontal");
		this.y = Input.GetAxisRaw("Vertical");
		this.jumping = Input.GetButton("Jump");
		this.crouching = Input.GetButton("Crouch");
		if (Input.GetButtonDown("Cancel"))
		{
			this.Pause();
		}
		if (this.paused)
		{
			return;
		}
		if (Input.GetButtonDown("Crouch"))
		{
			this.StartCrouch();
		}
		if (Input.GetButtonUp("Crouch"))
		{
			this.StopCrouch();
		}
		if (Input.GetButton("Fire1"))
		{
			if (this.detectWeapons.HasGun())
			{
				this.detectWeapons.Shoot(this.HitPoint());
			}
			else
			{
				this.GrabObject();
			}
		}
		if (Input.GetButtonUp("Fire1"))
		{
			this.detectWeapons.StopUse();
			if (this.objectGrabbing)
			{
				this.StopGrab();
			}
		}
		if (Input.GetButtonDown("Pickup"))
		{
			this.detectWeapons.Pickup();
		}
		if (Input.GetButtonDown("Drop"))
		{
			this.detectWeapons.Throw((this.HitPoint() - this.detectWeapons.weaponPos.position).normalized);
		}
	}

	// Token: 0x060000DD RID: 221 RVA: 0x00006978 File Offset: 0x00004B78
	private void Pause()
	{
		if (this.dead)
		{
			return;
		}
		if (this.paused)
		{
			Time.timeScale = 1f;
			UIManger.Instance.DeadUI(false);
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			this.paused = false;
			return;
		}
		this.paused = true;
		Time.timeScale = 0f;
		UIManger.Instance.DeadUI(true);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	// Token: 0x060000DE RID: 222 RVA: 0x000069E7 File Offset: 0x00004BE7
	private void UpdateTimescale()
	{
		if (Game.Instance.done || this.paused || this.dead)
		{
			return;
		}
		Time.timeScale = Mathf.SmoothDamp(Time.timeScale, this.desiredTimeScale, ref this.timeScaleVel, 0.15f);
	}

	// Token: 0x060000DF RID: 223 RVA: 0x00006A26 File Offset: 0x00004C26
	private void GrabObject()
	{
		if (this.objectGrabbing == null)
		{
			this.StartGrab();
			return;
		}
		this.HoldGrab();
	}

	// Token: 0x060000E0 RID: 224 RVA: 0x00006A44 File Offset: 0x00004C44
	private void DrawGrabbing()
	{
		if (!this.objectGrabbing)
		{
			return;
		}
		this.myGrabPoint = Vector3.Lerp(this.myGrabPoint, this.objectGrabbing.position, Time.deltaTime * 45f);
		this.myHandPoint = Vector3.Lerp(this.myHandPoint, this.grabJoint.connectedAnchor, Time.deltaTime * 45f);
		this.grabLr.SetPosition(0, this.myGrabPoint);
		this.grabLr.SetPosition(1, this.myHandPoint);
	}

	// Token: 0x060000E1 RID: 225 RVA: 0x00006AD4 File Offset: 0x00004CD4
	private void StartGrab()
	{
		RaycastHit[] array = Physics.RaycastAll(this.playerCam.transform.position, this.playerCam.transform.forward, 8f, this.whatIsGrabbable);
		if (array.Length < 1)
		{
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			MonoBehaviour.print("testing on: " + array[i].collider.gameObject.layer);
			if (array[i].transform.GetComponent<Rigidbody>())
			{
				this.objectGrabbing = array[i].transform.GetComponent<Rigidbody>();
				this.grabPoint = array[i].point;
				this.grabJoint = this.objectGrabbing.gameObject.AddComponent<SpringJoint>();
				this.grabJoint.autoConfigureConnectedAnchor = false;
				this.grabJoint.minDistance = 0f;
				this.grabJoint.maxDistance = 0f;
				this.grabJoint.damper = 4f;
				this.grabJoint.spring = 40f;
				this.grabJoint.massScale = 5f;
				this.objectGrabbing.angularDrag = 5f;
				this.objectGrabbing.drag = 1f;
				this.previousLookdir = this.playerCam.transform.forward;
				this.grabLr = this.objectGrabbing.gameObject.AddComponent<LineRenderer>();
				this.grabLr.positionCount = 2;
				this.grabLr.startWidth = 0.05f;
				this.grabLr.material = new Material(Shader.Find("Sprites/Default"));
				this.grabLr.numCapVertices = 10;
				this.grabLr.numCornerVertices = 10;
				return;
			}
		}
	}

	// Token: 0x060000E2 RID: 226 RVA: 0x00006CB0 File Offset: 0x00004EB0
	private void HoldGrab()
	{
		this.grabJoint.connectedAnchor = this.playerCam.transform.position + this.playerCam.transform.forward * 5.5f;
		this.grabLr.startWidth = 0f;
		this.grabLr.endWidth = 0.0075f * this.objectGrabbing.velocity.magnitude;
		this.previousLookdir = this.playerCam.transform.forward;
	}

	// Token: 0x060000E3 RID: 227 RVA: 0x00006D41 File Offset: 0x00004F41
	private void StopGrab()
	{
		UnityEngine.Object.Destroy(this.grabJoint);
		UnityEngine.Object.Destroy(this.grabLr);
		this.objectGrabbing.angularDrag = 0.05f;
		this.objectGrabbing.drag = 0f;
		this.objectGrabbing = null;
	}

	// Token: 0x060000E4 RID: 228 RVA: 0x00006D80 File Offset: 0x00004F80
	private void StartCrouch()
	{
		float d = 400f;
		base.transform.localScale = new Vector3(1f, 0.5f, 1f);
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 0.5f, base.transform.position.z);
		if (this.rb.velocity.magnitude > 0.1f && this.grounded)
		{
			this.rb.AddForce(this.orientation.transform.forward * d);
			AudioManager.Instance.Play("StartSlide");
			AudioManager.Instance.Play("Slide");
		}
	}

	// Token: 0x060000E5 RID: 229 RVA: 0x00006E5C File Offset: 0x0000505C
	private void StopCrouch()
	{
		base.transform.localScale = new Vector3(1f, 1.5f, 1f);
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y + 0.5f, base.transform.position.z);
	}

	// Token: 0x060000E6 RID: 230 RVA: 0x00006ED0 File Offset: 0x000050D0
	private void DrawGrapple()
	{
		if (this.grapplePoint == Vector3.zero || this.joint == null)
		{
			this.lr.positionCount = 0;
			return;
		}
		this.lr.positionCount = 2;
		this.endPoint = Vector3.Lerp(this.endPoint, this.grapplePoint, Time.deltaTime * 15f);
		this.offsetMultiplier = Mathf.SmoothDamp(this.offsetMultiplier, 0f, ref this.offsetVel, 0.1f);
		int num = 100;
		this.lr.positionCount = num;
		Vector3 position = this.gun.transform.GetChild(0).position;
		float num2 = Vector3.Distance(this.endPoint, position);
		this.lr.SetPosition(0, position);
		this.lr.SetPosition(num - 1, this.endPoint);
		float num3 = num2;
		float num4 = 1f;
		for (int i = 1; i < num - 1; i++)
		{
			float num5 = (float)i / (float)num;
			float num6 = num5 * this.offsetMultiplier;
			float num7 = (Mathf.Sin(num6 * num3) - 0.5f) * num4 * (num6 * 2f);
			Vector3 normalized = (this.endPoint - position).normalized;
			float num8 = Mathf.Sin(num5 * 180f * 0.017453292f);
			float num9 = Mathf.Cos(this.offsetMultiplier * 90f * 0.017453292f);

			//COMMENTED BELOW BUT PLS FIX IT!!
			//Vector3 position2 = position + (this.endPoint - position) / (float)num * (float)i + (num9 * num7 * Vector2.Perpendicular(normalized) + this.offsetMultiplier * num8 * Vector3.down);
			//this.lr.SetPosition(i, position2);
		}
	}

	// Token: 0x060000E7 RID: 231 RVA: 0x000070A4 File Offset: 0x000052A4
	private void FootSteps()
	{
		if (this.crouching || this.dead)
		{
			return;
		}
		if (this.grounded || this.wallRunning)
		{
			float num = 1.2f;
			float num2 = this.rb.velocity.magnitude;
			if (num2 > 20f)
			{
				num2 = 20f;
			}
			this.distance += num2;
			if (this.distance > 300f / num)
			{
				AudioManager.Instance.PlayFootStep();
				this.distance = 0f;
			}
		}
	}

	// Token: 0x060000E8 RID: 232 RVA: 0x0000712C File Offset: 0x0000532C
	private void Movement()
	{
		if (this.dead)
		{
			return;
		}
		this.rb.AddForce(Vector3.down * Time.deltaTime * 10f);
		Vector2 vector = this.FindVelRelativeToLook();
		float num = vector.x;
		float num2 = vector.y;
		this.FootSteps();
		this.CounterMovement(this.x, this.y, vector);
		if (this.readyToJump && this.jumping)
		{
			this.Jump();
		}
		float num3 = this.walkSpeed;
		if (this.sprinting)
		{
			num3 = this.runSpeed;
		}
		if (this.crouching && this.grounded && this.readyToJump)
		{
			this.rb.AddForce(Vector3.down * Time.deltaTime * 3000f);
			return;
		}
		if (this.x > 0f && num > num3)
		{
			this.x = 0f;
		}
		if (this.x < 0f && num < -num3)
		{
			this.x = 0f;
		}
		if (this.y > 0f && num2 > num3)
		{
			this.y = 0f;
		}
		if (this.y < 0f && num2 < -num3)
		{
			this.y = 0f;
		}
		float d = 1f;
		float d2 = 1f;
		if (!this.grounded)
		{
			d = 0.5f;
			d2 = 0.5f;
		}
		if (this.grounded && this.crouching)
		{
			d2 = 0f;
		}
		if (this.wallRunning)
		{
			d2 = 0.3f;
			d = 0.3f;
		}
		if (this.surfing)
		{
			d = 0.7f;
			d2 = 0.3f;
		}
		this.rb.AddForce(this.orientation.transform.forward * this.y * this.moveSpeed * Time.deltaTime * d * d2);
		this.rb.AddForce(this.orientation.transform.right * this.x * this.moveSpeed * Time.deltaTime * d);
		this.SpeedLines();
	}

	// Token: 0x060000E9 RID: 233 RVA: 0x00007368 File Offset: 0x00005568
	private void SpeedLines()
	{
		float num = Vector3.Angle(this.rb.velocity, this.playerCam.transform.forward) * 0.15f;
		if (num < 1f)
		{
			num = 1f;
		}
		float rateOverTimeMultiplier = this.rb.velocity.magnitude / num;
		if (this.grounded && !this.wallRunning)
		{
			rateOverTimeMultiplier = 0f;
		}
		this.psEmission.rateOverTimeMultiplier = rateOverTimeMultiplier;
	}

	// Token: 0x060000EA RID: 234 RVA: 0x000073E4 File Offset: 0x000055E4
	private void CameraShake()
	{
		float num = this.rb.velocity.magnitude / 9f;
		CameraShaker.Instance.ShakeOnce(num, 0.1f * num, 0.25f, 0.2f);
		base.Invoke("CameraShake", 0.2f);
	}

	// Token: 0x060000EB RID: 235 RVA: 0x00007438 File Offset: 0x00005638
	private void ResetJump()
	{
		this.readyToJump = true;
	}

	// Token: 0x060000EC RID: 236 RVA: 0x00007444 File Offset: 0x00005644
	private void Jump()
	{
		if ((this.grounded || this.wallRunning || this.surfing) && this.readyToJump)
		{
			MonoBehaviour.print("jumping");
			Vector3 velocity = this.rb.velocity;
			this.readyToJump = false;
			this.rb.AddForce(Vector2.up * this.jumpForce * 1.5f);
			this.rb.AddForce(this.normalVector * this.jumpForce * 0.5f);
			if (this.rb.velocity.y < 0.5f)
			{
				this.rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
			}
			else if (this.rb.velocity.y > 0f)
			{
				this.rb.velocity = new Vector3(velocity.x, velocity.y / 2f, velocity.z);
			}
			if (this.wallRunning)
			{
				this.rb.AddForce(this.wallNormalVector * this.jumpForce * 3f);
			}
			base.Invoke("ResetJump", this.jumpCooldown);
			if (this.wallRunning)
			{
				this.wallRunning = false;
			}
			AudioManager.Instance.PlayJump();
		}
	}

	// Token: 0x060000ED RID: 237 RVA: 0x000075B8 File Offset: 0x000057B8
	private void Look()
	{
		float num = Input.GetAxis("Mouse X") * this.sensitivity * Time.fixedDeltaTime * this.sensMultiplier;
		float num2 = Input.GetAxis("Mouse Y") * this.sensitivity * Time.fixedDeltaTime * this.sensMultiplier;
		Vector3 eulerAngles = this.playerCam.transform.localRotation.eulerAngles;
		this.desiredX = eulerAngles.y + num;
		this.xRotation -= num2;
		this.xRotation = Mathf.Clamp(this.xRotation, -90f, 90f);
		this.FindWallRunRotation();
		this.actualWallRotation = Mathf.SmoothDamp(this.actualWallRotation, this.wallRunRotation, ref this.wallRotationVel, 0.2f);
		this.playerCam.transform.localRotation = Quaternion.Euler(this.xRotation, this.desiredX, this.actualWallRotation);
		this.orientation.transform.localRotation = Quaternion.Euler(0f, this.desiredX, 0f);
	}

	// Token: 0x060000EE RID: 238 RVA: 0x000076C8 File Offset: 0x000058C8
	private void CounterMovement(float x, float y, Vector2 mag)
	{
		if (!this.grounded || this.jumping || this.exploded)
		{
			return;
		}
		float d = 0.16f;
		float num = 0.01f;
		if (this.crouching)
		{
			this.rb.AddForce(this.moveSpeed * Time.deltaTime * -this.rb.velocity.normalized * this.slideSlowdown);
			return;
		}
		if ((Math.Abs(mag.x) > num && Math.Abs(x) < 0.05f) || (mag.x < -num && x > 0f) || (mag.x > num && x < 0f))
		{
			this.rb.AddForce(this.moveSpeed * this.orientation.transform.right * Time.deltaTime * -mag.x * d);
		}
		if ((Math.Abs(mag.y) > num && Math.Abs(y) < 0.05f) || (mag.y < -num && y > 0f) || (mag.y > num && y < 0f))
		{
			this.rb.AddForce(this.moveSpeed * this.orientation.transform.forward * Time.deltaTime * -mag.y * d);
		}
		if (Mathf.Sqrt(Mathf.Pow(this.rb.velocity.x, 2f) + Mathf.Pow(this.rb.velocity.z, 2f)) > this.walkSpeed)
		{
			float num2 = this.rb.velocity.y;
			Vector3 vector = this.rb.velocity.normalized * this.walkSpeed;
			this.rb.velocity = new Vector3(vector.x, num2, vector.z);
		}
	}

	// Token: 0x060000EF RID: 239 RVA: 0x000078D4 File Offset: 0x00005AD4
	public void Explode()
	{
		this.exploded = true;
		base.Invoke("StopExplosion", 0.1f);
	}

	// Token: 0x060000F0 RID: 240 RVA: 0x000078ED File Offset: 0x00005AED
	private void StopExplosion()
	{
		this.exploded = false;
	}

	// Token: 0x060000F1 RID: 241 RVA: 0x000078F8 File Offset: 0x00005AF8
	public Vector2 FindVelRelativeToLook()
	{
		float current = this.orientation.transform.eulerAngles.y;
		float target = Mathf.Atan2(this.rb.velocity.x, this.rb.velocity.z) * 57.29578f;
		float num = Mathf.DeltaAngle(current, target);
		float num2 = 90f - num;
		float magnitude = this.rb.velocity.magnitude;
		float num3 = magnitude * Mathf.Cos(num * 0.017453292f);
		return new Vector2(magnitude * Mathf.Cos(num2 * 0.017453292f), num3);
	}

	// Token: 0x060000F2 RID: 242 RVA: 0x0000798C File Offset: 0x00005B8C
	private void FindWallRunRotation()
	{
		if (!this.wallRunning)
		{
			this.wallRunRotation = 0f;
			return;
		}
		Vector3 normalized = new Vector3(0f, this.playerCam.transform.rotation.y, 0f).normalized;
		new Vector3(0f, 0f, 1f);
		float current = this.playerCam.transform.rotation.eulerAngles.y;
		if (Math.Abs(this.wallNormalVector.x - 1f) >= 0.1f)
		{
			if (Math.Abs(this.wallNormalVector.x - -1f) >= 0.1f)
			{
				if (Math.Abs(this.wallNormalVector.z - 1f) >= 0.1f)
				{
					if (Math.Abs(this.wallNormalVector.z - -1f) < 0.1f)
					{
					}
				}
			}
		}
		float target = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), this.wallNormalVector, Vector3.up);
		float num = Mathf.DeltaAngle(current, target);
		this.wallRunRotation = -(num / 90f) * 15f;
		if (!this.readyToWallrun)
		{
			return;
		}
		if ((Mathf.Abs(this.wallRunRotation) >= 4f || this.y <= 0f || Math.Abs(this.x) >= 0.1f) && (Mathf.Abs(this.wallRunRotation) <= 22f || this.y >= 0f || Math.Abs(this.x) >= 0.1f))
		{
			this.cancelling = false;
			base.CancelInvoke("CancelWallrun");
			return;
		}
		if (this.cancelling)
		{
			return;
		}
		this.cancelling = true;
		base.CancelInvoke("CancelWallrun");
		base.Invoke("CancelWallrun", 0.2f);
	}

	// Token: 0x060000F3 RID: 243 RVA: 0x00007B90 File Offset: 0x00005D90
	private void CancelWallrun()
	{
		MonoBehaviour.print("cancelled");
		base.Invoke("GetReadyToWallrun", 0.1f);
		this.rb.AddForce(this.wallNormalVector * 600f);
		this.readyToWallrun = false;
		AudioManager.Instance.PlayLanding();
	}

	// Token: 0x060000F4 RID: 244 RVA: 0x00007BE3 File Offset: 0x00005DE3
	private void GetReadyToWallrun()
	{
		this.readyToWallrun = true;
	}

	// Token: 0x060000F5 RID: 245 RVA: 0x00007BEC File Offset: 0x00005DEC
	private void WallRunning()
	{
		if (this.wallRunning)
		{
			this.rb.AddForce(-this.wallNormalVector * Time.deltaTime * this.moveSpeed);
			this.rb.AddForce(Vector3.up * Time.deltaTime * this.rb.mass * 100f * this.wallRunGravity);
		}
	}

	// Token: 0x060000F6 RID: 246 RVA: 0x00007C6B File Offset: 0x00005E6B
	private bool IsFloor(Vector3 v)
	{
		return Vector3.Angle(Vector3.up, v) < this.maxSlopeAngle;
	}

	// Token: 0x060000F7 RID: 247 RVA: 0x00007C80 File Offset: 0x00005E80
	private bool IsSurf(Vector3 v)
	{
		float num = Vector3.Angle(Vector3.up, v);
		return num < 89f && num > this.maxSlopeAngle;
	}

	// Token: 0x060000F8 RID: 248 RVA: 0x00007CAC File Offset: 0x00005EAC
	private bool IsWall(Vector3 v)
	{
		return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.1f;
	}

	// Token: 0x060000F9 RID: 249 RVA: 0x00007CCB File Offset: 0x00005ECB
	private bool IsRoof(Vector3 v)
	{
		return v.y == -1f;
	}

	// Token: 0x060000FA RID: 250 RVA: 0x00007CDC File Offset: 0x00005EDC
	private void StartWallRun(Vector3 normal)
	{
		if (this.grounded || !this.readyToWallrun)
		{
			return;
		}
		this.wallNormalVector = normal;
		float d = 20f;
		if (!this.wallRunning)
		{
			this.rb.velocity = new Vector3(this.rb.velocity.x, 0f, this.rb.velocity.z);
			this.rb.AddForce(Vector3.up * d, ForceMode.Impulse);
		}
		this.wallRunning = true;
	}

	// Token: 0x060000FB RID: 251 RVA: 0x00007D62 File Offset: 0x00005F62
	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
		{
			this.KillEnemy(other);
		}
	}

	// Token: 0x060000FC RID: 252 RVA: 0x00003381 File Offset: 0x00001581
	private void OnCollisionExit(Collision other)
	{
	}

	// Token: 0x060000FD RID: 253 RVA: 0x00007D84 File Offset: 0x00005F84
	private void OnCollisionStay(Collision other)
	{
		int layer = other.gameObject.layer;
		if (this.whatIsGround != (this.whatIsGround | 1 << layer))
		{
			return;
		}
		for (int i = 0; i < other.contactCount; i++)
		{
			Vector3 normal = other.contacts[i].normal;
			if (this.IsFloor(normal))
			{
				if (this.wallRunning)
				{
					this.wallRunning = false;
				}
				if (!this.grounded && this.crouching)
				{
					AudioManager.Instance.Play("StartSlide");
					AudioManager.Instance.Play("Slide");
				}
				this.grounded = true;
				this.normalVector = normal;
				this.cancellingGrounded = false;
				base.CancelInvoke("StopGrounded");
			}
			if (this.IsWall(normal) && layer == LayerMask.NameToLayer("Ground"))
			{
				if (!this.onWall)
				{
					AudioManager.Instance.Play("StartSlide");
					AudioManager.Instance.Play("Slide");
				}
				this.StartWallRun(normal);
				this.onWall = true;
				this.cancellingWall = false;
				base.CancelInvoke("StopWall");
			}
			if (this.IsSurf(normal))
			{
				this.surfing = true;
				this.cancellingSurf = false;
				base.CancelInvoke("StopSurf");
			}
			this.IsRoof(normal);
		}
		float num = 3f;
		if (!this.cancellingGrounded)
		{
			this.cancellingGrounded = true;
			base.Invoke("StopGrounded", Time.deltaTime * num);
		}
		if (!this.cancellingWall)
		{
			this.cancellingWall = true;
			base.Invoke("StopWall", Time.deltaTime * num);
		}
		if (!this.cancellingSurf)
		{
			this.cancellingSurf = true;
			base.Invoke("StopSurf", Time.deltaTime * num);
		}
	}

	// Token: 0x060000FE RID: 254 RVA: 0x00007F3B File Offset: 0x0000613B
	private void StopGrounded()
	{
		this.grounded = false;
	}

	// Token: 0x060000FF RID: 255 RVA: 0x00007F44 File Offset: 0x00006144
	private void StopWall()
	{
		this.onWall = false;
		this.wallRunning = false;
	}

	// Token: 0x06000100 RID: 256 RVA: 0x00007F54 File Offset: 0x00006154
	private void StopSurf()
	{
		this.surfing = false;
	}

	// Token: 0x06000101 RID: 257 RVA: 0x00007F60 File Offset: 0x00006160
	private void KillEnemy(Collision other)
	{
		if (this.grounded && !this.crouching)
		{
			return;
		}
		if (this.rb.velocity.magnitude < 3f)
		{
			return;
		}
		Enemy enemy = (Enemy)other.transform.root.GetComponent(typeof(Enemy));
		if (!enemy)
		{
			return;
		}
		if (enemy.IsDead())
		{
			return;
		}
		UnityEngine.Object.Instantiate<GameObject>(PrefabManager.Instance.enemyHitAudio, other.contacts[0].point, Quaternion.identity);
		RagdollController ragdollController = (RagdollController)other.transform.root.GetComponent(typeof(RagdollController));
		if (this.grounded && this.crouching)
		{
			ragdollController.MakeRagdoll(this.rb.velocity * 1.2f * 34f);
		}
		else
		{
			ragdollController.MakeRagdoll(this.rb.velocity.normalized * 250f);
		}
		this.rb.AddForce(this.rb.velocity.normalized * 2f, ForceMode.Impulse);
		enemy.DropGun(this.rb.velocity.normalized * 2f);
	}

	// Token: 0x06000102 RID: 258 RVA: 0x000080B7 File Offset: 0x000062B7
	public Vector3 GetVelocity()
	{
		return this.rb.velocity;
	}

	// Token: 0x06000103 RID: 259 RVA: 0x000080C4 File Offset: 0x000062C4
	public float GetFallSpeed()
	{
		return this.rb.velocity.y;
	}

	// Token: 0x06000104 RID: 260 RVA: 0x000080D6 File Offset: 0x000062D6
	public Vector3 GetGrapplePoint()
	{
		return this.detectWeapons.GetGrapplerPoint();
	}

	// Token: 0x06000105 RID: 261 RVA: 0x000080E3 File Offset: 0x000062E3
	public Collider GetPlayerCollider()
	{
		return this.playerCollider;
	}

	// Token: 0x06000106 RID: 262 RVA: 0x000080EB File Offset: 0x000062EB
	public Transform GetPlayerCamTransform()
	{
		return this.playerCam.transform;
	}

	// Token: 0x06000107 RID: 263 RVA: 0x000080F8 File Offset: 0x000062F8
	public Vector3 HitPoint()
	{
		RaycastHit[] array = Physics.RaycastAll(this.playerCam.transform.position, this.playerCam.transform.forward, (float)this.whatIsHittable);
		if (array.Length < 1)
		{
			return this.playerCam.transform.position + this.playerCam.transform.forward * 100f;
		}
		if (array.Length > 1)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].transform.gameObject.layer == LayerMask.NameToLayer("Enemy"))
				{
					return array[i].point;
				}
			}
		}
		return array[0].point;
	}

	// Token: 0x06000108 RID: 264 RVA: 0x000081BC File Offset: 0x000063BC
	public float GetRecoil()
	{
		return this.detectWeapons.GetRecoil();
	}

	// Token: 0x06000109 RID: 265 RVA: 0x000081CC File Offset: 0x000063CC
	public void KillPlayer()
	{
		if (Game.Instance.done)
		{
			return;
		}
		CameraShaker.Instance.ShakeOnce(3f * GameState.Instance.cameraShake, 2f, 0.1f, 0.6f);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		UIManger.Instance.DeadUI(true);
		Timer.Instance.Stop();
		this.dead = true;
		this.rb.freezeRotation = false;
		this.playerCollider.material = this.deadMat;
		this.detectWeapons.Throw(Vector3.zero);
		this.paused = false;
		this.ResetSlowmo();
	}

	// Token: 0x0600010A RID: 266 RVA: 0x00008272 File Offset: 0x00006472
	public void Respawn()
	{
		this.detectWeapons.StopUse();
	}

	// Token: 0x0600010B RID: 267 RVA: 0x0000827F File Offset: 0x0000647F
	public void Slowmo(float timescale, float length)
	{
		if (!GameState.Instance.slowmo)
		{
			return;
		}
		base.CancelInvoke("Slowmo");
		this.desiredTimeScale = timescale;
		base.Invoke("ResetSlowmo", length);
		AudioManager.Instance.Play("SlowmoStart");
	}

	// Token: 0x0600010C RID: 268 RVA: 0x000082BB File Offset: 0x000064BB
	private void ResetSlowmo()
	{
		this.desiredTimeScale = 1f;
		AudioManager.Instance.Play("SlowmoEnd");
	}

	// Token: 0x0600010D RID: 269 RVA: 0x000082D7 File Offset: 0x000064D7
	public bool IsCrouching()
	{
		return this.crouching;
	}

	// Token: 0x0600010E RID: 270 RVA: 0x000082DF File Offset: 0x000064DF
	public bool HasGun()
	{
		return this.detectWeapons.HasGun();
	}

	// Token: 0x0600010F RID: 271 RVA: 0x000082EC File Offset: 0x000064EC
	public bool IsDead()
	{
		return this.dead;
	}

	// Token: 0x06000110 RID: 272 RVA: 0x000082F4 File Offset: 0x000064F4
	public Rigidbody GetRb()
	{
		return this.rb;
	}

	// Token: 0x06000111 RID: 273 RVA: 0x000082FC File Offset: 0x000064FC
	private void UpdateActionMeter()
	{
		float target = 0.09f;
		if (this.rb.velocity.magnitude > 15f && (!this.dead || !Game.Instance.playing))
		{
			target = 1f;
		}
		this.actionMeter = Mathf.SmoothDamp(this.actionMeter, target, ref this.vel, 0.7f);
	}

	// Token: 0x06000112 RID: 274 RVA: 0x00008360 File Offset: 0x00006560
	public float GetActionMeter()
	{
		return this.actionMeter * 22000f;
	}

	// Token: 0x040000B0 RID: 176
	public GameObject spawnWeapon;

	// Token: 0x040000B1 RID: 177
	private float sensitivity = 50f;

	// Token: 0x040000B2 RID: 178
	private float sensMultiplier = 1f;

	// Token: 0x040000B3 RID: 179
	private bool dead;

	// Token: 0x040000B4 RID: 180
	public PhysicMaterial deadMat;

	// Token: 0x040000B5 RID: 181
	public Transform playerCam;

	// Token: 0x040000B6 RID: 182
	public Transform orientation;

	// Token: 0x040000B7 RID: 183
	public Transform gun;

	// Token: 0x040000B8 RID: 184
	private float xRotation;

	// Token: 0x040000B9 RID: 185
	public Rigidbody rb;

	// Token: 0x040000BA RID: 186
	private float moveSpeed = 4500f;

	// Token: 0x040000BB RID: 187
	private float walkSpeed = 20f;

	// Token: 0x040000BC RID: 188
	private float runSpeed = 10f;

	// Token: 0x040000BD RID: 189
	public bool grounded;

	// Token: 0x040000BE RID: 190
	public Transform groundChecker;

	// Token: 0x040000BF RID: 191
	public LayerMask whatIsGround;

	// Token: 0x040000C0 RID: 192
	public LayerMask whatIsWallrunnable;

	// Token: 0x040000C1 RID: 193
	private bool readyToJump;

	// Token: 0x040000C2 RID: 194
	private float jumpCooldown = 0.25f;

	// Token: 0x040000C3 RID: 195
	private float jumpForce = 550f;

	// Token: 0x040000C4 RID: 196
	private float x;

	// Token: 0x040000C5 RID: 197
	private float y;

	// Token: 0x040000C6 RID: 198
	private bool jumping;

	// Token: 0x040000C7 RID: 199
	private bool sprinting;

	// Token: 0x040000C8 RID: 200
	private bool crouching;

	// Token: 0x040000C9 RID: 201
	public LineRenderer lr;

	// Token: 0x040000CA RID: 202
	private Vector3 grapplePoint;

	// Token: 0x040000CB RID: 203
	private SpringJoint joint;

	// Token: 0x040000CC RID: 204
	private Vector3 normalVector;

	// Token: 0x040000CD RID: 205
	private Vector3 wallNormalVector;

	// Token: 0x040000CE RID: 206
	private bool wallRunning;

	// Token: 0x040000CF RID: 207
	private Vector3 wallRunPos;

	// Token: 0x040000D0 RID: 208
	private DetectWeapons detectWeapons;

	// Token: 0x040000D1 RID: 209
	public ParticleSystem ps;

	// Token: 0x040000D2 RID: 210
	private ParticleSystem.EmissionModule psEmission;

	// Token: 0x040000D3 RID: 211
	private Collider playerCollider;

	// Token: 0x040000D4 RID: 212
	public bool exploded;

	// Token: 0x040000D6 RID: 214
	public bool paused;

	// Token: 0x040000D7 RID: 215
	public LayerMask whatIsGrabbable;

	// Token: 0x040000D8 RID: 216
	private Rigidbody objectGrabbing;

	// Token: 0x040000D9 RID: 217
	private Vector3 previousLookdir;

	// Token: 0x040000DA RID: 218
	private Vector3 grabPoint;

	// Token: 0x040000DB RID: 219
	private float dragForce = 700000f;

	// Token: 0x040000DC RID: 220
	private SpringJoint grabJoint;

	// Token: 0x040000DD RID: 221
	private LineRenderer grabLr;

	// Token: 0x040000DE RID: 222
	private Vector3 myGrabPoint;

	// Token: 0x040000DF RID: 223
	private Vector3 myHandPoint;

	// Token: 0x040000E0 RID: 224
	private Vector3 endPoint;

	// Token: 0x040000E1 RID: 225
	private Vector3 grappleVel;

	// Token: 0x040000E2 RID: 226
	private float offsetMultiplier;

	// Token: 0x040000E3 RID: 227
	private float offsetVel;

	// Token: 0x040000E4 RID: 228
	private float distance;

	// Token: 0x040000E5 RID: 229
	private float slideSlowdown = 0.2f;

	// Token: 0x040000E6 RID: 230
	private float actualWallRotation;

	// Token: 0x040000E7 RID: 231
	private float wallRotationVel;

	// Token: 0x040000E8 RID: 232
	private float desiredX;

	// Token: 0x040000E9 RID: 233
	private bool cancelling;

	// Token: 0x040000EA RID: 234
	private bool readyToWallrun = true;

	// Token: 0x040000EB RID: 235
	private float wallRunGravity = 1f;

	// Token: 0x040000EC RID: 236
	private float maxSlopeAngle = 35f;

	// Token: 0x040000ED RID: 237
	private float wallRunRotation;

	// Token: 0x040000EE RID: 238
	private bool airborne;

	// Token: 0x040000EF RID: 239
	private int nw;

	// Token: 0x040000F0 RID: 240
	private bool onWall;

	// Token: 0x040000F1 RID: 241
	private bool onGround;

	// Token: 0x040000F2 RID: 242
	private bool surfing;

	// Token: 0x040000F3 RID: 243
	private bool cancellingGrounded;

	// Token: 0x040000F4 RID: 244
	private bool cancellingWall;

	// Token: 0x040000F5 RID: 245
	private bool cancellingSurf;

	// Token: 0x040000F6 RID: 246
	public LayerMask whatIsHittable;

	// Token: 0x040000F7 RID: 247
	private float desiredTimeScale = 1f;

	// Token: 0x040000F8 RID: 248
	private float timeScaleVel;

	// Token: 0x040000F9 RID: 249
	private float actionMeter;

	// Token: 0x040000FA RID: 250
	private float vel;
}
