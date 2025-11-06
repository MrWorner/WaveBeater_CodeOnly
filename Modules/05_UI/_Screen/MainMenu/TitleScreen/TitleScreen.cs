using System.Collections;
using UnityEngine;

public class TitleScreen : MonoBehaviour
{

    void Start()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        ///ScreenFader.ActiveInstance.ShowLoadingScreen();
        ///yield return new WaitForSeconds(2f);
        SceneLoader.Instance.LoadNextScene(GameScene.MainMenu);
    }
}
