using UnityEngine;

namespace FreezeOrThrow.Visuals
{
    public class PlayerVisualAnimator : MonoBehaviour
    {
        [Header("Pieces")]
        [SerializeField] Transform hat;
        [SerializeField] Transform scarfTail;
        [SerializeField] Vector3 hatPivot;
        [SerializeField] Vector3 scarfTailPivot;

        [Header("Motion reading")]
        [SerializeField] float referenceSpeed = 6f;
        [SerializeField] float velocitySmoothTime = 0.07f;
        [SerializeField] float teleportDistance = 3f;
        [SerializeField] bool glideBetweenPhysicsSteps = true;

        [Header("Walk")]
        [SerializeField] float stepLength = 1.8f;
        [SerializeField] float hopHeight = 0.1f;
        [SerializeField] float waddleDegrees = 6f;
        [SerializeField] float landingSquash = 0.05f;
        [SerializeField] float hopStretch = 0.03f;
        [SerializeField] float baseRadius = 0.32f;

        [Header("Lean")]
        [SerializeField] float leanDegrees = 6f;
        [SerializeField] float maximumLeanDegrees = 10f;
        [SerializeField] float leanFrequency = 2.1f;
        [SerializeField] float leanDamping = 0.25f;

        [Header("Hat lag")]
        [SerializeField] float hatLagFactor = 1.3f;
        [SerializeField] float maximumHatDegrees = 4f;
        [SerializeField] float hatFrequency = 3.2f;
        [SerializeField] float hatDamping = 0.35f;

        [Header("Scarf tail")]
        [SerializeField] float tailTrailDegrees = 14f;
        [SerializeField] float tailHopFlapDegrees = 7f;
        [SerializeField] float tailInertiaDegrees = 0.35f;
        [SerializeField] float maximumTailDegrees = 32f;
        [SerializeField] float tailInwardLimitDegrees = 3f;
        [SerializeField] float tailFrequency = 1.9f;
        [SerializeField] float tailDamping = 0.22f;

        [Header("Idle")]
        [SerializeField] float idleBobHeight = 0.02f;
        [SerializeField] float idleBobPeriod = 2.6f;
        [SerializeField] float modelHeight = 1.6f;

        const float MaximumSpringStep = 1f / 120f;

        Transform motionSource;
        Rigidbody sourceBody;
        Vector3 physicsStepPosition;
        Quaternion physicsStepRotation;
        Vector3 restLocalPosition;
        Quaternion restLocalRotation;
        Vector3 restLocalScale;
        Vector3 hatRestPosition;
        Quaternion hatRestRotation;
        Vector3 tailRestPosition;
        Quaternion tailRestRotation;
        Vector2 tailOutwardDirection;

        Vector3 previousSourcePosition;
        Vector3 smoothedVelocity;
        Vector3 smoothedAcceleration;
        float stepPhase;
        float moveBlend;
        float idleTime;
        Vector2 leanTilt;
        Vector2 leanTiltVelocity;
        Vector2 hatFollowTilt;
        Vector2 hatFollowVelocity;
        Vector2 tailSwing;
        Vector2 tailSwingVelocity;

        void Awake()
        {
            motionSource = transform.parent != null ? transform.parent : transform;
            sourceBody = transform.parent != null ? motionSource.GetComponent<Rigidbody>() : null;
            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
            restLocalScale = transform.localScale;
            if (hat != null)
            {
                hatRestPosition = hat.localPosition;
                hatRestRotation = hat.localRotation;
            }
            if (scarfTail != null)
            {
                tailRestPosition = scarfTail.localPosition;
                tailRestRotation = scarfTail.localRotation;
                Vector2 pivotOffset = new Vector2(scarfTailPivot.x, scarfTailPivot.z);
                tailOutwardDirection = pivotOffset.sqrMagnitude > 0.0001f ? pivotOffset.normalized : Vector2.up;
            }
        }

        void OnEnable()
        {
            previousSourcePosition = motionSource.position;
            physicsStepPosition = motionSource.position;
            physicsStepRotation = motionSource.rotation;
            smoothedVelocity = Vector3.zero;
            smoothedAcceleration = Vector3.zero;
            stepPhase = 0f;
            moveBlend = 0f;
            idleTime = 0f;
            leanTilt = leanTiltVelocity = Vector2.zero;
            hatFollowTilt = hatFollowVelocity = Vector2.zero;
            tailSwing = tailSwingVelocity = Vector2.zero;
        }

