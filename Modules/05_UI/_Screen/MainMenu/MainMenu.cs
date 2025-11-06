using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    [Header("Fade Settings")]
    public Image fadeImage; // Черный фон на весь экран
    public float fadeDuration = 0.5f;

    [Header("Button")]
    public Button startButton;

    public GameObject _panel_MainMenu;
    public GameObject _panel_ChooseHero;
    public GameObject _panel_ChooseLevel;
    public GameObject _mainMenuBackground;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); } else { Instance = this; }

        _panel_MainMenu.SetActive(true);
        _mainMenuBackground.SetActive(true);
        _panel_ChooseHero.SetActive(false);
        _panel_ChooseLevel.SetActive(false);
    }

    private void Start()
    {
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        StartCoroutine(Fade(1f, 0f, fadeDuration));

        startButton.onClick.AddListener(GoToHeroSelection);

        MusicManager.Instance.PlayMenuMusic();

        // Если звук все еще на паузе, включаем его
        if (AudioListener.pause)
        {
            AudioListener.pause = false;
            Debug.Log("Звук был принудительно включен скриптом AudioFixer.");
        }
    }

    public void GoToHeroSelection()
    {
        SoundManager.Instance.PlayOneShot(SoundType.ButtonClickAlternative1);
        _panel_MainMenu.SetActive(false);
        _panel_ChooseHero.SetActive(true);
        _panel_ChooseLevel.SetActive(false);
    }

    public void GoToLevelSelection()
    {
        SoundManager.Instance.PlayOneShot(SoundType.ButtonClickAlternative1);
        _panel_MainMenu.SetActive(false);
        _panel_ChooseHero.SetActive(false);
        _panel_ChooseLevel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        SoundManager.Instance.PlayOneShot(SoundType.ButtonClickAlternative1);
        _panel_MainMenu.SetActive(true);
        _panel_ChooseHero.SetActive(false);
        _panel_ChooseLevel.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        Color c = fadeImage.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            c.a = Mathf.Lerp(from, to, t);
            fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
    }
}
