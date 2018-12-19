using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour {

    public float maxForwardSpeed = 8f;        // How fast Ellen can run.
    public float gravity = 20f;               // How fast Ellen accelerates downwards when airborne.
    public float jumpSpeed = 10f;             // How fast Ellen takes off when jumping.
    public CinemachineFreeLook current;            // Reference used to determine the camera's direction.
    public float minTurnSpeed = 400f;         // How fast Ellen turns when moving at maximum speed.
    public float maxTurnSpeed = 1200f;        // How fast Ellen turns when stationary.

    protected bool m_ReadyToJump;                  // Whether or not the input state and Ellen are correct to allow jumping.
    protected bool m_IsAnimatorTransitioning;
    protected bool m_PreviousIsAnimatorTransitioning;
    protected AnimatorStateInfo m_NextStateInfo;
    protected AnimatorStateInfo m_PreviousNextStateInfo;
    protected AnimatorStateInfo m_CurrentStateInfo;    // Information about the base layer of the animator cached.
    protected float m_DesiredForwardSpeed;         // How fast Ellen aims be going along the ground based on input.
    protected PlayerInput m_Input;                 // Reference used to determine how Ellen should move.
    protected float m_ForwardSpeed;                // How fast Ellen is currently going along the ground.
    protected float m_VerticalSpeed;               // How fast Ellen is currently moving up or down.
    protected Material m_CurrentWalkingSurface;    // Reference used to make decisions about audio.
    protected bool m_IsGrounded = true;            // Whether or not Ellen is currently standing on the ground.
    protected CharacterController m_CharCtrl;      // 参考角色的移动.
    protected Animator m_Animator;                 // Reference used to make decisions based on Ellen's current animation and to set parameters.
    protected AnimatorStateInfo m_PreviousCurrentStateInfo;    // Information about the base layer of the animator from last frame.
    protected bool m_InCombo;                      // Whether Ellen is currently in the middle of her melee combo.
    protected float m_AngleDiff;                   // Angle in degrees between Ellen's current rotation and her target rotation.
    protected Quaternion m_TargetRotation;         // What rotation Ellen is aiming to have based on input.


    // These constants are used to ensure Ellen moves and behaves properly.
    // It is advised you don't change them without fully understanding what they do in code.
    const float k_AirborneTurnSpeedProportion = 5.4f;
    const float k_GroundedRayDistance = 1f;
    const float k_JumpAbortSpeed = 10f;
    const float k_MinEnemyDotCoeff = 0.2f;
    const float k_InverseOneEighty = 1f / 180f;
    const float k_StickingGravityProportion = 0.3f;
    const float k_GroundAcceleration = 20f;
    const float k_GroundDeceleration = 25f;

    readonly int m_HashAirborneVerticalSpeed = Animator.StringToHash("AirborneVerticalSpeed");
    readonly int m_HashGrounded = Animator.StringToHash("Grounded");
    readonly int m_HashForwardSpeed = Animator.StringToHash("ForwardSpeed");
    readonly int m_HashStateTime = Animator.StringToHash("StateTime");
    readonly int m_HashAngleDeltaRad = Animator.StringToHash("AngleDeltaRad");

    // Tags
    readonly int m_HashBlockInput = Animator.StringToHash("BlockInput");

    protected bool IsMoveInput
    {
        get { return !Mathf.Approximately(m_Input.MoveInput.sqrMagnitude, 0f); }
    }

    private void Awake()
    {
        m_Input = GetComponent<PlayerInput>();
        m_Animator = GetComponent<Animator>();
        m_CharCtrl = gameObject.GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        //CacheAnimatorState();

        //m_Animator.SetFloat(m_HashStateTime, Mathf.Repeat(m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f));

        CalculateForwardMovement();
        //CalculateVerticalMovement();

        SetTargetRotation();

        if (IsMoveInput)
            UpdateOrientation();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            m_Animator.SetTrigger("Attack");
        }

    }

    // Called after the animator state has been cached to determine whether this script should block user input.
    void UpdateInputBlocking()
    {
        bool inputBlocked = m_CurrentStateInfo.tagHash == m_HashBlockInput && !m_IsAnimatorTransitioning;
        inputBlocked |= m_NextStateInfo.tagHash == m_HashBlockInput;
        m_Input.playerControllerInputBlocked = inputBlocked;
    }

    // Called each physics step to set the rotation Ellen is aiming to have.
    void SetTargetRotation()
    {
        // Create three variables, move input local to the player, flattened forward direction of the camera and a local target rotation.
        Vector2 moveInput = m_Input.MoveInput;
        Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        Vector3 forward = Quaternion.Euler(0f, current.m_XAxis.Value, 0f) * Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion targetRotation;

        // If the local movement direction is the opposite of forward then the target rotation should be towards the camera.
        if (Mathf.Approximately(Vector3.Dot(localMovementDirection, Vector3.forward), -1.0f))
        {
            targetRotation = Quaternion.LookRotation(-forward);
        }
        else
        {
            // Otherwise the rotation should be the offset of the input from the camera's forward.
            Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
            targetRotation = Quaternion.LookRotation(cameraToInputOffset * forward);
        }

        // The desired forward direction of Ellen.
        Vector3 resultingForward = targetRotation * Vector3.forward;

        //// If attacking try to orient to close enemies.
        //if (m_InAttack)
        //{
        //    // Find all the enemies in the local area.
        //    Vector3 centre = transform.position + transform.forward * 2.0f + transform.up;
        //    Vector3 halfExtents = new Vector3(3.0f, 1.0f, 2.0f);
        //    int layerMask = 1 << LayerMask.NameToLayer("Enemy");
        //    int count = Physics.OverlapBoxNonAlloc(centre, halfExtents, m_OverlapResult, targetRotation, layerMask);

        //    // Go through all the enemies in the local area...
        //    float closestDot = 0.0f;
        //    Vector3 closestForward = Vector3.zero;
        //    int closest = -1;

        //    for (int i = 0; i < count; ++i)
        //    {
        //        // ... and for each get a vector from the player to the enemy.
        //        Vector3 playerToEnemy = m_OverlapResult[i].transform.position - transform.position;
        //        playerToEnemy.y = 0;
        //        playerToEnemy.Normalize();

        //        // Find the dot product between the direction the player wants to go and the direction to the enemy.
        //        // This will be larger the closer to Ellen's desired direction the direction to the enemy is.
        //        float d = Vector3.Dot(resultingForward, playerToEnemy);

        //        // Store the closest enemy.
        //        if (d > k_MinEnemyDotCoeff && d > closestDot)
        //        {
        //            closestForward = playerToEnemy;
        //            closestDot = d;
        //            closest = i;
        //        }
        //    }

        //    // If there is a close enemy...
        //    if (closest != -1)
        //    {
        //        // The desired forward is the direction to the closest enemy.
        //        resultingForward = closestForward;

        //        // We also directly set the rotation, as we want snappy fight and orientation isn't updated in the UpdateOrientation function during an atatck.
        //        transform.rotation = Quaternion.LookRotation(resultingForward);
        //    }
        //}

        // Find the difference between the current rotation of the player and the desired rotation of the player in radians.
        float angleCurrent = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(resultingForward.x, resultingForward.z) * Mathf.Rad2Deg;

        m_AngleDiff = Mathf.DeltaAngle(angleCurrent, targetAngle);
        m_TargetRotation = targetRotation;
    }

    // Called each physics step after SetTargetRotation if there is move input and Ellen is in the correct animator state according to IsOrientationUpdated.
    void UpdateOrientation()
    {
        m_Animator.SetFloat(m_HashAngleDeltaRad, m_AngleDiff * Mathf.Deg2Rad);

        Vector3 localInput = new Vector3(m_Input.MoveInput.x, 0f, m_Input.MoveInput.y);
        float groundedTurnSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, m_ForwardSpeed / m_DesiredForwardSpeed);
        float actualTurnSpeed = m_IsGrounded ? groundedTurnSpeed : Vector3.Angle(transform.forward, localInput) * k_InverseOneEighty * k_AirborneTurnSpeedProportion * groundedTurnSpeed;
        m_TargetRotation = Quaternion.RotateTowards(transform.rotation, m_TargetRotation, actualTurnSpeed * Time.deltaTime);

        transform.rotation = m_TargetRotation;
    }

    // Called at the start of FixedUpdate to record the current state of the base layer of the animator.
    void CacheAnimatorState()
    {
        m_PreviousCurrentStateInfo = m_CurrentStateInfo;
        m_PreviousNextStateInfo = m_NextStateInfo;
        m_PreviousIsAnimatorTransitioning = m_IsAnimatorTransitioning;

        m_CurrentStateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);
        m_NextStateInfo = m_Animator.GetNextAnimatorStateInfo(0);
        m_IsAnimatorTransitioning = m_Animator.IsInTransition(0);
    }

    // Called each physics step.
    void CalculateForwardMovement()
    {
        // Cache the move input and cap it's magnitude at 1.
        // moveInput.sqrMagnitude: 返回向量的平方长度.
        Vector2 moveInput = m_Input.MoveInput;
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        // Calculate the speed intended by input.
        m_DesiredForwardSpeed = moveInput.magnitude * maxForwardSpeed;

        // Determine change to speed based on whether there is currently any move input.
        float acceleration = IsMoveInput ? k_GroundAcceleration : k_GroundDeceleration;

        // Adjust the forward speed towards the desired speed.
        m_ForwardSpeed = Mathf.MoveTowards(m_ForwardSpeed, m_DesiredForwardSpeed, acceleration * Time.deltaTime);

        // Set the animator parameter to control what animation is being played.
        m_Animator.SetFloat(m_HashForwardSpeed, m_ForwardSpeed);
    }

    //// Called each physics step.
    //void CalculateVerticalMovement()
    //{
    //    // If jump is not currently held and Ellen is on the ground then she is ready to jump.
    //    if (!m_Input.JumpInput && m_IsGrounded)
    //        m_ReadyToJump = true;

    //    if (m_IsGrounded)
    //    {
    //        // When grounded we apply a slight negative vertical speed to make Ellen "stick" to the ground.
    //        m_VerticalSpeed = -gravity * k_StickingGravityProportion;

    //        // If jump is held, Ellen is ready to jump and not currently in the middle of a melee combo...
    //        if (m_Input.JumpInput && m_ReadyToJump && !m_InCombo)
    //        {
    //            // ... then override the previously set vertical speed and make sure she cannot jump again.
    //            m_VerticalSpeed = jumpSpeed;
    //            m_IsGrounded = false;
    //            m_ReadyToJump = false;
    //        }
    //    }
    //    else
    //    {
    //        // If Ellen is airborne, the jump button is not held and Ellen is currently moving upwards...
    //        if (!m_Input.JumpInput && m_VerticalSpeed > 0.0f)
    //        {
    //            // ... decrease Ellen's vertical speed.
    //            // This is what causes holding jump to jump higher that tapping jump.
    //            m_VerticalSpeed -= k_JumpAbortSpeed * Time.deltaTime;
    //        }

    //        // If a jump is approximately peaking, make it absolute.
    //        if (Mathf.Approximately(m_VerticalSpeed, 0f))
    //        {
    //            m_VerticalSpeed = 0f;
    //        }

    //        // If Ellen is airborne, apply gravity.
    //        m_VerticalSpeed -= gravity * Time.deltaTime;
    //    }
    //}

    // Called each physics step (so long as the Animator component is set to Animate Physics) after FixedUpdate to override root motion.
    void OnAnimatorMove()
    {
        Vector3 movement;

        // If Ellen is on the ground...
        if (m_IsGrounded)
        {
            // ... raycast into the ground...
            RaycastHit hit;
            Ray ray = new Ray(transform.position + Vector3.up * k_GroundedRayDistance * 0.5f, -Vector3.up);
            if (Physics.Raycast(ray, out hit, k_GroundedRayDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                // ... and get the movement of the root motion rotated to lie along the plane of the ground.
                movement = Vector3.ProjectOnPlane(m_Animator.deltaPosition, hit.normal);

                // Also store the current walking surface so the correct audio is played.
                Renderer groundRenderer = hit.collider.GetComponentInChildren<Renderer>();
                m_CurrentWalkingSurface = groundRenderer ? groundRenderer.sharedMaterial : null;
            }
            else
            {
                // If no ground is hit just get the movement as the root motion.
                // Theoretically this should rarely happen as when grounded the ray should always hit.
                movement = m_Animator.deltaPosition;
                m_CurrentWalkingSurface = null;
            }
        }
        else
        {
            // If not grounded the movement is just in the forward direction.
            movement = m_ForwardSpeed * transform.forward * Time.deltaTime;
        }

        // Rotate the transform of the character controller by the animation's root rotation.
        m_CharCtrl.transform.rotation *= m_Animator.deltaRotation;

        // Add to the movement with the calculated vertical speed.
        movement += m_VerticalSpeed * Vector3.up * Time.deltaTime;

        // Move the character controller.
        m_CharCtrl.Move(movement);

        // After the movement store whether or not the character controller is grounded.
        m_IsGrounded = m_CharCtrl.isGrounded;

        // If Ellen is not on the ground then send the vertical speed to the animator.
        // This is so the vertical speed is kept when landing so the correct landing animation is played.
        //if (!m_IsGrounded)
        //    m_Animator.SetFloat(m_HashAirborneVerticalSpeed, m_VerticalSpeed);

        // Send whether or not Ellen is on the ground to the animator.
        //m_Animator.SetBool(m_HashGrounded, m_IsGrounded);
    }
}
