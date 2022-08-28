using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveToGoalAgent : Agent
{


    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform obstacleTransform;
    public bool isGrounded;
    private Rigidbody playerRigidBody = null;

    /*void Start()
    {
        playerRigidBody = GetComponent<Rigidbody>();
        playerRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
        isGrounded = true;
    }*/

    public override void Initialize()
    {
        playerRigidBody = GetComponent<Rigidbody>();
        playerRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("obstacle") == true)
        {
            SetReward(-1f);
            //Destroy(collision.gameObject);
            EndEpisode();
        }
        if (collision.gameObject.CompareTag("Floor") == true && isGrounded == false)
        {
            isGrounded = true;
        }
    }

    public override void OnEpisodeBegin()
    {
        playerRigidBody.velocity = Vector3.zero;
        isGrounded = false;
        transform.localPosition = new Vector3(Random.Range(-5f,-3f), 1f, Random.Range(-3.3f,3.2f));
        targetTransform.localPosition = new Vector3(Random.Range(2f, 4f), 1f, Random.Range(-3.3f, 3.2f));
        
        
        obstacleTransform.gameObject.SetActive(true);
        obstacleTransform.GetChild(0).gameObject.SetActive(true);
        obstacleTransform.localPosition = new Vector3(Random.Range(-1f, 1f), 0.75f, 0f);

    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);
        sensor.AddObservation(obstacleTransform.localPosition);
    }

    /*private void FixedUpdate()
    {
        Debug.Log(isGrounded);
        if (Input.GetKey(KeyCode.Space) == true && isGrounded)
        {
            Jump();
        }
    }*/
    private void Jump()
    {

        SetReward(-50f / 1000f);
        float force = 5f;
        //transform.localPosition += new Vector3(0, 1, 0) * Time.deltaTime * force;
        playerRigidBody.AddForce(Vector3.up * force, ForceMode.VelocityChange);
        isGrounded = false;

    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        SetReward(-7.5f / 1000);
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        if(actions.ContinuousActions[2]==1 & isGrounded)
        {
            Jump();
        }
        
        float moveSpeed = 2.5f;
        transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
        /*if(this.transform.localPosition.y > 3f)
        {
            SetReward(-1.0f);
            EndEpisode();
        }*/
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continiousActions = actionsOut.ContinuousActions;
        continiousActions[1] = -Input.GetAxisRaw("Horizontal");
        continiousActions[0] = Input.GetAxisRaw("Vertical");
        continiousActions[2] = 0;
        if (Input.GetKey(KeyCode.Space) == true && isGrounded)
        {
            continiousActions[2] = 1;
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Goal>(out Goal goal))
        {
            SetReward(+3f);
            EndEpisode();
        }
        if(other.TryGetComponent<Wall>(out Wall wall))
        {
            SetReward(-1f);
            EndEpisode();
        }
        if (other.TryGetComponent<ObstacleReward>(out ObstacleReward obstacleReward))
        {
            SetReward(+2f);
            obstacleTransform.GetChild(0).gameObject.SetActive(false);
        }
        
    }


}