        void OnDisable()
        {
            transform.localPosition = restLocalPosition;
            transform.localRotation = restLocalRotation;
            transform.localScale = restLocalScale;
            if (hat != null)
            {
                hat.localPosition = hatRestPosition;
                hat.localRotation = hatRestRotation;
            }
            if (scarfTail != null)
            {
                scarfTail.localPosition = tailRestPosition;
                scarfTail.localRotation = tailRestRotation;
            }
        }

        void FixedUpdate()
        {
            physicsStepPosition = motionSource.position;
            physicsStepRotation = motionSource.rotation;
        }

        void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 sourcePosition = motionSource.position;
            Quaternion sourceRotation = motionSource.rotation;
            bool movedByPhysics = sourceBody != null && !sourceBody.isKinematic;
            if (glideBetweenPhysicsSteps && movedByPhysics && (sourcePosition - physicsStepPosition).magnitude < teleportDistance)
            {
                float stepFraction = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
                sourcePosition = Vector3.Lerp(physicsStepPosition, sourcePosition, stepFraction);
                sourceRotation = Quaternion.Slerp(physicsStepRotation, sourceRotation, stepFraction);
            }

            ReadMotion(sourcePosition, deltaTime);

            float speedFraction = Mathf.Clamp01(smoothedVelocity.magnitude / referenceSpeed);
            moveBlend = Mathf.MoveTowards(moveBlend, speedFraction > 0.08f ? 1f : 0f, deltaTime * 6f);
            if (moveBlend <= 0f)
            {
                stepPhase = 0f;
                idleTime += deltaTime;
            }
            else
            {
                stepPhase += smoothedVelocity.magnitude * deltaTime / stepLength;
                idleTime = 0f;
            }

            Vector3 localVelocity = Quaternion.Inverse(sourceRotation) * smoothedVelocity;
            Vector2 leanTarget = Vector2.ClampMagnitude(new Vector2(smoothedVelocity.x, smoothedVelocity.z) / referenceSpeed, 1f) * leanDegrees;
            StepSpring(ref leanTilt, ref leanTiltVelocity, leanTarget, leanFrequency, leanDamping, deltaTime);
            leanTilt = Vector2.ClampMagnitude(leanTilt, maximumLeanDegrees);

            float hopWave = Mathf.Abs(Mathf.Sin(stepPhase * Mathf.PI));
            float waddleWave = Mathf.Sin(stepPhase * Mathf.PI);
            float hop = hopHeight * hopWave * moveBlend;
            float landing = Mathf.Pow(1f - hopWave, 3f) * moveBlend;
            float idleWave = moveBlend <= 0f ? Mathf.Sin(idleTime * 2f * Mathf.PI / idleBobPeriod) : 0f;
            float heightScale = 1f + hopStretch * hopWave * moveBlend - landingSquash * landing + idleBobHeight / modelHeight * idleWave;
            float widthScale = 1f - 0.5f * (heightScale - 1f);

            Vector3 localLean = Quaternion.Inverse(sourceRotation) * new Vector3(leanTilt.x, 0f, leanTilt.y);
            Vector2 bodyTilt = new Vector2(localLean.x + waddleDegrees * waddleWave * moveBlend, localLean.z);

            float rimLift = baseRadius * widthScale * Mathf.Sin(bodyTilt.magnitude * Mathf.Deg2Rad);
            Quaternion animatedRotation = TiltRotation(bodyTilt) * restLocalRotation;
            if (transform.parent != null)
            {
                Vector3 sourceScale = motionSource.lossyScale;
                Vector3 animatedPosition = restLocalPosition + Vector3.up * ((hop + rimLift) / Mathf.Max(0.0001f, sourceScale.y));
                transform.SetPositionAndRotation(sourcePosition + sourceRotation * Vector3.Scale(sourceScale, animatedPosition), sourceRotation * animatedRotation);
            }
            else
            {
                transform.localPosition = restLocalPosition + Vector3.up * (hop + rimLift);
                transform.localRotation = animatedRotation;
            }
            transform.localScale = Vector3.Scale(restLocalScale, new Vector3(widthScale, heightScale, widthScale));

