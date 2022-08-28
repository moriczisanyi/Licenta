using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObjects2 : MonoBehaviour
{
    // Start is called before the first frame update
    MovingDirection dir;
    void Start()
    {
        dir = MovingDirection.left;
    }

    // Update is called once per frame
    public enum MovingDirection
    {
        left, right
    }

    // Update is called once per frame1

    void Update()
    {
        //if (this.transform.localPosition.z < -3f && dir == MovingDirection.left) dir = MovingDirection.right;
        //if (this.transform.localPosition.z > 3f && dir == MovingDirection.right) dir = MovingDirection.left;
        if (dir == MovingDirection.left)
        {
            this.transform.Translate(new Vector3(0, 0, -1) * 2f * Time.deltaTime);
        }
        if (dir == MovingDirection.right)
        {
            this.transform.Translate(new Vector3(0, 0, 1) * 2f * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {

            if (dir == MovingDirection.left)
            {
                dir = MovingDirection.right;
            }
            else dir = MovingDirection.left;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Agent"))
        {

            if (transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>() != null)
            {
                RLAgent rlAgent = transform.parent.parent.parent.transform.Find("RLAgent").GetComponent<RLAgent>();
                if (rlAgent.gameObject.activeSelf)
                {
                    rlAgent.SetReward(-3f);
                    rlAgent.EndEpisode();
                }
            }

        }
    }
}
