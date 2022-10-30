using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision : MonoBehaviour
{

    [Header("Layers")]
    public LayerMask groundLayer;

    [Space]

    public bool onGround;
    public bool onWall;
    public bool onRightWall;
    public bool onLeftWall;
    public int wallSide;

    [Space]

    [Header("Collision")]

    public float collisionRadius = 0.25f;
    // Tai Wen - Added new variables to control the ground collision box
    public float groundCollisionWidth = 0.5f;
    public float groundCollisionHeight = 0.1f;
    public Vector2 bottomOffset, rightOffset, leftOffset;
    private Color debugCollisionColor = Color.red;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Tai Wen - Modified ground collision detection to remedy edge cases (change from circle to area overlap)
        onGround = GetComponent<Movement>().Modified ? Physics2D.OverlapArea((Vector2)transform.position + bottomOffset + new Vector2(-groundCollisionWidth / 2, -groundCollisionHeight / 2),
            (Vector2)transform.position + bottomOffset + new Vector2(groundCollisionWidth / 2, groundCollisionHeight / 2), groundLayer)
            : Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, collisionRadius, groundLayer);
        onWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer)
            || Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);

        onRightWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer);
        onLeftWall = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);

        wallSide = onRightWall ? -1 : 1;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        var positions = new Vector2[] { bottomOffset, rightOffset, leftOffset };

        // Tai Wen - Changed the wire frame for the ground collision (circle to rectangle)
        if (GetComponent<Movement>().Modified)
        {
            Gizmos.DrawWireCube((Vector2)transform.position + bottomOffset, new Vector2(groundCollisionWidth, groundCollisionHeight));
        }
        else
        {
            Gizmos.DrawWireSphere((Vector2)transform.position + bottomOffset, collisionRadius);
        }
        Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, collisionRadius);
    }
}
