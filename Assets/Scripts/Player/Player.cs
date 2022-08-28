using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class Player : Agent
{
    /*public float Force = 5f;
    public bool isGrounded;
    private Rigidbody playerRigidBody = null;
    void Start()
    {
        playerRigidBody = GetComponent<Rigidbody>();
        playerRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        isGrounded = true; 
    }

    private void FixedUpdate()
    {
        Debug.Log(isGrounded);
        if (Input.GetKey(KeyCode.Space)==true && isGrounded)
        {
            Jump();
        }
    }
    private void Jump()
    {
       
        playerRigidBody.AddForce(Vector3.up * Force, ForceMode.VelocityChange);
        isGrounded = false;
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        /*if (collision.gameObject.CompareTag("obstacle") == true)
        {
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.CompareTag("Floor") == true && isGrounded==false)
        {
            isGrounded = true;
        }
    }



    */
}
