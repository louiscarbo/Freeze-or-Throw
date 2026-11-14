using UnityEngine;
using System.Collections;

public class Snowball : MonoBehaviour
{
    [Header("Size Settings")]
    [SerializeField] private float _minSize = 0.3f;
    [SerializeField] private float _maxSize = 2.0f;
    [SerializeField] private float _growthMult = 0.05f;

    [Header("Ownership")]
    private int _ownerId = -1;
    private bool _isBeingRolled = false;

    [Header("Visuals")]
    [SerializeField] private GameObject _outlineVisual;

    private Vector3 _lastPos;
    public bool IsFullyGrown { get; private set; }

    // UNITY LIFECYCLE
    
    void Start()
    {
        transform.localScale = Vector3.one*_minSize;
        _lastPos = transform.position;

        if (_outlineVisual != null) _outlineVisual.SetActive(false);
    }

    void Update()
    {
        HandleGrowth();
    }

    // PUBLIC FUNCTIONS

    // Call from Player script when grabbed (Q)
    public void StartRolling(int playerId)
    {
        Debug.Log($"Somebody grabbed {gameObject.name}");
        _ownerId = playerId;
        _isBeingRolled = true;
        _lastPos = transform.position;  // reset to avoid growth spikes
    }

    // Call from Player script whe the snowball is dropped (Q) or thrown (Space)
    public void StopRolling()
    {
        Debug.Log($"Somebody dropped {gameObject.name}");
        _ownerId = -1;
        _isBeingRolled = false;
    }

    // UPDATE CHECK FUNCTIONS

    private void HandleGrowth()
    {
        if (IsFullyGrown || !_isBeingRolled) return;

        Vector2 currentPosXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 lastPosXZ = new Vector2(_lastPos.x, _lastPos.z);
        float distanceRolled = Vector2.Distance(currentPosXZ, lastPosXZ);
        _lastPos = transform.position;

        if (distanceRolled > 0.001f)
        {
            // mocking connection to snow on ground system
            float snowGathered = MockTakeSnow(transform.position, transform.localScale.x / 2f);

            if (snowGathered > 0f)
            {
                Grow(distanceRolled*snowGathered*_growthMult);
            }
        }
    }

    // INTERNAL FUNCTIONS

    private void Grow(float amount)
    {
        float currentSize = transform.localScale.x;
        float newSize = Mathf.Clamp(currentSize+amount, _minSize, _maxSize);

        transform.localScale = Vector3.one*newSize;

        if (newSize >= _maxSize && !IsFullyGrown)
        {
            IsFullyGrown = true;
            // "ready for snowman" outline activated
            if (_outlineVisual != null) _outlineVisual.SetActive(true);

        }
    }

    // Temporary mock for picking up snow from ground system.
    private float MockTakeSnow(Vector3 position, float radius)
    {
        return 1f;
    }
}
