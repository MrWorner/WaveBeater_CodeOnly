using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleUnitAnimator : MonoBehaviour
{
    #region Поля: Required
    [PropertyOrder(-1), BoxGroup("Required"), Required(InfoMessageType.Error), SerializeField] private List<SpriteRenderer> _colorSprites;
    #endregion Поля: Required

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _recoilDistance = 0.5f;
    [BoxGroup("SETTINGS"), SerializeField] private float _dodgeDistance = 0.5f;
    [BoxGroup("SETTINGS"), SerializeField] private float _dodgeDuration = 0.2f;

    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private bool _isRecoiling = false;
    [BoxGroup("DEBUG"), SerializeField] private bool _ColoredDebug;
    #endregion Поля

    private BattleUnit _battleUnit;

    #region Методы UNITY
    private void Awake()
    {
        if (_colorSprites.Count == 0) DebugUtils.LogMissingReference(this, nameof(_colorSprites));
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void Initialize(BattleUnit battleUnit)
    {
        _battleUnit = battleUnit;
        SetUnitColorByLevel(battleUnit.Level);
    }

    public void PlayHitAnimation()
    {
        RecoilAnimation(_battleUnit.WatchingDirection == BattleUnit.Direction.Left ? BattleUnit.Direction.Right : BattleUnit.Direction.Left);
    }

    public void PlayAttackAnimation()
    {
        RecoilAnimation(_battleUnit.WatchingDirection);
    }

    public void PlayDodgeAnimation()
    {
        transform.DOShakePosition(_dodgeDuration, new Vector3(_dodgeDistance, 0, 0), 10, 90, false, true).SetEase(Ease.OutQuad);
    }

    public void SetUnitColorByLevel(BattleUnit.UnitLevel level)
    {
        Color newColor = GetColorForLevel(level);

        if (_colorSprites.Any() == false)
        {
            return;
        }

        if (_colorSprites[0] != null)
        {
            _colorSprites[0].color = newColor;
            ColoredDebug.CLog(gameObject, "<color=purple>BattleUnitAnimator:</color> Цвет изменен для уровня <color=yellow>{0}</color>.", _ColoredDebug, level);
        }
    }

    [Button]
    public void PlayJumpAnimation()
    {
        ColoredDebug.CLog(gameObject, "<color=purple>BattleUnitAnimator:</color> Запуск анимации прыжка.", _ColoredDebug);
    }

    [Button]
    public void PlayRepairAnimation()
    {
        ColoredDebug.CLog(gameObject, "<color=purple>BattleUnitAnimator:</color> Запуск анимации починки.", _ColoredDebug);
    }

    [Button]
    public void PlayBuildAnimation()
    {
        ColoredDebug.CLog(gameObject, "<color=purple>BattleUnitAnimator:</color> Запуск анимации строительства.", _ColoredDebug);
    }
    #endregion Публичные методы

    #region Личные методы
    private void RecoilAnimation(BattleUnit.Direction dir)
    {
        if (_isRecoiling) return;
        _isRecoiling = true;

        var originalPosition = transform.position;
        Vector3 recoilDirection = (dir == BattleUnit.Direction.Left) ? -transform.right : transform.right;
        Vector3 recoilPosition = originalPosition + recoilDirection * _recoilDistance;

        Sequence recoilSequence = DOTween.Sequence();
        recoilSequence.Append(transform.DOMove(recoilPosition, Settings.HitAttackAnimationSpeed));
        recoilSequence.Append(transform.DOMove(originalPosition, Settings.HitAttackAnimationSpeed));
        recoilSequence.OnComplete(() => _isRecoiling = false);
    }

    private Color GetColorForLevel(BattleUnit.UnitLevel level)
    {
        switch (level)
        {
            case BattleUnit.UnitLevel.Green_00: return new Color(0.2f, 0.8f, 0.2f);
            case BattleUnit.UnitLevel.Blue_01: return new Color(0.0f, 0.8f, 1.0f);
            case BattleUnit.UnitLevel.Yellow_02: return new Color(1.0f, 1.0f, 0.0f);
            case BattleUnit.UnitLevel.Orange_03: return new Color(1.0f, 0.5f, 0.0f);
            case BattleUnit.UnitLevel.Red_04: return new Color(0.9f, 0.1f, 0.1f);
            case BattleUnit.UnitLevel.Pink_05: return new Color(1.0f, 0.4f, 0.7f);
            case BattleUnit.UnitLevel.Turquoise_06: return new Color(0.25f, 0.88f, 0.82f);
            case BattleUnit.UnitLevel.DarkBlue_07: return new Color(0.0f, 0.2f, 0.8f);
            case BattleUnit.UnitLevel.Violet_08: return new Color(0.6f, 0.2f, 1.0f);
            case BattleUnit.UnitLevel.Maroon_09: return new Color(0.6f, 0.0f, 0.0f);
            case BattleUnit.UnitLevel.Indigo_10: return new Color(0.29f, 0.0f, 0.5f);
            case BattleUnit.UnitLevel.Grey_11: return new Color(0.5f, 0.5f, 0.5f);
            case BattleUnit.UnitLevel.Brown_12: return new Color(0.6f, 0.4f, 0.2f);
            case BattleUnit.UnitLevel.Silver_13: return new Color(0.75f, 0.75f, 0.75f);
            case BattleUnit.UnitLevel.Gold_14: return new Color(1.0f, 0.84f, 0.0f);
            case BattleUnit.UnitLevel.White_15: return new Color(1.0f, 1.0f, 1.0f);
            case BattleUnit.UnitLevel.Black_16: return new Color(0.1f, 0.1f, 0.1f);
            default: return Color.grey;
        }
    }
    #endregion Личные методы
}