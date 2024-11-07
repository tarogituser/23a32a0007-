using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleSceneDirector : MonoBehaviour
{
    [SerializeField]
    Text textWinCount, textLoseCount;

    //勝敗カウント
    public static int winCount, loseCount;

    [SerializeField, Header("タイルのプレハブ")]
    GameObject prefabTile;

    [SerializeField]
    List<GameObject> prefabUnits;

    //ボード配置 ※2次元配列
    int[,] boardSetting =
    {
        {4, 0, 1, 0, 0, 0,11, 0, 14},
        {5, 2, 1, 0, 0, 0,11,13, 15},
        {6, 0, 1, 0, 0, 0,11, 0, 16},
        {7, 0, 1, 0, 0, 0,11, 0, 17},
        {8, 0, 1, 0, 0, 0,11, 0, 18},
        {7, 0, 1, 0, 0, 0,11, 0, 17},
        {6, 0, 1, 0, 0, 0,11, 0, 16},
        {5, 3, 1, 0, 0, 0,11,12, 15},
        {4, 0, 1, 0, 0, 0,11, 0, 14},
    };

    // Use this for initialization
    void Start()
    {
        //勝敗数表示
        textWinCount.text = "現在の勝利数:" + winCount + "勝";
        textLoseCount.text = "現在の敗北数:" + loseCount + "敗";

        int boardWidth = 9, boardHeight = 9;

        //将棋盤のタイルとユニットを生成
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                //生成位置
                float x = i - boardWidth / 2;
                float z = j - boardHeight / 2;

                Vector3 pos = new Vector3(x, 0, z);

                //タイル生成
                Instantiate(prefabTile, pos, Quaternion.identity);

                //ユニット
                int type = boardSetting[i, j] % 10;
                int player = boardSetting[i, j] / 10;

                //空(0)はスキップ
                if (0 == type) continue;

                pos.y = 0.6f;

                GameObject prefab = prefabUnits[type - 1];
                Instantiate(prefab, pos, Quaternion.Euler(90, player * 180, 0));
            }
        }
    }

    //開始
    public void OnClickStart()
    {
        SceneManager.LoadScene("SelectScene");
    }

    //チュートリアル
    public void OnClickTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }

    //終了
    public void OnClickQuit()
    {
        Application.Quit();
    }
}