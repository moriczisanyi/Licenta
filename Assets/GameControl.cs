using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class GameControl : MonoBehaviour
{
    public TMP_Dropdown startNodeDropdown;
    public TMP_Dropdown endNodeDropdown;
    public TMP_Text currentBrainText;
    public TMP_Text succesRateValue;
    public TMP_Text brainStatusValue;
    public TMP_Text numberOfTotalEpisodes;
    public TMP_Text numberOfSuccessfulEpisodes;
    public TMP_Text numberOfUnsuccessfulEpisodes;
    public int mapNumber=1;
    private RLAgent agent;
    private void Start()
    {
        
    }
    private void Update()
    {

        switch (mapNumber)
        {
            case 0:
                {
                    agent = GameObject.Find("DemoMap").transform.Find("RLAgent").GetComponent<RLAgent>();
                    if(GameObject.Find("Map2")!=null)
                        GameObject.Find("Map2").transform.gameObject.SetActive(false);
                    transform.Find("Map1").gameObject.SetActive(true);
                    transform.Find("Map2").gameObject.SetActive(false);
                }
                break;
            case 1:
                {
                    agent = GameObject.Find("Map2").transform.Find("RLAgent").GetComponent<RLAgent>();
                    if (GameObject.Find("DemoMap") != null)
                        GameObject.Find("DemoMap").transform.gameObject.SetActive(false);
                    transform.Find("Map1").gameObject.SetActive(false);
                    transform.Find("Map2").gameObject.SetActive(true);
                }
                break;
            default:
                return;
        }
        if (agent.episodeCounter - 1 < 0)
        {
            numberOfTotalEpisodes.text = "0";
            numberOfUnsuccessfulEpisodes.text = (agent.episodeCounter - agent.successfulEpisodeCounter).ToString();
        }
        else
        {
            numberOfTotalEpisodes.text = (agent.episodeCounter - 1).ToString();
            numberOfUnsuccessfulEpisodes.text = (agent.episodeCounter - 1 - agent.successfulEpisodeCounter).ToString();
        }
        numberOfSuccessfulEpisodes.text = agent.successfulEpisodeCounter.ToString();

        currentBrainText.text = "Current brain: " + agent.currentBrain.name;
        if (agent.StartAgentMap1 || agent.StartAgentMap2)
        {
            brainStatusValue.text = "active";
            brainStatusValue.color = Color.green;
        }
        else
        {
            brainStatusValue.text = "incative";
            brainStatusValue.color = Color.red;
        }
        if (agent.episodeCounter > 1)
        {
            float succesRate = (agent.successfulEpisodeCounter) / ((float)agent.episodeCounter-1);

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
            succesRateValue.text = "?";
        }
    }
    public void OnStartButtonClick()
    {
        if (startNodeDropdown.value != endNodeDropdown.value)
        {
            if (agent != null)
            {
                //agent.successfulEpisodeCounter++;
                switch (mapNumber)
                {
                    case 0: agent.StartAgentMap1 = true;
                        break;
                    case 1: agent.StartAgentMap2 = true;
                        break;
                    default: return;
                }
                
                agent.StartNode = startNodeDropdown.value;
                agent.EndNode = endNodeDropdown.value;
                agent.EndEpisode();
            }
        }
    }

    public void OnStopButtonClick()
    {
        
        if (agent != null)
        {
            switch (mapNumber)
            {
                case 0:
                    agent.StartAgentMap1 = false;
                    break;
                case 1:
                    agent.StartAgentMap2 = false;
                    break;
                default: return;
            }
            agent.Training = false;
            agent.EndEpisode();
            agent.SetModel("none",null);
            agent.ResetAgentToNode_0();
        }
    }

    public void OnExitButtonClick()
    {
        transform.parent.GetChild(0).gameObject.SetActive(true);
        transform.parent.GetChild(3).gameObject.SetActive(false);
        //transform.parent.GetChild(4).gameObject.SetActive(false);
        transform.gameObject.SetActive(false);
    }
}
