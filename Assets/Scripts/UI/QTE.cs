using DG.Tweening;
using DG.Tweening.Plugins.Options;
using System;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QTE : MonoBehaviour
{
    [Range(0f, 360f)]
    [SerializeField] public float StartAngle;
    [Range(0f, 360f)]
    [SerializeField] public float EndAngle;
    [Tooltip("Angular Velocity in Degree")]
    [SerializeField] float RotateSpeed;
    [SerializeField] AudioClip SucceedAudio;
    [SerializeField] AudioClip FailAudio;
    private Transform _detectBar;
    private Transform _rotateFinger;
    private Image _image;
    private bool _enabled = false;
    private Material _material;
    private InputSystem_Actions _actions;
    private Action<InputAction.CallbackContext> _func;
    private float _angle = 0;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _detectBar = transform.Find("DetectBar");
        _rotateFinger = transform.Find("RotateFinger");
        _image = _detectBar.GetComponent<Image>();
        _material = new Material(_image.material);
        _image.material = _material;
        _rotateFinger.rotation = Quaternion.Euler(0, 0, _angle);
        _actions = Inputs.Actions;
        _func ??= _ =>
        {
            float degree = GetStandardDegree(_angle);
            if (degree <= EndAngle && degree >= StartAngle)
                Success();
            else
                Failure();
        };
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0.0f;
    }

    private void OnEnable()
    {
        if (EndAngle < StartAngle) Swap(ref EndAngle, ref StartAngle);
        _material.SetFloat("_StartAngle", StartAngle);
        _material.SetFloat("_EndAngle", EndAngle);
        QTEStatus.QTEStart();
        QTEStatus.AllowCallQTE = false;
        _canvasGroup.DOFade(1, 0.2f).OnComplete(() =>
        {
            _actions.Player.Use.canceled += _func;
            if (!_actions.Player.Use.IsPressed())
            {
                Failure();
            }
            _enabled = true;
        });
    }

    private void Update()
    {
        if (!_enabled) return;

        _angle -= RotateSpeed * Time.deltaTime;
        _rotateFinger.rotation = Quaternion.Euler(0, 0, _angle);

        if (_angle <= -360f)
        {
            Failure();
        }
    }

    private void OnDestroy() => _actions.Player.Use.canceled -= _func;

    private void OnDisable() => _actions.Player.Use.canceled -= _func;

    private void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

    private float GetStandardDegree(float degree) => Mathf.Repeat(degree + 450f, 360f);

    private void Success()
    {
        _enabled = false;
        QTEStatus.QTEFinish(true);
        _canvasGroup.DOFade(0, 0.1f).OnComplete(() =>
        {
            Destroy(gameObject);
            QTEStatus.AllowCallQTE = true;
        }).SetLink(gameObject);
    }

    private void Failure()
    {
        _enabled = false;
        QTEStatus.QTEFinish(false);
        RectTransform rect = (RectTransform)this.transform;
        DOTween.Sequence()
            .Join(rect.DOShakeAnchorPos(0.5f, 10f, 20, 90f, false, true))
            .Append(_canvasGroup.DOFade(0, 0.1f))
            .OnComplete(() => {
                Destroy(gameObject);
                QTEStatus.AllowCallQTE = true;
            }).SetLink(gameObject);
    }
}
