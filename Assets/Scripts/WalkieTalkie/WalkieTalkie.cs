using DG.Tweening;
using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WalkieTalkie : MonoBehaviour
{
    [SerializeField] private float DetectDistance;
    [SerializeField] private float FadeoutTime;
    private GameObject refugeeManager;
    private float _deltaTime = 0f;
    private InputSystem_Actions _actions;
    [CanBeNull] private Tween closeTween;
    [CanBeNull] private Tween openTween;
    private CanvasGroup _canvaGroups;
    private RectTransform _rectTransform;
    private Action<InputAction.CallbackContext> WalkieTalkieShow;
    private Action<InputAction.CallbackContext> WalkieTalkie1;
    private Action<InputAction.CallbackContext> WalkieTalkie2;

    private void OnEnable()
    {
        _actions ??= Inputs.Actions;
        _actions.UI.Enable();
        refugeeManager ??= Managers.RefugeeManager;

        WalkieTalkieShow ??= _ =>
        {
            if (openTween != null && closeTween != null)
                if (openTween.IsPlaying() || closeTween.IsPlaying()) return;

            if (_canvaGroups.alpha != 0)
            {
                CloseAnimation();
                return;
            }

            _deltaTime = 0;
            OpenAnimation();
        };

        WalkieTalkie1 ??= _ =>
        {
            if (_canvaGroups.alpha != 0)
                NearbyRefugeeFollowing();

            if (closeTween == null && _canvaGroups.alpha == 1)
                CloseAnimation();
        };

        WalkieTalkie2 ??= _ =>
        {
            if (_canvaGroups.alpha != 0)
                NearbyRefugeeStaying();

            if (closeTween == null && _canvaGroups.alpha == 1)
                CloseAnimation();
        };

        _actions.UI.WalkieTalkieShow.started += WalkieTalkieShow;
        _actions.UI.WalkieTalkie1.started += WalkieTalkie1;
        _actions.UI.WalkieTalkie2.started += WalkieTalkie2;
    }

    private void OnDisable()
    {
        _actions.UI.WalkieTalkieShow.started -= WalkieTalkieShow;
        _actions.UI.WalkieTalkie1.started -= WalkieTalkie1;
        _actions.UI.WalkieTalkie2.started -= WalkieTalkie2;
    }

    private void Start()
    {
        _canvaGroups = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();

        _rectTransform.localScale = Vector3.one;
        _canvaGroups.alpha = 0;
    }

    private void Update()
    {
        if (_canvaGroups.alpha != 0)
            _deltaTime += Time.deltaTime;
        _deltaTime = _deltaTime >= FadeoutTime ? FadeoutTime : _deltaTime;
        if (_deltaTime >= FadeoutTime && closeTween == null && _canvaGroups.alpha == 1)
            CloseAnimation();
    }

    private void NearbyRefugeeFollowing() =>
        refugeeManager.GetComponent<RefugeeManager>().GetRefugeesInRadius(GameObject.FindFirstObjectByType<Player>().transform.position, DetectDistance).ForEach(a => a.GetComponent<Refugee>().IsFollowing = true);

    private void NearbyRefugeeStaying() =>
        refugeeManager.GetComponent<RefugeeManager>().GetRefugeesInRadius(GameObject.FindFirstObjectByType<Player>().transform.position, DetectDistance).ForEach(a => a.GetComponent<Refugee>().IsFollowing = false);

    private void CloseAnimation() => closeTween = _rectTransform.DOScale(0, 0.1f).OnComplete(() =>
    {
        _rectTransform.localScale = Vector3.one;
        _canvaGroups.alpha = 0;
        closeTween = null;
    });

    private void OpenAnimation() => openTween = _canvaGroups.DOFade(1, 0.1f).OnComplete(() =>
    {
        openTween = null;
    });
}
