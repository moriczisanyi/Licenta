using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEditor;
using System.IO;
using Unity.Barracuda;

public class RLAgent : Agent
{

    //Observations

    //MoveToCheckpoint
    [SerializeField] private Transform targetTransform;

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
    [SerializeField] private float gravity;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float bigJumpHeight;
    private float canJumpTime;
    private CharacterController characterController;
    private Animator animator;
    private bool isStandingBlocked = false;

    [SerializeField] private bool isGrounded;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask ignoredLayersDownwardRay;
    [SerializeField] private LayerMask ignoredLayersForwardRay;
    

    [SerializeField] private float pushForceMagnitude;




    //brains
    [SerializeField] public NNModel goToGoalBrain;
    [SerializeField] private NNModel crouchBrain;
    [SerializeField] private NNModel jumpBrain; 
    [SerializeField] private NNModel pushBoxBrain;
    [SerializeField] private NNModel pushButtonBrain;
    [SerializeField] private NNModel collectableBrain;
    [SerializeField] private NNModel mazeBrain;
    [SerializeField] private NNModel movingObjectsBrain;

    public NNModel currentBrain;

    float rewardTimeJump = 0;
    private int goalCountMax = 1;
    private int goalCounter = 0;
    private int currentNode = -1;
    Vector3[] goalSpawnPositions = new Vector3[20];
    Vector3[] checkpointsSpawnPositions = new Vector3[5];


    //Maze
    private int mazeNumber;
    private int mazeCheckpointsCount;
    private int mazeCheckpointsCounter = 0;
    private bool mazeBrainActive = false;

    //collectable
    private int collectableCount = 100;
    private int collected = 0;
    private int collectableCount_edge_0_1=100;
    private int collected_edge_0_1;
    private int collectableCount_edge_1_2 = 100;
    private int collected_edge_1_2;
    private int collectableCount_edge_2_3 = 100;
    private int collected_edge_2_3;
    private int collectableCount_edge_3_0 = 100;
    private int collected_edge_3_0;

    //random Checkpoints
    private int randomCheckpointsCount;
    private int randomCheckpointsCounter = 0;

    private bool isButtonPressed = false;

    private Vector3 dirToTarget;
    List<int> path = new List<int>();
    private bool _training = false;
    private bool _startAgentMap2 = false;
    private bool _startAgentMap1 = false;
    private int _gameModeIndex;
    private int _startNode;
    private int _endNode;

    public int episodeCounter;
    public int successfulEpisodeCounter;


