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
    public ParticleSystem dashEffect;

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
        dashEffect.Play(); // 대시 이펙트 재생
        dashesLeft--; // 🌟 대시 사용 시 횟수 차감
        pm.dashing = true;

        // 대시 중에는 그래플 낙하 감속을 꺼줍니다.

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

        // 2. 방향 계산
        Vector3 direction = GetDirection(forwardT);
        Vector3 forceToApply = direction * dashForce;

        if (disableGravity)
            pm.isGraviting = false; // pm의 중력 변수명 오타 주의 (isGraviting -> isGravity 등 확인 필요)

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
    }

    

