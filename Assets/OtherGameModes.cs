using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherGameModes : MonoBehaviour
{

    private RLAgent agent;
    void Start()
    {
        agent = transform.parent.transform.GetChild(4).GetComponent<RLAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGoToGoalButtonClick()
    {
        if (agent != null)
        {
            agent.GameModeIndex = 0;
            agent.EndEpisode();
        }
    }
}
