using System.Collections.Generic;
using UnityEngine;

//ユニットの種類
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
    //成り
    Tokin,
    Uma,
    Ryuu,
    NariKyou,
    NariKei,
    NariGin
}

//ユニットの場所
public enum FieldStatus { OnBoard, Captured }

//ユニットを制御する
public class UnitController : MonoBehaviour
{
    public int PlayerNum; //プレイヤーの番号
    public UnitType UnitType, OldUnitType; //ユニットの種類
    public FieldStatus FieldStatus; //ユニットの場所

    Dictionary<UnitType, UnitType> evolutionTable = new Dictionary<UnitType, UnitType>
    {
        { UnitType.Hu, UnitType.Tokin }, { UnitType.Kaku, UnitType.Uma },
        { UnitType.Hisha, UnitType.Ryuu }, { UnitType.Kyousha, UnitType.NariKyou },
        { UnitType.Keima, UnitType.NariKei }, { UnitType.Gin, UnitType.NariGin },
        { UnitType.Kin, UnitType.None }, { UnitType.Gyoku, UnitType.None }
    };

    //選択したときに浮かせる位置
    const float SelectUnitY = 1.5f;
    const float UnSelectUnitY = 0.6f;

    //位置情報
    public Vector2Int Pos;

    float oldPosY;

    //初期設定
    public void Init(int player, int unittype, GameObject tile, Vector2Int pos)
    {
        PlayerNum = player;
        UnitType = (UnitType)unittype;
        OldUnitType = (UnitType)unittype;

        transform.eulerAngles = getDefaultAngles(player);
        Move(tile, pos);
    }

    //指定された角度を返す
    Vector3 getDefaultAngles(int player)
    {
        return new Vector3(90, player * 180, 0);
    }

    //移動
    public void Move(GameObject tile, Vector2Int tileidx)
    {
        Vector3 pos = tile.transform.position;
        pos.y = UnSelectUnitY;
        transform.position = pos;

        Pos = tileidx;
    }

    //選択
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

