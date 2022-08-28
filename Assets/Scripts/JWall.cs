using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JWall : MonoBehaviour
{

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Agent")
        {
            
            Box box = transform.parent.transform.Find("Box").GetComponent<Box>();
            AgentMove agentMove = collision.gameObject.GetComponent<AgentMove>();

            if (box != null && !box.HasTouchedWallObstacle)
            {
                agentMove.SetReward(-0.1f);
            }
        }

    }
    private void OnCollisionEnter(Collision collision)
    {
        
    }
}
