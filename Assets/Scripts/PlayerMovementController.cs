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

    private int currentCameraIndex = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialStartPosition = transform.position;
        InitializeCameraPoints();
    }

    private void InitializeCameraPoints()
    {
        if (cameraStartPoint == null || secondCameraPoint == null || Camera == null)
        {
            return;
        }

        currentCameraIndex = 0;
        ApplyCameraPoint(cameraStartPoint, Quaternion.Euler(24f, -90f, 0f));
    }

    private void ApplyCameraPoint(GameObject cameraPoint, Quaternion rotation)
    {
        if (cameraPoint == null || Camera == null) return;

        Camera.transform.position = cameraPoint.transform.position;
        Camera.transform.rotation = rotation;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwitchCamera();
        }
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

    private void SwitchCamera()
    {
        if (cameraStartPoint == null || secondCameraPoint == null || Camera == null)
        {
            return;
        }

        currentCameraIndex = (currentCameraIndex + 1) % 2;

        if (currentCameraIndex == 0)
        {
            ApplyCameraPoint(cameraStartPoint, Quaternion.Euler(24f, -90f, 0f));
        }
        else
        {
            ApplyCameraPoint(secondCameraPoint, Quaternion.Euler(24f, -270f, 0f));
        }
    }
}
