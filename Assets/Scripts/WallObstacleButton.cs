using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallObstacleButton : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Agent")
        {
            if (transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>() != null)
            {

                SmartAgent smartAgent = transform.parent.parent.transform.Find("SmartAgent").GetComponent<SmartAgent>();
                if (smartAgent.gameObject.activeSelf)
                {
                    smartAgent.SetReward(-2f);
                    smartAgent.EndEpisode();
                }
            }
            if (transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
            {
                RLAgent rlAgent = transform.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                if (rlAgent.gameObject.activeSelf)
                {
                    rlAgent.SetReward(-2f);
                    rlAgent.EndEpisode();
                }
            }
        }
    }
}
