using System.Collections;
using UnityEngine;

public class JellyAnimation : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visualRoot;

    [Header("Idle")]
    [SerializeField] private bool playIdle = false;
    [SerializeField] private float idleSpeed = 2.1f;
    [SerializeField] private float idleAmountX = 0.025f;
    [SerializeField] private float idleAmountY = 0.03f;
    [SerializeField] private float idleSecondaryWave = 0.012f;

    [Header("Drag")]
    [SerializeField] private float dragJiggleSpeed = 15f;
    [SerializeField] private float dragJiggleAmountX = 0.05f;
    [SerializeField] private float dragJiggleAmountY = 0.04f;
    [SerializeField] private float dragStretchAmount = 0.16f;
    [SerializeField] private float dragTiltAmount = 10f;
    [SerializeField] private float dragReturnTime = 0.16f;

    [Header("Landing")]
    [SerializeField] private float landSquashX = 1.18f;
    [SerializeField] private float landSquashY = 0.82f;
    [SerializeField] private float landBounceX = 0.92f;
    [SerializeField] private float landBounceY = 1.08f;
    [SerializeField] private float landRecoverTime = 0.2f;

    [Header("Match")]
    [SerializeField] private float preCollectPunchScale = 1.16f;
    [SerializeField] private float preCollectOvershootScale = 0.94f;
    [SerializeField] private float preCollectTime = 0.12f;

    [Header("Collect")]
    [SerializeField] private float collectMoveTime = 0.2f;
    [SerializeField] private float collectShrinkTime = 0.12f;
    [SerializeField] private float collectStretchAlongPath = 0.14f;

    [Header("Spring")]
    [SerializeField] private float springOscillationCount = 2.5f;
    [SerializeField] private float springDamping = 6.5f;

    [Header("Curves")]
    [SerializeField] private AnimationCurve easeOut = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve easeInOut = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 baseScale = Vector3.one;
    private Quaternion baseRotation = Quaternion.identity;

    private Coroutine idleRoutine;
    private Coroutine animRoutine;
    private Coroutine dragRoutine;
    private Coroutine returnRoutine;

    private bool isBusy;
    private bool isDragging;
    private Vector3 dragVelocity;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        baseScale = visualRoot.localScale;
        baseRotation = visualRoot.localRotation;
    }

    private void OnEnable()
    {
        ResetImmediateVisualState();

        if (playIdle)
            StartIdle();
    }

    private void OnDisable()
    {
        StopAllAnimationRoutines();
        ResetImmediateVisualState();
    }

    public void SetBaseScale(Vector3 newScale)
    {
        baseScale = newScale;

        if (!isBusy && !isDragging && visualRoot != null)
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
        StopReturnRoutine();

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

        if (snapToBase && gameObject.activeInHierarchy)
        {
            StopReturnRoutine();
            returnRoutine = StartCoroutine(ReturnToBaseRoutine());
        }
        else
        {
            StopReturnRoutine();
        }
    }

    public void StartIdle()
    {
        if (!playIdle || isBusy || isDragging || !gameObject.activeInHierarchy)
            return;

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
        if (!gameObject.activeInHierarchy)
            return;

        StopDragJiggle(false);
        RestartAnim(LandingRoutine());
    }

    public void PlayPreCollectPulse()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopDragJiggle(false);
        RestartAnim(PreCollectPulseRoutine());
    }

    public void PlayCollectToPoint(Vector3 target, System.Action onComplete = null)
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopDragJiggle(false);
        RestartAnim(CollectRoutine(target, onComplete));
    }

    private void RestartAnim(IEnumerator routine)
    {
        StopIdle();
        StopReturnRoutine();

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

    private void StopReturnRoutine()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
    }

    private void StopAllAnimationRoutines()
    {
        StopIdle();
        StopReturnRoutine();

        if (dragRoutine != null)
        {
            StopCoroutine(dragRoutine);
            dragRoutine = null;
        }

        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }

        isBusy = false;
        isDragging = false;
        dragVelocity = Vector3.zero;
    }

    private void ResetImmediateVisualState()
    {
        if (visualRoot == null)
            return;

        visualRoot.localScale = baseScale;
        visualRoot.localRotation = baseRotation;
    }

    private IEnumerator IdleRoutine()
    {
        while (true)
        {
            float t = Time.time * idleSpeed;

            float sx = 1f + Mathf.Sin(t) * idleAmountX + Mathf.Sin(t * 0.47f) * idleSecondaryWave;
            float sy = 1f - Mathf.Sin(t * 0.9f) * idleAmountY;
            float rot = Mathf.Sin(t * 0.7f) * 1.5f;

            visualRoot.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rot);

            yield return null;
        }
    }

    private IEnumerator DragJiggleRoutine()
    {
        while (isDragging)
        {
            float t = Time.time * dragJiggleSpeed;

            Vector2 v = new Vector2(dragVelocity.x, dragVelocity.y);
            float speed = Mathf.Clamp01(v.magnitude * 10f);

            Vector2 dir = v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.zero;

            float waveX = Mathf.Sin(t) * dragJiggleAmountX * (0.4f + speed);
            float waveY = Mathf.Cos(t * 0.92f) * dragJiggleAmountY * (0.4f + speed);

            float stretchMain = dragStretchAmount * speed;
            float stretchX = dir.x * stretchMain - dir.y * stretchMain * 0.35f;
            float stretchY = dir.y * stretchMain - dir.x * stretchMain * 0.35f;

            float sx = 1f + waveX + stretchX;
            float sy = 1f - waveY + stretchY;

            float rot = -dir.x * dragTiltAmount * speed + Mathf.Sin(t * 0.8f) * 1.2f * speed;

            visualRoot.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rot);

            yield return null;
        }
    }

    private IEnumerator ReturnToBaseRoutine()
    {
        Vector3 scaleStart = visualRoot.localScale;
        Quaternion rotStart = visualRoot.localRotation;

        float t = 0f;
        while (t < dragReturnTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dragReturnTime);

            float spring = EvaluateDampedSpring01(p, springOscillationCount, springDamping);

            visualRoot.localScale = Vector3.LerpUnclamped(scaleStart, baseScale, spring);
            visualRoot.localRotation = Quaternion.SlerpUnclamped(rotStart, baseRotation, spring);

            yield return null;
        }

        visualRoot.localScale = baseScale;
        visualRoot.localRotation = baseRotation;
        returnRoutine = null;

        if (playIdle && !isBusy && !isDragging)
            StartIdle();
    }

    private IEnumerator LandingRoutine()
    {
        isBusy = true;

        Vector3 squash = new Vector3(baseScale.x * landSquashX, baseScale.y * landSquashY, baseScale.z);
        Vector3 rebound = new Vector3(baseScale.x * landBounceX, baseScale.y * landBounceY, baseScale.z);

        yield return AnimateScale(baseScale, squash, landRecoverTime * 0.24f, easeInOut);
        yield return AnimateScale(squash, rebound, landRecoverTime * 0.28f, easeOut);
        yield return AnimateScaleSpring(rebound, baseScale, landRecoverTime * 0.48f);

        visualRoot.localScale = baseScale;
        visualRoot.localRotation = baseRotation;

        isBusy = false;
        animRoutine = null;

        if (playIdle && !isDragging)
            StartIdle();
    }

    private IEnumerator PreCollectPulseRoutine()
    {
        isBusy = true;

        Vector3 big = baseScale * preCollectPunchScale;
        Vector3 overshoot = baseScale * preCollectOvershootScale;

        yield return AnimateScale(baseScale, big, preCollectTime * 0.38f, easeOut);
        yield return AnimateScale(big, overshoot, preCollectTime * 0.27f, easeInOut);
        yield return AnimateScaleSpring(overshoot, baseScale, preCollectTime * 0.55f);

        visualRoot.localScale = baseScale;
        visualRoot.localRotation = baseRotation;

        isBusy = false;
        animRoutine = null;

        if (playIdle && !isDragging)
            StartIdle();
    }

    private IEnumerator CollectRoutine(Vector3 target, System.Action onComplete)
    {
        isBusy = true;

        Vector3 startPos = transform.position;
        Vector3 startScale = visualRoot.localScale;
        Vector3 punchScale = baseScale * preCollectPunchScale;

        yield return AnimateScale(startScale, punchScale, preCollectTime, easeOut);

        float t = 0f;
        Vector3 prevPos = startPos;

        while (t < collectMoveTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / collectMoveTime);
            float eased = easeOut.Evaluate(p);

            Vector3 nextPos = Vector3.Lerp(startPos, target, eased);
            Vector3 delta = nextPos - prevPos;
            Vector2 dir = new Vector2(delta.x, delta.y);
            float speed = Mathf.Clamp01(dir.magnitude * 30f);

            transform.position = nextPos;

            if (dir.sqrMagnitude > 0.00001f)
            {
                dir.Normalize();
                float stretchX = 1f + dir.x * collectStretchAlongPath * speed - dir.y * collectStretchAlongPath * 0.25f * speed;
                float stretchY = 1f + dir.y * collectStretchAlongPath * speed - dir.x * collectStretchAlongPath * 0.25f * speed;

                visualRoot.localScale = new Vector3(
                    baseScale.x * stretchX,
                    baseScale.y * stretchY,
                    baseScale.z
                );

                float rot = -dir.x * 8f * speed;
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, rot);
            }

            prevPos = nextPos;
            yield return null;
        }

        t = 0f;
        Vector3 shrinkStart = visualRoot.localScale;
        Quaternion shrinkRotStart = visualRoot.localRotation;

        while (t < collectShrinkTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / collectShrinkTime);
            float eased = easeInOut.Evaluate(p);

            visualRoot.localScale = Vector3.LerpUnclamped(shrinkStart, Vector3.zero, eased);
            visualRoot.localRotation = Quaternion.SlerpUnclamped(shrinkRotStart, baseRotation, eased);
            yield return null;
        }

        visualRoot.localScale = Vector3.zero;
        visualRoot.localRotation = baseRotation;

        isBusy = false;
        animRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        if (duration <= 0f)
        {
            visualRoot.localScale = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = curve != null ? curve.Evaluate(p) : p;
            visualRoot.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        visualRoot.localScale = to;
    }

    private IEnumerator AnimateScaleSpring(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            visualRoot.localScale = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float spring = EvaluateDampedSpring01(p, springOscillationCount, springDamping);
            visualRoot.localScale = Vector3.LerpUnclamped(from, to, spring);
            yield return null;
        }

        visualRoot.localScale = to;
    }

    private float EvaluateDampedSpring01(float t, float oscillations, float damping)
    {
        t = Mathf.Clamp01(t);

        float expo = Mathf.Exp(-damping * t);
        float wave = Mathf.Cos(oscillations * Mathf.PI * 2f * t);

        return 1f - expo * wave;
    }
}