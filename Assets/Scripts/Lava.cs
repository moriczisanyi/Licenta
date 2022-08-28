using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    public enum MovingDirection{
        forward,backward
    }

    // Update is called once per frame1
    private bool _movingLava=false;
    MovingDirection dir= MovingDirection.forward;
    void Update()
    {
        Collider[] colliderArray = Physics.OverlapBox(transform.position, Vector3.one * 0.1f);
        foreach (Collider collider in colliderArray)
        {
            if (collider.CompareTag("MazeTrigger"))
            {
                if (dir == MovingDirection.forward)
                {
                    dir = MovingDirection.backward;
                }
                else dir = MovingDirection.forward;
            }
        }
        if (_movingLava)
        {
            if (dir == MovingDirection.forward)
            {
                this.transform.Translate(new Vector3(-1, 0, 0) * 2f * Time.deltaTime);
            }
            if (dir == MovingDirection.backward)
            {
                this.transform.Translate(new Vector3(1, 0, 0) * 2f * Time.deltaTime);
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Agent") == true)
        {
            /*JumpAgent jumpAgent = transform.parent.transform.Find("JumpAgent").GetComponent<JumpAgent>();
            if (jumpAgent != null)
            {
                jumpAgent.SetReward(-3f);
                jumpAgent.EndEpisode();
            }*/
            if (transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>() != null)
            {
                
                SmartAgent smartAgent = transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>();
                if (smartAgent.gameObject.activeSelf)
                {
                    smartAgent.SetReward(-3f);
                    smartAgent.EndEpisode();
                }
            }
            if (transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
            {
                RLAgent rlAgent = transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                if (rlAgent.gameObject.activeSelf)
                {
                    rlAgent.SetReward(-3f);
                    rlAgent.EndEpisode();
                }
            }

        }

    }

    public bool MovingLava
    {
        get => _movingLava;
        set
        {
            _movingLava = value;
        }
    }
}
