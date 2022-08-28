using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameStatistics : MonoBehaviour
{
    public TMP_Text currentBrainText;
    public TMP_Text succesRateValue;
    public TMP_Text brainStatusValue;
    public int gameModeIndex=-1;
    private RLAgent agent;
    private void Start()
    {
        
    }
    private void Update()
    {
        switch (gameModeIndex)
        {
            case 0: agent = transform.parent.transform.GetChild(5).transform.Find("RLAgent").GetComponent<RLAgent>();
                break;
            default: break;
        }
        
        currentBrainText.text = "Current brain: " + agent.currentBrain.name;
        if (agent.StartAgentMap2)
        {
            brainStatusValue.text = "active";
            brainStatusValue.color = Color.green;
        }
        else
        {
            brainStatusValue.text = "incative";
            brainStatusValue.color = Color.red;
        }
        if (agent.episodeCounter != 0)
        {
            float succesRate = (agent.successfulEpisodeCounter) / ((float)agent.episodeCounter);

            if (succesRate * 100f >= 80f)
            {
                succesRateValue.text = (succesRate * 100).ToString() + "%";
                succesRateValue.color = Color.green;
            }
            else if (succesRate * 100f < 80f && succesRate * 100f >= 50f)
            {
                succesRateValue.text = (succesRate * 100).ToString() + "%";
                succesRateValue.color = Color.yellow;
            }
            else
            {
                succesRateValue.text = (succesRate * 100).ToString() + "%";
                succesRateValue.color = Color.red;
            }
        }
        else
        {
            succesRateValue.text = "";
        }
    }
    public void OnStartButtonClick()
    {
       
    }

    public void OnStopButtonClick()
    {

        if (agent != null)
        {
            agent.StartAgentMap2 = false;
            agent.Training = false;
            agent.EndEpisode();
            agent.SetModel("none", null);
            agent.ResetAgentToNode_0();
        }
    }

    public void OnExitButtonClick()
    {
        transform.parent.GetChild(0).gameObject.SetActive(true);
        transform.parent.GetChild(3).gameObject.SetActive(false);
        transform.gameObject.SetActive(false);
    }
}
