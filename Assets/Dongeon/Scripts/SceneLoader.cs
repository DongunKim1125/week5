using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환과 관련된 공통 기능을 제공하는 클래스
/// </summary>
public static class SceneLoader
{
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        SceneManager.LoadScene(sceneName);
    }

    public static void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public static void BackToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public static void GoToStageSelect()
    {
        SceneManager.LoadScene("StageSelectScene");
    }
}
