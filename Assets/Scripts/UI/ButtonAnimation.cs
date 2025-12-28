using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color HoveredColor;
    [SerializeField] private float FadeDuration;
    private bool _isHovered = false;
    private TextMeshProUGUI _text;
    private Color _originalColor;
    private float _deltaTime = 0f;

    private void Start()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _originalColor = _text.color;
    }

    private void Update()
    {
        if (_isHovered)
        {
            _deltaTime += Time.deltaTime;
            if (_deltaTime >= FadeDuration) _deltaTime = FadeDuration;
        }
        else
        {
            _deltaTime -= Time.deltaTime;
            if (_deltaTime <= 0f) _deltaTime = 0f;
        }
        FadingUpdate();
    }

    private void FadingUpdate()
    {
        _text.color = Color.Lerp(_originalColor, HoveredColor, _deltaTime / FadeDuration);
    }

    public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;

    public void OnPointerExit(PointerEventData eventData) =>  _isHovered = false;
}
