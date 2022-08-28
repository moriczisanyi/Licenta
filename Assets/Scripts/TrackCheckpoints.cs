using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCheckpoints : MonoBehaviour
{

    private List<CheckpointSingle> checkpointSingleList;
    private int nextCheckpointSingleIndex;
    private void Awake()
    {
        Transform checkpointsTransform = transform;
        checkpointSingleList = new List<CheckpointSingle>();
        foreach(Transform checkpointSingleTransfrom in checkpointsTransform)
        {
            CheckpointSingle checkpointSingle = checkpointSingleTransfrom.GetComponent<CheckpointSingle>();
            checkpointSingle.setTrackCheckpoints(this);
            checkpointSingleList.Add(checkpointSingle);
        }

        nextCheckpointSingleIndex = 0;
    }


    public CheckpointSingle getNextCheckpoint()
    {
        return checkpointSingleList[nextCheckpointSingleIndex];
    }
    public void AgentTroughCheckpoint(CheckpointSingle checkPointSingle)
    {
        //AgentMove agentMove = GameObject.Find("Agent").GetComponent<AgentMove>();
        AgentMove agentMove = transform.parent.transform.Find("Agent").GetComponent<AgentMove>();
        if (checkpointSingleList.IndexOf(checkPointSingle) == nextCheckpointSingleIndex)
        {
            nextCheckpointSingleIndex = (nextCheckpointSingleIndex +1) % checkpointSingleList.Count;
            if (agentMove != null)
            {
                agentMove.SetReward(+1f);
            }
        }
        else
        {
            if (agentMove != null)
            {
                agentMove.SetReward(-1f);
            }
        }
    }

    public void resetNextCheckpointSingle()
    {
        this.nextCheckpointSingleIndex = 0;
    }
}
