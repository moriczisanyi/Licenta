using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;

public class AgentMove : Agent
{

    //Observations

    //MoveToCheckpoint
    [SerializeField] private Transform targetTransform;
    [SerializeField] private TrackCheckpoints trackCheckpoints;

    //Obstacles
    [SerializeField] private Transform obstacleTransform;
    [SerializeField] private Transform boxTransform;
    [SerializeField] private Transform jWallTransform;
    [SerializeField] private Transform lavaTransform;

    //Floor parts
    [SerializeField] private Transform floorP1Transform;
    [SerializeField] private Transform floorP2Transform;

    

    // Agent movement
    [SerializeField] private float moveSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float crouchSpeed;

    private Vector3 moveDirection;
    private Vector3 velocity;

    [SerializeField] private bool isGrounded;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private float gravity;

    [SerializeField] private float jumpHeight;
    [SerializeField] private float bigJumpHeight;
    private float canJump;

    [SerializeField] private float pushForceMagnitude;


    //References
    private CharacterController characterController;
    private Animator animator;


    //private RaycastHit upRay;
    private bool isStandingBlocked=false;


    //brains
    [SerializeField] private NNModel crouchBrain;
    [SerializeField] private NNModel jumpBrain;
    [SerializeField] private NNModel goToGoalBrain;

    private NNModel currentBrain;

    private int goalCountMax=1;
    private int goalCounter=0;
    Vector3[] goalSpawnPositions = new Vector3[5];

    Vector3 dirToTarget;

