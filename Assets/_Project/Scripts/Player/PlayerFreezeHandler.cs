using UnityEngine;
using System.Collections;

//TODO
public class PlayerFreezeHandler : MonoBehaviour
{
    [Header("Freeze Settings")]
    [SerializeField] private float _maxFreezeTime = 3.0f;
    [SerializeField] private float _thawMult = 2.0f;
    [SerializeField] private float _freezeDuration = 2.0f;

    private float _currentFreezeTime = 0f;
    public bool IsFrozen { get; private set; }

    // this needs to be set to the script that handles picking up/dropping
    // set for Snowball script for local testing
    public Snowball CurrentHeldRollball;

    void Update()
    {
        if (IsFrozen) return;

        HandleTemperature();
    }

    private void HandleTemperature()
    {
        if (CurrentHeldRollball != null && CurrentHeldRollball.IsFullyGrown)
        {
            _currentFreezeTime += Time.deltaTime;

            if (_currentFreezeTime >= _maxFreezeTime)
            {
                TriggerFreeze();
            }
        } else
        {
            _currentFreezeTime = Mathf.Max(0, _currentFreezeTime-Time.deltaTime*_thawMult);
        }
    }

    private void TriggerFreeze()
    {
        IsFrozen = true;
        _currentFreezeTime = 0f;

        Debug.Log($"{gameObject.name} froze from hogging the snowball.");

        // force player to drop the snowball
        if (CurrentHeldRollball != null)
        {
            CurrentHeldRollball.StopRolling();
            CurrentHeldRollball = null;
        }

        StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        // movement script should check IsFrozen and stop movement while it's true
        // for local testing, i just disable DummyPusher here
        DummyPusher dummy = GetComponent<DummyPusher>();
        if (dummy != null) dummy.enabled = false;

        yield return new WaitForSeconds(_freezeDuration);

        IsFrozen = false;
        if (dummy != null) dummy.enabled = true;
        Debug.Log($"{gameObject.name} thawed out and can move again.");
    }
}
