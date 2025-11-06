using UnityEngine;
using DG.Tweening;

/// <summary>
/// Этот компонент непрерывно вращает GameObject по оси Z,
/// когда он активен, используя DOTween.
/// Вращение автоматически останавливается, когда GameObject становится неактивным.
/// </summary>
public class ShieldRotator : MonoBehaviour
{
    [Tooltip("Скорость вращения в градусах в секунду.")]
    [SerializeField] private float _rotationSpeed = 90f;

    private Tween _rotationTween; // Переменная для хранения нашего твина вращения

    // Этот метод вызывается, когда GameObject становится активным и включенным
    private void OnEnable()
    {
        // Убиваем предыдущий твин, если он по какой-то причине еще существует
        // Это хорошая практика для избежания дублирования твинов
        if (_rotationTween != null)
        {
            _rotationTween.Kill();
        }

        // Вычисляем продолжительность одного полного оборота (360 градусов)
        // на основе заданной скорости
        float duration = 360f / _rotationSpeed;

        // Запускаем анимацию вращения
        _rotationTween = transform.DORotate(
            new Vector3(0, 0, 360), // Целевой угол вращения (полный оборот)
            duration,               // Продолжительность одного оборота
            RotateMode.FastBeyond360 // Режим вращения, который добавляет 360 к текущему углу
        )
        .SetEase(Ease.Linear)       // Устанавливаем линейную скорость вращения (без ускорений и замедлений)
        .SetLoops(-1, LoopType.Restart); // Зацикливаем анимацию бесконечно (-1)
    }

    // Этот метод вызывается, когда GameObject становится неактивным
    private void OnDisable()
    {
        // Останавливаем и уничтожаем твин, чтобы он не работал в фоновом режиме
        if (_rotationTween != null)
        {
            _rotationTween.Kill();
            _rotationTween = null; // Обнуляем ссылку
        }
    }
}
