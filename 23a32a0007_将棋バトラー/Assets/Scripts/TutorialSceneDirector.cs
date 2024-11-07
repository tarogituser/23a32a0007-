using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//チュートリアル

public class TutorialSceneDirector : MonoBehaviour
{
    //UI
    [SerializeField]
    Text textDialogInfo;
    [SerializeField]
    float typingSpeed;

    [SerializeField]
    GameObject buttonTitle;

    const int PlayerMax = 2;
    //将棋盤の幅と高さ
    int boardWidth, boardHeight;

    [SerializeField, Header("将棋盤タイル")]
    GameObject prefabTile;

    [SerializeField]
    List<GameObject> prefabUnits;

    //ボード配置 ※2次元配列
    //0→空、1→歩、2→角、3→飛車、4→香車、5→桂馬、6→銀、7→金、8→玉
    int[,] boardSetting =
    {
        {4, 0, 1, 0, 0, 0, 0, 0, 0},
        {5, 2, 1, 0, 0, 0, 0, 0, 0},
        {6, 0, 1, 0, 0, 0, 0, 0, 0},
        {7, 0, 1, 0, 0, 0, 0, 0, 0},
        {8, 0, 1, 0, 0, 0, 0, 0, 0},
        {7, 0, 1, 0, 0, 0, 0, 0, 0},
        {6, 0, 1, 0, 0, 0, 0, 0, 0},
        {5, 3, 1, 0, 0, 0, 0, 0, 0},
        {4, 0, 1, 0, 0, 0, 0, 0, 0},
    };

    //フィールドデータ
    Dictionary<Vector2Int, GameObject> tiles;
    UnitController[,] units;

    //選択ユニット
    UnitController selectUnit;
    //移動関連
    Dictionary<GameObject, Vector2Int> movableTiles;

    [SerializeField, Header("カーソル")]
    GameObject prefabCursor;

    List<GameObject> cursors;

    //表示するダイアログ
    [SerializeField, TextArea]
    List<string> dialogs;

    [SerializeField, Header("敵陣")]
    GameObject enemySide;

    //敵陣3マス
    const int EnemyLine = 3;
    List<int>[] enemyLines;

    [SerializeField, Header("BGM&SE制御")]
    SoundController sound;

    //チュートリアル中フラグ
    bool isTeaching = true;

    // Start is called before the first frame update
    void Start()
    {
        sound.PlayBGM();
        //初期化
        buttonTitle.SetActive(false);
        textDialogInfo.text = "";

        boardWidth = 9;
        boardHeight = 9;

        tiles = new Dictionary<Vector2Int, GameObject>();
        units = new UnitController[boardWidth, boardHeight];

        movableTiles = new Dictionary<GameObject, Vector2Int>();
        cursors = new List<GameObject>();

        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                //生成位置
                float x = i - boardWidth / 2;
                float z = j - boardHeight / 2;

                Vector3 pos = new Vector3(x, 0, z);

                Vector2Int idx = new Vector2Int(i, j);

                //タイル生成
                GameObject tile = Instantiate(prefabTile, pos, Quaternion.identity);
                tiles.Add(idx, tile);

                //ユニット
                int type = boardSetting[i, j] % 10;
                int player = boardSetting[i, j] / 10;

                //空(0)はスキップ
                if (0 == type) continue;

                pos.y = 0.6f;

                //ユニット生成
                GameObject prefab = prefabUnits[type - 1];
                GameObject unit = Instantiate(prefab, pos, Quaternion.Euler(90, player * 180, 0));

                UnitController unitctrl = unit.AddComponent<UnitController>();
                unitctrl.Init(player, type, tile, idx);

                units[i, j] = unitctrl;
            }
        }

        //敵陣設定
        enemyLines = new List<int>[PlayerMax];
        for (int i = 0; i < PlayerMax; i++)
        {
            enemyLines[i] = new List<int>();
            int rangemin = 0;

            if (0 == i)
            {
                rangemin = boardHeight - EnemyLine;
            }

            for (int j = 0;  j < EnemyLine; j++)
            {
                enemyLines[i].Add(rangemin + j);
            }
        }

        StartCoroutine(Tutorial());
    }

    //チュートリアル
    IEnumerator Tutorial()
    {
        for (int i = 0; i < dialogs.Count; i++)
        {
            //敵陣3マスを表示
            if (11 == i)
            {
                enemySide.SetActive(true);
            }
            //敵陣3マスを非表示
            if (15 == i)
            {
                enemySide.SetActive(false);
            }

            //ダイアログ表示
            textDialogInfo.text = "";
            foreach (char moji in dialogs[i].ToCharArray())
            {
                textDialogInfo.text += moji;
                yield return new WaitForSeconds(typingSpeed / 30);
            }

            yield return new WaitForSeconds(2);
        }
        //チュートリアル終了
        isTeaching = false;

        yield return new WaitForSeconds(14);

        textDialogInfo.text = "では、健闘を祈る！";
        sound.PlaySE(1);
        buttonTitle.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        GameObject tile = null;
        UnitController unit = null;

        if (Input.GetMouseButtonUp(0))
        {
            //Rayを投射して全ての当たり判定を取得
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray))
            {
                //タイル選択
                if (tiles.ContainsValue(hit.transform.gameObject))
                {
                    tile = hit.transform.gameObject;
                    foreach (var item in tiles)
                    {
                        if (item.Value == tile)
                        {
                            unit = units[item.Key.x, item.Key.y];
                        }
                    }
                    break;
                }
            }
        }

        //チュートリアル中は処理しない
        if (isTeaching) return;

        //移動先選択
        if (tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            MoveUnit(selectUnit, movableTiles[tile]);
            selectUnit = null;
        }
        //ユニット選択
        if (unit)
        {
            SetSelectCursors(unit);
        }
    }

    //選択カーソル設定
    void SetSelectCursors(UnitController unit = null, bool playerunit = true)
    {
        //カーソル削除
        foreach (var item in cursors)
        {
            Destroy(item);
        }
        cursors.Clear();

        //選択解除
        if (selectUnit)
        {
            selectUnit.Select(false);
            selectUnit = null;
        }

        //選択していないなら処理しない
        if (!unit) return;

        //移動可能範囲を取得
        List<Vector2Int> movabletiles = unit.GetMovableTiles(units);
        movableTiles.Clear();

        foreach (var item in movabletiles)
        {
            movableTiles.Add(tiles[item], item);

            //カーソル生成
            Vector3 pos = tiles[item].transform.position;
            pos.y += 0.51f;
            GameObject cursor = Instantiate(prefabCursor, pos, Quaternion.identity);
            cursors.Add(cursor);
        }

        //選択状態
        if (playerunit)
        {
            unit.Select();
            selectUnit = unit;
        }
    }

    //ユニット移動
    void MoveUnit(UnitController unit, Vector2Int tileidx)
    {
        //現在地
        Vector2Int oldpos = unit.Pos;

        unit.Move(tiles[tileidx], tileidx);

        units[tileidx.x, tileidx.y] = unit;

        //ボード上にあるなら移動元を更新
        if (FieldStatus.OnBoard == unit.FieldStatus)
        {
            units[oldpos.x, oldpos.y] = null;

            //成り
            if (unit.IsEvolution() && enemyLines[0].Contains(tileidx.y))
            {
                unit.Evolution();
            }
        }

        unit.FieldStatus = FieldStatus.OnBoard;

        sound.PlaySE(0);
    }

    //タイトル
    public void OnClickTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}