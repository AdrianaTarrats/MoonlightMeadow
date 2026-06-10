using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement via Rigidbody2D, drives walk animations,
/// plays footstep sounds, halves speed when exhausted, and exposes
/// <see cref="SpeedBoost"/> for potion effects.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 2f;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;
    public Vector2 FacingDirection => facingDirection;
    private Animator animator;
    private bool playingFootstep = false;
    public float footstepSpacing = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (PauseController.IsGamePaused || PlayerState.Instance?.CurrentState == State.Dead)
        {
            StopPlayerMovement();
            return;
        }

        float speed = PlayerState.Instance?.CurrentState == State.Exhausted ? moveSpeed * 0.5f : moveSpeed;
        rb.linearVelocity = moveInput * speed;
        animator.SetBool("isWalking", rb.linearVelocity.magnitude > 0);

        if(rb.linearVelocity.magnitude > 0 && !playingFootstep)
        {
            StartFootsteps();
        }
        else if(rb.linearVelocity.magnitude == 0 && playingFootstep)
        {
            StopFootsteps();
        }
    }

    public void StopPlayerMovement()
    {
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        StopMovementAnimations();
        StopFootsteps();
    }

    void StopMovementAnimations()
    {
        animator.SetBool("isWalking", false);
        animator.SetFloat("LastInputX", facingDirection.x);
        animator.SetFloat("LastInputY", facingDirection.y);
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (PauseController.IsGamePaused)
        {
            return;
        }
        Vector2 input = context.ReadValue<Vector2>();

        if (input != Vector2.zero)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                facingDirection = new Vector2(Mathf.Sign(input.x), 0);
            else
                facingDirection = new Vector2(0, Mathf.Sign(input.y));
        }

        moveInput = input;

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);

        if (context.canceled)
        {
            StopMovementAnimations();
        }
    }

    void StartFootsteps()
    {
        playingFootstep = true;
        InvokeRepeating(nameof(PlayFootstep), 0f, footstepSpacing);
    
    }

    void StopFootsteps()
    {
        playingFootstep = false;
        CancelInvoke(nameof(PlayFootstep));
    }

    void PlayFootstep()
    {
        SoundEffectManager.Play("Footstep", true);
    }

    public void SpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        float originalSpeed = moveSpeed;
        moveSpeed *= multiplier;

        yield return new WaitForSeconds(duration);

        moveSpeed = originalSpeed;
    }

}

