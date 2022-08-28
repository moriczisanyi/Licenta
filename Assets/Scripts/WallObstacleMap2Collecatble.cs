using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallObstacleMap2Collecatble : MonoBehaviour
{

    

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Agent")
        {
            if (transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
            {
                RLAgent rlAgent = transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                if (rlAgent.gameObject.activeSelf)
                {
                    rlAgent.SetReward(-1f);
                    rlAgent.EndEpisode();
                }
            }

        }
    }
}
