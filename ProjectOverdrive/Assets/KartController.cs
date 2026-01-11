using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class KartController : MonoBehaviour
{

    public Rigidbody rb;

    public Transform kartModel;
    public Transform groundCheck;
    public bool grounded;
    public float groundCheckHeight;

    public float speed, currentSpeed;
    public float rotate, currentRotate;
    public float acceleration;
    public float steering = 80f;
    public float groundLD;
    public float airLD;
    public float gravity;
    public LayerMask layerMask;

    [Header("Grip")]
    public float sidewaysGrip = 12f;
    public float sidewaysGripAtMax = 6f;
    public float maxSpeedForGripCurve = 35f;
    public float driftGripMultiplier = 0.5f;

    //public float speedInput;
    public float steerInput;

    public InputController inputController;

    public Transform visualRotation;


    private void Start()
    {
        rb.transform.parent = null;
    }

    private void Update()
    {

        transform.position = rb.transform.position - new Vector3(0, 0.4f, 0);

        if(inputController.throttle == 1)
        {
            speed = acceleration;
        }

        if(inputController.steering != 0)
        {
            int dir = inputController.steering > 0 ? 1 : -1;
            float amount = Mathf.Abs(inputController.steering);
            Steer(dir, amount);
        }

        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * 12f); speed = 0f;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f); rotate = 0f;

    }

    private void FixedUpdate()
    {


        rb.AddForce(kartModel.forward * currentSpeed);


        Vector3 vel = rb.linearVelocity;
        Vector3 lateralVel = Vector3.ProjectOnPlane(vel, kartModel.right);

        float speed01 = Mathf.Clamp01(vel.magnitude / maxSpeedForGripCurve);
        float grip = Mathf.Lerp(sidewaysGrip, sidewaysGripAtMax, speed01);

        rb.AddForce(-lateralVel * grip, ForceMode.Acceleration);

        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + currentRotate, transform.eulerAngles.z), Time.deltaTime * 5f);

        RaycastHit hitOn;
        RaycastHit hitNear;

        if(Physics.Raycast(groundCheck.position, Vector3.down, out hitOn, groundCheckHeight, layerMask))
        {
            grounded = true;
            rb.linearDamping = groundLD;
            
        }
        else
        {
            grounded = false;
            rb.linearDamping = airLD;
            rb.AddForce(-transform.up * gravity);
        }
        Physics.Raycast(groundCheck.position, Vector3.down, out hitNear, 2.0f, layerMask);

        Quaternion groundAlgin = Quaternion.FromToRotation(kartModel.transform.up, hitNear.normal) * kartModel.rotation;
        kartModel.rotation = Quaternion.Slerp(kartModel.rotation, groundAlgin, Time.fixedDeltaTime * 8f);


    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, visualRotation.rotation, Time.deltaTime * 7f);

    }

    public void Steer(int direction, float amount)
    {
        rotate = (steering * direction) * amount;
    }

}