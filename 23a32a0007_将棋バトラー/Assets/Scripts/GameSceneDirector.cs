using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

//�Q�[�����Ǘ�����
public class GameSceneDirector : MonoBehaviour
{
    //UI
    [SerializeField]
    Text textTurnInfo, textResultInfo, textTurnCount;
    [SerializeField]
    GameObject buttonRematch, buttonEvolutionApply,
        buttonEvolutionCancel;

    //�Q�[���ݒ�
    const int PlayerMax = 2;
    //�{�[�h�̕��ƍ���
    int boardWidth, boardHeight;

    [Header("�����Ղ̃^�C��")]
    [SerializeField]
    GameObject prefabTile;
    //�g����
    [SerializeField]
    List<GameObject> prefabUnits;

    //�{�[�h�z�u
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

    //�t�B�[���h�f�[�^
    Dictionary<Vector2Int, GameObject> tiles;
    UnitController[,] units;

    //�ړ��֘A
    Dictionary<GameObject, Vector2Int> movableTiles;
    //�I�����j�b�g
    UnitController selectUnit;

    [Header("�J�[�\��")]
    [SerializeField]
    GameObject prefabCursor;

    List<GameObject> cursors;

    //�v���C���̃v���C���[�E�^�[����
    int nowPlayer, turnCount;

    //�Q�[�����[�h
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

    //CPU�v���C
    bool isCpu;

    [Header("������^�C��")]
    [SerializeField]
    GameObject prefabUnitTile;

    List<GameObject>[] unitTiles;

    //������̃��j�b�g
    List<UnitController> captureUnits;

    //�G�w3�}�X
    const int EnemyLine = 3;
    List<int>[] enemyLines;

    //CPU�ݒ�
    const float EnemyWaitTimerMin = 0.16f;
    const float EnemyWaitTimerMax = 1.3f;
    float enemyWaitTimer;

    //�v���C���[��
    public static int PlayerCount;

    [Header("BGM&SE����")]
    [SerializeField]
    SoundController sound;

