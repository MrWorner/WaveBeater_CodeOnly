using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{
    private BattleUnit _target;
    private int _damage;
    private bool _willHit;
    private BattleUnit _owner;
    private bool _isCritical;
    private UnityAction _onCompletion;

    #region Поля
    [BoxGroup("SETTINGS"), SerializeField] private float _arcModifier = 1.1f;
    [BoxGroup("SETTINGS"), SerializeField] private float _minDistanceForArc = 3f;
    [BoxGroup("SETTINGS"), SerializeField] private float _minArcHeight = 0.1f;

    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector3 _startPosition;
    [BoxGroup("DEBUG"), SerializeField, ReadOnly] private Vector3 _endPosition;
    [BoxGroup("DEBUG"), SerializeField] protected bool _ColoredDebug;
    #endregion Поля

    #region Методы UNITY
    private void OnDisable()
    {
        _target = null;
        _owner = null;
        transform.DOKill();
    }
    #endregion Методы UNITY

    #region Публичные методы
    public void Initialize(BattleUnit targetInstance, int dmg, BattleUnit owner, UnityAction onCompletion = null, bool willHit = true, Vector3 missPosition = new Vector3(), bool isCritical = false)
    {
        _target = targetInstance;
        _damage = dmg;
        _willHit = willHit;
        _owner = owner;
        _startPosition = transform.position;
        _isCritical = isCritical;

        if (_willHit)
        {
            _endPosition = _target.DamagePoint.position;
        }
        else
        {
            _endPosition = missPosition;
        }

        _onCompletion = onCompletion;

        ColoredDebug.CLog(gameObject, "<color=lime>Projectile:</color> Инициализация. Владелец: <color=yellow>{0}</color>, Цель: <color=yellow>{1}</color>, Урон: <color=lime>{2}</color>, Попадет: <color=cyan>{3}</color>, Крит: <color=magenta>{4}</color>.", _ColoredDebug,
            _owner != null ? _owner.name : "NONE",
            _target != null ? (targetInstance as Component).name : "MISS_POSITION",
            _damage,
            _willHit,
            _isCritical);

        SimulateProjectileTrajectory();
    }
    #endregion Публичные методы

    #region Личные методы
    private void SimulateProjectileTrajectory()
    {
        float distance = Vector3.Distance(_startPosition, _endPosition);
        float flightDuration = distance / Settings.ProjectileSpeed;

        float currentArcHeight;
        if (distance <= _minDistanceForArc)
        {
            currentArcHeight = _minArcHeight;
        }
        else
        {
            currentArcHeight = distance * _arcModifier;
        }

        currentArcHeight = Mathf.Clamp(currentArcHeight, _minArcHeight, 5f);
        Vector3[] path = GenerateParabolicPath(_startPosition, _endPosition, currentArcHeight, 10);
        transform.DOPath(path, flightDuration, PathType.CatmullRom).SetEase(Ease.Linear).OnComplete(OnProjectileHit);
    }

    private Vector3[] GenerateParabolicPath(Vector3 start, Vector3 end, float height, int steps)
    {
        Vector3[] path = new Vector3[steps];
        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / (steps - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            point.y += height * 4 * (t - t * t);
            path[i] = point;
        }
        return path;
    }

    private void OnProjectileHit()
    {
        if (_willHit)
        {
            if (_target != null && _target.IsAlive)
            {
                ColoredDebug.CLog(gameObject, "<color=lime>Projectile:</color> Снаряд достиг цели <color=yellow>{0}</color> и нанес урон.", _ColoredDebug, (_target as Component).name);
                _target.Health.TakeDamage(_damage, _owner, _isCritical);
            }
        }
        else
        {
            ColoredDebug.CLog(gameObject, "<color=yellow>Projectile:</color> Снаряд промахнулся, попав в <color=orange>{0}</color>.", _ColoredDebug, _endPosition);
            FloatingTextManager.Instance.SpawnFloatingText("MISS", FloatingText.TextType.Neutral, _endPosition);
            SoundManager.Instance.PlayOneShot(SoundType.BulletRicoshet);
        }

        StartCoroutine(WaitBeforeCompletion());
    }

    private IEnumerator WaitBeforeCompletion()
    {
        yield return new WaitForSeconds(0.1f);
        _onCompletion?.Invoke();
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ObjectPoolProjectiles.Instance.ReturnObject(gameObject);
    }
    #endregion Личные методы
}