            AnimateHat(bodyTilt, deltaTime);
            Vector3 localAcceleration = Quaternion.Inverse(sourceRotation) * smoothedAcceleration;
            AnimateScarfTail(bodyTilt, localVelocity, localAcceleration, hopWave, deltaTime);
        }

        void ReadMotion(Vector3 sourcePosition, float deltaTime)
        {
            Vector3 travelled = sourcePosition - previousSourcePosition;
            previousSourcePosition = sourcePosition;
            travelled.y = 0f;
            if (travelled.magnitude > teleportDistance)
            {
                travelled = Vector3.zero;
            }
            smoothedVelocity = Vector3.SmoothDamp(smoothedVelocity, travelled / deltaTime, ref smoothedAcceleration, velocitySmoothTime, Mathf.Infinity, deltaTime);
        }

        void AnimateHat(Vector2 bodyTilt, float deltaTime)
        {
            if (hat == null)
            {
                return;
            }
            StepSpring(ref hatFollowTilt, ref hatFollowVelocity, bodyTilt, hatFrequency, hatDamping, deltaTime);
            Vector2 hatTilt = Vector2.ClampMagnitude((hatFollowTilt - bodyTilt) * hatLagFactor, maximumHatDegrees);
            ApplyAroundPivot(hat, hatRestPosition, hatRestRotation, hatPivot, TiltRotation(hatTilt));
        }

        void AnimateScarfTail(Vector2 bodyTilt, Vector3 localVelocity, Vector3 localAcceleration, float hopWave, float deltaTime)
        {
            if (scarfTail == null)
            {
                return;
            }
            Vector2 trail = -new Vector2(localVelocity.x, localVelocity.z) / referenceSpeed * tailTrailDegrees;
            Vector2 hopFlap = tailOutwardDirection * (tailHopFlapDegrees * hopWave * moveBlend);
            Vector2 inertia = -new Vector2(localAcceleration.x, localAcceleration.z) * tailInertiaDegrees;
            Vector2 swingTarget = Vector2.ClampMagnitude(bodyTilt + trail + hopFlap + inertia, maximumTailDegrees);
            StepSpring(ref tailSwing, ref tailSwingVelocity, swingTarget, tailFrequency, tailDamping, deltaTime);

            float outward = Vector2.Dot(tailSwing, tailOutwardDirection);
            if (outward < -tailInwardLimitDegrees)
            {
                tailSwing += tailOutwardDirection * (-tailInwardLimitDegrees - outward);
                float outwardSpeed = Vector2.Dot(tailSwingVelocity, tailOutwardDirection);
                if (outwardSpeed < 0f)
                {
                    tailSwingVelocity -= tailOutwardDirection * outwardSpeed * 1.3f;
                }
            }
            tailSwing = Vector2.ClampMagnitude(tailSwing, maximumTailDegrees);
            ApplyAroundPivot(scarfTail, tailRestPosition, tailRestRotation, scarfTailPivot, TiltRotation(-tailSwing));
        }

        static Quaternion TiltRotation(Vector2 tilt)
        {
            float angle = tilt.magnitude;
            if (angle < 0.0001f)
            {
                return Quaternion.identity;
            }
            Vector3 direction = new Vector3(tilt.x, 0f, tilt.y) / angle;
            return Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, direction));
        }

        static void ApplyAroundPivot(Transform piece, Vector3 restPosition, Quaternion restRotation, Vector3 pivot, Quaternion rotation)
        {
            piece.localPosition = pivot + rotation * (restPosition - pivot);
            piece.localRotation = rotation * restRotation;
        }

        static void StepSpring(ref Vector2 value, ref Vector2 velocity, Vector2 target, float frequency, float damping, float deltaTime)
        {
            float angularFrequency = frequency * 2f * Mathf.PI;
            float remaining = deltaTime;
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, MaximumSpringStep);
                Vector2 acceleration = (target - value) * (angularFrequency * angularFrequency) - velocity * (2f * damping * angularFrequency);
                velocity += acceleration * step;
                value += velocity * step;
                remaining -= step;
            }
        }
    }
}
