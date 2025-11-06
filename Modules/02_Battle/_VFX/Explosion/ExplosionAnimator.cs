using UnityEngine;

public class ExplosionAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] explosionFrames;
    [SerializeField] private float frameRate = 0.05f;

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    private float timer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        
    }

    private void OnEnable()
    {
        
        currentFrame = 0;
        timer = 0f;
        if (explosionFrames.Length > 0)
            spriteRenderer.sprite = explosionFrames[0];
    }

    private void Update()
    {
        if (explosionFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer -= frameRate;
            currentFrame++;

            if (currentFrame >= explosionFrames.Length)
            {
                // Анимация закончилась — возвращаем в пул
                ObjectPoolExplosion.Instance.ReturnObject(gameObject);
                return;
            }

            spriteRenderer.sprite = explosionFrames[currentFrame];
        }
    }
}
