using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Movement : MonoBehaviour
{
    private Collision coll;
    [HideInInspector]
    public Rigidbody2D rb;
    private AnimationScript anim;
    // Zac - Added in audiosources unity to pull from
    public AudioSource audioSource;
    public AudioClip clip;
    public AudioClip clip2;
    public AudioClip clip3;
    public float volume;
    //Elizabeth - Get sprite renderer to change color when dash is used
    public SpriteRenderer renderer;
    // Liam - Get Virtual Cams to shake when ground pounding
    public GameObject Vcam1;
    public GameObject Vcam2;
    public GameObject Vcam3;
    public GameObject Vcam4;

    [Space]
    [Header("Stats")]
    public float speed = 10;
    public float jumpForce = 12;
    public float slideSpeed = 5;
    public float wallJumpLerp = 10;
    public float dashSpeed = 20;
    // Liam - Boolean for our added mechanics
    public bool Modified = false;
    public bool ModifiedD = false;
    // Liam - Ground shake fall velocity threshold
    public float fallspeed = 21;
    // Tai Wen - Time given for players to jump after falling off a platform
    public float coyoteTime = 0f;
    // Tai Wen - Time given for players to jump before touching the ground
    public float jumpBufferTime = 0f;
    // Tai Wen - Strength given to the edge nudge
    public float edgeNudgeStrength = 0f;
    // Tai Wen - Time given for the edge nudge to be active
    public float edgeNudgeTime = 0f;
    //Elizabeth - variables for different control schemes when modified/unmodified
    public String climbButton;
    public String dashButton;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool wallGrab;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;
    // Liam - Boolean for whether you can do extra jump or not
    public bool doubleJump;

    [Space]

    private bool groundTouch;
    private bool hasDashed;
    // Liam - variable getting max fall speed
    private float maxFall = 0;
    // Tai Wen - counter for coyote time
    private float coyoteTimeCounter;
    // Tai Wen - counter for jump buffer time
    private float jumpBufferCounter;
    // Tai Wen - counter for edge nudge time
    private float edgeNudgeCounter;


    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem wallJumpParticle;
    public ParticleSystem slideParticle;

    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<AnimationScript>();
        // Zac - GetComponent to actually get the audio sources
        audioSource = GetComponent<AudioSource>();

        Base();
    }

    // Button triggers the stats to go to base version &
    // disables extra added mechanics
    public void Base()
    {
        speed = 10;
        jumpForce = 12;
        slideSpeed = 1;
        wallJumpLerp = 5;
        dashSpeed = 40;
        Modified = false;
        ModifiedD = false;
        fallspeed = 21;
        coyoteTime = 0f;
        jumpBufferTime = 0f;
        edgeNudgeStrength = 0f;
        edgeNudgeTime = 0f;
    }

    // Button triggers the stats to go to base version &
    // enables extra added mechanics
    public void Polished()
    {
        speed = 10;
        jumpForce = 14;
        slideSpeed = 1;
        wallJumpLerp = 5;
        dashSpeed = 50;
        Modified = true;
        ModifiedD = false;
        fallspeed = 21;
        coyoteTime = 0.05f;
        jumpBufferTime = 0.3f;
        edgeNudgeStrength = 7f;
        edgeNudgeTime = 0.05f;
    }

    // Button triggers the stats to go to modified version &
    // enables extra added mechanics (WIP)
    public void Distinct()
    {
        speed = 12;
        jumpForce = 18;
        slideSpeed = 5;
        wallJumpLerp = 5;
        dashSpeed = 70;
        ModifiedD = true;
        Modified = false;
        fallspeed = 18;
        coyoteTime = 0.1f;
        jumpBufferTime = 0.3f;
        edgeNudgeStrength = 10f;
        edgeNudgeTime = 0.01f;
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        float xRaw = Input.GetAxisRaw("Horizontal");
        float yRaw = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(x, y);

        Walk(dir);
        anim.SetHorizontalMovement(x, y, rb.velocity.y);

        //Elizabeth - if dash has been used, turn character red
        if((Modified || ModifiedD) && hasDashed) {
            renderer.color = Color.cyan;
        }
        else {
            renderer.color = Color.white;
        } 

        //Elizabeth - Changes control scheme when modified or not
        if(Modified || ModifiedD) {
            climbButton = "Fire1";
            dashButton = "Fire3";
        }
        else{
            climbButton = "Fire5";
            dashButton = "Fire4";

        }

        // Tai Wen - updates coyote time counter
        if (coll.onGround)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Tai Wen - updates jump buffer counter
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Tai Wen - resets coyote counter if player jumps
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
            coyoteTimeCounter = -1f;

        // Liam - updates maxFall variable to have highest downward velocity
        if (rb.velocity.y < maxFall)
        {
            maxFall = rb.velocity.y;
        }

        if (coll.onWall && Input.GetButton(climbButton) && canMove) //Elizabeth - using dashButton/climbButton instead of string name, to allow control changes
        {
            if (side != coll.wallSide)
                anim.Flip(side * -1);
            wallGrab = true;
            wallSlide = false;
            //Noah - Wall resets Dash, make it so dashing up and down walls is slower
            if (ModifiedD)
            {
                dashSpeed = 40;
                hasDashed = false;
            }
        }

        // Tai Wen - Set counter when player climbs off a wall 
        if (!coll.onWall && wallGrab == true && rb.velocity.y > 0 && (Modified || ModifiedD))
        {
            edgeNudgeCounter = edgeNudgeTime;
        }

        // Tai Wen - While counter is active, nudge player upwards towards the wall
        if (edgeNudgeCounter > 0 && (Modified || ModifiedD))
        {
            edgeNudgeCounter -= Time.deltaTime;
            rb.velocity = new Vector2(-coll.wallSide * edgeNudgeStrength, edgeNudgeStrength);
            if (side != coll.wallSide)
                anim.Flip(-coll.wallSide);
        }

        if (Input.GetButtonUp(climbButton) || !coll.onWall || !canMove)
        {
            wallGrab = false;
            wallSlide = false;
            //Noah - Set Dash speed back to normal when not on wall
            if (Modified)
            {
                dashSpeed = 50;
            }
            if (ModifiedD)
            {
                dashSpeed = 70;
            }
        }

        if (coll.onGround && !isDashing)
        {
            wallJumped = false;
            GetComponent<BetterJumping>().enabled = true;
        }

        if (wallGrab && !isDashing)
        {
            rb.gravityScale = 0;
            if (x > .2f || x < -.2f)
                rb.velocity = new Vector2(rb.velocity.x, 0);

            float speedModifier = y > 0 ? .5f : 1;

            rb.velocity = new Vector2(rb.velocity.x, y * (speed * (speedModifier)));

            if(ModifiedD){
                rb.velocity = new Vector2(rb.velocity.x, y * (speed * (speedModifier+.25f)));
            }
        }
        else
        {
            rb.gravityScale = 3;
        }

        if (coll.onWall && !coll.onGround)
        {
            if (x != 0 && !wallGrab)
            {
                wallSlide = true;
                // Liam - reset doubleJump boolean to allow double jump again
                doubleJump = true;
                WallSlide();
            }
        }

        if (!coll.onWall || coll.onGround)
            wallSlide = false;

        // Tai Wen - changed from checking for jump input to checking for jump buffer
        if (jumpBufferCounter >= 0f)
        {
            anim.SetTrigger("jump");

            // Tai Wen - changed from checking for ground collision to checking for coyote counter
            if (coyoteTimeCounter >= 0f)
            {
                Jump(Vector2.up, false);
                jumpBufferCounter = -1f;   // Tai Wen - resets jump buffer counter
            }
            // Liam - If in air, Modified enabled, and has charge of jump, do extra jump
            /*else if (!coll.onGround && !coll.onWall && doubleJump && Modified)
            {
                Jump(Vector2.up, false);
                doubleJump = false;
                jumpBufferCounter = -1f;  // Tai Wen - resets jump buffer counter
            }*/
            if (coll.onWall && !coll.onGround)
            {
                WallJump();
                jumpBufferCounter = -1f;  // Tai Wen - resets jump buffer counter
            }
        }

        if (Input.GetButtonDown(dashButton) && !hasDashed)
        {
            //Noah and Liam - Dash while not moving
            if ((Modified || ModifiedD) && xRaw == 0 && yRaw == 0)
            {
                Dash(40 * side, 4);
            }
            else if (xRaw != 0 || yRaw != 0)
            {
                Dash(xRaw, yRaw);
            }

        }

        if (coll.onGround && !groundTouch)
        {
            GroundTouch();
            groundTouch = true;
            // Liam - reset doubleJump boolean to allow double jump again
            doubleJump = true;
        }

        if (!coll.onGround && groundTouch)
        {
            groundTouch = false;
        }

        WallParticle(y);

        if (wallGrab || wallSlide || !canMove)
            return;

        if (x > 0)
        {
            side = 1;
            anim.Flip(side);
        }
        if (x < 0)
        {
            side = -1;
            anim.Flip(side);
        }


    }

    void GroundTouch()
    {
        // Liam - if downwards speed is higher than threshold, shake screen on impact
        if ((Modified || ModifiedD) && maxFall < -fallspeed)
        {
            Camera.main.transform.DOComplete();
            Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
            Vcam1.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
            Vcam2.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
            Vcam3.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
            Vcam4.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
            // Zac- Where the ground hit audio will play
            audioSource.Play();
        }
        maxFall = 0;

        hasDashed = false;
        isDashing = false;

        side = anim.sr.flipX ? -1 : 1;

        jumpParticle.Play();
    }

    private void Dash(float x, float y)
    {
        Camera.main.transform.DOComplete();
        Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        Vcam1.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        Vcam2.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        Vcam3.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        Vcam4.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

        hasDashed = true;

        anim.SetTrigger("dash");
        // Zac - Where the theoretical dash sfx would play
        // audioClip.PlayOneShot(Dashing);

        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);

        rb.velocity += dir.normalized * dashSpeed;
        StartCoroutine(DashWait());
    }

    IEnumerator DashWait()
    {
        FindObjectOfType<GhostTrail>().ShowGhost();
        StartCoroutine(GroundDash());
        DOVirtual.Float(14, 0, .8f, RigidbodyDrag);

        dashParticle.Play();
        rb.gravityScale = 0;
        GetComponent<BetterJumping>().enabled = false;
        wallJumped = true;
        isDashing = true;
        if ((Modified || ModifiedD) && isDashing)
        {
            audioSource.PlayOneShot(clip2);
        }


        yield return new WaitForSeconds(.3f);

        dashParticle.Stop();
        rb.gravityScale = 3;
        GetComponent<BetterJumping>().enabled = true;
        wallJumped = false;
        isDashing = false;
    }

    IEnumerator GroundDash()
    {
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
            hasDashed = false;
    }

    private void WallJump()
    {
        if ((side == 1 && coll.onRightWall) || side == -1 && !coll.onRightWall)
        {
            side *= -1;
            anim.Flip(side);
        }

        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(.1f));

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;


        if (Modified || ModifiedD)
        {
            Jump((Vector2.up / 1.0f + wallDir / 4.5f), true);
        }
        else
        {
            Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);
        }
        // Qinglan - consistent jumping on wall

        wallJumped = true;
    }

    private void WallSlide()
    {
        if (coll.wallSide != side)
            anim.Flip(side * -1);

        if (!canMove)
            return;

        bool pushingWall = false;
        if ((rb.velocity.x > 0 && coll.onRightWall) || (rb.velocity.x < 0 && coll.onLeftWall))
        {
            pushingWall = true;
        }
        float push = pushingWall ? 0 : rb.velocity.x;

        rb.velocity = new Vector2(push, -slideSpeed);
    }

    private void Walk(Vector2 dir)
    {
        if (!canMove)
            return;

        if (wallGrab)
            return;

        if (!wallJumped)
        {
            rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(dir.x * speed, rb.velocity.y)), wallJumpLerp * Time.deltaTime);
        }
    }

    private void Jump(Vector2 dir, bool wall)
    {
        // Nile - Call stretch coroutine on jump
        // Zac - Where the jump sfx would play
        if (Modified || ModifiedD)
        {
            StartCoroutine(JumpStretch());
            audioSource.PlayOneShot(clip);
        }
        slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        particle.Play();
    }

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void RigidbodyDrag(float x)
    {
        rb.drag = x;
    }

    void WallParticle(float vertical)
    {
        var main = slideParticle.main;

        if (wallSlide || (wallGrab && vertical < 0))
        {
            slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
            main.startColor = Color.white;
        }
        else
        {
            main.startColor = Color.clear;
        }
    }

    int ParticleSide()
    {
        int particleSide = coll.onRightWall ? 1 : -1;
        return particleSide;
    }

    // Nile - Coroutine for stretching animation
    IEnumerator JumpStretch()
    {
        for (float scale = 1f; scale < 2f; scale += 0.1f)
        {
            transform.localScale = new Vector3(1 / scale, scale, 1);
            yield return new WaitForSeconds(0.01f);
        }

        for (float scale = 2f; scale > 1f; scale -= 0.1f)
        {
            transform.localScale = new Vector3(1 / scale, scale, 1);
            yield return new WaitForSeconds(0.01f);
        }
        transform.localScale = new Vector3(1, 1, 1);
    }

    //Noah - Teleport player between levels
    public void TeleportLevel()
    {
        gameObject.transform.position = new Vector3(-9.5f, -45f, 0f);
    }

    public void TeleportTutorial()
    {
        gameObject.transform.position = new Vector3(-9.5f, -3f, 0f);
    }
}