using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private Transform startPoint;
    [SerializeField] private GameObject cameraStartPoint;

    [SerializeField] private GameObject secondCameraPoint;

    [SerializeField] private GameObject Camera;


  
    private Vector3 initialStartPosition;
    private bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialStartPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (!canMove)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        float moveHorizontal = Input.GetAxis("Vertical");
        float moveVertical = Input.GetAxis("Horizontal");

        Vector3 movement = new Vector3(-moveHorizontal, 0.0f, moveVertical);
        rb.velocity = movement * moveSpeed;
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
        if (rb != null && !canMove)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetToStartPoint()
    {
        Vector3 targetPosition = startPoint != null ? startPoint.position : initialStartPosition;

        if (rb != null)
        {
            rb.position = targetPosition;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position = targetPosition;
        }
    }

   
}
