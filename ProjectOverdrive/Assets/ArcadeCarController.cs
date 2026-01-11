using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class ArcadeCarController : MonoBehaviour
{

    [Header("References")]
    public Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private Transform accelerationPoint;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private InputController inputController;

    [Header("Suspension Settings")]
    [SerializeField] private float springStiffness;
    [SerializeField] private float dampenerStiffness;
    [SerializeField] private float restLength;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    [SerializeField] private bool[] wheelsAreGrounded = new bool[4];
    public bool isGrounded;

    [Header("Input")]
    public float moveInput = 0;
    private float steerInput = 0;

    [Header("Car Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float steerStrength = 15f;
    [SerializeField] private AnimationCurve steeringCurve;
    [SerializeField] private float dragCoefficient = 1f;

    public Vector3 currentCarLocalVelocity = Vector3.zero;
    public float currentSpeed;
    private float carVelocityRatio = 0;

    private void Update()
    {
        if(inputController != null)
        {
            GetInput();
        }

            currentSpeed = currentCarLocalVelocity.magnitude;

    }

    private void FixedUpdate()
    {
        Suspension();
        GroundCheck();
        CalculateCarVelocity();
        Movement();
        
    }



    #region Car Status Check

    
    private void GroundCheck()
    {

        isGrounded = true;

        for(int i = 0; i < wheelsAreGrounded.Length; i++)
        {
            if(wheelsAreGrounded[i] == false)
            {
                isGrounded = false;
            }
        }
    }


    private void CalculateCarVelocity()
    {
        currentCarLocalVelocity = transform.InverseTransformDirection(carRB.linearVelocity);
        carVelocityRatio = currentCarLocalVelocity.z / maxSpeed;
    }

    #endregion


    #region Input Handling


    private void GetInput()
    {
        moveInput = inputController.throttle;
        steerInput = inputController.steering;
    }
    public void SetInputs(float forward, float steering)
    {
        moveInput = forward;
        steerInput = steering;

        Debug.Log($"MoveInput = {moveInput}, Steering = {steerInput}");
    }

    #endregion


    private void Movement()
    {
        if(isGrounded)
        {
            Acceleration();
            Turn();
            SidewaysDrag();
            //Deceleration();
            carRB.linearDamping = 3;
        }
        else
        {
            carRB.linearDamping = 0.1f;
        }
    }

    private void Acceleration()
    {
        carRB.AddForceAtPosition(acceleration * moveInput * transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Deceleration()
    {
        carRB.AddForceAtPosition(acceleration * moveInput * -transform.forward, accelerationPoint.position, ForceMode.Acceleration);
    }

    private void Turn()
    {
        carRB.AddRelativeTorque(steerStrength * steerInput * steeringCurve.Evaluate(Mathf.Abs(carVelocityRatio)) * Mathf.Sign(carVelocityRatio) * carRB.transform.up, ForceMode.Acceleration);
    }

    private void SidewaysDrag()
    {
        float currentSidewaysSpeed = currentCarLocalVelocity.x;

        float dragMagnitude = -currentSidewaysSpeed * dragCoefficient;

        Vector3 dragForce = transform.right * dragMagnitude;

        carRB.AddForceAtPosition(dragForce, carRB.worldCenterOfMass, ForceMode.Acceleration);
    }

    private void Suspension()
    {
        for (int i = 0; i < rayPoints.Length; i++)
        {
            RaycastHit hit;
            float maxLenght = restLength + springTravel;

            if(Physics.Raycast(rayPoints[i].position, -rayPoints[i].up, out hit, maxLenght + wheelRadius, layerMask))
            {

                wheelsAreGrounded[i] = true;

                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = restLength - currentSpringLength / springTravel;

                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoints[i].position), rayPoints[i].up);
                float dampForce = dampenerStiffness * springVelocity;

                float springForce = springCompression * springStiffness;

                float netForce = springForce - dampForce;

                carRB.AddForceAtPosition(netForce * rayPoints[i].up, rayPoints[i].position);
                Debug.DrawLine(rayPoints[i].position, hit.point, Color.red);
            }
            else
            {
                wheelsAreGrounded[i] = false;

                Debug.DrawLine(rayPoints[i].position, rayPoints[i].position + (wheelRadius + maxLenght) * -rayPoints[i].up, Color.green);
            }
        }
    }

}
