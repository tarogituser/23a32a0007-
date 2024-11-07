using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

//ゲームを管理する
public class GameSceneDirector : MonoBehaviour
{
    //UI
    [SerializeField]
    Text textTurnInfo, textResultInfo, textTurnCount;
    [SerializeField]
    GameObject buttonRematch, buttonEvolutionApply,
        buttonEvolutionCancel;

    //ゲーム設定
    const int PlayerMax = 2;
    //ボードの幅と高さ
    int boardWidth, boardHeight;

    [Header("将棋盤のタイル")]
    [SerializeField]
    GameObject prefabTile;
    //使う駒
    [SerializeField]
    List<GameObject> prefabUnits;

    //ボード配置
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

    //フィールドデータ
    Dictionary<Vector2Int, GameObject> tiles;
    UnitController[,] units;

    //移動関連
    Dictionary<GameObject, Vector2Int> movableTiles;
    //選択ユニット
    UnitController selectUnit;

    [Header("カーソル")]
    [SerializeField]
    GameObject prefabCursor;

    List<GameObject> cursors;

    //プレイ中のプレイヤー・ターン数
    int nowPlayer, turnCount;

    //ゲームモード
    enum Mode
    {
        None,
        Start,
        Select,
        TurnChange,
        WaitEvolution,
        Result
    }

    Mode nowMode, nextMode;

    //CPUプレイ
    bool isCpu;

    [Header("持ち駒タイル")]
    [SerializeField]
    GameObject prefabUnitTile;

    List<GameObject>[] unitTiles;

    //持ち駒のユニット
    List<UnitController> captureUnits;

    //敵陣3マス
    const int EnemyLine = 3;
    List<int>[] enemyLines;

    //CPU設定
    const float EnemyWaitTimerMin = 0.16f;
    const float EnemyWaitTimerMax = 1.3f;
    float enemyWaitTimer;

    //プレイヤー数
    public static int PlayerCount;

    [Header("BGM&SE制御")]
    [SerializeField]
    SoundController sound;

