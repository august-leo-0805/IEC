using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    public float wallrunSpeed;
    public float swingSpeed;
    public float dashSpeed;
    public float dashSpeedChangeFactor;
    private float speedChangeFactor;

    public Vector3 currentGravity = Vector3.down;
    private float gravityForce = 9.8f * 2;
    public bool isGraviting = true;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;
    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;


    [Header("Interaction")]
    public Interaction interaction;



    [Header("Interaction")]
    public Interaction interaction;


    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode interactionKey = KeyCode.E;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handle")]
    public float maxSlopeAngel;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Camera Effects")]
    public PlayerCam cam;
    public float grappleFov = 95f;

    [Header("Basic References")]
    public Transform orientation;

    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;

    [Header("Animator")]
    public Animator anim;

    public MovementState state;
    public enum MovementState
    {
        freeze, walking, sprinting, wallrunning, crouching, dashing, sliding, grappling, swinging, air,
    }

    public bool swinging;
    public bool dashing;
    public bool sliding;
    public bool wallrunning;
    public bool crouching;
    public bool freeze;
    public bool activeGrapple;
    public bool grappling;

    public bool isTransitioning = false;
    public float lastGravityChangeTime = -1f;

    private Quaternion fixedWallRotation;
    private Coroutine speedCoroutine;
    private Coroutine rotationCoroutine;
    private MovementState lastState;
    private bool keepMomentum;

    [Header("Fall Speed Limits")]
    public bool isDroppingFromGrapple = false;
    [Tooltip("일반적으로 높은 곳에서 떨어질 때의 최고 속도 (뚝뚝 끊김 방지용)")]
    public float maxFallSpeed = 25f;
    [Tooltip("그래플링이 끝난 직후 떨어질 때의 최고 속도 (더 천천히 떨어지도록 설정)")]
    public float maxGrappleFallSpeed = 10f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false;

        readyToJump = true;
        startYScale = transform.localScale.y;

        // 자식 오브젝트에서 애니메이터 컴포넌트 획득
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {

        // 🌟 수정됨: 중력 변환 중에는 모든 움직임 및 입력 억제!
        if (isTransitioning) return;

        //ground check

        if (isTransitioning) return;


        grounded = Physics.Raycast(transform.position, currentGravity.normalized, playerHeight * 0.5f + 0.2f, whatIsGround);

        if (grounded)
        {
            rb.linearDamping = groundDrag;
            isDroppingFromGrapple = false;

            Dashing dashingScript = GetComponent<Dashing>();
            if (dashingScript != null) dashingScript.ResetDashCount();
        }

        else
        {
            // 🌟 Damping을 원상 복구(0)하여, 대시나 점프 시 앞으로 날아가는 관성이 방해받지 않도록 합니다.
            rb.linearDamping = 0f;


        MyInput();
        SpeedControl();
        StateHandler();
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();

        if (!dashing && !wallrunning && !freeze)
        {
            rb.AddForce(currentGravity * gravityForce, ForceMode.Acceleration);
        }
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButton("Jump") && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // 🌟 공중에서 컨트롤(Crouch)을 눌렀을 때 내리꽂히는 현상 차단
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            if (grounded) rb.AddForce(currentGravity.normalized * 5f, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            if (grounded) rb.AddForce(currentGravity.normalized * 5f, ForceMode.Impulse);
        }


        // start interaction

        if (Input.GetKeyDown(interactionKey) && interaction != null)
        {
            interaction.InteractWithTarget();
        }

    }

    private void StateHandler()
    {
        if (swinging) state = MovementState.swinging;
        else if (grappling) state = MovementState.grappling;
        else if (freeze) { state = MovementState.freeze; moveSpeed = 0; rb.linearVelocity = Vector3.zero; }
        else if (dashing) { state = MovementState.dashing; desiredMoveSpeed = dashSpeed; speedChangeFactor = dashSpeedChangeFactor; }
        else if (wallrunning) { state = MovementState.wallrunning; desiredMoveSpeed = wallrunSpeed; }
        else if (sliding)
        {
            state = MovementState.sliding;
            if (OnSlope() && rb.linearVelocity.y < 0.1f) desiredMoveSpeed = slideSpeed;
            else desiredMoveSpeed = sprintSpeed;
        }
        else if (Input.GetKey(crouchKey)) { state = MovementState.crouching; desiredMoveSpeed = crouchSpeed; }
        else if (grounded && Input.GetKey(sprintKey)) { state = MovementState.sprinting; desiredMoveSpeed = sprintSpeed; }
        else if (grounded) { state = MovementState.walking; desiredMoveSpeed = walkSpeed; }
        else state = MovementState.air;

        // 🌟 [애니메이션 실행 제어부 삽입]
        AnimatePlayer();

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        if (lastState == MovementState.dashing) keepMomentum = true;

        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                if (speedCoroutine != null) StopCoroutine(speedCoroutine);
                speedCoroutine = StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                if (speedCoroutine != null) StopCoroutine(speedCoroutine);
                moveSpeed = desiredMoveSpeed;
            }
        }

        lastState = state; // 상태 추적용 백업
    }

    /// <summary>
    /// 결정된 MovementState에 따라 애니메이터 파라미터 셋팅
    /// </summary>
    private void AnimatePlayer()
    {
        if (anim == null) return;

        // 현재 수평 속도 계산 (블렌드 트리 구동용 파라미터가 있을 경우 사용)
        Vector3 flatVel = Vector3.ProjectOnPlane(rb.linearVelocity, currentGravity);
        float currentSpeed = flatVel.magnitude;
        anim.SetFloat("MoveSpeed", currentSpeed);

        // 정수형(Int) 파라미터 1개로 상태 변화를 제어하는 방식 (추천: 애니메이터 내에서 정수 조건 전이 규칙 생성)
        anim.SetInteger("MovementState", (int)state);

        // 개별 개별 Bool 파라미터 형태로 제어하는 방식 (CyberSoldier 세팅 방식에 맞춰 택일)
        anim.SetBool("isGrounded", grounded);
        anim.SetBool("isWalking", state == MovementState.walking);
        anim.SetBool("isSprinting", state == MovementState.sprinting);
        anim.SetBool("isWallrunning", state == MovementState.wallrunning);
        anim.SetBool("isSliding", state == MovementState.sliding);
        anim.SetBool("isCrouching", state == MovementState.crouching);
        anim.SetBool("isDashing", state == MovementState.dashing);
        anim.SetBool("isAir", state == MovementState.air);
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;
        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            time += Time.deltaTime * boostFactor;
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
        speedChangeFactor = 1f;
        keepMomentum = false;
    }

    private void MovePlayer()
    {
        if (state == MovementState.dashing || state == MovementState.grappling || state == MovementState.swinging) return;

        Vector3 camForward = cam.camHolder.forward;
        Vector3 camRight = cam.camHolder.right;

        Vector3 forwardOnWall = Vector3.ProjectOnPlane(camForward, currentGravity).normalized;
        Vector3 rightOnWall = Vector3.ProjectOnPlane(camRight, currentGravity).normalized;

        moveDirection = (forwardOnWall * verticalInput) + (rightOnWall * horizontalInput);

        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);
            if (rb.linearVelocity.y > 0)
                rb.AddForce(currentGravity.normalized * -80f, ForceMode.Force);
        }
        else if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        isGraviting = !OnSlope() && !dashing;
    }

    private void SpeedControl()
    {
        if (activeGrapple || dashing || swinging) return;

        float targetMaxSpeed = moveSpeed;

        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > targetMaxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * targetMaxSpeed;
        }
        else
        {
            Vector3 flatVel = Vector3.ProjectOnPlane(rb.linearVelocity, currentGravity);

            if (flatVel.magnitude > targetMaxSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * targetMaxSpeed;
                Vector3 gravityVel = rb.linearVelocity - flatVel;
                rb.linearVelocity = limitedVel + gravityVel;
            }

            Vector3 gravityDir = currentGravity.normalized;
            float fallSpeed = Vector3.Dot(rb.linearVelocity, gravityDir);
            float limitFallSpeed = isDroppingFromGrapple ? maxGrappleFallSpeed : maxFallSpeed;

            if (fallSpeed > limitFallSpeed)
            {
                Vector3 nonFallVel = rb.linearVelocity - (gravityDir * fallSpeed);
                rb.linearVelocity = nonFallVel + (gravityDir * limitFallSpeed);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        Vector3 flatVel = Vector3.ProjectOnPlane(rb.linearVelocity, currentGravity);
        rb.linearVelocity = flatVel;
        rb.AddForce(-currentGravity * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool enableMovementOnNextTouch;
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        SetVelocity();
        Invoke(nameof(ResetRestrictions), 3f);
    }

    private Vector3 velocityToSet;
    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.linearVelocity = velocityToSet;
        cam.DoFov(grappleFov);
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
        cam.DoFov(85f);
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, currentGravity.normalized, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(-currentGravity.normalized, slopeHit.normal);
            return angle < maxSlopeAngel && angle != 0;
        }
        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        Vector3 gravityUp = -currentGravity.normalized;
        float gravity = -gravityForce;
        float displacementY = Vector3.Dot(endPoint - startPoint, gravityUp);
        Vector3 displacementXZ = Vector3.ProjectOnPlane(endPoint - startPoint, gravityUp);

        if (trajectoryHeight < 0.1f) trajectoryHeight = 0.1f;
        if (trajectoryHeight < displacementY + 1.0f) trajectoryHeight = displacementY + 1.0f;

        float time = Mathf.Sqrt(Mathf.Abs(-2 * trajectoryHeight / gravity))
                   + Mathf.Sqrt(Mathf.Abs(2 * (displacementY - trajectoryHeight) / gravity));

        if (time < 0.01f) time = 0.01f;

        Vector3 velocityY = gravityUp * Mathf.Sqrt(Mathf.Abs(-2 * gravity * trajectoryHeight));
        Vector3 velocityXZ = displacementXZ / time;

        return velocityXZ + velocityY;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();
            GetComponent<Grappling>().StopGrapple();
        }

        CheckGravityWall(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckGravityWall(collision);
    }

    private void CheckGravityWall(Collision collision)
    {
        if (collision.gameObject.CompareTag("GravityWall"))
        {
            if (isTransitioning) return;
            if (Time.time - lastGravityChangeTime < 0.5f) return;

            Vector3 wallNormal = collision.contacts[0].normal;
            Vector3 contactPoint = collision.contacts[0].point;

            if (currentGravity != -wallNormal)
            {
                currentGravity = -wallNormal;
                lastGravityChangeTime = Time.time;
                rb.linearVelocity = Vector3.zero;

                if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
                rotationCoroutine = StartCoroutine(RotatePlayerToSurface(wallNormal, contactPoint));
            }
        }
    }

    private IEnumerator RotatePlayerToSurface(Vector3 targetUp, Vector3 contactPoint)
    {
        isTransitioning = true;
        rb.isKinematic = true;

        wallrunning = false;
        cam.DoFov(80f);
        cam.DoTilt(0f);

        cam.enabled = false;

        Behaviour cineBrain = cam.GetComponent("CinemachineBrain") as Behaviour;
        if (cineBrain != null) cineBrain.enabled = false;

        Quaternion startBodyRot = transform.rotation;
        Vector3 startPosition = transform.position;
        Quaternion startCamRot = cam.camHolder.rotation;

        float currentDist = Vector3.Dot(transform.position - contactPoint, targetUp);
        float desiredDist = playerHeight * 0.5f + 0.1f;
        Vector3 targetPosition = transform.position + targetUp * (desiredDist - currentDist);

        Vector3 currentForward = cam.camHolder.forward;
        Vector3 newForward = Vector3.ProjectOnPlane(currentForward, targetUp).normalized;
        if (newForward.sqrMagnitude < 0.01f)
            newForward = Vector3.ProjectOnPlane(transform.up, targetUp).normalized;

        fixedWallRotation = Quaternion.LookRotation(newForward, targetUp);

        Quaternion targetCamRot;
        if (Mathf.Abs(Vector3.Dot(currentForward, targetUp)) > 0.99f)
        {
            targetCamRot = fixedWallRotation;
        }
        else
        {
            targetCamRot = Quaternion.LookRotation(currentForward, targetUp);
        }

        float time = 0f;
        float duration = 0.35f;

        while (time < 1f)
        {
            time += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, time);
            transform.rotation = Quaternion.Slerp(startBodyRot, fixedWallRotation, time);
            cam.camHolder.rotation = Quaternion.Slerp(startCamRot, targetCamRot, time);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = fixedWallRotation;
        cam.camHolder.rotation = targetCamRot;

        if (targetUp == Vector3.up)
        {
            Vector3 finalCamForward = targetCamRot * Vector3.forward;
            float finalYaw = Mathf.Atan2(finalCamForward.x, finalCamForward.z) * Mathf.Rad2Deg;
            float finalPitch = -Mathf.Asin(finalCamForward.y) * Mathf.Rad2Deg;

            cam.yRotation = finalYaw / 2f;
            cam.xRotation = finalPitch;
        }
        else
        {
            cam.xRotation = 0f;
            cam.yRotation = 0f;
        }

        if (cineBrain != null) cineBrain.enabled = true;
        cam.enabled = true;
        rb.isKinematic = false;
        isTransitioning = false;
    }

    private void LateUpdate()
    {
        if (!isTransitioning && currentGravity != Vector3.down)
        {
            transform.rotation = fixedWallRotation;
        }
    }
}