using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;

public class SmartAgent : Agent
{

    //Observations

    //MoveToCheckpoint
    [SerializeField] private Transform targetTransform;
    [SerializeField] private TrackCheckpoints trackCheckpoints;

    //Obstacles
    [SerializeField] private Transform crouchObstacleTransform;
    [SerializeField] private Transform lavaTransform;




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
    [SerializeField] private LayerMask ignoredLayers;
    [SerializeField] private float gravity;

    [SerializeField] private float jumpHeight;
    [SerializeField] private float bigJumpHeight;
    private float canJump;

    [SerializeField] private float pushForceMagnitude;


    //References
    private CharacterController characterController;
    private Animator animator;


    //private RaycastHit upRay;
    private bool isStandingBlocked = false;


    //brains
    [SerializeField] private NNModel crouchBrain;
    [SerializeField] private NNModel jumpBrain;
    [SerializeField] private NNModel goToGoalBrain;
    [SerializeField] private NNModel pushBoxBrain;

    private NNModel currentBrain;

    float rewardTimeJump = 0;
    private int goalCountMax = 1;
    private int goalCounter = 0;
    Vector3[] goalSpawnPositions = new Vector3[5];
    Vector3[] checkpointsSpawnPositions = new Vector3[5];
    private int checkpointsCounter = 1;

    private int collectableCount = 4;
    private int collected = 0;
    private bool isButtonPressed=false;

    private Vector3 dirToTarget;
    private Transform currentObstacleTransform;
    private Vector3 dirToCurrentObstacle;

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
        //SmartAgent_MoveToGoal_Lvl_3();
        //SmartAgent_Jump_lvl4();
        //SmartAgent_Crouch_lvl3();
        //SmartAgent_PushBox_lvl4();
        //Demo();

