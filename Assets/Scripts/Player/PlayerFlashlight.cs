using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerFlashlight : MonoBehaviour
{
    [SerializeField] private float Center;
    [SerializeField] private float HalfRange;
    [SerializeField] private float Speed;
    [SerializeField] private float Seed;
    private Light2D _light2d;

    private void OnEnable() => _light2d ??= GetComponent<Light2D>();

    private void Update()
    {
        var noise = (Mathf.PerlinNoise(Time.time * Speed, Seed) - 0.5f) * 2f + 0.5f;
        _light2d.falloffIntensity = noise * HalfRange + Center;
    }
}
