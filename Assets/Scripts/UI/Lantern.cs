using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Lantern : MonoBehaviour
{
    [SerializeField] private Light2D Light2d;
    [SerializeField] private float Center;
    [SerializeField] private float HalfRange;
    [SerializeField] private float Speed;
    [SerializeField] private float Seed;
    
    private void Update()
    {
        var noise = (Mathf.PerlinNoise(Time.time * Speed, Seed) - 0.5f) * 2f;
        Light2d.pointLightOuterRadius = HalfRange * noise + Center;
    }
}