    public override void Initialize()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        currentBrain = goToGoalBrain;
    }

 



    public override void OnEpisodeBegin()
    {
        currentBrain = goToGoalBrain;
        goalCounter = 0;

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-5f, -3f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0,90,0);
        characterController.enabled = true;

        //SpawnGoal();

        JumpMap();

        //Obstacle spawn



        /*obstacleTransform.gameObject.SetActive(true);
        obstacleTransform.GetChild(0).gameObject.SetActive(true);
        obstacleTransform.localPosition = new Vector3(Random.Range(-1f, 1f), 0.75f, 0f);
          */


        // Checkpoint index reset
        //trackCheckpoints.resetNextCheckpointSingle();



        //Box spwan
        /*boxTransform.localPosition = new Vector3(Random.Range(2.5f, 6f), 1f, Random.Range(-2.5f, 2.5f));
        Box box = transform.parent.transform.Find("Box").GetComponent<Box>();
        if (box != null)
        {
            
            box.setHasTouchedJWall(false);
            if (box.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
                
            }
        }*/

    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(characterController.transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);
        sensor.AddObservation(dirToTarget.normalized);
        sensor.AddObservation(characterController.transform.forward);
        //sensor.AddObservation((targetTransform.position - characterController.transform.position).normalized);
        //sensor.AddObservation(characterController.transform.forward);
        //sensor.AddObservation(characterController.velocity);


        //sensor.AddObservation(obstacleTransform.localPosition);
        //sensor.AddObservation(boxTransform.localPosition);
        //sensor.AddObservation(jWallTransform.localPosition);
        //Vector3 checkpointForward = trackCheckpoints.getNextCheckpoint().transform.forward;
        //sensor.AddObservation(trackCheckpoints.getNextCheckpoint().transform.localPosition);
        //float directionDot = Vector3.Dot(transform.forward, checkpointForward);
       
        //sensor.AddObservation(trackCheckpoints.getNextCheckpoint().transform.localPosition);
        //float dot = Vector3.Dot(characterController.transform.forward, (targetTransform.position - characterController.transform.position).normalized);
        //sensor.AddObservation(dot);
    }

   
    public override void OnActionReceived(ActionBuffers actions)
    {
        //SetModel("current",currentBrain);
        //Debug.Log(currentBrain.name);
        RaycastHit downRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -1, 3f)), Color.yellow);
        if (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -1, 3f)), out downRay, 5f, ~playerMask))
        {
            if (downRay.collider.gameObject.tag == "Lava")
            {
               currentBrain = jumpBrain;
            }
        }

        RaycastHit crounchRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, transform.TransformDirection(new Vector3(0f, 1, 3f)), Color.blue);
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, transform.TransformDirection(new Vector3(0f, 1, 3f)), out crounchRay, 5f, ~playerMask))
        {
            if (crounchRay.collider.gameObject.tag == "JWall")
            {
                currentBrain = crouchBrain;
                
            }
        }

        dirToTarget = targetTransform.localPosition - characterController.transform.localPosition;
        AddReward(
            +0.03f * Vector3.Dot(dirToTarget.normalized, characterController.velocity) +
            +0.01f * Vector3.Dot(dirToTarget.normalized, characterController.transform.forward)
            );


        /*if(characterController.transform.localPosition.y < 0)
        {
            SetReward(-5f);
            EndEpisode();
        }*/
        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        SetReward(-10f / 1000f);


        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        


        RaycastHit upRay;
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.up)*2.5f, Color.yellow);
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.up), out upRay, 5f,~playerMask))
        {
            if (upRay.collider.gameObject.tag == "JWall")
            {
                isStandingBlocked = true;
            }
            
            
        }
        else
        {
            isStandingBlocked = false;
        }
        if (isGrounded)
        {
            moveDirection = new Vector3(0, 0, moveZ);
            //moveDirection = transform.right * moveX + transform.forward * moveZ;
            
            
            moveDirection = transform.TransformDirection(moveDirection);
            
            /*if (moveX != 0 || moveZ != 0)
            {
                transform.localRotation = Quaternion.LookRotation(moveDirection);
            }*/

            if (actions.DiscreteActions[2] == 1 || isStandingBlocked)
            {
                animator.SetBool("IsCrouching", true);
                characterController.height = 1f;
                characterController.center = new Vector3(0, 0.5f, 0);
                if (moveDirection != Vector3.zero && actions.DiscreteActions[1] == 0)
                {

                    CrouchWalking();

                }
                else if(moveDirection == Vector3.zero)
                {
                    Crouching();
                }
            }
            else
            {
                if (!isStandingBlocked)
                {
                    characterController.height = 1.75f;
                    characterController.center = new Vector3(0, 0.88f, 0);
                    animator.SetBool("IsCrouching", false);

                    if (moveDirection != Vector3.zero && actions.DiscreteActions[1] == 0)
                    {

                        Walk();

                    }
                    else if (moveDirection != Vector3.zero && actions.DiscreteActions[1] == 1)
                    {
                        Run();
                    }
                    else if (moveDirection == Vector3.zero)
                    {
                        Idle();
                    }



                    if (actions.DiscreteActions[0] == 1 && Time.time > canJump && actions.DiscreteActions[1] == 0)
                    {
                        Jump();
                        canJump = Time.time + 1.0f;
                    }
                    else if (actions.DiscreteActions[0] == 1 && Time.time > canJump && actions.DiscreteActions[1] == 1)
                    {
                        BigJump();
                        canJump = Time.time + 1.0f;
                    }
                }
            }
            

            moveDirection *= moveSpeed;
        }


        //transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
        characterController.Move(moveDirection * Time.deltaTime);
        characterController.transform.Rotate(Vector3.up * moveX * (rotationSpeed * Time.deltaTime), Space.Self);
        //characterController.transform.RotateAround(characterController.transform.position, Vector3.up, moveX * 100f * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {


        ActionSegment<int> actions = actionsOut.DiscreteActions;
        //actions[0] = Mathf.RoundToInt(Input.GetAxisRaw("Horizontal"));
        //actions[1] = Mathf.RoundToInt(Input.GetAxisRaw("Vertical"));
        actions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        actions[1] = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;
        actions[2] = Input.GetKey(KeyCode.LeftControl) ? 1 : 0;
        ActionSegment<float> continiousActions = actionsOut.ContinuousActions;
        continiousActions[0] = Input.GetAxisRaw("Horizontal");
        continiousActions[1] = Input.GetAxisRaw("Vertical");



    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Goal>(out Goal goal))
        {
            if (goalCounter == goalCountMax - 1)
            {
                SetReward(+20f);
                EndEpisode();
            }
            else
            {
                goalCounter++;
                SetReward(+5f);
                SpawnGoal();
            }
        }
        if (other.TryGetComponent<Wall>(out Wall wall))
        {
            SetReward(-5f);
            EndEpisode();
        }
        if (other.TryGetComponent<ObstacleReward>(out ObstacleReward obstacleReward))
        {
            SetReward(+3f);
            obstacleTransform.GetChild(0).gameObject.SetActive(false);
        }

    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Box") == true)
        {
            Rigidbody rigidbody = hit.collider.attachedRigidbody;
            if (rigidbody != null && rigidbody.gameObject.CompareTag("Box") == true)
            {
                Vector3 forceDirection = hit.gameObject.transform.position - transform.position;
                forceDirection.y = 0f;
                forceDirection.Normalize();

                rigidbody.AddForceAtPosition(forceDirection * pushForceMagnitude, transform.position, ForceMode.Impulse);

            }
        }
    }


    private void SpawnGoal()
    {
        goalSpawnPositions[0] = new Vector3(Random.Range(1f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
        //goalSpawnPositions[0]= new Vector3(Random.Range(1f, 2f), 0.85f, Random.Range(-3.3f, 3.2f));
        //goalSpawnPositions[1]= new Vector3(Random.Range(3f, 5f), 0.85f, Random.Range(-3.3f, 3.2f));
        goalSpawnPositions[1] = new Vector3(Random.Range(6f, 8f), 0.85f, Random.Range(-3.3f, 3.2f));
        //goalSpawnPositions[3] = new Vector3(Random.Range(10f, 12f), 0.85f, Random.Range(-3.3f, 3.2f));
        goalSpawnPositions[2] = new Vector3(Random.Range(13f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];
    }

    private void Idle()
    {
        animator.SetFloat("Speed", 0, 0.1f, Time.deltaTime);
    }
    private void Walk()
    {
        moveSpeed = walkSpeed;
        animator.SetFloat("Speed", 0.5f, 0.1f, Time.deltaTime);
    }
    private void Run()
    {
        moveSpeed = runSpeed;
        animator.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }


    private void Jump()
    {

        velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);

    }

    private void BigJump()
    {
        velocity.y = Mathf.Sqrt(bigJumpHeight * -2 * gravity);
    }

    private void CrouchWalking()
    {
        moveSpeed = crouchSpeed;
        animator.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }
    private void Crouching()
    {
        animator.SetFloat("Speed", 0, 0.1f, Time.deltaTime);
    }

    private void JumpMap()
    {
        int number = Random.Range(-6, 0);
        lavaTransform.localPosition = new Vector3(number + 5, lavaTransform.localPosition.y, lavaTransform.localPosition.z);
        goalSpawnPositions[0] = new Vector3(Random.Range(number+6.5f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];
    }


}
