using UnityEngine;

public class JellyPopEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSystemFx;

    private void Awake()
    {
        if (particleSystemFx == null)
            particleSystemFx = GetComponent<ParticleSystem>();
    }

    public void Play(Color color)
    {
        if (particleSystemFx == null)
            return;

        var main = particleSystemFx.main;
        main.startColor = color;

        particleSystemFx.Play();

        float life = main.duration + main.startLifetime.constantMax + 0.1f;
        Destroy(gameObject, life);
    }
}