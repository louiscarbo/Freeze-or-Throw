using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(LineRenderer))]
public class GrappleHook : NetworkBehaviour
{
    [Header("Mouse Targeting & Range")]
    [SerializeField] private LayerMask grappleLayer; // Select "GrapplePoint" in Inspector
    [SerializeField] private float maxGrappleDistance = 20f;
    [SerializeField] private float clickForgivenessRadius = 1.0f; // SphereCast radius so clicking thin poles feels snappy
    [SerializeField] private float cooldown = 1.2f;

    [Header("Smooth Sling Physics")]
    [SerializeField] private float targetOrbitSpeed = 16f;      // Peak orbital swing speed
    [SerializeField] private float orbitAcceleration = 38f;     // How smoothly speed ramps up into the sling
    [SerializeField] private float radiusPullSpeed = 6f;        // Gently pulls you into a clean orbit radius
    [SerializeField] private float minOrbitRadius = 2.8f;
    [SerializeField] private float releaseBoostMultiplier = 1.35f;

    [Header("Visuals")]
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private Color ropeColor = Color.cyan;
    [SerializeField] private float ropeWidth = 0.1f;

    private Rigidbody _rb;
    private LineRenderer _lineRenderer;
    private Camera _mainCamera;

    private NetworkVariable<bool> isGrappling = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<Vector3> grappleTarget = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float _ropeLength;
    private float _currentAngleRad;
    private float _currentAngularSpeed;
    private int _orbitDirection = 1; // +1 = Clockwise (approaching on left), -1 = Counter-Clockwise (approaching on right)
    private float _lastGrappleTime = -99f;

    public bool IsSwinging => isGrappling.Value;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _lineRenderer = GetComponent<LineRenderer>();

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = ropeWidth;
        _lineRenderer.endWidth = ropeWidth;
        _lineRenderer.startColor = ropeColor;
        _lineRenderer.endColor = ropeColor;

