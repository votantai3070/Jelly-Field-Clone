using System;
using System.Collections;
using UnityEngine;

public class JellyAnimation : MonoBehaviour
{
    [Header("References")]
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
    private Coroutine animationRoutine;
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

    // Set scale gốc để các animation sau luôn quay về đúng trạng thái chuẩn
    public void SetBaseScale(Vector3 newScale)
    {
        baseScale = newScale;

        if (!isBusy && !isDragging && visualRoot != null)
            visualRoot.localScale = baseScale;
    }

    // Input từ drag sẽ update velocity liên tục để animation biết đang kéo theo hướng nào
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
        StopCurrentAnimationRoutine();

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

        StopReturnRoutine();

        if (snapToBase && gameObject.activeInHierarchy)
            returnRoutine = StartCoroutine(ReturnToBaseRoutine());
    }

    public void StartIdle()
    {
        if (!playIdle)
            return;

        if (isBusy || isDragging)
            return;

        if (!gameObject.activeInHierarchy)
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
        RestartMainAnimation(LandingRoutine());
    }

    public void PlayPreCollectPulse()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopDragJiggle(false);
        RestartMainAnimation(PreCollectPulseRoutine());
    }

    public void PlayCollectToPoint(Vector3 target, Action onComplete = null)
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopDragJiggle(false);
        RestartMainAnimation(CollectRoutine(target, onComplete));
    }

    // Chỉ cho phép 1 animation chính chạy tại một thời điểm
    private void RestartMainAnimation(IEnumerator routine)
    {
        StopIdle();
        StopReturnRoutine();

        if (dragRoutine != null)
        {
            StopCoroutine(dragRoutine);
            dragRoutine = null;
        }

        isDragging = false;
        StopCurrentAnimationRoutine();

        animationRoutine = StartCoroutine(routine);
    }

    private void StopCurrentAnimationRoutine()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
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
        StopCurrentAnimationRoutine();

        if (dragRoutine != null)
        {
            StopCoroutine(dragRoutine);
            dragRoutine = null;
        }

        isBusy = false;
        isDragging = false;
        dragVelocity = Vector3.zero;
    }

    // Reset ngay lập tức scale và rotation về trạng thái gốc
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
            float timeValue = Time.time * idleSpeed;

            float scaleX = 1f + Mathf.Sin(timeValue) * idleAmountX
                              + Mathf.Sin(timeValue * 0.47f) * idleSecondaryWave;

            float scaleY = 1f - Mathf.Sin(timeValue * 0.9f) * idleAmountY;
            float rotationZ = Mathf.Sin(timeValue * 0.7f) * 1.5f;

            ApplyVisualScale(scaleX, scaleY);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            yield return null;
        }
    }

    private IEnumerator DragJiggleRoutine()
    {
        while (isDragging)
        {
            float timeValue = Time.time * dragJiggleSpeed;

            Vector2 velocity2D = new Vector2(dragVelocity.x, dragVelocity.y);
            float speedPercent = Mathf.Clamp01(velocity2D.magnitude * 10f);
            Vector2 direction = GetNormalizedDirectionOrZero(velocity2D);

            float waveX = Mathf.Sin(timeValue) * dragJiggleAmountX * (0.4f + speedPercent);
            float waveY = Mathf.Cos(timeValue * 0.92f) * dragJiggleAmountY * (0.4f + speedPercent);

            float stretchBase = dragStretchAmount * speedPercent;
            float stretchX = direction.x * stretchBase - direction.y * stretchBase * 0.35f;
            float stretchY = direction.y * stretchBase - direction.x * stretchBase * 0.35f;

            float scaleX = 1f + waveX + stretchX;
            float scaleY = 1f - waveY + stretchY;
            float rotationZ = -direction.x * dragTiltAmount * speedPercent
                              + Mathf.Sin(timeValue * 0.8f) * 1.2f * speedPercent;

            ApplyVisualScale(scaleX, scaleY);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            yield return null;
        }
    }

    private IEnumerator ReturnToBaseRoutine()
    {
        Vector3 startScale = visualRoot.localScale;
        Quaternion startRotation = visualRoot.localRotation;

        float elapsedTime = 0f;

        while (elapsedTime < dragReturnTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / dragReturnTime);
            float springValue = EvaluateDampedSpring01(progress, springOscillationCount, springDamping);

            visualRoot.localScale = Vector3.LerpUnclamped(startScale, baseScale, springValue);
            visualRoot.localRotation = Quaternion.SlerpUnclamped(startRotation, baseRotation, springValue);

            yield return null;
        }

        visualRoot.localScale = baseScale;
        visualRoot.localRotation = baseRotation;
        returnRoutine = null;

        if (playIdle && !isBusy && !isDragging)
            StartIdle();
    }

    // Animation lúc piece vừa được thả xuống board
    private IEnumerator LandingRoutine()
    {
        isBusy = true;

        Vector3 squashScale = new Vector3(baseScale.x * landSquashX, baseScale.y * landSquashY, baseScale.z);
        Vector3 reboundScale = new Vector3(baseScale.x * landBounceX, baseScale.y * landBounceY, baseScale.z);

        yield return AnimateScale(baseScale, squashScale, landRecoverTime * 0.24f, easeInOut);
        yield return AnimateScale(squashScale, reboundScale, landRecoverTime * 0.28f, easeOut);
        yield return AnimateScaleSpring(reboundScale, baseScale, landRecoverTime * 0.48f);

        FinishMainAnimation();
    }

    // Animation nhịp phồng nhẹ trước khi collect
    private IEnumerator PreCollectPulseRoutine()
    {
        isBusy = true;

        Vector3 bigScale = baseScale * preCollectPunchScale;
        Vector3 overshootScale = baseScale * preCollectOvershootScale;

        yield return AnimateScale(baseScale, bigScale, preCollectTime * 0.38f, easeOut);
        yield return AnimateScale(bigScale, overshootScale, preCollectTime * 0.27f, easeInOut);
        yield return AnimateScaleSpring(overshootScale, baseScale, preCollectTime * 0.55f);

        FinishMainAnimation();
    }

    // Animation piece bay về điểm collect rồi thu nhỏ lại
    private IEnumerator CollectRoutine(Vector3 target, Action onComplete)
    {
        isBusy = true;

        Vector3 startPosition = transform.position;
        Vector3 startScale = visualRoot.localScale;
        Vector3 punchScale = baseScale * preCollectPunchScale;

        yield return AnimateScale(startScale, punchScale, preCollectTime, easeOut);
        yield return MoveToTargetWithStretch(startPosition, target);
        yield return ShrinkToZero();

        isBusy = false;
        animationRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator MoveToTargetWithStretch(Vector3 startPosition, Vector3 target)
    {
        float elapsedTime = 0f;
        Vector3 previousPosition = startPosition;

        while (elapsedTime < collectMoveTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / collectMoveTime);
            float easedProgress = easeOut.Evaluate(progress);

            Vector3 nextPosition = Vector3.Lerp(startPosition, target, easedProgress);
            Vector3 movementDelta = nextPosition - previousPosition;
            Vector2 moveDirection = new Vector2(movementDelta.x, movementDelta.y);
            float speedPercent = Mathf.Clamp01(moveDirection.magnitude * 30f);

            transform.position = nextPosition;

            if (moveDirection.sqrMagnitude > 0.00001f)
            {
                moveDirection.Normalize();

                float scaleX = 1f + moveDirection.x * collectStretchAlongPath * speedPercent
                                  - moveDirection.y * collectStretchAlongPath * 0.25f * speedPercent;

                float scaleY = 1f + moveDirection.y * collectStretchAlongPath * speedPercent
                                  - moveDirection.x * collectStretchAlongPath * 0.25f * speedPercent;

                ApplyVisualScale(scaleX, scaleY);

                float rotationZ = -moveDirection.x * 8f * speedPercent;
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            }

            previousPosition = nextPosition;
            yield return null;
        }
    }

    private IEnumerator ShrinkToZero()
    {
        float elapsedTime = 0f;
        Vector3 startScale = visualRoot.localScale;
        Quaternion startRotation = visualRoot.localRotation;

        while (elapsedTime < collectShrinkTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / collectShrinkTime);
            float easedProgress = easeInOut.Evaluate(progress);

            visualRoot.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, easedProgress);
            visualRoot.localRotation = Quaternion.SlerpUnclamped(startRotation, baseRotation, easedProgress);

            yield return null;
        }

        visualRoot.localScale = Vector3.zero;
        visualRoot.localRotation = baseRotation;
    }

    private void FinishMainAnimation()
    {
        visualRoot.localScale = baseScale;
        visualRoot.localRotation = baseRotation;

        isBusy = false;
        animationRoutine = null;

        if (playIdle && !isDragging)
            StartIdle();
    }

    private void ApplyVisualScale(float normalizedScaleX, float normalizedScaleY)
    {
        visualRoot.localScale = new Vector3(
            baseScale.x * normalizedScaleX,
            baseScale.y * normalizedScaleY,
            baseScale.z
        );
    }

    private Vector2 GetNormalizedDirectionOrZero(Vector2 vector)
    {
        if (vector.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        return vector.normalized;
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        if (duration <= 0f)
        {
            visualRoot.localScale = to;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);
            float easedProgress = curve != null ? curve.Evaluate(progress) : progress;

            visualRoot.localScale = Vector3.LerpUnclamped(from, to, easedProgress);
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

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);
            float springValue = EvaluateDampedSpring01(progress, springOscillationCount, springDamping);

            visualRoot.localScale = Vector3.LerpUnclamped(from, to, springValue);
            yield return null;
        }

        visualRoot.localScale = to;
    }

    // Hàm spring đơn giản để tạo cảm giác nảy mềm
    private float EvaluateDampedSpring01(float progress, float oscillations, float damping)
    {
        progress = Mathf.Clamp01(progress);

        float exponentialValue = Mathf.Exp(-damping * progress);
        float waveValue = Mathf.Cos(oscillations * Mathf.PI * 2f * progress);

        return 1f - exponentialValue * waveValue;
    }
}