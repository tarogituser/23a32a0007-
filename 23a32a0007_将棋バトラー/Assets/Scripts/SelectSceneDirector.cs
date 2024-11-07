using UnityEngine;
using UnityEngine.SceneManagement;

//‘I‘ð‰æ–Ê
public class SelectSceneDirector : MonoBehaviour
{
    //1Pvs2P
    public void OnClickPvP()
    {
        GameSceneDirector.PlayerCount = 2;
        SceneManager.LoadScene("GameScene");
    }

    //1PvsCPU
    public void OnClickPvE()
    {
        GameSceneDirector.PlayerCount = 1;
        SceneManager.LoadScene("GameScene");
    }

    //CPUvsCPU
    public void OnClickEvE()
    {
        GameSceneDirector.PlayerCount = 0;
        SceneManager.LoadScene("GameScene");
    }

    //–ß‚é
    public void OnClickBack()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
