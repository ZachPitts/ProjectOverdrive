using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEditor.VersionControl;
public class DrivingModel :Agent
{

    [SerializeField] private TrackCheckpoints trackCheckpoints;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private ArcadeCarController controller;
    [SerializeField] private RayPerceptionSensorComponent3D rayPerceptionSensor;
    [SerializeField] private InputController inputController;

    public float lastTimeCompleted = 0;
    public float timeCompleted = 0;
    public float timeStarted;

    public int nextTrackIndex = 0;

    private float prevDist;

    public override void OnEpisodeBegin()
    {
        timeStarted = Time.time;
        if(lastTimeCompleted != 0 && timeCompleted != 0)
        {
            if(timeCompleted < lastTimeCompleted)
            {
                AddReward(0.4f);
            }
            else
            {
                AddReward(-0.2f);
            }
            lastTimeCompleted = timeCompleted;
        }
        else if(timeCompleted != 0)
        {
            lastTimeCompleted = timeCompleted;
        }



            transform.position = spawnPosition.position;
        transform.forward = spawnPosition.forward;
        nextTrackIndex = 0;
        prevDist = Vector3.Distance(transform.position, trackCheckpoints.checkpoints[nextTrackIndex].transform.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Transform checkpoint = trackCheckpoints.checkpoints[nextTrackIndex].transform;

        Vector3 toCheckpoint = checkpoint.position - transform.position;

        Vector3 localToCheckpoint = transform.InverseTransformDirection(toCheckpoint.normalized);

        float heading = Mathf.Clamp01(localToCheckpoint.z);
        AddReward(heading * 0.01f);

        sensor.AddObservation(localToCheckpoint.x);
        sensor.AddObservation(localToCheckpoint.z);

        sensor.AddObservation(Mathf.Clamp01(toCheckpoint.magnitude / 50f));

        sensor.AddObservation(controller.carRB.linearVelocity.normalized);
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {

        Transform checkpoint = trackCheckpoints.checkpoints[nextTrackIndex].transform;
        float dist = Vector3.Distance(transform.position, checkpoint.position);

        float progress = prevDist - dist;

        AddReward(progress * .6f);
        prevDist = dist;

        AddReward(-0.001f);

        float forwardAmount = 0f;
        float steeringAmount = 0f;

        switch(actions.DiscreteActions[0])
        {
            case 0:
                forwardAmount = 0f;
                break;
            case 1:
                forwardAmount = 1f;
                break;
        }
        switch(actions.DiscreteActions[1])
        {
            case 0:
                steeringAmount = 0f;
                break;
            case 1:
                steeringAmount = 1f;
                break;
            case 2:
                steeringAmount = -1f;
                break;
        }

        controller.SetInputs(forwardAmount, steeringAmount);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int forwardAction = 0;
        switch(inputController.throttle)
        {
            case 0: {  forwardAction = 0; break; }
            case 1: {  forwardAction = 1; break; }
            case -1: {  forwardAction = 2; break; }
            

        }
        int steeringAction = 0;
        switch (inputController.steering)
        {
            case 0: { steeringAction = 0; break; };
            case 1: { steeringAction = 1; break; };
            case -1: { steeringAction = 2; break; };
        }

        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = forwardAction;
        discreteActions[1] = steeringAction;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Reward")
        {
            if(trackCheckpoints.ValidateCheckpoint(other.gameObject, nextTrackIndex))
            {
                Debug.Log("Hit correct checkpoint");
                // hit correct checkpoint
                if(nextTrackIndex == trackCheckpoints.checkpoints.Count - 1)
                {
                    nextTrackIndex = 0;
                }
                else
                {
                    nextTrackIndex++;
                }
                    AddReward(1f);
            }
            else
            {
                Debug.Log("Hit wrong checkpoint");
                // hit wrong checkpoint
                AddReward(-1f);
                EndEpisode();
            }
        }
        else if(other.tag == "Final")
        {
            AddReward(1.5f);
            timeCompleted = Time.time - timeStarted;
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == "Barrier")
        {
            Debug.Log("Hit barrier");
            // hit barrier
            AddReward(-1f);
            EndEpisode();
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if(collision.transform.tag == "Barrier")
        {
            AddReward(-.1f);
            EndEpisode();
        }
    }

}
