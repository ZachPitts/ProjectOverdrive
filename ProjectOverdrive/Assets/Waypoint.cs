using UnityEngine;

public class Waypoint : MonoBehaviour
{

    public Transform point;
    public float targetSpeed = 0f;

    private void Awake()
    {
        point = this.transform;
    }

}
