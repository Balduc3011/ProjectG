// Heavy bag: swings around its pivot (the hook on the beam) when punched, then settles.
using UnityEngine;

public class BlockGymBag : MonoBehaviour
{
    public float stiffness = 9f;
    public float damping = 1.6f;
    float angle, velocity;
    Quaternion rest;

    void Awake() { rest = transform.localRotation; }

    public void Punch(float strength) { velocity += 0.9f * strength; }

    void Update()
    {
        float dt = Time.deltaTime;
        velocity -= (angle * stiffness + velocity * damping) * dt;
        angle += velocity * dt;
        // the puncher stands on +Z of the bag and hits toward -Z, so the bag swings around X
        transform.localRotation = rest * Quaternion.Euler(angle * Mathf.Rad2Deg, 0f, 0f);
    }
}
