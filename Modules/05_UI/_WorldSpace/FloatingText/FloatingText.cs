using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class FloatingText : MonoBehaviour
{
    public enum TextType { Damage, Heal, Neutral, Coin, Shield, Vest, CriticalDamage }

    [SerializeField] private TextMeshProUGUI _textMesh;
    [SerializeField] private GameObject _coin;
    [SerializeField] private Image _coinImage;
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private RectTransform _panelRectTransform;
    [Header("Movement & Fade")]
    [SerializeField] private float _duration = 1.2f;
    [SerializeField] private float _height = 50f;

    public void SetText(string text, TextType type)
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
        }

        transform.SetAsLastSibling();
        DOTween.Kill(this.transform);
        Vector3 initialPosition = transform.localPosition;
        Color resetColor = Color.white;
        _textMesh.color = resetColor;
        if (_coinImage != null) _coinImage.color = resetColor;

        switch (type)
        {
            case TextType.Damage:
                _textMesh.text = "-" + text;
                _textMesh.color = Color.red;
                _coin.SetActive(false);
                break;
            case TextType.Heal:
                _textMesh.text = "+" + text;
                _textMesh.color = Color.green;
                _coin.SetActive(false);
                break;
            case TextType.Neutral:
                _textMesh.text = text;
                _textMesh.color = Color.white;
                _coin.SetActive(false);
                break;
            case TextType.Coin:
                _textMesh.text = "+" + text;
                _textMesh.color = Color.yellow;
                _coin.SetActive(true);
                break;
            case TextType.Shield:
                _textMesh.text = "-" + text;
                _textMesh.color = Color.cyan;
                _coin.SetActive(false);
                break;
            case TextType.Vest:
                _textMesh.text = "-" + text;
                _textMesh.color = Color.grey;
                _coin.SetActive(false);
                break;
            case TextType.CriticalDamage:
                _textMesh.text = "CRIT -" + text;
                _textMesh.color = new Color(1f, 0.5f, 0f);
                _coin.SetActive(false);
                break;
        }

        Sequence textSequence = DOTween.Sequence();
        textSequence.SetTarget(this.transform);
        Vector3 targetPosition = initialPosition + new Vector3(0, _height, 0);
        textSequence.Append(transform.DOLocalMove(targetPosition, _duration).SetEase(Ease.OutSine));
        textSequence.Join(_textMesh.DOFade(0, _duration));
        if (_coin.activeSelf)
        {
            textSequence.Join(_coinImage.DOFade(0, _duration));
        }
        textSequence.OnComplete(() =>
        {
            transform.localPosition = initialPosition;
            ObjectPoolFloatingText.Instance.ReturnObjectToPool(gameObject);
        });

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);
    }

    private void OnDisable() => DOTween.Kill(this.transform);
}
