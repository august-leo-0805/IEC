using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dashing : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerCam;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Dashing")]
    public float dashForce;
    public float dashDuration;

    [Header("Dash Limits")]
    public int maxDashes = 1;
    private int dashesLeft;

    [Header("CameraEffects")]
    public PlayerCam cam;
    public float dashFov;

    [Header("Settings")]
    public bool useCameraForward = true;
    public bool allowAllDirections = true;
    public bool disableGravity = false;
    public bool resetVel = true;

    [Header("Momentum Settings")]
    [Range(0f, 1f)]
    public float momentumRetention = 0.2f;

    [Header("Cooldown")]
    public float dashCd;
    private float dashCdTimer;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        dashesLeft = maxDashes;
    }

    private void Update()
    {
        if (pm != null && pm.isTransitioning) return;

        if (Input.GetKeyDown(dashKey) && dashesLeft > 0 && dashCdTimer <= 0)
            Dash();

        if (dashCdTimer > 0)
            dashCdTimer -= Time.deltaTime;
    }

    private void Dash()
    {
        if (dashCdTimer > 0) return;
        else dashCdTimer = dashCd;

        dashesLeft--;
        pm.dashing = true;

        // 🌟 수정 1: 그래플 상태 강제 리셋 (무한 가속도 우주 발사 버그 완벽 차단!)
        // 속도 제한(SpeedControl)을 마비시키던 그래플 상태를 대시 시점에 강제로 해제합니다.
        if (pm.activeGrapple || pm.grappling)
        {
            pm.activeGrapple = false;
            GetComponent<Grappling>().StopGrapple();
        }

        pm.isDroppingFromGrapple = false;

        cam.DoFov(dashFov);

        Transform forwardT;

        if (useCameraForward)
            forwardT = playerCam;
        else
            forwardT = orientation;

        Vector3 direction = GetDirection(forwardT);
        Vector3 forceToApply = direction * dashForce;

        if (disableGravity)
            pm.isGraviting = false;

        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);
        Invoke(nameof(ResetDash), dashDuration);
    }

    private Vector3 delayedForceToApply;
    private void DelayedDashForce()
    {
        if (pm.state == PlayerMovement.MovementState.air || pm.state == PlayerMovement.MovementState.grappling)
        {
            rb.linearVelocity = rb.linearVelocity * momentumRetention;
        }
        else if (resetVel)
        {
            rb.linearVelocity = Vector3.zero;
        }

        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        pm.dashing = false;
        cam.DoFov(80f);

        if (disableGravity)
            pm.isGraviting = true;
    }

    private Vector3 GetDirection(Transform forwardT)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 forwardOnWall = Vector3.ProjectOnPlane(forwardT.forward, pm.currentGravity).normalized;
        Vector3 rightOnWall = Vector3.ProjectOnPlane(forwardT.right, pm.currentGravity).normalized;

        Vector3 direction = new Vector3();

        if (allowAllDirections)
            direction = (forwardOnWall * verticalInput) + (rightOnWall * horizontalInput);
        else
            direction = forwardOnWall;

        if (verticalInput == 0 && horizontalInput == 0)
            direction = forwardOnWall;

        // 🚨 수정 2: direction.y = 0; 삭제!!
        // 이 한 줄 때문에 벽에서 W(앞)를 누르면 Y축 상승이 잘려나가고 옆으로만 튕겨 나갔던 것입니다!

        return direction.normalized;
    }

    public void ResetDashCount()
    {
        dashesLeft = maxDashes;
    }
}