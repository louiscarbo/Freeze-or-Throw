using TreeEditor;
using UnityEngine;

public class Snowball : MonoBehaviour
{
    [Header("Size settings")]
    [SerializeField] private float _minSize = 0.3f;
    [SerializeField] private float _maxSize = 2.0f;
    [SerializeField] private float _growthMult = 0.05f;

    [Header("Visuals")]
    [SerializeField] private GameObject _outlineVisual;

    private Vector3 _lastPos;
    public bool IsFullyGrown { get; private set; }

    // UNITY LIFECYCLE
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.localScale = Vector3.one*_minSize;
        _lastPos = transform.position;

        if (_outlineVisual != null) _outlineVisual.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsFullyGrown) return;

        Vector2 currentPosXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 lastPosXZ = new Vector2(_lastPos.x, _lastPos.z);
        float distanceRolled = Vector3.Distance(currentPosXZ, lastPosXZ);
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

    // INTERNAL MECHANICS
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

    // Temporary mock for picking up snow from ground system
    private float MockTakeSnow(Vector3 position, float radius)
    {
        return 1f;
    }
}
