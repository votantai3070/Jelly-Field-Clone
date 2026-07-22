using System.Collections;
using UnityEngine;

public class JellyAnimation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visualRoot;

    [Header("Idle")]
    [SerializeField] private bool playIdle = true;
    [SerializeField] private float idleSpeed = 2.2f;
    [SerializeField] private float idleAmount = 0.04f;

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
    private bool isBusy;

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
        if (!isBusy)
            visualRoot.localScale = baseScale;
    }

    public void StartIdle()
    {
        if (!playIdle || isBusy) return;

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
        RestartAnim(LandingRoutine());
    }

    public void PlayPreCollectPulse()
    {
        if (!gameObject.activeInHierarchy) return;
        RestartAnim(PreCollectPulseRoutine());
    }

    public void PlayCollectToPoint(Vector3 target, System.Action onComplete = null)
    {
        if (!gameObject.activeInHierarchy) return;
        RestartAnim(CollectRoutine(target, onComplete));
    }

    private void RestartAnim(IEnumerator routine)
    {
        StopIdle();

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
        StartIdle();
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