    public override void Initialize()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        currentBrain = goToGoalBrain;
        episodeCounter = 0;
        successfulEpisodeCounter = 0;
        

    }
    public override void OnEpisodeBegin()
    {
        currentBrain = goToGoalBrain;
        if (_startAgentMap2)
        {
            Map_2(startNode:_startNode,endNode:_endNode);   
        }
        if (_startAgentMap1)
        {
            Map_1(startNode: _startNode, endNode: _endNode);
        }
        /*switch (_gameModeIndex)
        {
            case 0:
                {
                    SmartAgent_MoveToGoal_Lvl_5(lvl: 4, isModelActive:true);
                    break;
                }
            default: break;
        }*/


        //RLAgent_MazeBigField();
        //SmartAgent_Collectable_lvl1(4, true, 3);
        //RLAgent_RandomCheckpoints(checkpointNumber:5, false, lvl:3);
        //RLAgent_Maze(endGoal:false, lvl:3);
        //RLAgent_PushButton(endGoal:true,lvl:2);
        //RLAgent_MovingObjects(lvl:2);


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
        //Debug.Log("succesfulEpisodeCounter: " + succesfulEpisodeCounter);
        //Debug.Log("episodeCounter: " + episodeCounter);
        RLAgent_Collectable_OnCollected(false);
        RLAgent_Collectable_OnCollected_Map2();
        if (_training)
        {
            WallRay();
            DownRay(actions);
            ForwardRay(actions);
            CrouchRay();
        }


        dirToTarget = targetTransform.localPosition - characterController.transform.localPosition;
        if(targetTransform.gameObject.activeSelf)
            AddReward(
            +0.003f * Vector3.Dot(dirToTarget.normalized, characterController.velocity) +
            +0.001f * Vector3.Dot(dirToTarget.normalized, characterController.transform.forward)
            );
        

        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        if(characterController.transform.localPosition.y < -1f)
        {
            ResetAgentToNode_0();
        }
        SetReward(-2f / MaxStep);

        Movement(actions);




    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {


        ActionSegment<int> actions = actionsOut.DiscreteActions;
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
        if (other.CompareTag("O1"))
        {
            SetReward(+1f);
            
            other.gameObject.SetActive(false);
            //EndEpisode(); //moving objects
        }
        if (other.TryGetComponent<Goal>(out Goal goal))
        {
            if (goalCounter == goalCountMax - 1)
            {
                currentNode = -1;
                SetReward(+2f);
                successfulEpisodeCounter++;
                EndEpisode();
                
            }
            else
            {
                currentNode = path[goalCounter];
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
        if (other.CompareTag("Checkpoint"))
        {
            SetReward(+1f);
            other.gameObject.SetActive(false);
             
            randomCheckpointsCounter++;
            mazeCheckpointsCounter++;
            MazeNextCheckpoint();
            RandomNextCheckPoint();
        }
        if (other.CompareTag("Checkpoint") && other.gameObject.name=="JumpReward")
        {
            currentBrain = goToGoalBrain;
            SetModel("current", currentBrain);
        }
        if (other.CompareTag("Collectable"))
        {
            SetReward(+0.5f); //from +2f
            if (other.transform.parent.name == "0_1")
            {
                collected_edge_0_1++;
            }
            if (other.transform.parent.name == "1_2")
                collected_edge_1_2++;
            if (other.transform.parent.name == "2_3")
                collected_edge_2_3++;
            if (other.transform.parent.name == "3_0")
                collected_edge_3_0++;
            collected++;
            other.gameObject.SetActive(false);
        }

    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Box") == true && isGrounded)
        {
            Rigidbody rigidbody = hit.collider.attachedRigidbody;
            if (rigidbody != null && !rigidbody.isKinematic)
            {
                if(hit.transform.TryGetComponent<Box>(out Box box)){
                    if(!box.HasTouchedAgent)
                    {
                        SetReward(+1f); // +2 for lvl1
                        box.HasTouchedAgent = true;
                        //EndEpisode();
                    }
                }
                rigidbody.velocity = hit.moveDirection * moveSpeed;

            }
        }
    }
    private void SpawnGoal()
    {
        //goal spawn location

        targetTransform.gameObject.SetActive(true);
        targetTransform.localPosition = goalSpawnPositions[goalCounter];
        characterController.enabled = false;
        characterController.transform.LookAt(targetTransform);
        characterController.enabled = true;
        Transform goalTarget = transform.parent.Find("GoalTarget");
        goalTarget.gameObject.SetActive(true);
        goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);
    }
    public void ResetAgentToNode_0()
    {
        Transform node = transform.parent.Find("Nodes").transform.Find("0");
        if (node != null)
        {
            characterController.enabled = false;
            characterController.transform.localPosition = new Vector3(node.localPosition.x, 0.55f, node.localPosition.z);
            characterController.transform.LookAt(targetTransform);
            characterController.enabled = true;
        }
    }
    private void Movement(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        if (actions.DiscreteActions[3] == 1)
        {
            Collider[] colliderArray = Physics.OverlapBox(transform.position, Vector3.one * 0.1f);
            foreach (Collider collider in colliderArray)
            {
                if (collider.TryGetComponent<Button>(out Button button) && !isButtonPressed)
                {
                    isButtonPressed = true;
                    
                    Transform pushButton = transform.parent.transform.Find("PushButton");
                    if (pushButton != null)
                    {
                        Transform wallObstacle = pushButton.Find("WallObstacle");
                        if (wallObstacle != null)
                            wallObstacle.gameObject.SetActive(false);
                    }
                    SetReward(+1f);
                    //EndEpisode();
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



                    if (actions.DiscreteActions[0] == 1 && Time.time > canJumpTime && actions.DiscreteActions[1] == 0)
                    {
                        Jump();
                        
                        canJumpTime = Time.time + 1.0f;
                    }
                    else if (actions.DiscreteActions[0] == 1 && Time.time > canJumpTime && actions.DiscreteActions[1] == 1)
                    {
                        BigJump();
                        canJumpTime = Time.time + 1.0f;
                    }
                }
            }

            if (moveZ < 0)
                moveDirection *= moveSpeed / 4f;
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
    private void WallRay()
    {
        if(currentBrain == goToGoalBrain)
        {
            Vector3 D = targetTransform.localPosition - characterController.transform.localPosition;
            Quaternion rot = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(D), 2f * Time.deltaTime);
            transform.localRotation = rot;
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }
        RaycastHit wallRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, 0f, 2f)), Color.blue);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(2f, 0f, 2f)), Color.blue);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-2f, 0f, 2f)), Color.blue);
        if ((Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, 0, 1f)), out wallRay, 1f, ~wallMask)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(2f, 0f, 1f)), out wallRay, 1f, ~wallMask)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-2f, 0f, 1f)), out wallRay, 1f, ~ignoredLayersDownwardRay))
            )
        {
            if (wallRay.collider.gameObject.tag == "Wall")
            {

                Vector3 D = targetTransform.localPosition - characterController.transform.localPosition;
                Quaternion rot = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(D), 2f * Time.deltaTime);
                transform.localRotation = rot;
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            }
        }
    }
    private void DownRay(ActionBuffers actions)
    {
        
        RaycastHit downRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -1f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -1f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -1f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -1f, 7f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -1f, 7f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -1f, 7f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -2f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -2f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -2f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -3f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -3f, 5f)), Color.red);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -3f, 5f)), Color.red);

        if (
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -1f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -1f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -2f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -2f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -2f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -1f, 7f)), out downRay, 7f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -1f, 7f)), out downRay, 7f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -1f, 7f)), out downRay, 7f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -3f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-1f, -3f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -3f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(1f, -1f, 5f)), out downRay, 5f, ~ignoredLayersDownwardRay))
            )
        {
            if (downRay.collider.gameObject.CompareTag("Lava"))
            {

                if (actions.DiscreteActions[0] == 1 && Time.time > rewardTimeJump)
                {
                    rewardTimeJump = Time.time + 1.0f;
                    SetReward(0.05f);
                }
                currentBrain = jumpBrain;
                SetModel("current", currentBrain);
            }
            if (downRay.collider.gameObject.CompareTag( "Button"))
            {
                currentBrain = pushButtonBrain;
                SetModel("current", currentBrain);
            }

        }
    }
    private void ForwardRay(ActionBuffers actions)
    {

        RaycastHit forwardRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -0.5f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(2f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-2f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(3f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-3f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(5f, 0f, 5f)), Color.yellow);
        Debug.DrawRay(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-5f, 0f, 5f)), Color.yellow);
        if (
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, -0.5f, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(5f, 0f, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-5f, 0f, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(0f, 0, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(2f, 0f, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-2f, 0f, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(3f, 0f, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay)) ||
            (Physics.Raycast(transform.position + Vector3.up * 0.4f, transform.TransformDirection(new Vector3(-3f, 0f, 5f)), out forwardRay, 5f, ~ignoredLayersForwardRay))
            )
        {
            if (forwardRay.collider.gameObject.CompareTag("O1") && currentBrain != mazeBrain)
            {
                currentBrain = goToGoalBrain;
                SetModel("current", currentBrain);
            }
            if (forwardRay.collider.gameObject.CompareTag("Box"))
            {
                currentBrain = pushBoxBrain;
                SetModel("current", currentBrain);
            }
            if (forwardRay.collider.gameObject.CompareTag("Collectable"))
            {
                currentBrain = collectableBrain;
                SetModel("current", currentBrain);
            }
            if (forwardRay.collider.gameObject.CompareTag("MovingObjects"))
            {
                currentBrain = movingObjectsBrain;
                SetModel("current", currentBrain);
            }
            if (forwardRay.collider.gameObject.CompareTag("Checkpoint") && forwardRay.collider.gameObject.transform.parent.name == "MazeCheckpoints3" && !mazeBrainActive)
            {

                Transform maze = transform.parent.Find("Mazes").transform.GetChild(mazeNumber - 1);
                if (maze != null)
                {
                    maze.GetChild(11).gameObject.SetActive(true);
                    maze.GetChild(12).gameObject.SetActive(true);
                }

                currentBrain = mazeBrain;
                SetModel("current", currentBrain);
                mazeBrainActive = true;
            }

        }
    }
    private void CrouchRay()
    {
        RaycastHit crouchRay;
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, transform.TransformDirection(new Vector3(0f, 1, 3f)), Color.blue);
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, transform.TransformDirection(new Vector3(0f, 1, 3f)), out crouchRay, 5f, ~ignoredLayersDownwardRay))
        {
            if (crouchRay.collider.gameObject.tag == "CrouchObstacle")
            {
                currentBrain = crouchBrain;
                SetModel("current", currentBrain);

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

        //SpawnGoal();


        //lava spawn

        //lavaTransform.localPosition = new Vector3(10f, lavaTransform.localPosition.y, lavaTransform.localPosition.z);
        if (lavaTransform.TryGetComponent<Lava>(out Lava lava))
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
    public void Map_1(int startNode, int endNode)
    {
        _training=true;
        episodeCounter++;
        SetModel("current", currentBrain);
        goalCounter = 0;
        Transform nodes = transform.parent.Find("Nodes");
        
        Graph g = new Graph(nodes.childCount);

        g.addEdge(0, 2);
        g.addEdge(0, 3);
        g.addEdge(0, 4);
        g.addEdge(1, 2);
        g.addEdge(2, 0);
        g.addEdge(2, 1);
        g.addEdge(2, 4);
        g.addEdge(3, 1);
        g.addEdge(4, 0);
        g.addEdge(4, 2);
        g.addEdge(4, 5);
        g.addEdge(4, 6);
        g.addEdge(5, 4);
        g.addEdge(6, 5);
        Transform node;
        /*if (currentNode == -1)
        {
            g.printAllPaths(startNode, endNode);
            node = nodes.Find(startNode.ToString());
        }
        else
        {
            g.printAllPaths(currentNode, endNode);
            node = nodes.Find(currentNode.ToString());
        }*/
        g.printAllPaths(startNode, endNode);
        node = nodes.Find(startNode.ToString());
        
        //string pathToFile = "Assets/Scripts/test.txt";
        string pathToFile = Application.streamingAssetsPath + "/test.txt";
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(pathToFile);
        int min = 99;
        string pathToNode="";
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            if (line.Length < min)
            {
                min = line.Length;
                pathToNode = line;
            }
        }
        path.Clear();
        for(int i = 1; i < pathToNode.Length; i++)
        {
            if (char.IsDigit(pathToNode[i]))
            {
                path.Add(int.Parse(pathToNode[i].ToString()));
            }
        }
        Transform[] nodesOnPath = new Transform[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            nodesOnPath[i] = nodes.Find(path[i].ToString());
            goalSpawnPositions[i] = new Vector3(nodesOnPath[i].localPosition.x, targetTransform.localPosition.y, nodesOnPath[i].localPosition.z);
        }
        reader.Close();

        goalCountMax = path.Count;




        Edge_0_4();
        Edge_4_6();
        Edge_6_5();
        Edge_1_3();
        Edge_0_3(maze_Number: 3);
        Edge_1_2();
        SpawnGoal();
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(node.localPosition.x, 0.55f, node.localPosition.z);
        characterController.transform.LookAt(targetTransform);
        characterController.enabled = true;


    }

    public void Map_2(int startNode, int endNode)
    {
        _training = true;
        episodeCounter++;
        SetModel("current", currentBrain);
        goalCounter = 0;
        Transform nodes = transform.parent.Find("Nodes");

        Graph g = new Graph(nodes.childCount);

        g.addEdge(0, 1);
        g.addEdge(0, 4);
        g.addEdge(0, 6);
        g.addEdge(1, 2);
        g.addEdge(2, 3);
        g.addEdge(3, 0);
        g.addEdge(4, 0);
        g.addEdge(4, 5);
        g.addEdge(5, 4);
        g.addEdge(5, 6);
        g.addEdge(6, 5);
        g.addEdge(6, 0);
        Transform node;
        g.printAllPaths(startNode, endNode);
        node = nodes.Find(startNode.ToString());
        string pathToFile = Application.streamingAssetsPath + "/test.txt";
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(pathToFile);
        int min = 99;
        string pathToNode = "";
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            if (line.Length < min)
            {
                min = line.Length;
                pathToNode = line;
            }
        }
        path.Clear();
        for (int i = 1; i < pathToNode.Length; i++)
        {
            if (char.IsDigit(pathToNode[i]))
            {
                path.Add(int.Parse(pathToNode[i].ToString()));
            }
        }
        Transform[] nodesOnPath = new Transform[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            nodesOnPath[i] = nodes.Find(path[i].ToString());
            goalSpawnPositions[i] = new Vector3(nodesOnPath[i].localPosition.x, targetTransform.localPosition.y, nodesOnPath[i].localPosition.z);
        }
        reader.Close();

        goalCountMax = path.Count;




        Edge_0_1_Map2();
        Edge_1_2_Map2();
        Edge_2_3_Map2();
        Edge_3_0_Map2();
        SpawnGoal();
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(node.localPosition.x, 0.55f, node.localPosition.z);
        characterController.transform.LookAt(targetTransform);
        characterController.enabled = true;


    }

    private void RLAgent_MoveToGoal_Lvl_1()
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

    private void RLAgent_MoveToGoal_Lvl_2()
    {
        // agent on random position, random rotation
        // target on fixed positon

        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-5f, 15f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(0f, 0.85f, 0f);
        targetTransform.localPosition = goalSpawnPositions[goalCounter];

    }

    private void RLAgent_MoveToGoal_Lvl_3()
    {
        // agent on random position, random rotation
        // target on random positon

        //Charcter spawn location and rotation
        goalCountMax = 1;
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(Random.Range(-5f, 33f), 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(Random.Range(-5f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];

    }

    private void RLAgent_MoveToGoal_Lvl_4()
    {
        // agent on random position, random rotation
        // target on random positon

        //Charcter spawn location and rotation
        goalCountMax = 1;
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(-5f, 0.55f, Random.Range(-3.3f, 3.2f));
        characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        characterController.enabled = true;

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(33f, 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];

    }
    private void RLAgent_MoveToGoal_Lvl_5(int lvl, bool isModelActive)
    {

        goalCountMax = 1;
        if (isModelActive)
        {
            episodeCounter++;
            SetModel("current", goToGoalBrain);
        }


        targetTransform.gameObject.SetActive(true);
        Transform goalTarget = transform.parent.Find("GoalTarget");
        goalTarget.gameObject.SetActive(true);
        
        switch (lvl)
        {
            case 1:
                {
                    goalSpawnPositions[0] = new Vector3(-5f, 0.85f, 0f);
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];
                    goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);

                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(-8f, 0.55f, 0f);
                    characterController.transform.LookAt(targetTransform);
                    characterController.enabled = true;
                }
                break;
            case 2:
                {
                    goalSpawnPositions[0] = new Vector3(-5f, 0.85f, 0f);
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];
                    goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);

                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(Random.Range(-10f, 0f), 0.55f, 0f);
                    characterController.transform.LookAt(targetTransform);
                    characterController.enabled = true;
                }
                break;
            case 3:
                {
                    goalSpawnPositions[0] = new Vector3(Random.Range(-10f, 10f), 0.85f, Random.Range(-5f, 5f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];
                    goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);

                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(Random.Range(-10f, 0f), 0.55f, 0f);
                    characterController.transform.LookAt(targetTransform);
                    characterController.enabled = true;
                }
                break;
            case 4:
                {
                    goalSpawnPositions[0] = new Vector3(Random.Range(-10f, 38f), 0.85f, Random.Range(-24f, 24f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];
                    goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);

                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(Random.Range(-10f, 38f), 0.55f, Random.Range(-24f, 24f));
                    characterController.transform.LookAt(targetTransform);
                    characterController.enabled = true;
                }
                break;
        }
        
    }
    private void RLAgent_Jump_lvl1()
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
    private void RLAgent_Jump_lvl2()
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
    private void RLAgent_Jump_lvl3()
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
            goalSpawnPositions[0] = new Vector3(Random.Range(lavaTransform.localPosition.x + 2f, 15f), 0.85f, Random.Range(-3.3f, 3.2f));
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
    private void RLAgent_Jump_lvl4()
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
    private void RLAgent_Jump_dinamic()
    {

        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on random position, anywhere

        int randomNumber = Random.Range(0, 2);

        //obstacle
        Transform lava = transform.parent.transform.Find("LavaObstacles").Find("Lava");   
        if(lava != null)
        {
            lava.GetComponent<Lava>().MovingLava = Random.Range(0,2)==1 ? true : false;
            lava.localPosition = new Vector3(Random.Range(5f, 25f), lava.localPosition.y, lava.localPosition.z);
            Transform lavaTrigger1 = transform.parent.transform.Find("LavaObstacles").Find("LavaTrigger1");
            Transform lavaTrigger2 = transform.parent.transform.Find("LavaObstacles").Find("LavaTrigger2");
            if(lavaTrigger1 != null && lavaTrigger2 != null)
            {
                lavaTrigger1.localPosition = new Vector3(lava.localPosition.x - 5f, lavaTrigger1.localPosition.y, lavaTrigger1.localPosition.z);
                lavaTrigger2.localPosition = new Vector3(lava.localPosition.x + 5f, lavaTrigger2.localPosition.y, lavaTrigger2.localPosition.z);

            }
            if (randomNumber == 0)
            {
                //goal spawn location
                goalSpawnPositions[0] = new Vector3(Random.Range(lava.localPosition.x + 2f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
                targetTransform.localPosition = goalSpawnPositions[goalCounter];

                //Charcter spawn location and rotation
                characterController.enabled = false;
                characterController.transform.localPosition = new Vector3(Random.Range(-5f, lava.localPosition.x - 0.5f), 0.55f, Random.Range(-3.3f, 3.2f));
                characterController.transform.LookAt(targetTransform);
                characterController.enabled = true;
            }
            else
            {
                //goal spawn location
                goalSpawnPositions[0] = new Vector3(Random.Range(-5f, lava.localPosition.x - 2f), 0.85f, Random.Range(-3.3f, 3.2f));
                targetTransform.localPosition = goalSpawnPositions[goalCounter];

                //Charcter spawn location and rotation
                characterController.enabled = false;
                characterController.transform.localPosition = new Vector3(Random.Range(lava.localPosition.x + 0.5f, 33f), 0.55f, Random.Range(-3.3f, 3.2f));
                characterController.transform.LookAt(targetTransform);
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
        


    }
    private void RLAgent_Crouch_lvl1()
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
    private void RLAgent_Crouch_lvl2()
    {


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
    private void RLAgent_Crouch_lvl3()
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
            goalSpawnPositions[0] = new Vector3(Random.Range(crouchObstacleTransform.localPosition.x + 2f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
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
            characterController.transform.localPosition = new Vector3(Random.Range(crouchObstacleTransform.localPosition.x + 2f, 33f), 0.55f, Random.Range(-3.3f, 3.2f));
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
        }

    }
    private void RLAgent_PushBox_lvl1()
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
                box.HasTouchedAgent = false;
                if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~RigidbodyConstraints.FreezePosition;
                }
                box.transform.localPosition = new Vector3(Random.Range(0f, 5f), box.transform.localPosition.y, -0.2f);

                //Charcter spawn location and rotation
                characterController.enabled = false;
                characterController.transform.localPosition = new Vector3(Random.Range(-5f, box.transform.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                characterController.enabled = true;

                if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                {
                    wallObstacle.gameObject.SetActive(true);
                    wallObstacle.transform.localPosition = new Vector3(box.transform.localPosition.x + 2f, wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                    targetTransform.gameObject.SetActive(false);


                }
            }


        }
    }
    private void RLAgent_PushBox_lvl2()
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
                box.HasTouchedAgent = false;
                if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~RigidbodyConstraints.FreezePosition;
                }
                box.transform.localPosition = new Vector3(Random.Range(0f, 5f), box.transform.localPosition.y, Random.Range(-3f, 3f));

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
    private void RLAgent_PushBox_lvl3()
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
                box.HasTouchedAgent = false;
                if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~RigidbodyConstraints.FreezePosition;
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
                    wallObstacle.transform.localPosition = new Vector3(Random.Range(box.transform.localPosition.x + 2f, box.transform.localPosition.x + 7f), wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                    goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.transform.localPosition.x + 2f, 33f), 0.85f, Random.Range(-3.3f, 3.2f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];


                }
            }


        }
    }
    private void RLAgent_PushBox_lvl4()
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
                    box.HasTouchedAgent = false;
                    box.transform.gameObject.SetActive(true);
                    if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                    {
                        boxRigidbody.constraints = ~RigidbodyConstraints.FreezePosition;
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
                        wallObstacle.transform.localPosition = new Vector3(Random.Range(box.transform.localPosition.x + 2f, box.transform.localPosition.x + 4f), wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
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
                    box.transform.gameObject.SetActive(true);
                    if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                    {
                        boxRigidbody.constraints = ~RigidbodyConstraints.FreezePosition;
                    }
                    box.transform.localPosition = new Vector3(Random.Range(0f, 30f), box.transform.localPosition.y, Random.Range(-3f, 3f));

                    //Charcter spawn location and rotation
                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(Random.Range(box.transform.localPosition.x + 2f, 33f), 0.55f, Random.Range(-3.3f, 3.2f));
                    characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    characterController.enabled = true;

                    if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                    {
                        wallObstacle.gameObject.SetActive(true);
                        wallObstacle.transform.localPosition = new Vector3(Random.Range(box.transform.localPosition.x - 2f, box.transform.localPosition.x - 4f), wallObstacle.transform.localPosition.y, wallObstacle.transform.localPosition.z);
                        goalSpawnPositions[0] = new Vector3(Random.Range(-4f, wallObstacle.transform.localPosition.x - 2f), 0.85f, Random.Range(-3.3f, 3.2f));
                        targetTransform.localPosition = goalSpawnPositions[goalCounter];


                    }
                }
            }
        }




    }
    private void RLAgent_Collectable(int cn, bool endGoal, int lvl)
    {
        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on fixed position
        collectableCount = cn;
        collected = 0;



        Transform collectables = transform.parent.transform.Find("Collectables");
        if (collectables != null)
        {
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            wallObstacle.localPosition = new Vector3(Random.Range(5f, 25f), wallObstacle.localPosition.y, wallObstacle.localPosition.z);

            //goal spawn location
            goalSpawnPositions[0] = new Vector3(Random.Range(wallObstacle.localPosition.x + 2f, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
            targetTransform.localPosition = goalSpawnPositions[goalCounter];
            targetTransform.gameObject.SetActive(endGoal);



            //Charcter spawn location and rotation
            characterController.enabled = false;
            switch (lvl)
            {
                case 1:
                    characterController.transform.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                    break;
                case 2:
                    characterController.transform.localPosition = new Vector3(Random.Range(0, wallObstacle.localPosition.x - 7f), 0.55f, Random.Range(-3.3f, 3.2f));
                    break;
                case 3:
                    characterController.transform.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                    break;
                default:
                    break;
            }
            
            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            characterController.enabled = true;
            for (int i = 1; i < cn+1; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                    switch (lvl)
                    {
                        case 1:
                            collectable.localPosition = new Vector3(Random.Range(characterController.transform.localPosition.x - 1f, characterController.transform.localPosition.x + 1f), collectable.localPosition.y, Random.Range(-3.3f, 3.2f));
                            break;
                        case 2:
                            collectable.localPosition = new Vector3(Random.Range(characterController.transform.localPosition.x - 5f, characterController.transform.localPosition.x + 5f), collectable.localPosition.y, Random.Range(-3.3f, 3.2f));
                            break;
                        case 3:
                            collectable.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), collectable.localPosition.y, Random.Range(-3.3f, 3.2f));
                            break;
                        default:
                            break;
                    }
                }
            }
        }


    }
    private void RLAgent_Collectable_OnCollected(bool endEpisode)
    {
        if (collected == collectableCount)
        {
            if (endEpisode)
                EndEpisode();
            else
            {
                Transform collectables = transform.parent.transform.Find("Collectables");
                if (collectables != null)
                {
                    Transform wallObstacle = collectables.GetChild(0);
                    wallObstacle.gameObject.SetActive(false);
                }
                currentBrain = goToGoalBrain;
                SetModel("current", currentBrain);
            }
        }
    }

    private void RLAgent_RandomCheckpoints(int checkpointNumber, bool endGoal, int lvl)
    {
        // agent on random position, random rotation
        // target on random positon
        // jump obstacle between agent and goal on fixed position
        
        randomCheckpointsCount = checkpointNumber;
        randomCheckpointsCounter = 0;



        Transform checkpoints = transform.parent.transform.Find("Checkpoints");
        if (checkpoints != null)
        {
            Transform randomCheckpoints = checkpoints.transform.Find("RandomCheckpoints");
            if(randomCheckpoints != null)
            {
                //goal spawn location
                goalSpawnPositions[0] = new Vector3(Random.Range(-4f, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
                targetTransform.localPosition = goalSpawnPositions[goalCounter];
                targetTransform.gameObject.SetActive(endGoal);

                //Charcter spawn location and rotation
                characterController.enabled = false;
                switch (lvl)
                {
                    case 1:
                        characterController.transform.localPosition = new Vector3(-4f, 0.55f, Random.Range(-3.3f, 3.2f));
                        break;
                    case 2:
                        characterController.transform.localPosition = new Vector3(Random.Range(1f, 33f), 0.55f, Random.Range(-3.3f, 3.2f));
                        break;
                    case 3:
                        characterController.transform.localPosition = new Vector3(Random.Range(-4f, 33f), 0.55f, Random.Range(-3.3f, 3.2f));
                        break;
                    default:
                        characterController.transform.localPosition = new Vector3(-4f, 0.55f, Random.Range(-3.3f, 3.2f));
                        break;
                }

                characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                characterController.enabled = true;
                for (int i = 0; i < randomCheckpoints.childCount; i++)
                {
                    if (randomCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                    {
                        Transform checkpoint = randomCheckpoints.GetChild(i);
                        checkpoint.gameObject.SetActive(false);
                        switch (lvl)
                        {
                            case 1:
                                checkpoint.localPosition = new Vector3(characterController.transform.localPosition.x + 1f, checkpoint.localPosition.y, checkpoint.localPosition.z);
                                break;
                            case 2:
                                checkpoint.localPosition = new Vector3(Random.Range(characterController.transform.localPosition.x - 5f, characterController.transform.localPosition.x + 5f), checkpoint.localPosition.y, Random.Range(-3.3f, 3.2f));
                                break;
                            case 3:
                                checkpoint.localPosition = new Vector3(Random.Range(-4f,33f), checkpoint.localPosition.y, Random.Range(-3.3f, 3.2f));
                                break;
                            default:
                                checkpoint.localPosition = new Vector3(characterController.transform.localPosition.x + 1f, checkpoint.localPosition.y, checkpoint.localPosition.z);
                                break;
                        }
                    }
                }

            }     
        }
        RandomNextCheckPoint();
    }

    private void RandomNextCheckPoint()
    {
        
        if (randomCheckpointsCounter == randomCheckpointsCount)
        {
            EndEpisode();
        }
        else
        {
            Transform checkpoints = transform.parent.transform.Find("Checkpoints");
            if (checkpoints != null)
            {
                Transform randomCheckpoints = checkpoints.transform.Find("RandomCheckpoints");
                if (randomCheckpoints != null)
                {

                    for (int i = 0; i < randomCheckpoints.childCount; i++)
                    {
                        if (randomCheckpoints.GetChild(i).gameObject.name == "Checkpoint" + (randomCheckpointsCounter + 1))
                        {
                            randomCheckpoints.GetChild(i).gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

    }
    private void MazeNextCheckpoint()
    {
        
        switch (mazeNumber)
        {
            case 1:
                {
                    if (mazeCheckpointsCounter == mazeCheckpointsCount)
                    {
                        currentBrain = goToGoalBrain;
                        SetModel("current", currentBrain);
                    }
                    else
                    {
                        Transform checkpoints = transform.parent.transform.Find("Checkpoints");
                        if (checkpoints != null)
                        {
                            Transform randomCheckpoints = checkpoints.transform.Find("MazeCheckpoints");
                            if (randomCheckpoints != null)
                            {

                                for (int i = 0; i < randomCheckpoints.childCount; i++)
                                {
                                    if (randomCheckpoints.GetChild(i).gameObject.name == "Checkpoint" + (mazeCheckpointsCounter + 1))
                                    {
                                        randomCheckpoints.GetChild(i).gameObject.SetActive(true);
                                    }
                                }
                            }
                        }
                    }
                }
                break;
            case 2:
                {
                    if (mazeCheckpointsCounter == mazeCheckpointsCount)
                    {
                        EndEpisode();
                    }
                    else
                    {
                        Transform checkpoints = transform.parent.transform.Find("Checkpoints");
                        if (checkpoints != null)
                        {
                            Transform randomCheckpoints = checkpoints.transform.Find("MazeCheckpoints2");
                            if (randomCheckpoints != null)
                            {

                                for (int i = 0; i < randomCheckpoints.childCount; i++)
                                {
                                    if (randomCheckpoints.GetChild(i).gameObject.name == "Checkpoint" + (mazeCheckpointsCounter + 1))
                                    {
                                        randomCheckpoints.GetChild(i).gameObject.SetActive(true);
                                    }
                                }
                            }
                        }
                    }
                }
                break;
            case 3:
                {
                    if (mazeCheckpointsCounter == mazeCheckpointsCount)
                    {
                        currentBrain = goToGoalBrain;
                        SetModel("current", currentBrain);
                    }
                    else
                    {
                        Transform checkpoints = transform.parent.transform.Find("Checkpoints");
                        if (checkpoints != null)
                        {
                            Transform randomCheckpoints = checkpoints.transform.Find("MazeCheckpoints3");
                            if (randomCheckpoints != null)
                            {

                                for (int i = 0; i < randomCheckpoints.childCount; i++)
                                {
                                    if (randomCheckpoints.GetChild(i).gameObject.name == "Checkpoint" + (mazeCheckpointsCounter + 1))
                                    {
                                        randomCheckpoints.GetChild(i).gameObject.SetActive(true);
                                        
                                    }
                                }
                            }
                        }
                    }
                }
                break;
            default:
                break;
        }
        

    }
    private void RLAgent_Maze(bool endGoal, int lvl)
    {
        // agent on random position, random rotation
        // target on random positon
        // random maze obstacle between agent and goal

        //goal spawn location
        goalSpawnPositions[0] = new Vector3(Random.Range(17f, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
        targetTransform.localPosition = goalSpawnPositions[goalCounter];
        targetTransform.gameObject.SetActive(endGoal);


        
        mazeCheckpointsCounter = 0;
        switch (lvl)
        {
            case 1:
                {
                    mazeNumber = 1;
                }
                break;
            case 2: mazeNumber = Random.Range(1, 3);
                break;
            case 3:
                    mazeNumber = Random.Range(1, 4);
                break;
            default: mazeNumber = 1;
                break;

        }

        Transform mazes = transform.parent.transform.Find("Mazes");
        if (mazes != null)
        {
            for (int i = 0; i < mazes.childCount; i++)
            {
                mazes.GetChild(i).gameObject.SetActive(false);
            }
        }
        Transform checkpoints = transform.parent.transform.Find("Checkpoints");
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.childCount; i++)
            {
                checkpoints.GetChild(i).gameObject.SetActive(false);
            }
        }
        switch (mazeNumber)
        {
            case 1:
                {
                    mazeCheckpointsCount = 7;
                    
                    Transform maze = mazes.GetChild(mazeNumber-1);
                    maze.gameObject.SetActive(true);                   
                    if (checkpoints != null)
                    {
                        Transform mazeCheckpoints = checkpoints.transform.Find("MazeCheckpoints");
                        mazeCheckpoints.gameObject.SetActive(true);
                        if (mazeCheckpoints != null)
                        {
                            for (int i = 0; i < mazeCheckpoints.childCount; i++)
                            {
                                if (mazeCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                                {
                                    Transform checkpoint = mazeCheckpoints.GetChild(i);
                                    checkpoint.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    MazeNextCheckpoint();
                }
                break;
            case 2:
                {

                    mazeCheckpointsCount = 7;
                    Transform maze = mazes.GetChild(mazeNumber-1);
                    maze.gameObject.SetActive(true);
                    if (checkpoints != null)
                    {
                        Transform mazeCheckpoints = checkpoints.transform.Find("MazeCheckpoints2");
                        mazeCheckpoints.gameObject.SetActive(true);
                        if (mazeCheckpoints != null)
                        {
                            for (int i = 0; i < mazeCheckpoints.childCount; i++)
                            {
                                if (mazeCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                                {
                                    Transform checkpoint = mazeCheckpoints.GetChild(i);
                                    checkpoint.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    MazeNextCheckpoint();
                }
                break;
            case 3:
                {

                    mazeCheckpointsCount = 11;
                    Transform maze = mazes.GetChild(mazeNumber - 1);
                    maze.gameObject.SetActive(true);
                    if (checkpoints != null)
                    {
                        Transform mazeCheckpoints = checkpoints.transform.Find("MazeCheckpoints3");
                        mazeCheckpoints.gameObject.SetActive(true);
                        if (mazeCheckpoints != null)
                        {
                            for (int i = 0; i < mazeCheckpoints.childCount; i++)
                            {
                                if (mazeCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                                {
                                    Transform checkpoint = mazeCheckpoints.GetChild(i);
                                    checkpoint.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    MazeNextCheckpoint();
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
    private void RLAgent_MazeBigField()
    {
        // agent on random position, random rotation
        // target on random positon
        // random maze obstacle between agent and goal

        //goal spawn location


        int random = Random.Range(0, 4);
        float rotation=0;
        switch (random)
        {
            case 0: rotation = 0;
                break;
            case 1: rotation = 90;
                break;
            case 2: rotation = 180;
                break;
            case 3: rotation = 270;
                break;
            default : rotation = 0;
                break;
        }
        rotation = Random.Range(0, 270f);

        mazeCheckpointsCounter = 0;
        mazeNumber = 3;
        Transform mazes = transform.parent.transform.Find("Mazes");
        if (mazes != null)
        {
            for (int i = 0; i < mazes.childCount; i++)
            {
                mazes.GetChild(i).gameObject.SetActive(false);
            }
        }
        Transform checkpoints = transform.parent.transform.Find("Checkpoints");
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.childCount; i++)
            {
                checkpoints.GetChild(i).gameObject.SetActive(false);
            }
        }
        mazeCheckpointsCount = 11;

        Transform maze = mazes.GetChild(mazeNumber - 1);
        maze.gameObject.SetActive(true);
        maze.transform.rotation = Quaternion.Euler(0,maze.transform.rotation.y + rotation,0);
        if (checkpoints != null)
        {
            Transform mazeCheckpoints = checkpoints.transform.Find("MazeCheckpoints3");
            mazeCheckpoints.gameObject.SetActive(true);
            mazeCheckpoints.transform.rotation = Quaternion.Euler(0, mazeCheckpoints.transform.rotation.y + rotation, 0);
            if (mazeCheckpoints != null)
            {
                for (int i = 0; i < mazeCheckpoints.childCount; i++)
                {
                    if (mazeCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                    {
                        Transform checkpoint = mazeCheckpoints.GetChild(i);
                        checkpoint.gameObject.SetActive(false);
                    }
                }
            }
        }
        MazeNextCheckpoint();
        //Charcter spawn location and rotation
        characterController.enabled = false;
        characterController.transform.localPosition = new Vector3(8.99f, 0.62f, -0.48f);
        characterController.transform.rotation = Quaternion.Euler(0, rotation+90, 0);
        characterController.enabled = true;


    }

    private void RLAgent_PushButton(int lvl, bool endGoal)
    {
        // agent on random position, random rotation
        // target on random positon
        // button close to agent, fixed position

        isButtonPressed = false;
        switch(lvl)
        {
            case 1:
                {
                    //goal spawn location
                    goalSpawnPositions[0] = new Vector3(Random.Range(0, 30f), 0.85f, Random.Range(-3.3f, 3.2f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];
                    targetTransform.gameObject.SetActive(endGoal);
                    //Charcter spawn location and rotation
                    characterController.enabled = false;
                    characterController.transform.localPosition = new Vector3(Random.Range(-4f, -1f), 0.55f, Random.Range(-3.3f, 3.2f));
                    characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    characterController.enabled = true;
                    Transform pushButton = transform.parent.transform.Find("PushButton");
                    if (pushButton != null)
                    {
                        Transform button = pushButton.Find("Button");
                        if (button != null)
                        {
                            button.localPosition = new Vector3(characterController.transform.localPosition.x + 2f, button.localPosition.y, characterController.transform.localPosition.z);
                            Transform buttonCheckpoint = button.Find("ButtonCheckpoint");
                            buttonCheckpoint.gameObject.SetActive(true);
                        }
                    }
                }
                break;
            case 2:
                {
                    Transform pushButton = transform.parent.transform.Find("PushButton");
                    if (pushButton != null)
                    {
                        Transform wallObstacle = pushButton.Find("WallObstacle");
                        if (wallObstacle != null)
                        {
                            wallObstacle.gameObject.SetActive(true);
                            wallObstacle.localPosition = new Vector3(Random.Range(2f, 30f), wallObstacle.localPosition.y, wallObstacle.localPosition.z);
                            //Charcter spawn location and rotation
                            characterController.enabled = false;
                            characterController.transform.localPosition = new Vector3(Random.Range(-4f, wallObstacle.localPosition.x - 2f), 0.55f, Random.Range(-3.3f, 3.2f));
                            characterController.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            characterController.enabled = true;

                            //goal spawn location
                            goalSpawnPositions[0] = new Vector3(wallObstacle.localPosition.x + 2f, 0.85f, Random.Range(-3.3f, 3.2f));
                            targetTransform.localPosition = goalSpawnPositions[goalCounter];
                            targetTransform.gameObject.SetActive(endGoal);

                        }
                        Transform button = pushButton.Find("Button");
                        if (button != null)
                        {
                            Transform buttonCheckpoint = button.Find("ButtonCheckpoint");
                            buttonCheckpoint.gameObject.SetActive(true);
                            button.localPosition = new Vector3(Random.Range(characterController.transform.localPosition.x +2f, wallObstacle.localPosition.x - 2f), button.localPosition.y, Random.Range(-3.3f, 3.2f));

                        }

                    }
                }
                break;
            default:
                break;
        }
        

        


    }

    private void RLAgent_MovingObjects(int lvl)
    {
        goalCountMax = 1;
        Transform movingObjects = transform.parent.Find("MovingObjects");
        float distanceX = 8.5f;
        foreach (Transform obj in movingObjects)
        {
            obj.localPosition = new Vector3(distanceX, 1.1f, Random.Range(-2.5f, 2.5f));
            distanceX += 5f;
        }
        Transform goalTarget = transform.parent.Find("GoalTarget");
        goalTarget.gameObject.SetActive(true);
        switch (lvl)
        {
            case 1:
                {
                    goalSpawnPositions[0] = new Vector3(Random.Range(20f, 30f), 0.85f, Random.Range(-3f, 3f));
                    targetTransform.localPosition = goalSpawnPositions[goalCounter];
                    goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);
                    characterController.transform.localPosition = new Vector3(-2.7f, 0.55f, Random.Range(-3f, 3f));
                    characterController.transform.LookAt(targetTransform);
                    characterController.enabled = true;
                }
                break;
            case 2:
                {
                    int rnd = Random.Range(0, 2);
                    switch (rnd)
                    {
                        case 0:
                            {
                                goalSpawnPositions[0] = new Vector3(Random.Range(-5f, 4f), 0.85f, Random.Range(-3f, 3f));
                                targetTransform.localPosition = goalSpawnPositions[goalCounter];
                                goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);
                                characterController.transform.localPosition = new Vector3(Random.Range(25f,30f), 0.55f, Random.Range(-3f, 3f));
                                characterController.transform.LookAt(targetTransform);
                                characterController.enabled = true;
                            }
                            break;
                        case 1:
                            {
                                goalSpawnPositions[0] = new Vector3(Random.Range(20f, 30f), 0.85f, Random.Range(-3f, 3f));
                                targetTransform.localPosition = goalSpawnPositions[goalCounter];
                                goalTarget.localPosition = new Vector3(targetTransform.localPosition.x, goalTarget.localPosition.y, targetTransform.localPosition.z);
                                characterController.transform.localPosition = new Vector3(-2.7f, 0.55f, Random.Range(-3f, 3f));
                                characterController.transform.LookAt(targetTransform);
                                characterController.enabled = true;
                            }
                            break;
                    }
                }
                break;
        }
    }
    private void Edge_0_4()
    {
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
    private void Edge_4_6()
    {
        collected = 0;
        Transform collectables = transform.parent.transform.Find("Collectables");
        if (collectables != null)
        {
            collectableCount = collectables.childCount - 1;
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            for (int i = 1; i < collectables.childCount; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                }
            }
        }
    }
    private void Edge_6_5()
    {
        isButtonPressed = false;
        Transform pushButton = transform.parent.transform.Find("PushButton");
        if (pushButton != null)
        {
            Transform wallObstacle = pushButton.Find("WallObstacle");
            if (wallObstacle != null)
            {
                wallObstacle.gameObject.SetActive(true);
            }
            Transform button = pushButton.Find("Button");
            if (button != null)
            {
                Transform buttonCheckpoint = button.Find("ButtonCheckpoint");
                buttonCheckpoint.gameObject.SetActive(true);

            }
        }
    }
    private void Edge_1_3()
    {
        Transform pushObstacles = transform.parent.transform.Find("PushObstacles");
        if (pushObstacles != null)
        {
            if (pushObstacles.GetChild(0).TryGetComponent<Box>(out Box box))
            {
                box.gameObject.SetActive(true);
                box.transform.localPosition = new Vector3(-0.92f, 0.88f, -33.58f);
                box.HasTouchedWallObstacle = false;
                box.HasTouchedAgent = false;
                if (box.TryGetComponent<Rigidbody>(out Rigidbody boxRigidbody))
                {
                    boxRigidbody.constraints = ~RigidbodyConstraints.FreezePosition;
                }
                if (pushObstacles.GetChild(1).TryGetComponent<WallObstacle>(out WallObstacle wallObstacle))
                {
                    wallObstacle.gameObject.SetActive(true);                   
                }
            }
        }
    }
    private void Edge_1_2()
    {
        Transform lava = transform.parent.transform.Find("LavaObstacles").Find("MovingLava");
        if (lava != null)
        {
            Debug.Log("ceva");
            lava.GetComponent<Lava>().MovingLava = true;
        }
    }
    private void Edge_0_3(int maze_Number)
    {

        mazeCheckpointsCounter = 0;
        mazeNumber=maze_Number;

        Transform mazes = transform.parent.transform.Find("Mazes");
        if (mazes != null)
        {
            for (int i = 0; i < mazes.childCount; i++)
            {
                mazes.GetChild(i).gameObject.SetActive(false);
            }
        }
        Transform checkpoints = transform.parent.transform.Find("Checkpoints");
        if (checkpoints != null)
        {
            for (int i = 0; i < checkpoints.childCount; i++)
            {
                checkpoints.GetChild(i).gameObject.SetActive(false);
            }
        }
        switch (mazeNumber)
        {
            case 1:
                {
                    mazeCheckpointsCount = 7;

                    Transform maze = mazes.GetChild(mazeNumber - 1);
                    maze.gameObject.SetActive(true);
                    if (checkpoints != null)
                    {
                        Transform mazeCheckpoints = checkpoints.transform.Find("MazeCheckpoints");
                        mazeCheckpoints.gameObject.SetActive(true);
                        if (mazeCheckpoints != null)
                        {
                            for (int i = 0; i < mazeCheckpoints.childCount; i++)
                            {
                                if (mazeCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                                {
                                    Transform checkpoint = mazeCheckpoints.GetChild(i);
                                    checkpoint.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    MazeNextCheckpoint();
                }
                break;
            case 2:
                {

                    mazeCheckpointsCount = 7;
                    Transform maze = mazes.GetChild(mazeNumber - 1);
                    maze.gameObject.SetActive(true);
                    if (checkpoints != null)
                    {
                        Transform mazeCheckpoints = checkpoints.transform.Find("MazeCheckpoints2");
                        mazeCheckpoints.gameObject.SetActive(true);
                        if (mazeCheckpoints != null)
                        {
                            for (int i = 0; i < mazeCheckpoints.childCount; i++)
                            {
                                if (mazeCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                                {
                                    Transform checkpoint = mazeCheckpoints.GetChild(i);
                                    checkpoint.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    MazeNextCheckpoint();
                }
                break;
            case 3:
                {

                    mazeCheckpointsCount = 17;
                    mazeBrainActive = false;
                    Transform maze = mazes.GetChild(mazeNumber - 1);
                    maze.gameObject.SetActive(true);
                    if (checkpoints != null)
                    {
                        Transform mazeCheckpoints = checkpoints.transform.Find("MazeCheckpoints3");
                        mazeCheckpoints.gameObject.SetActive(true);
                        if (mazeCheckpoints != null)
                        {
                            for (int i = 0; i < mazeCheckpoints.childCount; i++)
                            {
                                if (mazeCheckpoints.GetChild(i).transform.CompareTag("Checkpoint") == true)
                                {
                                    Transform checkpoint = mazeCheckpoints.GetChild(i);
                                    checkpoint.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    maze.GetChild(11).gameObject.SetActive(false);
                    maze.GetChild(12).gameObject.SetActive(false);
                    MazeNextCheckpoint();
                }
                break;
            default: return;
        }
    }

    private void RLAgent_Collectable_OnCollected_Map2()
    {
        if (collected_edge_0_1 == collectableCount_edge_0_1)
        {
                Transform collectables = transform.parent.transform.Find("Collectables").Find("0_1");
                if (collectables != null)
                {
                    Transform wallObstacle = collectables.GetChild(0);
                    wallObstacle.gameObject.SetActive(false);
                }
                currentBrain = goToGoalBrain;
                SetModel("current", currentBrain);
                collected_edge_0_1++;
        }
        if (collected_edge_1_2 == collectableCount_edge_1_2)
        {
            Transform collectables = transform.parent.transform.Find("Collectables").Find("1_2");
            if (collectables != null)
            {
                Transform wallObstacle = collectables.GetChild(0);
                wallObstacle.gameObject.SetActive(false);
            }
            currentBrain = goToGoalBrain;
            SetModel("current", currentBrain);
            collected_edge_1_2++;
        }
        if (collected_edge_2_3 == collectableCount_edge_2_3)
        {
            Transform collectables = transform.parent.transform.Find("Collectables").Find("2_3");
            if (collectables != null)
            {
                Transform wallObstacle = collectables.GetChild(0);
                wallObstacle.gameObject.SetActive(false);
            }
            currentBrain = goToGoalBrain;
            SetModel("current", currentBrain);
            collected_edge_2_3++;
        }
        if (collected_edge_3_0 == collectableCount_edge_3_0)
        {
            Transform collectables = transform.parent.transform.Find("Collectables").Find("3_0");
            if (collectables != null)
            {
                Transform wallObstacle = collectables.GetChild(0);
                wallObstacle.gameObject.SetActive(false);
            }
            currentBrain = goToGoalBrain;
            SetModel("current", currentBrain);
            collected_edge_3_0++;
        }

    }
    private void Edge_0_1_Map2()
    {
        collected_edge_0_1 = 0;
        Transform collectables = transform.parent.transform.Find("Collectables").Find("0_1");
        if (collectables != null)
        {
            collectableCount_edge_0_1 = collectables.childCount - 1;
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            for (int i = 1; i < collectables.childCount; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                }
            }
        }
    }

    private void Edge_1_2_Map2()
    {
        collected_edge_1_2 = 0;
        Transform collectables = transform.parent.transform.Find("Collectables").Find("1_2");
        if (collectables != null)
        {
            collectableCount_edge_1_2 = collectables.childCount - 1;
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            for (int i = 1; i < collectables.childCount; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                }
            }
        }
    }

    private void Edge_2_3_Map2()
    {
        collected_edge_2_3 = 0;
        Transform collectables = transform.parent.transform.Find("Collectables").Find("2_3");
        if (collectables != null)
        {
            collectableCount_edge_2_3 = collectables.childCount - 1;
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            for (int i = 1; i < collectables.childCount; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                }
            }
        }
    }

    private void Edge_3_0_Map2()
    {
        collected_edge_3_0 = 0;
        Transform collectables = transform.parent.transform.Find("Collectables").Find("3_0");
        if (collectables != null)
        {
            collectableCount_edge_3_0 = collectables.childCount - 1;
            Transform wallObstacle = collectables.GetChild(0);
            wallObstacle.gameObject.SetActive(true);
            for (int i = 1; i < collectables.childCount; i++)
            {
                if (collectables.GetChild(i).transform.CompareTag("Collectable") == true)
                {
                    Transform collectable = collectables.GetChild(i);
                    collectable.gameObject.SetActive(true);
                }
            }
        }
    }
    public bool StartAgentMap2
    {
        get => _startAgentMap2;
        set
        {
            _startAgentMap2 = value;
        }
    }
    public bool StartAgentMap1
    {
        get => _startAgentMap1;
        set
        {
            _startAgentMap1 = value;
        }
    }
    public int StartNode
    {
        get => _startNode;
        set => _startNode = value;
    }
    public int EndNode
    {
        set => _endNode = value;
        get => _endNode;
    }
    public bool Training
    {
        get => _training;
        set => _training = value;
    }
    public int GameModeIndex
    {
        get => _gameModeIndex;
        set => _gameModeIndex = value;
    }
}

