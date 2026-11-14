using UnityEngine;
using System.Collections;

public class LocalFreezeTest_Snowball : MonoBehaviour
{
    [Header("Size Settings")]
    [SerializeField] private float _minSize = 0.3f;
    [SerializeField] private float _maxSize = 2.0f;
    [SerializeField] private float _growthMult = 0.05f;

    //IMP: should probably change it so that the player is the one counting the freeze timer
    // and max timer, otherwise a different player will grab the ball midway and have less
    // to hold it before they freeze than the og owner
    [Header("Freeze Penalty")]
    [SerializeField] private float _maxFreezeTime = 3.0f;   // seconds it can be held before fully freezing
    [SerializeField] private float _thawMult = 2.0f;
    private int _ownerId = -1;
    private bool _isBeingRolled = false;
    private float _currentFreezeTime = 0f;

    [Header("Visuals")]
    [SerializeField] private GameObject _outlineVisual;

    private Vector3 _lastPos;
    public bool IsFullyGrown { get; private set; }
    //public GameObject CurrentOwner{ get; private set; }

// for local testing. DELETE LATER
    private GameObject _locTestPlayer;
    private Coroutine _locTestFreezeRoutine;

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
        HandleFreezePenalty();
    }

    // PUBLIC FUNCTIONS

    // Call from Player script when grabbed (Q)
    public void StartRolling(int playerId)
    {
        _ownerId = playerId;
        _isBeingRolled = true;
        _lastPos = transform.position;  // reset to avoid growth spikes
    }

    // for local testing. DELETE LATER
    public void LocTestStartRolling(int playerId, GameObject playerObj)
    {
        _locTestPlayer = playerObj;
        _ownerId = playerId;
        _isBeingRolled = true;
        _lastPos = transform.position;  // reset to avoid growth spikes
    }

    // Call from Player script whe the snowball is dropped (Q) or thrown (Space)
    public void StopRolling()
    {
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

    private void HandleFreezePenalty()
    {
        if (IsFullyGrown && _isBeingRolled)
        {
            _currentFreezeTime += Time.deltaTime;
            if (_currentFreezeTime >= _maxFreezeTime)
            {
                LocTestTriggerFreezePenalty();  // for local testing. DELETE LATER
                //TriggerFreezePenalty();
            }
        } else
        {
            _currentFreezeTime = Mathf.Max(0, _currentFreezeTime-Time.deltaTime*_thawMult);
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

    private void TriggerFreezePenalty()
    {
        // currently works assuming ownerId. switch to other method depending on player implementation

        Debug.Log($"Player {_ownerId} rolled the snowball for too long and is now frozen.");

        // TODO: Fire event / method on Player to freeze for X sec.

        StopRolling();
    }

    // for local testing. DELETE LATER
    private void LocTestTriggerFreezePenalty()
    {
        Debug.Log($"Player {_ownerId} rolled the snowball for too long and is now frozen.");

        DummyPusher playerScript = _locTestPlayer.GetComponent<DummyPusher>();
        if (playerScript != null)
        {
            playerScript.enabled = false;
            _locTestFreezeRoutine = StartCoroutine(LocTestFreezeCoroutine());
        }

        StopRolling();
    }

    // for local testing. DELETE LATER
    private IEnumerator LocTestFreezeCoroutine()
    {
        yield return new WaitForSeconds(5f);
        DummyPusher playerScript = _locTestPlayer.GetComponent<DummyPusher>();
        if (playerScript != null)
        {
            playerScript.enabled = true;
        }
    }

    // Temporary mock for picking up snow from ground system.
    private float MockTakeSnow(Vector3 position, float radius)
    {
        return 1f;
    }
}
