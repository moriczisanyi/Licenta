using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float Movespeed = 2f;

    // Update is called once per frame1
    void Update()
    {
        
        this.transform.Translate(new Vector3(-1,0,0) * Movespeed * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {

        if (other.TryGetComponent<Wall>(out Wall wall))
        {
            this.gameObject.SetActive(false);
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
       if(collision.gameObject.GetComponent<AgentMove>() != null)
        {
            AgentMove agentMove = transform.parent.transform.Find("Agent").GetComponent<AgentMove>();
            agentMove.AddReward(-5f);
            agentMove.EndEpisode();
        }
    }
}
