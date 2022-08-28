using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{

    private TrackCheckpoints trackCheckPoints;
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<AgentMove>(out AgentMove agentMove))
        {
            trackCheckPoints.AgentTroughCheckpoint(this);
        }
    }

    public void setTrackCheckpoints(TrackCheckpoints trackCheckpoints)
    {
        this.trackCheckPoints = trackCheckpoints;
    }
}
