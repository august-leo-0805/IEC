using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform orientantion;
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")]
    public float maxSlidingTime;
    public float slideForce;
    private float slideTimer;

    public float slideYScale;
    private float startYScale;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        startYScale = playerObj.localScale.y;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // 🌟 핵심 수정: pm.grounded (땅에 있을 때만) 조건 추가로 공중 슬라이딩 완벽 차단!
        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0))
        {
            if (pm.grounded)
            {
                StartSlide();
            }
        }

        if (Input.GetKeyUp(slideKey))
        {
            StopSlide();
        }
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
        {
            SlidingMovement();
        }
    }

    private void StartSlide()
    {
        pm.sliding = true;

        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        rb.AddForce(pm.currentGravity.normalized * 5f, ForceMode.Impulse); // 안전하게 현재 중력 방향으로 당김

        slideTimer = maxSlidingTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientantion.forward * verticalInput + orientantion.right * horizontalInput;

        // sliding normal 
        if (!pm.OnSlope() || rb.linearVelocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
            slideTimer -= Time.deltaTime;
        }
        // sliding down a slope
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }

        if (slideTimer <= 0)
        {
            StopSlide();
        }
    }

    private void StopSlide()
    {
        pm.sliding = false;
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
    }
}

// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// public class Sliding : MonoBehaviour
// {
//     [Header("References")]
//     public Transform orientantion;
//     public Transform playerObj;
//     private Rigidbody rb;
//     private PlayerMovement pm;

//     [Header("Sliding")]
//     public float maxSlidingTime;
//     public float slideForce;
//     private float slideTimer;

//     public float slideYScale;
//     private float startYScale;

//     [Header("Input")]
//     public KeyCode slideKey = KeyCode.LeftControl;
//     private float horizontalInput;
//     private float verticalInput;

//     private void Start()
//     {
//         rb = GetComponent<Rigidbody>();
//         pm = GetComponent<PlayerMovement>();

//         startYScale = playerObj.localScale.y;
//     }

//     private void Update()
//     {
//         horizontalInput = Input.GetAxisRaw("Horizontal");
//         verticalInput = Input.GetAxisRaw("Vertical");

//         if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0))
//         {
//             StartSlide();
//         }

//         if (Input.GetKeyUp(slideKey))
//         {
//             StopSlide();
//         }
//     }

//     private void FixedUpdate()
//     {
//         if (pm.sliding)
//         {
//             SlidingMovement();
//         }
//     }

//     private void StartSlide()
//     {
//         pm.sliding = true;

//         playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
//         rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

//         slideTimer = maxSlidingTime;
//     }

//     private void SlidingMovement()
//     {
//         Vector3 inputDirection = orientantion.forward * verticalInput + orientantion.right * horizontalInput;

//         // sliding normal 
//         if (!pm.OnSlope() || rb.linearVelocity.y > -0.1f)
//         {
//             rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

//             slideTimer -= Time.deltaTime;
//         }

//         // sliding donw a slope
//         else
//         {
//             rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);


//         }

//         if (slideTimer <= 0)
//         {
//             StopSlide();
//         }
//     }

//     private void StopSlide()
//     {
//         pm.sliding = false;

//         playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);

//     }
// }