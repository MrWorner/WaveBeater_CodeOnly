using UnityEngine;

public class GameOverHandler : MonoBehaviour
{
    // Пока что этот метод будет вызывать тот же экран, что и при смерти.
    // В будущем вы можете создать отдельный UI для статистики и вызывать его отсюда.
    public void ShowGameOverScreen()
    {
        Debug.Log("GAME OVER: Показываем экран статистики (пока используется экран смерти).");
     
        if (UI_EndGame.Instance != null)
        {
            UI_EndGame.Instance.ShowEndScreen();
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayEndGameMusic();
        }       
    }
}
