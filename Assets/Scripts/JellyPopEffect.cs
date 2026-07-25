using System.Collections;
using UnityEngine;

public class JellyPopEffect : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem particleSystemFx;

    private Coroutine autoDespawnRoutine;

    private void Awake()
    {
        CacheReferences();
    }

    // Phát particle với màu được truyền vào, sau đó tự trả object về pool
    public void Play(Color color)
    {
        if (particleSystemFx == null)
            return;

        StopAutoDespawnRoutine();
        ApplyStartColor(color);
        RestartParticle();

        float effectLifetime = GetEffectLifetime();
        autoDespawnRoutine = StartCoroutine(AutoDespawnRoutine(effectLifetime));
    }

    public void OnSpawned()
    {
        ResetEffectState();
    }

    public void OnDespawned()
    {
        ResetEffectState();
    }

    private void CacheReferences()
    {
        if (particleSystemFx == null)
            particleSystemFx = GetComponent<ParticleSystem>();
    }

    private void ResetEffectState()
    {
        StopAutoDespawnRoutine();
        CacheReferences();

        if (particleSystemFx == null)
            return;

        particleSystemFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystemFx.Clear(true);
    }

    private void ApplyStartColor(Color color)
    {
        ParticleSystem.MainModule mainModule = particleSystemFx.main;
        mainModule.startColor = color;
    }

    private void RestartParticle()
    {
        particleSystemFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystemFx.Clear(true);
        particleSystemFx.Play(true);
    }

    // Tính thời gian sống gần đúng của effect để biết khi nào nên despawn
    private float GetEffectLifetime()
    {
        if (particleSystemFx == null)
            return 0.2f;

        ParticleSystem.MainModule mainModule = particleSystemFx.main;

        float duration = mainModule.duration;
        float startLifetime = GetStartLifetime(mainModule);

        return duration + startLifetime + 0.1f;
    }

    private float GetStartLifetime(ParticleSystem.MainModule mainModule)
    {
        switch (mainModule.startLifetime.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return mainModule.startLifetime.constant;

            case ParticleSystemCurveMode.TwoConstants:
                return mainModule.startLifetime.constantMax;

            default:
                return mainModule.startLifetime.constantMax;
        }
    }

    private IEnumerator AutoDespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Despawn(gameObject);
        else
            gameObject.SetActive(false);

        autoDespawnRoutine = null;
    }

    private void StopAutoDespawnRoutine()
    {
        if (autoDespawnRoutine == null)
            return;

        StopCoroutine(autoDespawnRoutine);
        autoDespawnRoutine = null;
    }
}