        if (_lineRenderer.sharedMaterial == null)
        {
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                _lineRenderer.material = new Material(defaultShader);
            }
        }

        _lineRenderer.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        isGrappling.OnValueChanged += OnGrappleStateChanged;
        UpdateRopeVisuals();
    }

    public override void OnNetworkDespawn()
    {
        isGrappling.OnValueChanged -= OnGrappleStateChanged;
    }

    private void OnGrappleStateChanged(bool previousValue, bool newValue)
    {
        UpdateRopeVisuals();
    }

    private void Update()
    {
        UpdateRopeVisuals();

        if (!IsOwner) return;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        // Left-Click on a nearby pole to latch on and start slinging
        if (Input.GetMouseButtonDown(0) && !isGrappling.Value && Time.time >= _lastGrappleTime + cooldown)
        {
            TryClickPole();
        }

        // Release Left-Click (or press Space) to sling-launch out of the orbit
        if (isGrappling.Value && (Input.GetMouseButtonUp(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            ReleaseSling();
        }
    }

    private void UpdateRopeVisuals()
    {
        if (isGrappling.Value)
        {
            Vector3 startPos = fireOrigin != null ? fireOrigin.position : transform.position + Vector3.up * 0.6f;
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, grappleTarget.Value);
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isGrappling.Value) return;

        // Smoothly accelerate angular speed up to targetOrbitSpeed for a natural sling feel
        float targetAngularSpeed = targetOrbitSpeed / Mathf.Max(0.5f, _ropeLength);
        float angularAccel = orbitAcceleration / Mathf.Max(0.5f, _ropeLength);
        _currentAngularSpeed = Mathf.MoveTowards(_currentAngularSpeed, targetAngularSpeed, angularAccel * Time.fixedDeltaTime);

        // Advance angle around the pole
        _currentAngleRad -= _orbitDirection * _currentAngularSpeed * Time.fixedDeltaTime;

        // Slightly tighten long ropes toward an ideal sling radius
        float idealRadius = Mathf.Clamp(_ropeLength, minOrbitRadius, maxGrappleDistance * 0.65f);
        _ropeLength = Mathf.MoveTowards(_ropeLength, idealRadius, radiusPullSpeed * Time.fixedDeltaTime);

        Vector3 offsetFromPole = new Vector3(
            Mathf.Cos(_currentAngleRad) * _ropeLength,
            0f,
            Mathf.Sin(_currentAngleRad) * _ropeLength
        );

        Vector3 nextPos = new Vector3(
            grappleTarget.Value.x + offsetFromPole.x,
            transform.position.y,
            grappleTarget.Value.z + offsetFromPole.z
        );

        Vector3 moveDelta = nextPos - transform.position;
        moveDelta.y = 0f;
        Vector3 tangentDir = moveDelta.sqrMagnitude > 0.0001f ? moveDelta.normalized : transform.forward;

        float currentLinearSpeed = _currentAngularSpeed * _ropeLength;
        _rb.MovePosition(nextPos);
        _rb.linearVelocity = new Vector3(tangentDir.x * currentLinearSpeed, _rb.linearVelocity.y, tangentDir.z * currentLinearSpeed);

        if (tangentDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(tangentDir);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 18f * Time.fixedDeltaTime));
        }
    }

    private void TryClickPole()
    {
        if (_mainCamera == null) return;

        Ray mouseRay = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // Raycast (with SphereCast fallback) from camera through mouse cursor onto GrapplePoint layer
        bool hitPole = Physics.Raycast(mouseRay, out RaycastHit hit, 150f, grappleLayer) ||
                       Physics.SphereCast(mouseRay, clickForgivenessRadius, out hit, 150f, grappleLayer);

        if (!hitPole) return;

        // Check horizontal distance between the player and the clicked pole
        Vector3 flatPlayerPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatPolePos = new Vector3(hit.collider.bounds.center.x, 0f, hit.collider.bounds.center.z);

        if (Vector3.Distance(flatPlayerPos, flatPolePos) <= maxGrappleDistance)
        {
            AttachToPole(hit.collider);
        }
    }

    private void AttachToPole(Collider poleCollider)
    {
        Transform anchor = poleCollider.transform.Find("AnchorPoint");
        Vector3 attachPos = anchor != null
            ? anchor.position
            : new Vector3(poleCollider.bounds.center.x, poleCollider.bounds.max.y * 0.75f, poleCollider.bounds.center.z);

        Vector3 offsetFromPole = transform.position - attachPos;
        offsetFromPole.y = 0f;

        _ropeLength = Mathf.Max(minOrbitRadius, offsetFromPole.magnitude);
        _currentAngleRad = Mathf.Atan2(offsetFromPole.z, offsetFromPole.x);

        // Determine Clockwise (+1) vs Counter-Clockwise (-1) from player's approach direction
        Vector3 toPoleDir = (-offsetFromPole).normalized;
        Vector3 moveDir = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (moveDir.sqrMagnitude < 0.05f)
        {
            moveDir = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        }
        else
        {
            moveDir.Normalize();
        }

        float sideSign = Vector3.Cross(moveDir, toPoleDir).y;
        _orbitDirection = sideSign >= 0f ? 1 : -1;

        // Start from player's current entry speed so the transition into the sling is seamless
        float entrySpeed = Mathf.Clamp(_rb.linearVelocity.magnitude, targetOrbitSpeed * 0.45f, targetOrbitSpeed);
        _currentAngularSpeed = entrySpeed / _ropeLength;

        grappleTarget.Value = attachPos;
        isGrappling.Value = true;
        _lastGrappleTime = Time.time;
        UpdateRopeVisuals();
    }

    private void ReleaseSling()
    {
        isGrappling.Value = false;
        UpdateRopeVisuals();

        // Compute exact forward tangent of the spin for the sling launch
        Vector3 radialOut = new Vector3(Mathf.Cos(_currentAngleRad), 0f, Mathf.Sin(_currentAngleRad));
        Vector3 clockwiseTangent = new Vector3(radialOut.z, 0f, -radialOut.x);
        Vector3 launchDir = (clockwiseTangent * _orbitDirection).normalized;

        float currentLinearSpeed = _currentAngularSpeed * _ropeLength;
        float launchSpeed = Mathf.Max(targetOrbitSpeed, currentLinearSpeed) * releaseBoostMultiplier;

        _rb.linearVelocity = new Vector3(launchDir.x * launchSpeed, _rb.linearVelocity.y, launchDir.z * launchSpeed);
    }
}