    // Start is called before the first frame update
    void Start()
    {
        sound.PlayBGM();
        //��\��
        buttonRematch.SetActive(false);
        buttonEvolutionApply.SetActive(false);
        buttonEvolutionCancel.SetActive(false);
        textResultInfo.text = "";

        boardWidth = 9;
        boardHeight = 9;

        //������
        tiles = new Dictionary<Vector2Int, GameObject>();
        units = new UnitController[boardWidth, boardHeight];

        movableTiles = new Dictionary<GameObject, Vector2Int>();
        cursors = new List<GameObject>();

        unitTiles = new List<GameObject>[PlayerMax];

        captureUnits = new List<UnitController>();

        //�����Ղ̃^�C���ƃ��j�b�g�𐶐�
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                float x = i - boardWidth / 2;
                float z = j - boardHeight / 2;

                Vector3 pos = new Vector3(x, 0, z);

                Vector2Int idx = new Vector2Int(i, j); 

                //�^�C������
                GameObject tile = Instantiate(prefabTile, pos, Quaternion.identity);
                tiles.Add(idx, tile);

                //���j�b�g
                int type = boardSetting[i, j] % 10;
                int player = boardSetting[i, j] / 10;

                if (0 == type) continue;

                pos.y = 0.6f;

                //���j�b�g����
                GameObject prefab = prefabUnits[type - 1];
                GameObject unit = Instantiate(prefab, pos, Quaternion.Euler(90, player * 180, 0));

                //UnitController�擾
                UnitController unitctrl = unit.AddComponent<UnitController>();
                unitctrl.Init(player, type, tile, idx);

                units[i, j] = unitctrl;
            }
        }

        //�������u���ꏊ�𐶐�
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

        //�G�w�ݒ�
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
        //���񃂁[�h
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

        //���[�h�؂�ւ�
        if (Mode.None != nextMode)
        {
            nowMode = nextMode;
            nextMode = Mode.None;
        }
    }

    //�I���J�[�\���ݒ�
    void SetSelectCursors(UnitController unit = null, bool playerunit = true)
    {
        //�J�[�\���폜
        foreach (var item in cursors)
        {
            Destroy(item);
        }
        cursors.Clear();

        //�I������
        if (selectUnit)
        {
            selectUnit.Select(false);
            selectUnit = null;
        }

        //�I�����Ă��Ȃ��Ȃ珈�����Ȃ�
        if (!unit) return;

        //�ړ��\�Ȕ͈͂��擾
        List<Vector2Int> movabletiles = getMovableTiles(unit);
        movableTiles.Clear();

        foreach (var item in movabletiles)
        {
            movableTiles.Add(tiles[item], item);

            //�J�[�\������
            Vector3 pos = tiles[item].transform.position;
            pos.y += 0.51f;
            GameObject cursor = Instantiate(prefabCursor, pos, Quaternion.identity);
            cursors.Add(cursor);
        }

        //�I�����
        if (playerunit)
        {
            unit.Select();
            selectUnit = unit;
        }
    }

    //���j�b�g�ړ�
    void MoveUnit(UnitController unit, Vector2Int tileidx)
    {
        nextMode = Mode.TurnChange;
        
        //���ݒn
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

                //����
                if (isCpu || 1 > unit.GetMovableTiles(copyunits).Count)
                {
                    //�����I�ɐ���
                    unit.Evolution();
                }
                else
                {
                    //��������Ԃ�\������
                    unit.Evolution();
                    SetSelectCursors(unit);

                    textResultInfo.text = "����܂����H";
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

        //���������ׂ�
        AlignCaptureUnits(nowPlayer);

        sound.PlaySE(0);
    }

    //�ړ��\�Ȕ͈͂��擾
    List<Vector2Int> getMovableTiles(UnitController unit)
    {
        List<Vector2Int> ret = unit.GetMovableTiles(units);

        //���肳��Ă��邩�`�F�b�N
        UnitController[,] copyunits = GetCopyArray(units);
        if (FieldStatus.OnBoard == unit.FieldStatus)
        {
            copyunits[unit.Pos.x, unit.Pos.y] = null;
        }
        int outecount = GetOuteUnits(copyunits, unit.PlayerNum).Count;
        //���������ł���ꏊ���`�F�b�N
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

    //�Q�[���J�n
    void StartMode()
    {
        textTurnInfo.text = (nowPlayer + 1) + "P�̔Ԃł��B";
        textResultInfo.text = "";
        nextMode = Mode.Select;

        //����
        List<UnitController> outeunits = GetOuteUnits(units, nowPlayer);
        bool isoute = 0 < outeunits.Count;
        if (isoute)
        {
            textResultInfo.text = "����I�I";
        }

        //200�胋�[��
        if (200 < turnCount)
        {
            textResultInfo.text = "200�胋�[���I�I\n" + "��������";
            textTurnCount.text = "";
            sound.PlaySE(3);
            nextMode = Mode.Result;
        }

        int movablecount = 0;
        foreach (var item in GetUnits(nowPlayer))
        {
            movablecount += getMovableTiles(item).Count;
        }

        //�l�ݔ���
        if (1 > movablecount && isoute)
        {
            textResultInfo.text = "�l�݁I�I\n" + (GetNextPlayer(nowPlayer) + 1) + "P�̏����I";
            sound.PlaySE(1);
            sound.PlaySE(2);
            nextMode = Mode.Result;

            //���s�J�E���g�𑝂₷
            if (GetNextPlayer(nowPlayer) + 1 == 1)
            {
                TitleSceneDirector.winCount++;
            }
            else
            {
                TitleSceneDirector.loseCount++;
            }
        }

        //����
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

    //�v���C���[�I��
    void SelectMode()
    {
        GameObject tile = null;
        UnitController unit = null;

        //�v���C���[�̔�
        if (Input.GetMouseButtonUp(0))
        {
            //Ray�𓊎˂��đS�Ă̓����蔻����擾
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray))
            {
                //�����蔻��̂��郆�j�b�g
                UnitController hitunit = hit.transform.GetComponent<UnitController>();
                //������
                if (hitunit && FieldStatus.Captured == hitunit.FieldStatus)
                {
                    unit = hitunit;
                }
                //�^�C���I��
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

        //CPU�̔�
        if (isCpu)
        {
            if (0 < enemyWaitTimer)
            {
                enemyWaitTimer -= Time.deltaTime;
                return;
            }

            //��I��
            if (!selectUnit)
            {
                //�S���j�b�g�擾���ă����_���I��
                List<UnitController> allunits = GetUnits(nowPlayer);
                unit = allunits[Random.Range(0, allunits.Count)];

                if (1 > getMovableTiles(unit).Count)
                {
                    unit = null;
                }
            }
            else
            {
                //�����_���ɓ�����
                List<GameObject> tiles = new List<GameObject>(movableTiles.Keys);
                tile = tiles[Random.Range(0, tiles.Count)];
                selectUnit.gameObject.SetActive(true);
            }
        }

        //�ړ���I��
        if (tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            MoveUnit(selectUnit, movableTiles[tile]);
        }
        //���j�b�g�I��
        else if (unit)
        {
            SetSelectCursors(unit, nowPlayer == unit.PlayerNum);
        }
    }

    //�^�[���؂�ւ�
    void TurnChangeMode()
    {
        SetSelectCursors();
        buttonEvolutionApply.SetActive(false);
        buttonEvolutionCancel.SetActive(false);

        isCpu = false;
        //���̃v���C���[�̃^�[��
        nowPlayer = GetNextPlayer(nowPlayer);

        if (0 == nowPlayer)
        {
            turnCount++;
            textTurnCount.text = turnCount + "���";
        }

        nextMode = Mode.Start;
    }

    //���̃v���C���[�̔ԍ���Ԃ�
    public static int GetNextPlayer(int player)
    {
        int next = player + 1;
        if (PlayerMax <= next) next = 0;

        return next;
    }

    //������ɂ���
    void CaptureUnit(Vector2Int tileidx)
    {
        UnitController unit = units[tileidx.x, tileidx.y];
        if (!unit) return;
        unit.Capture(nowPlayer);
        captureUnits.Add(unit);
        units[tileidx.x, tileidx.y] = null;
    }

    //���������ׂ�
    void AlignCaptureUnits(int player)
    {
        foreach (var item in unitTiles[player])
        {
            item.SetActive(false);
        }

        //���j�b�g���Ƃɕ�����
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

            //������̐���\������
            tile.transform.GetChild(0).GetComponent<TextMeshPro>().text
                = "" + item.Value.Count;

            //������ނ̎��������ׂ�
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

    //�z����R�s�[
    public static UnitController[,] GetCopyArray(UnitController[,] ary)
    {
        UnitController[,] ret = new UnitController[9, 9];
        System.Array.Copy(ary, ret, ary.Length);
        return ret;
    }

    //���肵�Ă��郆�j�b�g��Ԃ�
    public static List<UnitController> GetOuteUnits(UnitController[,] units, int player, bool checkotherunit = true)
    {
        List<UnitController> ret = new List<UnitController>();
        foreach (var unit in units)
        {
            if (!unit || player == unit.PlayerNum) continue;

            //���j�b�g�̈ړ��\�Ȕ͈�
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

    //�S���j�b�g�擾
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