        //SmartAgent_Collectable_lvl2();
        SmartAgent_Maze_lvl2();
        //SmartAgent_PushButton_lvl2();




    }
    public override void CollectObservations(VectorSensor sensor)
    {
        //agent
        sensor.AddObservation(characterController.transform.localPosition.normalized);
        sensor.AddObservation(characterController.transform.localRotation.normalized);
        sensor.AddObservation(characterController.transform.forward.normalized);
        sensor.AddObservation(moveSpeed);
        sensor.AddObservation(moveDirection);

        //target
        sensor.AddObservation(targetTransform.localPosition.normalized);
        sensor.AddObservation(dirToTarget.normalized);
        sensor.AddObservation(Vector3.Dot(characterController.transform.forward, dirToTarget.normalized));       
    }


    
    public override void OnActionReceived(ActionBuffers actions)
    {
        //SmartAgent_Collectable_lvl1_onAction();

        //SetModel("current",currentBrain);
        //Debug.Log(currentBrain.name);
        DownRay(actions);

        CrouchRay(actions);
        

        dirToTarget = targetTransform.localPosition - characterController.transform.localPosition;
        AddReward(
            +0.003f * Vector3.Dot(dirToTarget.normalized, characterController.velocity) +
            +0.001f * Vector3.Dot(dirToTarget.normalized, characterController.transform.forward)
            );


        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        SetReward(-2f / MaxStep);

        Movement(actions);


        

    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {


        ActionSegment<int> actions = actionsOut.DiscreteActions;
        //actions[0] = Mathf.RoundToInt(Input.GetAxisRaw("Horizontal"));
        //actions[1] = Mathf.RoundToInt(Input.GetAxisRaw("Vertical"));
        actions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        actions[1] = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;
        actions[2] = Input.GetKey(KeyCode.LeftControl) ? 1 : 0;
        actions[3] = Input.GetKey(KeyCode.Tab) ? 1 : 0;
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
                SetReward(+2f);
                EndEpisode();
            }
            else
            {
                goalCounter++;
                currentBrain = goToGoalBrain;
                SetReward(+1f);
                SpawnGoal();
                
            }
        }
        if (other.TryGetComponent<Wall>(out Wall wall))
        {
            SetReward(-1f);
            EndEpisode();
        }
        if(other.CompareTag("Checkpoint"))
        {
            SetReward(+0.5f);
            other.gameObject.SetActive(false);
            checkpointsCounter++;
            NextCheckPoint();
        }
        if(other.CompareTag("Collectable"))
        {
            SetReward(+0.25f);
            collected++;
            other.gameObject.SetActive(false);
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
        //goal spawn location

        goalSpawnPositions[0] = new Vector3(17f, targetTransform.localPosition.y, 0f);
        goalSpawnPositions[1] = new Vector3(29.2f, targetTransform.localPosition.y, -2.45f);
        goalSpawnPositions[2] = new Vector3(31.26f, targetTransform.localPosition.y, -6.77f);
        goalSpawnPositions[3] = new Vector3(49.17f, targetTransform.localPosition.y, -26.26f);
        goalSpawnPositions[4] = new Vector3(37.36f, targetTransform.localPosition.y, -39.99f);
        targetTransform.localPosition = goalSpawnPositions[goalCounter];
    }

    private void Movement(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        if (actions.DiscreteActions[3] == 1)
        {
            Collider[] colliderArray = Physics.OverlapBox(transform.position, Vector3.one * 0.5f);
            foreach (Collider collider in colliderArray)
            {
                if(collider.TryGetComponent<Button>(out Button button) && !isButtonPressed)
                {
                    isButtonPressed = true;
                    Transform pushButton = transform.parent.transform.Find("PushButton");
                    if (pushButton != null)
                    {
                        Transform wallObstacle = pushButton.Find("WallObstacle");
                        if (wallObstacle != null)
                            wallObstacle.gameObject.SetActive(false);
                    }
                }
            }
        }

        RaycastHit upRay;
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.up) * 2.5f, Color.yellow);
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.up), out upRay, 5f, ~playerMask))
        {
            if (upRay.collider.gameObject.tag == "CrouchObstacle")
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


            moveDirection = transform.TransformDirection(moveDirection);

            if (actions.DiscreteActions[2] == 1 || isStandingBlocked)
            {
                animator.SetBool("IsCrouching", true);
                characterController.height = 1f;
                characterController.center = new Vector3(0, 0.5f, 0);
                if (moveDirection != Vector3.zero)
                {

                    CrouchWalking();

                }
                else if (moveDirection == Vector3.zero)
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

                    if (moveDirection != Vector3.zero && actions.DiscreteActions[1] == 0 && actions.DiscreteActions[2] == 0)
                    {

                        Walk();

                    }
                    else if (moveDirection != Vector3.zero && actions.DiscreteActions[1] == 1 && actions.DiscreteActions[2] == 0)
                    {
                        Run();
                    }
                    else if (moveDirection == Vector3.zero && actions.DiscreteActions[2] == 0)
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

            if (moveZ < 0)
                moveDirection *= moveSpeed / 2f;
            else
                moveDirection *= moveSpeed;
        }


        characterController.Move(moveDirection * Time.deltaTime);
        characterController.transform.Rotate(Vector3.up * moveX * (rotationSpeed * Time.deltaTime), Space.Self);
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
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


    private void DownRay(ActionBuffers actions)
    {
        RaycastHit downRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -0.5f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(2f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-2f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(3f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-3f, 0f, 5f)), Color.yellow);
        if ( (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -0.5f, 5f)), out downRay, 5f, ~ignoredLayers)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, 0, 5f)), out downRay, 5f, ~ignoredLayers)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(2f, 0f, 5f)), out downRay, 5f, ~ignoredLayers)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-2f, 0f, 5f)), out downRay, 5f, ~ignoredLayers)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(3f, 0f, 5f)), out downRay, 5f, ~ignoredLayers)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-3f, 0f, 5f)), out downRay, 5f, ~ignoredLayers))
            )
        {
            if (downRay.collider.gameObject.tag == "Lava")
            {
                
                if (actions.DiscreteActions[0] == 1 && Time.time > rewardTimeJump)
                {
                    rewardTimeJump = Time.time + 1.0f;
                    SetReward(0.05f);
                }
                currentBrain = jumpBrain;
            }
            if (downRay.collider.gameObject.tag == "Box")
            {
                currentBrain = pushBoxBrain;
            }
        }
    }

    private void CrouchRay(ActionBuffers action)
    {
        RaycastHit crouchRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, transform.TransformDirection(new Vector3(0f, 1, 3f)), Color.blue);
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, transform.TransformDirection(new Vector3(0f, 1, 3f)), out crouchRay, 5f, ~ignoredLayers))
        {
            if (crouchRay.collider.gameObject.tag == "CrouchObstacle")
            {
                currentBrain = crouchBrain;

            }
        }
    }

    private void Demo()
    {
        // agent on fixed position, fixed rotation
        // target on fixed positon

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(-5.46f, 0.55f, -1f);
        characterController.transform.rotation = Quaternion.Euler(0, 90, 0);
        characterController.enabled = true;

        SpawnGoal();


        //lava spawn

        //lavaTransform.localPosition = new Vector3(10f, lavaTransform.localPosition.y, lavaTransform.localPosition.z);
        if(lavaTransform.TryGetComponent<Lava>(out Lava lava))
        {
            lava.MovingLava = false;
        }
        Transform lavaObstacles = transform.parent.transform.Find("LavaObstacles");
        if (lavaObstacles != null)
        {
            for (int i = 0; i < lavaObstacles.childCount; i++)
            {
                if (lavaObstacles.GetChild(i).transform.CompareTag("Lava") == true)
                {
                    lavaObstacles.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }
            }
        }


        //pushbox
        Transform pushObstacles = transform.parent.transform.Find("PushObstacles");
        if (pushObstacles != null)
        {

            if (pushObstacles.GetChild(0).TryGetComponent<Box>(out Box box))
            {
                box.HasTouchedWallObstacle = false;
                if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~RigidbodyConstraints.FreezePositionX;
                }

                if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                {
                    box.transform.localPosition = new Vector3(wallObstacle.transform.localPosition.x - 2.5f, box.transform.localPosition.y, box.transform.localPosition.z);
                    wallObstacle.gameObject.SetActive(true);
                }
            }
        }

    }

    private void SmartAgent_MoveToGoal_Lvl_1()
    {
        // agent on fixed position, fixed rotation
        // target on fixed positon

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(-2f, 0.55f, 0f);
        characterController.transform.rotation = Quaternion.Euler(0, 90, 0);
        characterController.enabled = true;

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(0f, 0.85f, 0f);
        targetTransform.localPosition = goalSpawnPositions[goalCounter];

    }

    private void SmartAgent_MoveToGoal_Lvl_2()
    {
        // agent on random position, random rotation
        // target on fixed positon

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-5f, 15f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0,360), 0);
        characterController.enabled = true;

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(0f, 0.85f, 0f);
        targetTransform.localPosition = goalSpawnPositions[goalCounter];

    }

    private void SmartAgent_MoveToGoal_Lvl_3()
    {
        // agent on random position, random rotation
        // target on random positon

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-5f, 15f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(Random.Range(-5f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];

    }

    private void SmartAgent_Jump_lvl1()
    {
        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on fixed position

        int randomNumber = Random.Range(2, 4);
        randomNumber = randomNumber * (Random.Range(0, 2) * 2 - 1);

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(lavaTransform.localPosition.x + randomNumber, 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];


        if (randomNumber < 0)
            randomNumber -= 1;
        else
            randomNumber += 1;

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(lavaTransform.localPosition.x - randomNumber, 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

        Transform lavaObstacles = transform.parent.transform.Find("LavaObstacles");
        if (lavaObstacles != null)
        {
            for (int i = 0; i < lavaObstacles.childCount; i++)
            {
                if (lavaObstacles.GetChild(i).transform.CompareTag("Lava") == true)
                {
                    lavaObstacles.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }
            }
        }


    }


    private void SmartAgent_Jump_lvl2()
    {
        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on random position, close to obstacle

        int randomNumber = Random.Range(2, 4);
        randomNumber = randomNumber * (Random.Range(0, 2) * 2 - 1);

        //obstacle
        lavaTransform.localPosition = new Vector3(Random.Range(-2f, 11f), lavaTransform.localPosition.y, lavaTransform.localPosition.z);
        //goal spawn location
        goalSpawnPositions[0] = new Vector3(lavaTransform.localPosition.x + randomNumber, 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];


        if (randomNumber < 0)
            randomNumber -= 1;
        else
            randomNumber += 1;

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(lavaTransform.localPosition.x - randomNumber, 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

        Transform lavaObstacles = transform.parent.transform.Find("LavaObstacles");
        if (lavaObstacles != null)
        {
            for (int i = 0; i < lavaObstacles.childCount; i++)
            {
                if (lavaObstacles.GetChild(i).transform.CompareTag("Lava") == true)
                {
                    lavaObstacles.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }
            }
        }

    }

    private void SmartAgent_Jump_lvl3()
    {

        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on random position, anywhere

        int randomNumber = Random.Range(0, 2);

        //obstacle
        lavaTransform.localPosition = new Vector3(Random.Range(-2f, 11f), lavaTransform.localPosition.y, lavaTransform.localPosition.z);

        if (randomNumber == 0)
        {
            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(lavaTransform.localPosition.x +2f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];

            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(-5f, lavaTransform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
        }
        else
        {
            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(-5f, lavaTransform.localPosition.x - 2f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];

            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(lavaTransform.localPosition.x + 2f, 15f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
        }

        Transform lavaObstacles = transform.parent.transform.Find("LavaObstacles");
        if (lavaObstacles != null)
        {
            for (int i = 0; i < lavaObstacles.childCount; i++)
            {
                if (lavaObstacles.GetChild(i).transform.CompareTag("Lava") == true)
                {
                    lavaObstacles.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }
            }
        }


    }

    private void SmartAgent_Jump_lvl4()
    {

        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on random position, anywhere

        int randomNumber = Random.Range(0, 2);

        //obstacle
        lavaTransform.localPosition = new Vector3(Random.Range(0f, 30f), lavaTransform.localPosition.y, lavaTransform.localPosition.z);

        if (randomNumber == 0)
        {
            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(lavaTransform.localPosition.x + 2f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];

            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(-5f, lavaTransform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
        }
        else
        {
            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(-5f, lavaTransform.localPosition.x - 2f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];

            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(lavaTransform.localPosition.x + 2f, 33f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
        }

        Transform lavaObstacles = transform.parent.transform.Find("LavaObstacles");
        if (lavaObstacles != null)
        {
            for (int i = 0; i < lavaObstacles.childCount; i++)
            {
                if (lavaObstacles.GetChild(i).transform.CompareTag("Lava") == true)
                {
                    lavaObstacles.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }
            }
        }


    }
    private void SmartAgent_Crouch_lvl1()
    {
        // agent on random position, random rotation
        // target on random positon
        // crouch obstacle between agent and goal on fixed position

        int randomNumber = Random.Range(2, 4);
        randomNumber = randomNumber * (Random.Range(0, 2) * 2 - 1);

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(crouchObstacleTransform.localPosition.x + randomNumber, 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];


        if (randomNumber < 0)
            randomNumber -= 1;
        else
            randomNumber += 1;

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(crouchObstacleTransform.localPosition.x - randomNumber, 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;



    }
    private void SmartAgent_Crouch_lvl2()
    {
        // agent on random position, random rotation
        // target on random positon
        // crouch obstacle between agent and goal on random position, close to obstacle

        int randomNumber = Random.Range(2, 4);
        randomNumber = randomNumber * (Random.Range(0, 2) * 2 - 1);

        //obstacle
        crouchObstacleTransform.localPosition = new Vector3(Random.Range(-2f, 11f), crouchObstacleTransform.localPosition.y, crouchObstacleTransform.localPosition.z);
        //goal spawn location
        goalSpawnPositions[0] = new Vector3(crouchObstacleTransform.localPosition.x + randomNumber, 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];


        if (randomNumber < 0)
            randomNumber -= 1;
        else
            randomNumber += 1;

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(crouchObstacleTransform.localPosition.x - randomNumber, 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

    }
    private void SmartAgent_Crouch_lvl3()
    {
        // agent on random position, random rotation
        // target on random positon
        // crouch obstacle between agent and goal on random position, close to obstacle

        int randomNumber = Random.Range(0, 2);

        //obstacle
        crouchObstacleTransform.localPosition = new Vector3(Random.Range(-2f, 11f), crouchObstacleTransform.localPosition.y, crouchObstacleTransform.localPosition.z);

        if (randomNumber == 0)
        {
            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(crouchObstacleTransform.localPosition.x + 2f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];

            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(-5f, crouchObstacleTransform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
        }
        else
        {
            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(-5f, crouchObstacleTransform.localPosition.x - 2f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];

            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(crouchObstacleTransform.localPosition.x + 2f, 15f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
        }

    }

    private void SmartAgent_PushBox_lvl1()
    {
        // agent on fixed position, fixed rotation
        // target on fixed positon
        // push box fixed position on Z axis, close to WallObstacle
        

        

        

        Transform pushObstacles = transform.parent.transform.Find("PushObstacles");
        if (pushObstacles != null)
        {
            
            if (pushObstacles.GetChild(0).TryGetComponent<Box>(out Box box))
            {
                box.HasTouchedWallObstacle = false;
                if(box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~ RigidbodyConstraints.FreezePositionX;
                }
                box.transform.localPosition = new Vector3(Random.Range(0f, 5f), box.transform.localPosition.y, box.transform.localPosition.z);

                //Charcter spawn location and rotation
                characterController.enabled = false;
                characterController.transform.localPosition = new Vector3(Random.Range(-5f, box.transform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                characterController.enabled = true;

                if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                {
                    wallObstacle.gameObject.SetActive(true);
                    wallObstacle.transform.localPosition = new Vector3(box.transform.localPosition.x + 2f, wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                    goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.transform.localPosition.x + 2f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];
                    

                }
            }


        }
    }

    private void SmartAgent_PushBox_lvl2()
    {
        // agent on fixed position, fixed rotation
        // target on fixed positon
        // push box on random position on Z axis, close to WallObstacle






        Transform pushObstacles = transform.parent.transform.Find("PushObstacles");
        if (pushObstacles != null)
        {

            if (pushObstacles.GetChild(0).TryGetComponent<Box>(out Box box))
            {
                box.HasTouchedWallObstacle = false;
                if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~RigidbodyConstraints.FreezePositionX;
                }
                box.transform.localPosition = new Vector3(Random.Range(0f, 5f), box.transform.localPosition.y, Random.Range(-3f,3f));

                //Charcter spawn location and rotation
                characterController.enabled = false;
                characterController.transform.localPosition = new Vector3(Random.Range(-5f, box.transform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                characterController.enabled = true;

                if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                {
                    wallObstacle.gameObject.SetActive(true);
                    wallObstacle.transform.localPosition = new Vector3(box.transform.localPosition.x + 2f, wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                    goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.transform.localPosition.x + 2f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];


                }
            }


        }
    }

    private void SmartAgent_PushBox_lvl3()
    {
        // agent on fixed position, fixed rotation
        // target on fixed positon
        // push box on random position on Z axis, close to WallObstacle






        Transform pushObstacles = transform.parent.transform.Find("PushObstacles");
        if (pushObstacles != null)
        {

            if (pushObstacles.GetChild(0).TryGetComponent<Box>(out Box box))
            {
                box.HasTouchedWallObstacle = false;
                if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~RigidbodyConstraints.FreezePositionX;
                }
                box.transform.localPosition = new Vector3(Random.Range(0f, 25f), box.transform.localPosition.y, Random.Range(-3f, 3f));

                //Charcter spawn location and rotation
                characterController.enabled = false;
                characterController.transform.localPosition = new Vector3(Random.Range(-5f, box.transform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                characterController.enabled = true;

                if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                {
                    wallObstacle.gameObject.SetActive(true);
                    wallObstacle.transform.localPosition = new Vector3(Random.Range(box.transform.localPosition.x + 2f,30f), wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                    goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.transform.localPosition.x + 2f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];


                }
            }


        }
    }

    private void SmartAgent_PushBox_lvl4()
    {
        // agent on fixed position, fixed rotation
        // target on fixed positon
        // push box on random position on Z axis, close to WallObstacle


        int randomNumber = Random.Range(0, 2);


        if (randomNumber == 0)
        {
            Transform pushObstacles = transform.parent.transform.Find("PushObstacles");
            if (pushObstacles != null)
            {

                if (pushObstacles.GetChild(0).TryGetComponent<Box>(out Box box))
                {
                    box.HasTouchedWallObstacle = false;
                    if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                    {
                        boxRigidbody.constraints = ~RigidbodyConstraints.FreezePositionX;
                    }
                    box.transform.localPosition = new Vector3(Random.Range(0f, 25f), box.transform.localPosition.y, Random.Range(-3f, 3f));

                    //Charcter spawn location and rotation
                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(Random.Range(-5f, box.transform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                    characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    characterController.enabled = true;

                    if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                    {
                        wallObstacle.gameObject.SetActive(true);
                        wallObstacle.transform.localPosition = new Vector3(Random.Range(box.transform.localPosition.x + 2f, 30f), wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                        goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.transform.localPosition.x + 2f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
                        targetTransform.localPosition = goalSpawnPositions[goalCounter];


                    }
                }
            }
        }
        else
        {
            Transform pushObstacles = transform.parent.transform.Find("PushObstacles");
            if (pushObstacles != null)
            {

                if (pushObstacles.GetChild(0).TryGetComponent<Box>(out Box box))
                {
                    box.HasTouchedWallObstacle = false;
                    if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                    {
                        boxRigidbody.constraints = ~RigidbodyConstraints.FreezePositionX;
                    }
                    box.transform.localPosition = new Vector3(Random.Range(0f, 30f), box.transform.localPosition.y, Random.Range(-3f, 3f));

                    //Charcter spawn location and rotation
                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(Random.Range(box.transform.localPosition.x + 2f,33f), 0.55f, Random.Range(-3.3f, 3.2f));
                    characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    characterController.enabled = true;

                    if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                    {
                        wallObstacle.gameObject.SetActive(true);
                        wallObstacle.transform.localPosition = new Vector3(Random.Range(0f,box.transform.localPosition.x - 2f), wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                        goalSpawnPositions[0] = new Vector3(Random.Range(-4f,wallObstacle.transform.localPosition.x -2f), 0.85f, Random.Range(-3.3f, 3.2f));
                        targetTransform.localPosition = goalSpawnPositions[goalCounter];


                    }
                }
            }
        }



        
    }


    private void SmartAgent_Collectable_lvl1()
    {
        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on fixed position

        collected = 0;

        

        Transform collectables = transform.parent.transform.Find("Collectables");
        if (collectables != null)
        {
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            wallObstacle.localPosition = new Vector3(Random.Range(0, 25f), wallObstacle.localPosition.y, wallObstacle.localPosition.z);

            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.localPosition.x + 2f, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];



            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
            for (int i = 0; i < collectables.childCount; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                    collectable.localPosition = new Vector3(Random.Range(characterController.transform.localPosition.x -1f, characterController.transform.localPosition.x +1f), collectable.localPosition.y, Random.Range(-3.3f, 3.2f));
                     
                }
            }
        }


    }

    private void SmartAgent_Collectable_lvl1_onAction()
    {
        if(collected== collectableCount)
        {
            Transform collectables = transform.parent.transform.Find("Collectables");
            if (collectables != null)
            {
                Transform wallObstacle = collectables.GetChild(0);
                wallObstacle.gameObject.SetActive(false);
            }
        }
    }

    private void SmartAgent_Collectable_lvl2()
    {
        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on fixed position

        collected = 0;



        Transform collectables = transform.parent.transform.Find("Collectables");
        if (collectables != null)
        {
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            wallObstacle.localPosition = new Vector3(Random.Range(0, 25f), wallObstacle.localPosition.y, wallObstacle.localPosition.z);

            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.localPosition.x + 2f, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];



            //Charcter spawn location and rotation
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
            for (int i = 0; i < collectables.childCount; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                    collectable.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), collectable.localPosition.y, Random.Range(-3.3f, 3.2f));

                }
            }
        }


    }

    private void SmartAgent_Maze_lvl1()
    {
        // agent on random position, random rotation
        // target on random positon
        // same maze obstacle between agent and goal

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(Random.Range(17f, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];



        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-4f, -1f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;


    }

    private void CheckpointsSpawn()
    {
        checkpointsSpawnPositions[0] = new Vector3(2.90f, 1.44f, 2.8f);
        checkpointsSpawnPositions[1] = new Vector3(4.46f, 1.44f, -3.5f);
        checkpointsSpawnPositions[2] = new Vector3(5.52f, 1.44f, -0.23f);
        checkpointsSpawnPositions[3] = new Vector3(14.29f, 1.44f, -0.23f);
        checkpointsSpawnPositions[4] = new Vector3(15.73f, 1.44f, -3.21f);
        
    }
    private void NextCheckPoint()
    {
        Transform mazes = transform.parent.transform.Find("Mazes");
        
        if (mazes != null)
        {
            
            for (int j=0; j<mazes.childCount; j++)
            {
                if (mazes.GetChild(j).gameObject.activeSelf == true)
                {
                    Transform maze = mazes.GetChild(j);
                    for (int i = 0; i < maze.childCount; i++)
                    {
                        if (maze.GetChild(i).gameObject.CompareTag("Checkpoint") && maze.GetChild(i).gameObject.name == "CheckpointPath_" + checkpointsCounter)
                        {
                            maze.GetChild(i).gameObject.SetActive(true);
                        }
                    }
                    break;
                }
            }
            
            
        }
        
    }
    private void SmartAgent_Maze_lvl2()
    {
        // agent on random position, random rotation
        // target on random positon
        // random maze obstacle between agent and goal

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(Random.Range(17f, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];



        int mazeNumber = Random.Range(0, 2);
        checkpointsCounter = 1;
        
        switch (mazeNumber)
        {
            case 0:
                {
                    Transform mazes = transform.parent.transform.Find("Mazes");
                    if (mazes != null)
                    {
                        for (int i = 0; i < mazes.childCount; i++)
                        {
                            mazes.GetChild(i).gameObject.SetActive(false);
                        }
                    }
                    Transform maze = mazes.GetChild(0);

                    maze.gameObject.SetActive(true);
                    for (int i = 0; i < maze.childCount; i++)
                    {
                        if (maze.GetChild(i).gameObject.CompareTag("Checkpoint"))
                        {
                            maze.GetChild(i).gameObject.SetActive(false);
                        }
                    }
                    NextCheckPoint();
                }
                break;
            case 1:
                {
                    
                    Transform mazes = transform.parent.transform.Find("Mazes");
                    if (mazes != null)
                    {
                        for (int i = 0; i < mazes.childCount; i++)
                        {
                            mazes.GetChild(i).gameObject.SetActive(false);
                        }
                    }
                    Transform maze = mazes.GetChild(1);

                    maze.gameObject.SetActive(true);
                    for (int i = 0; i < maze.childCount; i++)
                    {
                        if (maze.GetChild(i).gameObject.CompareTag("Checkpoint"))
                        {
                            maze.GetChild(i).gameObject.SetActive(false);
                        }
                    }
                    NextCheckPoint();
                }
                break;
            case 2:
                {
                    Transform mazes = transform.parent.transform.Find("Mazes");
                    if (mazes != null)
                    {
                        for (int i = 0; i < mazes.childCount; i++)
                        {
                            mazes.GetChild(i).gameObject.SetActive(false);
                        }
                    }
                    Transform maze = mazes.GetChild(2);
                    maze.gameObject.SetActive(true);
                    for (int i = 0; i < maze.childCount; i++)
                    {
                        if (maze.GetChild(i).gameObject.CompareTag("Checkpoint"))
                        {
                            maze.GetChild(i).gameObject.SetActive(true);
                        }
                    }
                }
                break;
            case 3:
                {
                    Transform mazes = transform.parent.transform.Find("Mazes");
                    if (mazes != null)
                    {
                        for (int i = 0; i < mazes.childCount; i++)
                        {
                            mazes.GetChild(i).gameObject.SetActive(false);
                        }
                    }
                    Transform maze = mazes.GetChild(3);
                    maze.gameObject.SetActive(true);
                    for (int i = 0; i < maze.childCount; i++)
                    {
                        if (maze.GetChild(i).gameObject.CompareTag("Checkpoint"))
                        {
                            maze.GetChild(i).gameObject.SetActive(true);
                        }
                    }
                }
                break;
            default: return;
        }

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-4f, -1f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;


    }

    private void SmartAgent_PushButton_lvl1()
    {
        // agent on random position, random rotation
        // target on random positon
        // button close to agent, fixed position

        isButtonPressed = false;

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(Random.Range(0, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];
        targetTransform.gameObject.SetActive(false);



        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-4f, -1f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

        Transform pushButton = transform.parent.transform.Find("PushButton");
        if(pushButton != null)
        {
            Transform button = pushButton.Find("Button");
            if(button != null)
                button.localPosition = new Vector3(characterController.transform.localPosition.x+2f, button.localPosition.y, characterController.transform.localPosition.z);
        }


    }

    private void SmartAgent_PushButton_lvl2()
    {
        // agent on random position, random rotation
        // target on random positon
        // button close to agent, fixed position

        isButtonPressed = false;

        



        

        Transform pushButton = transform.parent.transform.Find("PushButton");
        if (pushButton != null)
        {
            Transform wallObstacle = pushButton.Find("WallObstacle");
            if (wallObstacle != null)
            {
                wallObstacle.gameObject.SetActive(true);
                wallObstacle.localPosition = new Vector3(Random.Range(-4f, 30f), wallObstacle.localPosition.y, wallObstacle.localPosition.z);
                //Charcter spawn location and rotation
                characterController.enabled = false;
                characterController.transform.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                characterController.enabled = true;

                //goal spawn location
                goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.localPosition.x + 2f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
                targetTransform.localPosition = goalSpawnPositions[goalCounter];

            }
            Transform button = pushButton.Find("Button");
            if (button != null)
            {
                Transform buttonCheckpoint = button.Find("ButtonCheckpoint");
                buttonCheckpoint.gameObject.SetActive(true);
                button.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), button.localPosition.y, Random.Range(-3.3f, 3.2f));
                
            }
            
        }


    }
}
