using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private bool _hasTouchedWallObstacle;
    private bool _hasTouchedAgent;
    private float oldPositionZ;
    private Rigidbody boxRigidbody;
    void Start()
    {
        boxRigidbody = GetComponent<Rigidbody>();
        _hasTouchedWallObstacle = false;
        _hasTouchedAgent=false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public bool HasTouchedWallObstacle
    {
        get => _hasTouchedWallObstacle;
        set
        {
            _hasTouchedWallObstacle=value;
        }
    }
    public bool HasTouchedAgent
    {
        get => _hasTouchedAgent;
        set { _hasTouchedAgent=value; }
    }
    private void FixedUpdate()
    {
        //if(!_hasTouchedAgent)
            //boxRigidbody.velocity = Vector3.zero;
        
        /*if (boxRigidbody.velocity.x > 0)
        {
            //AgentMove agentMove = GameObject.Find("Agent").GetComponent<AgentMove>();
            AgentMove agentMove = transform.parent.transform.Find("Agent").GetComponent<AgentMove>();
            if (agentMove != null)
            {
                agentMove.SetReward(+0.1f);
            }
        }*/
    }
    private void OnCollisionStay(Collision collision)
    {
        if(collision.gameObject.CompareTag("Agent"))
        {
            if (!_hasTouchedAgent)
            {
                
                HasTouchedAgent = true;
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {

        if(collision.gameObject.CompareTag("WallObstacle"))
        {
            if (!_hasTouchedWallObstacle)
            {
                _hasTouchedWallObstacle = true;
                boxRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                if (transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>() != null)
                {

                    SmartAgent smartAgent = transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>();
                    if (smartAgent.gameObject.activeSelf)
                    {
                        Transform wallObstacle = transform.parent.transform.Find("WallObstacle");
                        if (wallObstacle != null)
                        {
                            wallObstacle.gameObject.SetActive(false);
                            this.gameObject.SetActive(false);
                            smartAgent.SetReward(+1f);
                        }

                    }
                }

                if (transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
                {
                    RLAgent rlAgent = transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                    if (rlAgent.gameObject.activeSelf)
                    {
                        Transform wallObstacle = transform.parent.transform.Find("WallObstacle");
                        if (wallObstacle != null)
                        {
                            wallObstacle.gameObject.SetActive(false);
                            rlAgent.SetReward(+1f);
                            this.gameObject.SetActive(false);
                            rlAgent.currentBrain = rlAgent.goToGoalBrain;
                            rlAgent.SetModel("current", rlAgent.currentBrain);
                            //rlAgent.EndEpisode();
                        }
                    }
                }

            }

        }
        /*if (collision.gameObject.CompareTag("Agent"))
        {
            if (!_hasTouchedAgent)
            {
                _hasTouchedAgent = true;
                if (transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>() != null)
                {
                    SmartAgent smartAgent = transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>();
                    if (smartAgent.gameObject.activeSelf)
                    {
                        smartAgent.SetReward(+0.5f);

                    }
                }

                if (transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
                {
                    RLAgent rlAgent = transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                    if (rlAgent.gameObject.activeSelf)
                    {
                        Debug.Log("ceva");
                        rlAgent.SetReward(+0.5f);
                    }
                }
            }
        }*/
    }

}
