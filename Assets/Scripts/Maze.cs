using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour
{
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Agent")
        {
            if (transform.parent.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>() != null)
            {

                SmartAgent smartAgent = transform.parent.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>();
                if (smartAgent.gameObject.activeSelf)
                {
                    smartAgent.SetReward(-0.1f);
                }
            }
            if (transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
            {
                RLAgent rlAgent = transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                if (rlAgent.gameObject.activeSelf)
                {
                    rlAgent.SetReward(-0.1f);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Agent")
        {
            if (transform.parent.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>() != null)
            {

                SmartAgent smartAgent = transform.parent.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>();
                if (smartAgent.gameObject.activeSelf)
                {
                    smartAgent.SetReward(-1f);
                    smartAgent.EndEpisode();
                }
            }
            if (transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
            {
                RLAgent rlAgent = transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                if (rlAgent.gameObject.activeSelf)
                {
                    rlAgent.SetReward(-2f);
                    rlAgent.EndEpisode();
                }
            }

        }
    }
}
