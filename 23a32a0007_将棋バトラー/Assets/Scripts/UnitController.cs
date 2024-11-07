using System.Collections.Generic;
using UnityEngine;

//���j�b�g�̎��
public enum UnitType
{
    None = -1,
    Hu = 1,
    Kaku,
    Hisha,
    Kyousha,
    Keima,
    Gin,
    Kin,
    Gyoku,
    //����
    Tokin,
    Uma,
    Ryuu,
    NariKyou,
    NariKei,
    NariGin
}

//���j�b�g�̏ꏊ
public enum FieldStatus { OnBoard, Captured }

//���j�b�g�𐧌䂷��
public class UnitController : MonoBehaviour
{
    public int PlayerNum; //�v���C���[�̔ԍ�
    public UnitType UnitType, OldUnitType; //���j�b�g�̎��
    public FieldStatus FieldStatus; //���j�b�g�̏ꏊ

    Dictionary<UnitType, UnitType> evolutionTable = new Dictionary<UnitType, UnitType>
    {
        { UnitType.Hu, UnitType.Tokin }, { UnitType.Kaku, UnitType.Uma },
        { UnitType.Hisha, UnitType.Ryuu }, { UnitType.Kyousha, UnitType.NariKyou },
        { UnitType.Keima, UnitType.NariKei }, { UnitType.Gin, UnitType.NariGin },
        { UnitType.Kin, UnitType.None }, { UnitType.Gyoku, UnitType.None }
    };

    //�I�������Ƃ��ɕ�������ʒu
    const float SelectUnitY = 1.5f;
    const float UnSelectUnitY = 0.6f;

    //�ʒu���
    public Vector2Int Pos;

    float oldPosY;

    //�����ݒ�
    public void Init(int player, int unittype, GameObject tile, Vector2Int pos)
    {
        PlayerNum = player;
        UnitType = (UnitType)unittype;
        OldUnitType = (UnitType)unittype;

        transform.eulerAngles = getDefaultAngles(player);
        Move(tile, pos);
    }

    //�w�肳�ꂽ�p�x��Ԃ�
    Vector3 getDefaultAngles(int player)
    {
        return new Vector3(90, player * 180, 0);
    }

    //�ړ�
    public void Move(GameObject tile, Vector2Int tileidx)
    {
        Vector3 pos = tile.transform.position;
        pos.y = UnSelectUnitY;
        transform.position = pos;

        Pos = tileidx;
    }

    //�I��
    public void Select(bool select = true)
    {
        Vector3 pos = transform.position;

        if (select)
        {
            oldPosY = pos.y;
            pos.y = SelectUnitY;
        }
        else
        {
            pos.y = UnSelectUnitY;

            //������
            if (FieldStatus.Captured == FieldStatus)
            {
                pos.y = oldPosY;
            }
        }

        transform.position = pos;
    }

    //�ړ��\�Ȕ͈͂��擾(�O���f�[�^)
    public List<Vector2Int> GetMovableTiles(UnitController[,] units, bool checkotherunit = true)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        //������
        if (FieldStatus.Captured == FieldStatus)
        {
            foreach (var checkpos in getEmptyTiles(units))
            {
                //�ړ��\
                bool ismovable = true;

                //�ړ�������Ԃ����
                Pos = checkpos;
                FieldStatus = FieldStatus.OnBoard;

                if (1 > getMovableTiles(units, UnitType).Count)
                {
                    ismovable = false;
                }

                //��
                if (UnitType.Hu == UnitType)
                {
                    //���
                    for (int i = 0; i < units.GetLength(1); i++)
                    {
                        if (units[checkpos.x, i] && UnitType.Hu == units[checkpos.x, i].UnitType
                            && PlayerNum == units[checkpos.x, i].PlayerNum)
                        {
                            ismovable = false;
                            break;
                        }
                    }

                    //�ł����l��
                    int nextplayer = GameSceneDirector.GetNextPlayer(PlayerNum);

                    UnitController[,] copyunits = GameSceneDirector.GetCopyArray(units);
                    copyunits[checkpos.x, checkpos.y] = this;

                    int outecount = GameSceneDirector.GetOuteUnits(units, nextplayer, false).Count;
                    if (0 < outecount && ismovable)
                    {
                        //�ł����l�߂̏�Ԃɂ���
                        ismovable = false;

                        foreach (var unit in units)
                        {
                            if (!unit || nextplayer == unit.PlayerNum) continue;

                            if (!unit.GetMovableTiles(copyunits).Contains(checkpos)) continue;
                            copyunits[checkpos.x, checkpos.y] = unit;
                            outecount = GameSceneDirector.GetOuteUnits(copyunits, nextplayer, false).Count;

                            //�ړ��\�Ȃ�ł����l�߂ł͂Ȃ�
                            if (1 > outecount)
                            {
                                ismovable = true;
                            }
                        }
                    }
                }

                //�ړ��s�������`�F�b�N
                if (!ismovable) continue;
                ret.Add(checkpos);
            }

            //��Ԃ����ɖ߂�
            Pos = new Vector2Int(-1, -1);
            FieldStatus = FieldStatus.Captured;
        }
        //��
        else if (UnitType.Gyoku == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);

