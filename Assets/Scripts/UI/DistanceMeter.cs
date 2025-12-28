using DG.Tweening;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DistanceMeter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Text;
    private InputSystem_Actions actions;
    private Action<InputAction.CallbackContext> MeterStart;
    private Action<InputAction.CallbackContext> MeterCancled;
    public string Name;

    private void Awake() => Text.text = "Distance Meter";

    private void Start()
    {
        transform.localScale = Vector3.zero;
    }

    private void Update() => CalculateDistanceUpdate();

    private void CalculateDistanceUpdate()
    {
        var targetCoords = gameObject.GetComponentInParent<Status>().TargetCoords;
        var target = targetCoords.FirstOrDefault(a => a.Name == Name);
        if (target == null) return;
        var distance = Vector2.Distance(Informations.PlayerPosition, target.Position) * 2;
        Text.text = $"{distance.ToString("F1")}m";
    }

    private void OnEnable()
    {
        actions = Inputs.Actions;
        MeterStart ??= _ =>
        {
            actions.Player.Move.Disable();
            transform.DOScale(1, 0.1f);
        };
        MeterCancled ??= _ =>
        {
            actions.Player.Move.Enable();
            transform.DOScale(0, 0.1f);
        };
        actions.UI.DistanceMeter.started += MeterStart;
        actions.UI.DistanceMeter.canceled += MeterCancled;
    }

    private void OnDisable()
    {
        actions.UI.DistanceMeter.started -= MeterStart;
        actions.UI.DistanceMeter.canceled -= MeterCancled;
    }
}
