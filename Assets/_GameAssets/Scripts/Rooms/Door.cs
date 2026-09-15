using System.Collections;
using UnityEngine;
using Utils;

[ExecuteAlways]
[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class Door : MonoBehaviour
{
    [SerializeField] private DoorEnviro _doorEnviro;
    [SerializeField] private SpriteRenderer _enterZoneSprite;
    [SerializeField] private DoorDirection _direction;
    [SerializeField, Min(0f)] private float _enterDuration = 2f;

    private Coroutine _enterRoutine;

    public DoorDirection Direction => _direction;
    public bool IsLocked { get; private set; }

    public void SetEnabled(bool _enabled)
    {
        gameObject.SetActive(_enabled);
    }

    public void SetLocked(bool _locked)
    {
        IsLocked = _locked;
        _enterZoneSprite.enabled = !_locked;
    }

    private void OnEnable()
    {
        if (_doorEnviro != null)
            _doorEnviro.SetEnabled(true);
    }

    private void OnDisable()
    {
        if (_doorEnviro != null)
            _doorEnviro.SetEnabled(false);

        StopEntering();
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (!IsPlayer(_other))
            return;

        if (IsLocked)
        {
            this.Log($"{_direction} door is locked, complete the room first.");
            return;
        }

        this.Log($"Entering {_direction} door, stay {_enterDuration}s to go through.");
        StopEntering();
        _enterRoutine = StartCoroutine(EnterAfterDelay());
    }

    private void OnTriggerExit(Collider _other)
    {
        if (!IsPlayer(_other) || _enterRoutine == null)
            return;

        this.Log($"Left {_direction} door early.");
        StopEntering();
    }

    private IEnumerator EnterAfterDelay()
    {
        yield return new WaitForSeconds(_enterDuration);

        _enterRoutine = null;
        this.Log($"Going through {_direction} door.");
        RunManager.Instance.EnterNextRoom(_direction);
    }

    private void StopEntering()
    {
        if (_enterRoutine == null)
            return;

        StopCoroutine(_enterRoutine);
        _enterRoutine = null;
    }

    private bool IsPlayer(Collider _other)
    {
        return _other == RunManager.Instance.Player.Collider;
    }

    private void Reset()
    {
        Rigidbody _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;

        GetComponent<Collider>().isTrigger = true;

        _enterZoneSprite = GetComponentInChildren<SpriteRenderer>(true);
    }
}
