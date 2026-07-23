using System.Collections;
using UnityEngine;

public class JellyAnimation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visualRoot;

    [Header("Idle")]
    [SerializeField] private bool playIdle = false;
    [SerializeField] private float idleSpeed = 2.2f;
    [SerializeField] private float idleAmount = 0.04f;

    [Header("Drag")]
    [SerializeField] private float dragJiggleSpeed = 18f;
    [SerializeField] private float dragJiggleAmountX = 0.08f;
    [SerializeField] private float dragJiggleAmountY = 0.06f;
    [SerializeField] private float dragStretchAmount = 0.12f;
    [SerializeField] private float dragReturnTime = 0.08f;

    [Header("Landing")]
    [SerializeField] private float landSquashX = 1.14f;
    [SerializeField] private float landSquashY = 0.86f;
    [SerializeField] private float landRecoverTime = 0.12f;

    [Header("Match")]
    [SerializeField] private float preCollectPunchScale = 1.12f;
    [SerializeField] private float preCollectTime = 0.08f;
    [SerializeField] private float collectMoveTime = 0.16f;
    [SerializeField] private float collectShrinkTime = 0.1f;
    [SerializeField] private AnimationCurve easeOut = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 baseScale = Vector3.one;
    private Coroutine idleRoutine;
    private Coroutine animRoutine;
    private Coroutine dragRoutine;
    private bool isBusy;
    private bool isDragging;
    private Vector3 dragVelocity;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        baseScale = visualRoot.localScale;
    }

    private void OnEnable()
    {
        if (playIdle)
            StartIdle();
    }

    public void SetBaseScale(Vector3 newScale)
    {
        baseScale = newScale;
        if (!isBusy && !isDragging)
            visualRoot.localScale = baseScale;
    }

    public void SetDragVelocity(Vector3 worldDelta)
    {
        dragVelocity = worldDelta;
    }

    public void StartDragJiggle()
    {
        if (!gameObject.activeInHierarchy || isBusy)
            return;

        StopIdle();

        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }

        if (dragRoutine != null)
            StopCoroutine(dragRoutine);

        isDragging = true;
        dragRoutine = StartCoroutine(DragJiggleRoutine());
    }

    public void StopDragJiggle(bool snapToBase = true)
    {
        isDragging = false;
        dragVelocity = Vector3.zero;

        if (dragRoutine != null)
        {
            StopCoroutine(dragRoutine);
            dragRoutine = null;
        }

        if (snapToBase)
            StartCoroutine(ReturnToBaseRoutine());
    }

    public void StartIdle()
    {
        if (!playIdle || isBusy || isDragging) return;

        if (idleRoutine != null)
            StopCoroutine(idleRoutine);

        idleRoutine = StartCoroutine(IdleRoutine());
    }

    public void StopIdle()
    {
        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }
    }

    public void PlayLanding()
    {
        if (!gameObject.activeInHierarchy) return;

        StopDragJiggle(false);
        RestartAnim(LandingRoutine());
    }

    public void PlayPreCollectPulse()
    {
        if (!gameObject.activeInHierarchy) return;

        StopDragJiggle(false);
        RestartAnim(PreCollectPulseRoutine());
    }

    public void PlayCollectToPoint(Vector3 target, System.Action onComplete = null)
    {
        if (!gameObject.activeInHierarchy) return;

        StopDragJiggle(false);
        RestartAnim(CollectRoutine(target, onComplete));
    }

    private void RestartAnim(IEnumerator routine)
    {
        StopIdle();

        if (dragRoutine != null)
        {
            StopCoroutine(dragRoutine);
            dragRoutine = null;
        }

        isDragging = false;

        if (animRoutine != null)
            StopCoroutine(animRoutine);

        animRoutine = StartCoroutine(routine);
    }

    private IEnumerator IdleRoutine()
    {
        while (true)
        {
            float t = Time.time * idleSpeed;
            float sx = 1f + Mathf.Sin(t) * idleAmount;
            float sy = 1f - Mathf.Sin(t) * idleAmount;
            visualRoot.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            yield return null;
        }
    }

    private IEnumerator DragJiggleRoutine()
    {
        while (isDragging)
        {
            float t = Time.time * dragJiggleSpeed;

            float waveX = Mathf.Sin(t) * dragJiggleAmountX;
            float waveY = Mathf.Cos(t * 0.9f) * dragJiggleAmountY;

            Vector2 v = new Vector2(dragVelocity.x, dragVelocity.y);
            float speed = Mathf.Clamp01(v.magnitude * 12f);

            float stretchX = Mathf.Clamp(v.x, -1f, 1f) * dragStretchAmount * speed;
            float stretchY = Mathf.Clamp(v.y, -1f, 1f) * dragStretchAmount * speed;

            float sx = 1f + waveX + stretchX - stretchY * 0.35f;
            float sy = 1f - waveY + stretchY - stretchX * 0.35f;

            visualRoot.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            yield return null;
        }
    }

    private IEnumerator ReturnToBaseRoutine()
    {
        Vector3 start = visualRoot.localScale;
        float t = 0f;

        while (t < dragReturnTime)
        {
            t += Time.deltaTime;
            float p = t / dragReturnTime;
            visualRoot.localScale = Vector3.Lerp(start, baseScale, p);
            yield return null;
        }

        visualRoot.localScale = baseScale;
    }

    private IEnumerator LandingRoutine()
    {
        isBusy = true;

        Vector3 squash = new Vector3(baseScale.x * landSquashX, baseScale.y * landSquashY, baseScale.z);
        visualRoot.localScale = squash;

        float t = 0f;
        while (t < landRecoverTime)
        {
            t += Time.deltaTime;
            float p = t / landRecoverTime;
            visualRoot.localScale = Vector3.Lerp(squash, baseScale, p);
            yield return null;
        }

        visualRoot.localScale = baseScale;
        isBusy = false;
    }

    private IEnumerator PreCollectPulseRoutine()
    {
        isBusy = true;

        Vector3 big = baseScale * preCollectPunchScale;

        float t = 0f;
        while (t < preCollectTime)
        {
            t += Time.deltaTime;
            float p = t / preCollectTime;
            visualRoot.localScale = Vector3.Lerp(baseScale, big, p);
            yield return null;
        }

        t = 0f;
        while (t < preCollectTime)
        {
            t += Time.deltaTime;
            float p = t / preCollectTime;
            visualRoot.localScale = Vector3.Lerp(big, baseScale, p);
            yield return null;
        }

        visualRoot.localScale = baseScale;
        isBusy = false;
    }

    private IEnumerator CollectRoutine(Vector3 target, System.Action onComplete)
    {
        isBusy = true;

        Vector3 startPos = transform.position;
        Vector3 startScale = visualRoot.localScale;
        Vector3 punchScale = baseScale * preCollectPunchScale;

        float t = 0f;
        while (t < preCollectTime)
        {
            t += Time.deltaTime;
            float p = t / preCollectTime;
            visualRoot.localScale = Vector3.Lerp(startScale, punchScale, p);
            yield return null;
        }

        t = 0f;
        while (t < collectMoveTime)
        {
            t += Time.deltaTime;
            float p = t / collectMoveTime;
            float eased = easeOut.Evaluate(p);

            transform.position = Vector3.Lerp(startPos, target, eased);
            visualRoot.localScale = Vector3.Lerp(punchScale, baseScale * 0.78f, eased);
            yield return null;
        }

        t = 0f;
        Vector3 shrinkStart = visualRoot.localScale;
        while (t < collectShrinkTime)
        {
            t += Time.deltaTime;
            float p = t / collectShrinkTime;
            visualRoot.localScale = Vector3.Lerp(shrinkStart, Vector3.zero, p);
            yield return null;
        }

        visualRoot.localScale = Vector3.zero;
        isBusy = false;
        onComplete?.Invoke();
    }
}