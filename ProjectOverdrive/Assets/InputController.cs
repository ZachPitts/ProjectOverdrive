using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{

    private PlayerInputs playerInputs;

    public Vector2 steeringInput;
    public float throttle;
    public float steering;

    private void Awake()
    {
        playerInputs = new PlayerInputs();
    }
    private void OnEnable()
    {
        playerInputs.Enable();
    }
    private void OnDisable()
    {
        playerInputs.Disable();
    }

    public void Update()
    {
        
        throttle = playerInputs.Driving.Throttle.ReadValue<float>();
        steeringInput = playerInputs.Driving.Steering.ReadValue<Vector2>();
        steering = steeringInput.x;

    }

}