    // Start is called before the first frame update
    void Start()
    {
        sound.PlayBGM();
        //非表示
        buttonRematch.SetActive(false);
        buttonEvolutionApply.SetActive(false);
        buttonEvolutionCancel.SetActive(false);
        textResultInfo.text = "";

        boardWidth = 9;
        boardHeight = 9;

        //初期化
        tiles = new Dictionary<Vector2Int, GameObject>();
        units = new UnitController[boardWidth, boardHeight];

        movableTiles = new Dictionary<GameObject, Vector2Int>();
        cursors = new List<GameObject>();

        unitTiles = new List<GameObject>[PlayerMax];

        captureUnits = new List<UnitController>();

        //将棋盤のタイルとユニットを生成
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
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

                if (0 == type) continue;

                pos.y = 0.6f;

                //ユニット生成
                GameObject prefab = prefabUnits[type - 1];
                GameObject unit = Instantiate(prefab, pos, Quaternion.Euler(90, player * 180, 0));

                //UnitController取得
                UnitController unitctrl = unit.AddComponent<UnitController>();
                unitctrl.Init(player, type, tile, idx);

                units[i, j] = unitctrl;
            }
        }

        //持ち駒を置く場所を生成
        Vector3 startPos = new Vector3(5, 0.5f, -2);
        for (int i = 0; i < PlayerMax; i++)
        {
            unitTiles[i] = new List<GameObject>();
            int dir = (0 == i) ? 1 : -1;

            for (int j = 0; j < 9; j++)
            {
                Vector3 pos = startPos;
                pos.x = (pos.x + j % 3) * dir;
                pos.z = (pos.z - j / 3) * dir;

                GameObject tile = Instantiate(prefabUnitTile, pos, Quaternion.identity);
                unitTiles[i].Add(tile);

                tile.SetActive(false);
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

            for (int j = 0; j < EnemyLine; j++)
            {
                enemyLines[i].Add(rangemin + j);
            }
        }

        nowPlayer = -1;
        //初回モード
        nowMode = Mode.None;
        nextMode = Mode.TurnChange;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mode.Start == nowMode)
        {
            StartMode();
        }

        if (Mode.Select == nowMode)
        {
            SelectMode();
        }

        if (Mode.TurnChange == nowMode)
        {
            TurnChangeMode();
        }

        //モード切り替え
        if (Mode.None != nextMode)
        {
            nowMode = nextMode;
            nextMode = Mode.None;
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

        //移動可能な範囲を取得
        List<Vector2Int> movabletiles = getMovableTiles(unit);
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
        nextMode = Mode.TurnChange;
        
        //現在地
        Vector2Int oldpos = unit.Pos;

        CaptureUnit(tileidx);

        unit.Move(tiles[tileidx], tileidx);

        units[tileidx.x, tileidx.y] = unit;

        if (FieldStatus.OnBoard == unit.FieldStatus)
        {
            units[oldpos.x, oldpos.y] = null;

            if (unit.IsEvolution() && enemyLines[nowPlayer].Contains(tileidx.y))
            {
                UnitController[,] copyunits = new UnitController[boardWidth, boardHeight];
                copyunits[oldpos.x, oldpos.y] = unit;

                //成り
                if (isCpu || 1 > unit.GetMovableTiles(copyunits).Count)
                {
                    //強制的に成る
                    unit.Evolution();
                }
                else
                {
                    //成った状態を表示する
                    unit.Evolution();
                    SetSelectCursors(unit);

                    textResultInfo.text = "成りますか？";
                    buttonEvolutionApply.SetActive(true);
                    buttonEvolutionCancel.SetActive(true);

                    nextMode = Mode.WaitEvolution;
                }
            }
        }
        else
        {
            captureUnits.Remove(unit);
        }

        unit.FieldStatus = FieldStatus.OnBoard;

        //持ち駒を並べる
        AlignCaptureUnits(nowPlayer);

        sound.PlaySE(0);
    }

    //移動可能な範囲を取得
    List<Vector2Int> getMovableTiles(UnitController unit)
    {
        List<Vector2Int> ret = unit.GetMovableTiles(units);

        //王手されているかチェック
        UnitController[,] copyunits = GetCopyArray(units);
        if (FieldStatus.OnBoard == unit.FieldStatus)
        {
            copyunits[unit.Pos.x, unit.Pos.y] = null;
        }
        int outecount = GetOuteUnits(copyunits, unit.PlayerNum).Count;
        //王手を回避できる場所をチェック
        if (0 < outecount)
        {
            ret = new List<Vector2Int>();
            List<Vector2Int> movabletiles = unit.GetMovableTiles(units);
            foreach (var item in movabletiles)
            {
                UnitController[,] copyunits2 = GetCopyArray(copyunits);
                copyunits2[item.x, item.y] = unit;
                outecount = GetOuteUnits(copyunits2, unit.PlayerNum, false).Count;
                if (1 > outecount) ret.Add(item);
            }
        }

        return ret;
    }

    //ゲーム開始
    void StartMode()
    {
        textTurnInfo.text = (nowPlayer + 1) + "Pの番です。";
        textResultInfo.text = "";
        nextMode = Mode.Select;

        //王手
        List<UnitController> outeunits = GetOuteUnits(units, nowPlayer);
        bool isoute = 0 < outeunits.Count;
        if (isoute)
        {
            textResultInfo.text = "王手！！";
        }

        //200手ルール
        if (200 < turnCount)
        {
            textResultInfo.text = "200手ルール！！\n" + "引き分け";
            textTurnCount.text = "";
            sound.PlaySE(3);
            nextMode = Mode.Result;
        }

        int movablecount = 0;
        foreach (var item in GetUnits(nowPlayer))
        {
            movablecount += getMovableTiles(item).Count;
        }

        //詰み判定
        if (1 > movablecount && isoute)
        {
            textResultInfo.text = "詰み！！\n" + (GetNextPlayer(nowPlayer) + 1) + "Pの勝ち！";
            sound.PlaySE(1);
            sound.PlaySE(2);
            nextMode = Mode.Result;

            //勝敗カウントを増やす
            if (GetNextPlayer(nowPlayer) + 1 == 1)
            {
                TitleSceneDirector.winCount++;
            }
            else
            {
                TitleSceneDirector.loseCount++;
            }
        }

        //結果
        if (Mode.Result == nextMode)
        {
            textTurnInfo.text = "";
            buttonRematch.SetActive(true);
        }

        //CPU
        if (PlayerCount <= nowPlayer)
        {
            isCpu = true;
            enemyWaitTimer = Random.Range(EnemyWaitTimerMin, EnemyWaitTimerMax);
        }
    }

    //プレイヤー選択
    void SelectMode()
    {
        GameObject tile = null;
        UnitController unit = null;

        //プレイヤーの番
        if (Input.GetMouseButtonUp(0))
        {
            //Rayを投射して全ての当たり判定を取得
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray))
            {
                //当たり判定のあるユニット
                UnitController hitunit = hit.transform.GetComponent<UnitController>();
                //持ち駒
                if (hitunit && FieldStatus.Captured == hitunit.FieldStatus)
                {
                    unit = hitunit;
                }
                //タイル選択
                else if (tiles.ContainsValue(hit.transform.gameObject))
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

        //CPUの番
        if (isCpu)
        {
            if (0 < enemyWaitTimer)
            {
                enemyWaitTimer -= Time.deltaTime;
                return;
            }

            //非選択
            if (!selectUnit)
            {
                //全ユニット取得してランダム選択
                List<UnitController> allunits = GetUnits(nowPlayer);
                unit = allunits[Random.Range(0, allunits.Count)];

                if (1 > getMovableTiles(unit).Count)
                {
                    unit = null;
                }
            }
            else
            {
                //ランダムに動かす
                List<GameObject> tiles = new List<GameObject>(movableTiles.Keys);
                tile = tiles[Random.Range(0, tiles.Count)];
                selectUnit.gameObject.SetActive(true);
            }
        }

        //移動先選択
        if (tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            MoveUnit(selectUnit, movableTiles[tile]);
        }
        //ユニット選択
        else if (unit)
        {
            SetSelectCursors(unit, nowPlayer == unit.PlayerNum);
        }
    }

    //ターン切り替え
    void TurnChangeMode()
    {
        SetSelectCursors();
        buttonEvolutionApply.SetActive(false);
        buttonEvolutionCancel.SetActive(false);

        isCpu = false;
        //次のプレイヤーのターン
        nowPlayer = GetNextPlayer(nowPlayer);

        if (0 == nowPlayer)
        {
            turnCount++;
            textTurnCount.text = turnCount + "手目";
        }

        nextMode = Mode.Start;
    }

    //次のプレイヤーの番号を返す
    public static int GetNextPlayer(int player)
    {
        int next = player + 1;
        if (PlayerMax <= next) next = 0;

        return next;
    }

    //持ち駒にする
    void CaptureUnit(Vector2Int tileidx)
    {
        UnitController unit = units[tileidx.x, tileidx.y];
        if (!unit) return;
        unit.Capture(nowPlayer);
        captureUnits.Add(unit);
        units[tileidx.x, tileidx.y] = null;
    }

    //持ち駒を並べる
    void AlignCaptureUnits(int player)
    {
        foreach (var item in unitTiles[player])
        {
            item.SetActive(false);
        }

        //ユニットごとに分ける
        Dictionary<UnitType, List<UnitController>> typeunits
            = new Dictionary<UnitType, List<UnitController>>();

        foreach (var item in captureUnits)
        {
            if (player != item.PlayerNum) continue;
            typeunits.TryAdd(item.UnitType, new List<UnitController>());
            typeunits[item.UnitType].Add(item);
        }

        int tilecount = 0;
        foreach (var item in typeunits)
        {
            if (1 > item.Value.Count) continue;

            GameObject tile = unitTiles[player][tilecount++];

            tile.SetActive(true);

            //持ち駒の数を表示する
            tile.transform.GetChild(0).GetComponent<TextMeshPro>().text
                = "" + item.Value.Count;

            //同じ種類の持ち駒を並べる
            for (int i = 0; i < item.Value.Count; i++)
            {
                GameObject unit = item.Value[i].gameObject;
                Vector3 pos = tile.transform.position;
                unit.SetActive(true);
                unit.transform.position = pos;
                if (0 < i) unit.SetActive(false);
            }
        }
    }

    //配列をコピー
    public static UnitController[,] GetCopyArray(UnitController[,] ary)
    {
        UnitController[,] ret = new UnitController[9, 9];
        System.Array.Copy(ary, ret, ary.Length);
        return ret;
    }

    //王手しているユニットを返す
    public static List<UnitController> GetOuteUnits(UnitController[,] units, int player, bool checkotherunit = true)
    {
        List<UnitController> ret = new List<UnitController>();
        foreach (var unit in units)
        {
            if (!unit || player == unit.PlayerNum) continue;

            //ユニットの移動可能な範囲
            List<Vector2Int> movabletiles = unit.GetMovableTiles(units, checkotherunit);
            foreach (var tile in movabletiles)
            {
                if (!units[tile.x, tile.y]) continue;

                if (UnitType.Gyoku == units[tile.x, tile.y].UnitType)
                {
                    ret.Add(unit);
                }
            }
        }

        return ret;
    }

    public void OnClickEvolutionApply()
    {
        nextMode = Mode.TurnChange;
    }

    public void OnClickEvolutionCancel()
    {
        selectUnit.Evolution(false);
        OnClickEvolutionApply();
    }

    //全ユニット取得
    List<UnitController> GetUnits(int player)
    {
        List<UnitController> ret = new List<UnitController>();

        List<UnitController> allunits = new List<UnitController>(captureUnits);
        allunits.AddRange(units);

        foreach (var unit in allunits)
        {
            if (!unit || player != unit.PlayerNum) continue;

            ret.Add(unit);
        }

        return ret;
    }

    public void OnClickRestart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnClickTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
