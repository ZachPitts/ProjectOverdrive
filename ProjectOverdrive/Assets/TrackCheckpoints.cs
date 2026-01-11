using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TrackCheckpoints : MonoBehaviour
{

    public List<GameObject> checkpoints;
    public int nextCheckpointIndex;

    public float timeBetweenCheckpoints = 2f;
    public float lastTimeHit;

    public bool ValidateCheckpoint(GameObject checkpoint, int index)
    {

        if(checkpoint == checkpoints[index].gameObject)
        {
            nextCheckpointIndex++;
            lastTimeHit = Time.time;
            return true;
        }
        else
        {
            if(lastTimeHit + timeBetweenCheckpoints > Time.time)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }

}
