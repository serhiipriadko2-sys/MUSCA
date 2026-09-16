using UnityEngine;

namespace MUSCA.Gate3D
{
    public sealed class MuscaCompanion : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform leftWing;
        [SerializeField] private Transform rightWing;
        [SerializeField] private Vector3 localOffset = new Vector3(-1.05f, -0.15f, -1.15f);
        [SerializeField] private float followSharpness = 4f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float wingAmplitudeDegrees = 26f;

        public void Configure(Transform target, Transform wingLeft, Transform wingRight)
        {
            followTarget = target;
            leftWing = wingLeft;
            rightWing = wingRight;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            Vector3 target = followTarget.TransformPoint(localOffset);
            target.y += Mathf.Sin(Time.time * 3.5f) * bobAmplitude;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(followTarget.position - transform.position, Vector3.up), t);

            float flap = Mathf.Sin(Time.time * 18f) * wingAmplitudeDegrees;
            if (leftWing != null)
            {
                leftWing.localRotation = Quaternion.Euler(0f, flap, 18f);
            }
            if (rightWing != null)
            {
                rightWing.localRotation = Quaternion.Euler(0f, -flap, -18f);
            }
        }
    }
}
