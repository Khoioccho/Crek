using UnityEngine;

namespace PostApoc
{
    // Subtle "alive" idle for static villagers: a gentle breathing bob plus a slow
    // look-around turn. Applied to the villager root.
    public class PAIdleSway : MonoBehaviour
    {
        float _seed, _baseYaw;
        Vector3 _basePos;

        void Start()
        {
            _seed = Random.value * 12f;
            _basePos = transform.localPosition;
            _baseYaw = transform.localEulerAngles.y;
        }

        void Update()
        {
            float t = Time.time + _seed;
            float bob = Mathf.Sin(t * 1.6f) * 0.02f;
            float yaw = _baseYaw + Mathf.Sin(t * 0.35f) * 14f;
            transform.localPosition = _basePos + new Vector3(0f, bob, 0f);
            transform.localEulerAngles = new Vector3(0f, yaw, 0f);
        }
    }
}
