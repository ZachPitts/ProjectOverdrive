using Unity.VisualScripting;
using UnityEngine;

public class RacingAI : MonoBehaviour
{

    public Waypoint[] waypoints;
    public Vector3 targetPosition;
    public int currentWaypointIndex = 0;

    private ArcadeCarController carController;

    public float currentSpeed;

    public float distanceSpeedMultiplier;
    public float reachTargetDistance;
    public float adjustSpeedDistance;

    public float steeringSmoothing;
    public float steeringStrength;
    private float smoothedSteer;
    private float currentSteer;
    private float steerVelocity;
    public float targetSpeed;

    public LayerMask obastacleLayers;
    public float whiskerLength;
    public float whiskerSideOffset;
    public float whiskerAngle = 25f;
    public float avoidanceStrength = 1.25f;

    [Header("Personality settings")]
    public Vector2 targetSpeedMultiplierRange = new Vector2(0.92f, 1.04f);
    public Vector2 steeringWanderStrengthRange = new Vector2(0.0f, 0.25f);
    public Vector2 reactionTimeRange = new Vector2(0.05f, 0.22f);
    public Vector2 avoidanceStrengthRange = new Vector2(0.9f, 1.6f);

    private float targetSpeedMultiplier;
    private float wanderStrength;
    private float reactionTime;
    private float avoidanceStrengthPersonal;

    [Header("Wander")]
    public float wanderFrequency = 0.6f;
    public float wanderChangeRate = 0.8f;

    private float wanderSeed;

    [Header("Mistakes")]
    public float mistakesChancePerSecond = 0.8f;
    public Vector2 mistakeDurationRange = new Vector2(0.15f, 0.45f);
    public Vector2 mistakeSteerBiasRange = new Vector2(-0.25f, 0.25f);

    private float mistakeTimer;
    private float mistakeSteerBias;

    private Rigidbody rb;

    private void Awake()
    {
        carController = GetComponent<ArcadeCarController>();
        rb = GetComponent<Rigidbody>();

        // initialize randomness
        int seed = gameObject.GetInstanceID();
        Random.InitState(seed);

        targetSpeedMultiplier = Random.Range(targetSpeedMultiplierRange.x, targetSpeedMultiplierRange.y);
        wanderStrength = Random.Range(steeringWanderStrengthRange.x, steeringWanderStrengthRange.y);
        reactionTime = Random.Range(reactionTimeRange.x, reactionTimeRange.y);
        avoidanceStrengthPersonal = Random.Range(avoidanceStrengthRange.x, avoidanceStrengthRange.y);

        // wander randomization
        wanderSeed = Random.value * 1000f;

        // initialize
        targetSpeed = waypoints[currentWaypointIndex].targetSpeed;
        targetSpeed *= targetSpeedMultiplier;

    }


    private void Update()
    {
        SetTargetPosition(waypoints[currentWaypointIndex].point.position);
        

        Vector3 baseTarget = waypoints[currentWaypointIndex].point.position;

        float noise = Mathf.PerlinNoise(wanderSeed, Time.time * wanderFrequency) * 2f - 1f;
        Vector3 sideways = waypoints[currentWaypointIndex].point.right;
        Vector3 noisyTarget = baseTarget + sideways * noise * wanderStrength;

        SetTargetPosition(noisyTarget);

        float forwardAmount = 0f;
        float turnAmount = 0f;
        float desiredSteer = 0f;

       float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        currentSpeed = rb.linearVelocity.magnitude;

        if(distanceToTarget > reachTargetDistance)
        {
            Vector3 dirToMovePosition = (targetPosition - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToMovePosition);

            if (dot > 0 && currentSpeed < waypoints[currentWaypointIndex].targetSpeed)
            {
                forwardAmount = 1f;
            }
            else
            {
                float reversedDistance = 14f;
                if(distanceToTarget > reversedDistance)
                {
                    forwardAmount = 1f;
                }
                else
                {
                    forwardAmount = -1f;
                }
                    
            }

            // adjust speed once close enough
            if(distanceToTarget < adjustSpeedDistance)
            {
                targetSpeed = waypoints[currentWaypointIndex].targetSpeed;
                targetSpeed *= targetSpeedMultiplier;
            }


            float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);

            //if (angleToDir > 0)
            //{
            //    turnAmount = 1f;
            //}
            //else
            //{
            //    turnAmount = -1f;
            //}

            desiredSteer = Mathf.Clamp(angleToDir / steeringStrength, -1f, 1f);

            float avoidance = ComputeAvoidanceSteer();
            desiredSteer = Mathf.Clamp(desiredSteer + avoidance * avoidanceStrengthPersonal, -1f, 1f);

            currentSteer = Mathf.SmoothDamp(currentSteer, desiredSteer, ref steerVelocity, reactionTime);

            if(mistakeTimer <= 0f && Random.value < mistakesChancePerSecond * Time.deltaTime)
            {
                mistakeTimer = Random.Range(mistakeDurationRange.x, mistakeDurationRange.y);
                mistakeSteerBias = Random.Range(mistakeSteerBiasRange.x, mistakeSteerBiasRange.y);
            }
            if(mistakeTimer > 0f)
            {
                mistakeTimer -= Time.deltaTime;
                desiredSteer = Mathf.Clamp(desiredSteer + mistakeSteerBias, -1f, 1f);
            }

            //turnAmount = Mathf.Clamp(turnAmount + avoidance * avoidanceStrength, -1f, 1f);
            //turnInput = Mathf.SmoothDamp(smoothedSteer, turnInput, ref steerVelocity, steeringSmoothing);
        }
        else
        {
            if(currentWaypointIndex < waypoints.Length - 1)
            {
                currentWaypointIndex++;
            }
            else
            {
                currentWaypointIndex = 0;
            }
        }


        carController.SetInputs(forwardAmount, desiredSteer);

    }

    public void SetTargetPosition(Vector3 targetPos)
    {
        targetPosition = targetPos;
    }

    float ComputeAvoidanceSteer()
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Vector3 leftOrigin = transform.position - transform.right * whiskerSideOffset;
        Vector3 rightOrigin = transform.position + transform.right * whiskerSideOffset;

        Vector3 fwd = transform.forward;
        Vector3 leftDir = Quaternion.AngleAxis(-whiskerAngle, Vector3.up) * fwd;
        Vector3 rightDir = Quaternion.AngleAxis(whiskerAngle, Vector3.up) * fwd;

        float steer = 0f;

        if (Physics.Raycast(leftOrigin, leftDir, out RaycastHit hitL, whiskerLength, obastacleLayers))
        {
            float t = 1f - (hitL.distance / whiskerLength);
            steer += t;
        }

        if (Physics.Raycast(rightOrigin, rightDir, out RaycastHit hitR, whiskerLength, obastacleLayers))
        {
            float t = 1f - (hitR.distance / whiskerLength);
            steer -= t;
        }

        return Mathf.Clamp(steer, -1f, 1f);
    }

}
