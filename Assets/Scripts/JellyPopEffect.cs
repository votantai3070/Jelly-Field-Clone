using System.Collections;
using UnityEngine;

public class JellyPopEffect : MonoBehaviour, IPoolable
{
    [SerializeField] private ParticleSystem particleSystemFx;

    private Coroutine autoDespawnRoutine;

    private void Awake()
    {
        if (particleSystemFx == null)
            particleSystemFx = GetComponent<ParticleSystem>();
    }

    public void Play(Color color)
    {
        if (particleSystemFx == null)
            return;

        StopAutoDespawnRoutine();

        var main = particleSystemFx.main;
        main.startColor = color;

        particleSystemFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystemFx.Clear(true);
        particleSystemFx.Play(true);

        float life = GetEffectLifetime();
        autoDespawnRoutine = StartCoroutine(AutoDespawnRoutine(life));
    }

    private float GetEffectLifetime()
    {
        if (particleSystemFx == null)
            return 0.2f;

        var main = particleSystemFx.main;

        float duration = main.duration;
        float startLifetime = main.startLifetime.mode switch
        {
            ParticleSystemCurveMode.Constant => main.startLifetime.constant,
            ParticleSystemCurveMode.TwoConstants => main.startLifetime.constantMax,
            _ => main.startLifetime.constantMax
        };

        return duration + startLifetime + 0.1f;
    }

    private IEnumerator AutoDespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Despawn(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void StopAutoDespawnRoutine()
    {
        if (autoDespawnRoutine != null)
        {
            StopCoroutine(autoDespawnRoutine);
            autoDespawnRoutine = null;
        }
    }

    public void OnSpawned()
    {
        StopAutoDespawnRoutine();

        if (particleSystemFx == null)
            particleSystemFx = GetComponent<ParticleSystem>();

        if (particleSystemFx != null)
        {
            particleSystemFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystemFx.Clear(true);
        }
    }

    public void OnDespawned()
    {
        StopAutoDespawnRoutine();

        if (particleSystemFx == null)
            particleSystemFx = GetComponent<ParticleSystem>();

        if (particleSystemFx != null)
        {
            particleSystemFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystemFx.Clear(true);
        }
    }
}