            //持ち駒
            if (FieldStatus.Captured == FieldStatus)
            {
                pos.y = oldPosY;
            }
        }

        transform.position = pos;
    }

    //移動可能な範囲を取得(外部データ)
    public List<Vector2Int> GetMovableTiles(UnitController[,] units, bool checkotherunit = true)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        //持ち駒
        if (FieldStatus.Captured == FieldStatus)
        {
            foreach (var checkpos in getEmptyTiles(units))
            {
                //移動可能
                bool ismovable = true;

                //移動した状態を作る
                Pos = checkpos;
                FieldStatus = FieldStatus.OnBoard;

                if (1 > getMovableTiles(units, UnitType).Count)
                {
                    ismovable = false;
                }

                //歩
                if (UnitType.Hu == UnitType)
                {
                    //二歩
                    for (int i = 0; i < units.GetLength(1); i++)
                    {
                        if (units[checkpos.x, i] && UnitType.Hu == units[checkpos.x, i].UnitType
                            && PlayerNum == units[checkpos.x, i].PlayerNum)
                        {
                            ismovable = false;
                            break;
                        }
                    }

                    //打ち歩詰め
                    int nextplayer = GameSceneDirector.GetNextPlayer(PlayerNum);

                    UnitController[,] copyunits = GameSceneDirector.GetCopyArray(units);
                    copyunits[checkpos.x, checkpos.y] = this;

                    int outecount = GameSceneDirector.GetOuteUnits(units, nextplayer, false).Count;
                    if (0 < outecount && ismovable)
                    {
                        //打ち歩詰めの状態にする
                        ismovable = false;

                        foreach (var unit in units)
                        {
                            if (!unit || nextplayer == unit.PlayerNum) continue;

                            if (!unit.GetMovableTiles(copyunits).Contains(checkpos)) continue;
                            copyunits[checkpos.x, checkpos.y] = unit;
                            outecount = GameSceneDirector.GetOuteUnits(copyunits, nextplayer, false).Count;

                            //移動可能なら打ち歩詰めではない
                            if (1 > outecount)
                            {
                                ismovable = true;
                            }
                        }
                    }
                }

                //移動不可→他をチェック
                if (!ismovable) continue;
                ret.Add(checkpos);
            }

            //状態を元に戻す
            Pos = new Vector2Int(-1, -1);
            FieldStatus = FieldStatus.Captured;
        }
        //玉
        else if (UnitType.Gyoku == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);

            if (!checkotherunit) return ret;

            //削除対象のタイル
            List<Vector2Int> removetiles = new List<Vector2Int>();

            foreach (var item in ret)
            {
                UnitController[,] copyunits = GameSceneDirector.GetCopyArray(units);

                copyunits[Pos.x, Pos.y] = null;
                copyunits[item.x, item.y] = this;

                //王手しているユニット数
                int outecount = GameSceneDirector.GetOuteUnits(copyunits, PlayerNum, false).Count;
                if (0 < outecount) removetiles.Add(item);
            }

            foreach (var item in removetiles)
            {
                ret.Remove(item);
            }
        }
        //金と同じ動きの成り駒
        else if (UnitType.Tokin == UnitType || UnitType.NariKyou == UnitType 
            || UnitType.NariKei == UnitType || UnitType.NariGin == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Kin);
        }
        //馬(玉+角)
        else if (UnitType.Uma == UnitType)
        {
            ret = getMovableTiles(units, UnitType.Gyoku);
            foreach (var item in getMovableTiles(units, UnitType.Kaku))
            {
                if (!ret.Contains(item)) ret.Add(item);
            }
        }
        //龍(玉+飛車)
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

    //移動可能な範囲を取得(内部データ)
    List<Vector2Int> getMovableTiles(UnitController[,] units, UnitType unittype)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        //歩
        if (UnitType.Hu == unittype)
        {
            //向き
            int dir = (0 == PlayerNum) ? 1 : -1;
            //移動マス
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //ボードの外か仲間のユニットがいる場所はスキップ
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //移動マスを追加
                ret.Add(checkpos);
            }
        }
        //桂馬
        if (UnitType.Keima == unittype)
        {
            //向き
            int dir = (0 == PlayerNum) ? 1 : -1;
            //移動マス
            List<Vector2Int> vec = new()
            {
                new Vector2Int(1, 2 * dir),
                new Vector2Int(-1, 2 * dir),
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //ボードの外か仲間のユニットがいる場所はスキップ
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //移動マスを追加
                ret.Add(checkpos);
            }
        }
        //銀
        if (UnitType.Gin == unittype)
        {
            //向き
            int dir = (0 == PlayerNum) ? 1 : -1;
            //移動マス
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
                //ボードの外か仲間のユニットがいる場所はスキップ
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //移動マスを追加
                ret.Add(checkpos);
            }
        }
        //金
        if (UnitType.Kin == unittype)
        {
            //向き
            int dir = (0 == PlayerNum) ? 1 : -1;
            //移動マス
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
                //ボードの外か仲間のユニットがいる場所はスキップ
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //移動マスを追加
                ret.Add(checkpos);
            }
        }
        //玉
        if (UnitType.Gyoku == unittype)
        {
            //向き
            int dir = (0 == PlayerNum) ? 1 : -1;
            //移動マス
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
                //ボードの外か仲間のユニットがいる場所はスキップ
                if (!IsCheckable(units, checkpos) || IsFriendlyUnit(units[checkpos.x, checkpos.y]))
                {
                    continue;
                }
                //移動マスを追加
                ret.Add(checkpos);
            }
        }
        //角
        if (UnitType.Kaku == unittype)
        {
            //移動マス
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
                //ボード全体をチェック
                while (IsCheckable(units, checkpos))
                {
                    //他のユニットがあるかチェック
                    UnitController checkUnit = units[checkpos.x, checkpos.y];
                    if (checkUnit)
                    {
                        if (PlayerNum != checkUnit.PlayerNum)
                            ret.Add(checkpos);
                        break;
                    }
                    //移動マスを追加
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }
        //飛車
        if (UnitType.Hisha == unittype)
        {
            //移動マス
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
                //ボード全体をチェック
                while (IsCheckable(units, checkpos))
                {
                    //他のユニットがあるかチェック
                    UnitController checkUnit = units[checkpos.x, checkpos.y];
                    if (checkUnit)
                    {
                        if (PlayerNum != checkUnit.PlayerNum)
                            ret.Add(checkpos);
                        break;
                    }
                    //移動マスを追加
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }
        //香車
        if (UnitType.Kyousha == unittype)
        {
            //向き
            int dir = (0 == PlayerNum) ? 1 : -1;
            //移動マス
            List<Vector2Int> vec = new()
            {
                new Vector2Int(0, 1 * dir)
            };

            foreach (var item in vec)
            {
                Vector2Int checkpos = Pos + item;
                //ボード全体をチェック
                while (IsCheckable(units, checkpos))
                {
                    //他のユニットがあるかチェック
                    UnitController checkUnit = units[checkpos.x, checkpos.y];
                    if (checkUnit)
                    {
                        if (PlayerNum != checkUnit.PlayerNum)
                            ret.Add(checkpos);
                        break;
                    }
                    //移動マスを追加
                    ret.Add(checkpos);
                    checkpos += item;
                }
            }
        }

        return ret;
    }
    
    //配列オーバーか
    bool IsCheckable(UnitController[,] ary, Vector2Int idx)
    {
        if (idx.x < 0 || ary.GetLength(0) <= idx.x ||
            idx.y < 0 || ary.GetLength(1) <= idx.y)
        {
            return false;
        }

        return true;
    }

    //仲間のユニットか
    bool IsFriendlyUnit(UnitController unit)
    {
        if (unit && PlayerNum == unit.PlayerNum) return true;

        return false;
    }

    //キャプチャーされた
    public void Capture(int player)
    {
        PlayerNum = player;
        FieldStatus = FieldStatus.Captured;
        Evolution(false);
    }

    //成り
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

    //空のタイルを返す
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

    //成れるか
    public bool IsEvolution()
    {
        if (!evolutionTable.ContainsKey(UnitType) || UnitType.None == evolutionTable[UnitType])
        {
            return false;
        }

        return true;
    }
}
