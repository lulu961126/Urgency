using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Windows;

public class HandCrankFlashlight : MonoBehaviour
{
    [SerializeField] private float ChargeAmount;
    [SerializeField] private float MaxOuterRadius;
    [SerializeField] private float MaxInnerRadius;
    [SerializeField] private float MaxIntensity;
    [SerializeField] private float ProgressiveRate;
    [SerializeField] private float RecessionRate;
    [SerializeField] private Light2D Light;
    private float _chargingAmount = 0f;
    private InputSystem_Actions actions;
    private Action<InputAction.CallbackContext> Charge;

    private void Start()
    {
        Light.intensity = 0f;
        Light.pointLightInnerRadius = 0f;
        Light.pointLightOuterRadius = 0f;
        actions = Inputs.Actions;
    }
    
    private void Update()
    {
        if(_chargingAmount >= 100) _chargingAmount = 100;
        _chargingAmount *= RecessionRate;
        Light.intensity = Mathf.Lerp(Light.intensity, MaxIntensity * (_chargingAmount / 100), ProgressiveRate);
        Light.pointLightInnerRadius = Mathf.Lerp(Light.pointLightInnerRadius, MaxInnerRadius * (_chargingAmount / 100), ProgressiveRate);
        Light.pointLightOuterRadius = Mathf.Lerp(Light.pointLightOuterRadius, MaxOuterRadius * (_chargingAmount / 100), ProgressiveRate);
    }

    private void OnEnable()
    {
        Charge ??= _ => { _chargingAmount += ChargeAmount; };
        actions.Player.Jump.started += Charge;
    }
}
