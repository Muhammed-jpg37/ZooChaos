using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    private Rigidbody rb;
    private float moveSpeed = 3f;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Vertical");
        float moveVertical = Input.GetAxis("Horizontal");

        Vector3 movement = new Vector3(-moveHorizontal , 0.0f,moveVertical);
        rb.velocity = movement * moveSpeed;}

    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("AnimalCase") && Input.GetKey(KeyCode.E))
        {
            other.GetComponent<AnimalBehaviour>().isPlayerFixing = true;
        }
    }
}
