using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

public class XRButtonSink : XRSimpleInteractable
{
    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right,
        Forward,
        Back
    }

    [Header("Movement Settings")]
    public MoveDirection direction = MoveDirection.Down;
    public float sinkDistance = 0.125f;
    public float speed = 5f;

    [Header("Events")]
    public UnityEvent OnPressed;
    public UnityEvent OnReleased;

    private Vector3 _startPos;
    private bool _isAnimating = false;
    private Coroutine _currentCoroutine;

    protected override void Awake()
    {
        base.Awake();
        _startPos = transform.localPosition;
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);
        if (!_isAnimating)
        {
            _currentCoroutine = StartCoroutine(AnimatePressRelease());
        }
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);
        if (_isAnimating && _currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
        _isAnimating = false;

        // Return to start if the interaction is interrupted mid-way
        if (Vector3.Distance(transform.localPosition, _startPos) > 0.001f)
        {
            StartCoroutine(QuickReturn());
        }
    }

    private IEnumerator AnimatePressRelease()
    {
        _isAnimating = true;

        // Calculate the target position based on chosen direction
        Vector3 directionVector = GetDirectionVector(direction);
        Vector3 sunkPos = _startPos + (directionVector * sinkDistance);

        // Sink down
        yield return StartCoroutine(MoveTo(sunkPos));
        OnPressed.Invoke();

        // Brief hold
        yield return new WaitForSeconds(0.05f);

        // Return up
        yield return StartCoroutine(MoveTo(_startPos));
        OnReleased.Invoke();

        _isAnimating = false;
        _currentCoroutine = null;
    }

    private IEnumerator QuickReturn()
    {
        _isAnimating = true;
        yield return StartCoroutine(MoveTo(_startPos));
        OnReleased.Invoke();
        _isAnimating = false;
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.localPosition, target) > 0.0001f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.localPosition = target;
    }

    private Vector3 GetDirectionVector(MoveDirection dir)
    {
        switch (dir)
        {
            case MoveDirection.Up: return Vector3.up;
            case MoveDirection.Down: return Vector3.down;
            case MoveDirection.Left: return Vector3.left;
            case MoveDirection.Right: return Vector3.right;
            case MoveDirection.Forward: return Vector3.forward;
            case MoveDirection.Back: return Vector3.back;
            default: return Vector3.down;
        }
    }
}