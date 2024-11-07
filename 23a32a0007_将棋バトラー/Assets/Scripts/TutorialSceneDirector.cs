using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//�`���[�g���A��

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
    //�����Ղ̕��ƍ���
    int boardWidth, boardHeight;

    [SerializeField, Header("�����Ճ^�C��")]
    GameObject prefabTile;

    [SerializeField]
    List<GameObject> prefabUnits;

    //�{�[�h�z�u ��2�����z��
    //0����A1�����A2���p�A3����ԁA4�����ԁA5���j�n�A6����A7�����A8����
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

    //�t�B�[���h�f�[�^
    Dictionary<Vector2Int, GameObject> tiles;
    UnitController[,] units;

    //�I�����j�b�g
    UnitController selectUnit;
    //�ړ��֘A
    Dictionary<GameObject, Vector2Int> movableTiles;

    [SerializeField, Header("�J�[�\��")]
    GameObject prefabCursor;

    List<GameObject> cursors;

    //�\������_�C�A���O
    [SerializeField, TextArea]
    List<string> dialogs;

    [SerializeField, Header("�G�w")]
    GameObject enemySide;

    //�G�w3�}�X
    const int EnemyLine = 3;
    List<int>[] enemyLines;

    [SerializeField, Header("BGM&SE����")]
    SoundController sound;

    //�`���[�g���A�����t���O
    bool isTeaching = true;

    // Start is called before the first frame update
    void Start()
    {
        sound.PlayBGM();
        //������
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
                //�����ʒu
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

                //��(0)�̓X�L�b�v
                if (0 == type) continue;

                pos.y = 0.6f;

                //���j�b�g����
                GameObject prefab = prefabUnits[type - 1];
                GameObject unit = Instantiate(prefab, pos, Quaternion.Euler(90, player * 180, 0));

                UnitController unitctrl = unit.AddComponent<UnitController>();
                unitctrl.Init(player, type, tile, idx);

                units[i, j] = unitctrl;
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

            for (int j = 0;  j < EnemyLine; j++)
            {
                enemyLines[i].Add(rangemin + j);
            }
        }

        StartCoroutine(Tutorial());
    }

    //�`���[�g���A��
    IEnumerator Tutorial()
    {
        for (int i = 0; i < dialogs.Count; i++)
        {
            //�G�w3�}�X��\��
            if (11 == i)
            {
                enemySide.SetActive(true);
            }
            //�G�w3�}�X���\��
            if (15 == i)
            {
                enemySide.SetActive(false);
            }

            //�_�C�A���O�\��
            textDialogInfo.text = "";
            foreach (char moji in dialogs[i].ToCharArray())
            {
                textDialogInfo.text += moji;
                yield return new WaitForSeconds(typingSpeed / 30);
            }

            yield return new WaitForSeconds(2);
        }
        //�`���[�g���A���I��
        isTeaching = false;

        yield return new WaitForSeconds(14);

        textDialogInfo.text = "�ł́A�������F��I";
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
            //Ray�𓊎˂��đS�Ă̓����蔻����擾
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray))
            {
                //�^�C���I��
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

        //�`���[�g���A�����͏������Ȃ�
        if (isTeaching) return;

        //�ړ���I��
        if (tile && selectUnit && movableTiles.ContainsKey(tile))
        {
            MoveUnit(selectUnit, movableTiles[tile]);
            selectUnit = null;
        }
        //���j�b�g�I��
        if (unit)
        {
            SetSelectCursors(unit);
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

        //�ړ��\�͈͂��擾
        List<Vector2Int> movabletiles = unit.GetMovableTiles(units);
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
        //���ݒn
        Vector2Int oldpos = unit.Pos;

        unit.Move(tiles[tileidx], tileidx);

        units[tileidx.x, tileidx.y] = unit;

        //�{�[�h��ɂ���Ȃ�ړ������X�V
        if (FieldStatus.OnBoard == unit.FieldStatus)
        {
            units[oldpos.x, oldpos.y] = null;

            //����
            if (unit.IsEvolution() && enemyLines[0].Contains(tileidx.y))
            {
                unit.Evolution();
            }
        }

        unit.FieldStatus = FieldStatus.OnBoard;

        sound.PlaySE(0);
    }

    //�^�C�g��
    public void OnClickTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}