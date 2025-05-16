using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SonicController : MonoBehaviour
{
    private bool jumping;
    [Header("Movimiento")]
    public float moveSpeed = 24f;
    public float jumpForce = 12f;
    public float rotationSpeed = 15f;
    public float groundAcceleration = 70f;
    public float airAcceleration = 20f;

    [Header("Cámara")]
    public Transform cameraTransform;
    public float cameraFollowSpeed = 10f;
    public Vector3 cameraOffset = new Vector3(0, 3, -6);
    public float cameraRotationSpeed = 5f;

    [Header("Detección de Suelo")]
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.3f;
    public float groundCheckDistance = 0.6f;
    public Transform groundCheckPoint;

    [Header("Referencias")]
    public Animator animator;
    public Transform model;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 inputDirection;
    private Vector3 surfaceNormal = Vector3.up;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (!groundCheckPoint)
            Debug.LogWarning("Asignar Ground Check Point en el inspector.");
    }

    void Update()
    {
        HandleInput();
        HandleAnimations();
        FollowCamera();
        HandleJump();
    }

    void FixedUpdate()
    {
        GroundCheck();
        AlignToSurface(); 
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        inputDirection = (camForward * v + camRight * h).normalized;
    }

    void HandleMovement()
    {
        Vector3 targetVelocity = inputDirection * moveSpeed;

        if (isGrounded)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity + Vector3.up * rb.velocity.y, groundAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            Vector3 airVelocity = rb.velocity;
            Vector3 targetXZ = Vector3.Lerp(new Vector3(airVelocity.x, 0, airVelocity.z), new Vector3(targetVelocity.x, 0, targetVelocity.z), airAcceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector3(targetXZ.x, rb.velocity.y, targetXZ.z);
        }

        if (inputDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            model.rotation = Quaternion.Lerp(model.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    void HandleJump()
    {
        animator.SetBool("jumping", !isGrounded);
        if (Input.GetButtonDown("Jump"))
            Debug.Log("Jump button pressed");
       
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (jumping == false)
            {
              
                GetComponent<Rigidbody>().AddForce(new Vector3(0.0f, 960.0f, 0.0f));
                jumping = true;
            }
        }
        if (isGrounded)
            Debug.Log("Is grounded");

        if (isGrounded && Input.GetButtonDown("Jump"))
            
        {    
            
            

        
            Debug.Log("JUMPING!");
            Vector3 jumpDirection = surfaceNormal.normalized;
            rb.velocity = Vector3.ProjectOnPlane(rb.velocity, surfaceNormal);
            rb.AddForce(jumpDirection * jumpForce, ForceMode.VelocityChange);
            isGrounded = false;
        }
        

    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.velocity += Vector3.down * 20f * Time.fixedDeltaTime;
        }
    }

    void GroundCheck()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, -transform.up, 0.3f, groundLayer);
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, -transform.up * 0.3f, isGrounded ? Color.green : Color.red);

        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);

        
        RaycastHit hit;
        if (Physics.Raycast(groundCheckPoint.position + Vector3.up * 0.2f, Vector3.down, out hit, 1f, groundLayer))
        {
            surfaceNormal = hit.normal;
        }
        else
        {
            surfaceNormal = Vector3.up;
        }
    }

    void AlignToSurface()
    {
        if (isGrounded)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(model.up, surfaceNormal) * model.rotation;
            model.rotation = Quaternion.Slerp(model.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    void HandleAnimations()
    {
        float currentSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        animator.SetBool("Grounded", isGrounded);
        animator.SetBool("isJumping", !isGrounded);
        animator.SetFloat("Speed", currentSpeed);

        animator.SetBool("isRunning", currentSpeed > moveSpeed * 0.5f);
        animator.SetBool("IsMoving", currentSpeed > 0.1f);
        animator.SetFloat("VerticalVelocity", rb.velocity.y);
    }

    void FollowCamera()
    {
        if (cameraTransform)
        {
         
            Vector3 desiredPosition = transform.position + model.rotation * cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, cameraFollowSpeed * Time.deltaTime);

            Quaternion desiredRotation = Quaternion.LookRotation(transform.position + Vector3.up * 1.5f - cameraTransform.position);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, cameraRotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
    void OnCollisionEnter(Collision _col)
    {
        if (_col.gameObject.tag == "floor")
        {

            jumping = false;
        }
    }
}