            if (!checkotherunit) return ret;

            //�폜�Ώۂ̃^�C��
            List<Vector2Int> removetiles = new List<Vector2Int>();

            foreach (var item in ret)
            {
                UnitController[,] copyunits = GameSceneDirector.GetCopyArray(units);

                copyunits[Pos.x, Pos.y] = null;
                copyunits[item.x, item.y] = this;

                //���肵�Ă��郆�j�b�g��
                int outecount = GameSceneDirector.GetOuteUnits(copyunits, PlayerNum, false).Count;
                if (0 < outecount) removetiles.Add(item);
            }

            foreach (var item in removetiles)
            {
                ret.Remove(item);
            }
        }
        //���Ɠ��������̐����
        else if (UnitType.Tokin == UnitType || UnitType.NariKyou == UnitType 
            || UnitType.NariKei == UnitType || UnitType.NariGin == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Kin);
        }
        //�n(��+�p)
        else if (UnitType.Uma == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);
            foreach (var item in getMovableTiles(units, UnitType.Kaku))
            {
                if (!ret.Contains(item)) ret.Add(item);
            }
        }
        //��(��+���)
        else if (UnitType.Ryuu == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);
            foreach (var item in getMovableTiles(units, UnitType.Hisha))
            {
                if (!ret.Contains(item)) ret.Add(item);
            }
        }
        else
        {
            ret = getMovableTiles(units, UnitType);
        }

        return ret;
    }

    //�ړ��\�Ȕ͈͂��擾(�����f�[�^)
    List<Vector2Int> getMovableTiles(UnitController[,] units, UnitType unittype)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        //��
        if (UnitType.Hu == unittype)
        {
            //����
            int dir = (0 == PlayerNum) ? 1 : -1;
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�̊O�����Ԃ̃��j�b�g������ꏊ�̓X�L�b�v
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //�ړ��}�X��ǉ�
                ret.Add(checkpos);
            }
        }
        //�j�n
        if (UnitType.Keima == unittype)
        {
            //����
            int dir = (0 == PlayerNum) ? 1 : -1;
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(1, 2 * dir),
                new Vector2Int(-1, 2 * dir),
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�̊O�����Ԃ̃��j�b�g������ꏊ�̓X�L�b�v
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //�ړ��}�X��ǉ�
                ret.Add(checkpos);
            }
        }
        //��
        if (UnitType.Gin == unittype)
        {
            //����
            int dir = (0 == PlayerNum) ? 1 : -1;
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1 * dir),
                new Vector2Int(-1, 1 * dir),
                new Vector2Int(1, 1 * dir),
                new Vector2Int(-1, -1 * dir),
                new Vector2Int(1, -1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�̊O�����Ԃ̃��j�b�g������ꏊ�̓X�L�b�v
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //�ړ��}�X��ǉ�
                ret.Add(checkpos);
            }
        }
        //��
        if (UnitType.Kin == unittype)
        {
            //����
            int dir = (0 == PlayerNum) ? 1 : -1;
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1 * dir),
                new Vector2Int(-1, 1 * dir),
                new Vector2Int(1, 1 * dir),
                new Vector2Int(-1, 0 * dir),
                new Vector2Int(1, 0 * dir),
                new Vector2Int(0, -1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�̊O�����Ԃ̃��j�b�g������ꏊ�̓X�L�b�v
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //�ړ��}�X��ǉ�
                ret.Add(checkpos);
            }
        }
        //��
        if (UnitType.Gyoku == unittype)
        {
            //����
            int dir = (0 == PlayerNum) ? 1 : -1;
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1 * dir),
                new Vector2Int(-1, 1 * dir),
                new Vector2Int(1, 1 * dir),
                new Vector2Int(-1, 0 * dir),
                new Vector2Int(1, 0 * dir),
                new Vector2Int(-1, -1 * dir),
                new Vector2Int(1, -1 * dir),
                new Vector2Int(0, -1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�̊O�����Ԃ̃��j�b�g������ꏊ�̓X�L�b�v
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //�ړ��}�X��ǉ�
                ret.Add(checkpos);
            }
        }
        //�p
        if (UnitType.Kaku == unittype)
        {
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(-1, 1),
                new Vector2Int(1, 1),
                new Vector2Int(-1, -1),
                new Vector2Int(1, -1)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�S�̂��`�F�b�N
                while (IsCheckable(units, checkpos))
                {
                    //���̃��j�b�g�����邩�`�F�b�N
                    UnitController checkUnit = units[checkpos.x, checkpos.y];
                    if (checkUnit)
                    {
                        if (PlayerNum != checkUnit.PlayerNum)
                            ret.Add(checkpos);
                        break;
                    }
                    //�ړ��}�X��ǉ�
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }
        //���
        if (UnitType.Hisha == unittype)
        {
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�S�̂��`�F�b�N
                while (IsCheckable(units, checkpos))
                {
                    //���̃��j�b�g�����邩�`�F�b�N
                    UnitController checkUnit = units[checkpos.x, checkpos.y];
                    if (checkUnit)
                    {
                        if (PlayerNum != checkUnit.PlayerNum)
                            ret.Add(checkpos);
                        break;
                    }
                    //�ړ��}�X��ǉ�
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }
        //����
        if (UnitType.Kyousha == unittype)
        {
            //����
            int dir = (0 == PlayerNum) ? 1 : -1;
            //�ړ��}�X
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //�{�[�h�S�̂��`�F�b�N
                while (IsCheckable(units, checkpos))
                {
                    //���̃��j�b�g�����邩�`�F�b�N
                    UnitController checkUnit = units[checkpos.x, checkpos.y];
                    if (checkUnit)
                    {
                        if (PlayerNum != checkUnit.PlayerNum)
                            ret.Add(checkpos);
                        break;
                    }
                    //�ړ��}�X��ǉ�
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }

        return ret;
    }
    
    //�z��I�[�o�[��
    bool IsCheckable(UnitController[,] ary, Vector2Int idx)
    {
        if (idx.x < 0 || ary.GetLength(0) <= idx.x ||
            idx.y < 0 || ary.GetLength(1) <= idx.y)
        {
            return false;
        }

        return true;
    }

    //���Ԃ̃��j�b�g��
    bool IsFriendlyUnit(UnitController unit)
    {
        if (unit && PlayerNum == unit.PlayerNum) return true;

        return false;
    }

    //�L���v�`���[���ꂽ
    public void Capture(int player)
    {
        PlayerNum = player;
        FieldStatus = FieldStatus.Captured;
        Evolution(false);
    }

    //����
    public void Evolution(bool evolution = true)
    {
        Vector3 angle = transform.eulerAngles;
        
        if (evolution && UnitType.None != evolutionTable[UnitType])
        {
            UnitType = evolutionTable[UnitType];
            angle.x = 270;
            angle.y = (0 == PlayerNum) ? 180 : 0;
            transform.eulerAngles = angle;
        }
        else
        {
            UnitType = OldUnitType;
            transform.eulerAngles = getDefaultAngles(PlayerNum);
        }
    }

    //��̃^�C����Ԃ�
    List<Vector2Int> getEmptyTiles(UnitController[,] units)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        for (int i = 0; i < units.GetLength(0); i++)
        {
            for (int j = 0; j < units.GetLength(1); j++)
            {
                if (units[i, j]) continue;
                ret.Add(new Vector2Int(i, j));
            }
        }

        return ret;
    }

    //����邩
    public bool IsEvolution()
    {
        if (!evolutionTable.ContainsKey(UnitType) || UnitType.None == evolutionTable[UnitType])
        {
            return false;
        }

        return true;
    }
}
