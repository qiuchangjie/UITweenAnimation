using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UIArrayAnimation : IUIAnimationBase
{
    public enum ArrayLayoutType
    {
        None = 0,
        Increase, // 递增
        Decrease, // 递减
    }

    public enum ArrayFillType
    {
        RowFirst = 1, // 优先填充行
        ColFirst, // 优先填充列
        Random, // 随机
    }

    public bool AsArray = false;
    public bool IsChild = false;
    public ArrayLayoutType RowLayout = ArrayLayoutType.Increase; //行的排布
    public ArrayLayoutType ColLayout = ArrayLayoutType.Increase; // 列的排布
    public ArrayFillType FillType = ArrayFillType.RowFirst; // 填充方式
    public Vector2 Spacing;
    public Vector2 LayoutSizeDelta;
    public Vector2 TargetSizeDelta;

    public Transform LayoutParent;
    public AnimationCurve Timeline = AnimationCurve.Linear(0, 0, 1, 1);
    public float ArrayDuration;
    public int Count = 1;
    public float Interval = 1;
    public int ColSize = 1;
    public int RowSize = 1;
    public float CurArrayTime;
    public float CurArrayRate;
    public bool UseSpecify;
    public string ArrayName;
    public List<UITweenAnimation> SpecifyChildren;

    private bool _useTimeline = false;

    private UITweenAnimation _originAnim;
    private List<int> _childrenIndex;
    private List<GameObject> _childrenTarget;
    private List<UITweenAnimation> _children;
    private List<Vector2> _childrenPosition;

    public UITweenAnimation OriginAnim
    {
        get { return _originAnim; }
    }

    public List<UITweenAnimation> Children
    {
        get { return _children; }
    }
    public void Init(GameObject target)
    {
        CurArrayTime = 0;
        CurArrayRate = 0;
    }

    private void CreateChildIndex(int count, ArrayFillType fillType)
    {
        _childrenIndex = new List<int>();
        for (int i = 0; i < count; ++i)
        {
            _childrenIndex.Add(i);
        }
        if (fillType == ArrayFillType.Random)
        {
            Reshuffle(_childrenIndex);
        }
    }

    // 打乱顺序
    private void Reshuffle(List<int> list)
    {
        System.Random ram = new System.Random();
        int count = list.Count;
        int lastIndex = count - 1;
        int curIndex;
        int tempValue;
        for (int i = 0; i < count; i++)
        {
            curIndex = ram.Next(0, count - i);
            tempValue = list[curIndex];
            list[curIndex] = list[lastIndex - i];
            list[lastIndex - i] = tempValue;
        }
    }
    private void GetRowCol(int i, ref int row, ref int col)
    {
        i = _childrenIndex[i];
        if (FillType == ArrayFillType.RowFirst
            || FillType == ArrayFillType.Random)
        {
            row = i / ColSize;
            col = (i - row * ColSize);
        }
        else if (FillType == ArrayFillType.ColFirst)
        {
            col = i / RowSize;
            row = (i - col * RowSize);
        }
    }

    private void CaclSizeDelta(bool isSelfTarget)
    {
        Spacing = Vector2.zero;
        LayoutSizeDelta = Vector2.zero;
        int maxCol = ColSize;
        int maxRow = RowSize;

        if (FillType == ArrayFillType.RowFirst
            || FillType == ArrayFillType.Random)
        {
            maxRow = Count / ColSize;
            maxRow = Count % ColSize > 0 ? maxRow + 1 : maxRow;

            maxCol = maxRow > 0 ? ColSize : (Count - maxRow * ColSize);
        }
        else if (FillType == ArrayFillType.ColFirst)
        {
            maxCol = Count / RowSize;
            maxCol = Count % RowSize > 0 ? maxCol + 1 : maxCol;

            maxRow = maxCol > 0 ? RowSize : (Count - maxCol * RowSize);
        }

        //Debug.LogErrorFormat("maxCol={0}, maxRow={1}", maxCol, maxRow);

        if (null != LayoutParent)
        {
            RectTransform rectTrans = LayoutParent.GetComponent<RectTransform>();
            RectTransform targetRectTrans = (isSelfTarget ? _originAnim.GetComponent<RectTransform>() : _originAnim.Target.GetComponent<RectTransform>());
            if (UseSpecify && SpecifyChildren != null && SpecifyChildren.Count > 0)
            {
                targetRectTrans = SpecifyChildren[0].GetComponent<RectTransform>();
            }

            if (null != rectTrans && null != targetRectTrans)
            {
                LayoutSizeDelta = rectTrans.rect.size; // rectTrans.sizeDelta;
                TargetSizeDelta = targetRectTrans.rect.size; // targetRectTrans.sizeDelta;
                if (maxCol - 1 != 0)
                {
                    Spacing.x = (LayoutSizeDelta.x - (TargetSizeDelta.x * maxCol)) / (maxCol - 1);
                }
                if (maxRow - 1 != 0)
                {
                    Spacing.y = (LayoutSizeDelta.y - (TargetSizeDelta.y * maxRow)) / (maxRow - 1);
                }
            }
        }
    }

    private Vector2 CalcChildPosition(int i, Vector2 pivot, Vector2 anchorMin, Vector2 anchorMax)
    {
        Vector2 position = Vector2.zero;
        int row = 0;
        int col = 0;
        GetRowCol(i, ref row, ref col);
        if (RowLayout != ArrayLayoutType.None)
        {
            //position.x = (col + 0.5f) * TargetSizeDelta.x + col * Spacing.x;
            position.x = (col + pivot.x) * TargetSizeDelta.x + col * Spacing.x;

            if (RowLayout == ArrayLayoutType.Decrease)
            {
                position.x = LayoutSizeDelta.x - (col + 1 - pivot.x) * TargetSizeDelta.x - col * Spacing.x;
            }
        }
        if (ColLayout != ArrayLayoutType.None)
        {
            //position.y = (row + 0.5f) * TargetSizeDelta.y + row * Spacing.y;
            position.y = (row + pivot.y) * TargetSizeDelta.y + row * Spacing.y;

            if (ColLayout == ArrayLayoutType.Decrease)
            {
                position.y = LayoutSizeDelta.y - (row + 1 - pivot.y) * TargetSizeDelta.y - row * Spacing.y;
            }
        }

        //Debug.LogErrorFormat("row={0}, col={1}, sizeDelta={2}, position={3}", row, col, LayoutSizeDelta, position);
        //return new Vector2(position.x - LayoutSizeDelta.x * 0.5f, position.y - LayoutSizeDelta.y * 0.5f);// position - (LayoutSizeDelta * 0.5f);
        return new Vector2(position.x - LayoutSizeDelta.x * anchorMin.x, position.y - LayoutSizeDelta.y * anchorMin.y);
    }

    private void SetPositionOffset(int i, UITweenAnimation uiAnim)
    {
        //int row = 0;
        //int col = 0;
        //GetRowCol(i, ref row, ref col);
        //Vector2 offset = new Vector2(col * Spacing.x, row * Spacing.y);

        Vector2 animPosition = uiAnim.TransformAnim.GetPosition(1);
        RectTransform rectTrans = uiAnim.Target.GetComponent<RectTransform>();
        Vector2 position = CalcChildPosition(i, rectTrans.pivot, rectTrans.anchorMin, rectTrans.anchorMax);
        //Debug.LogErrorFormat("i={0}, position={1}, animPosition={2}", i, position, animPosition);
        Vector2 offset = position - animPosition;
        uiAnim.TransformAnim.SetPositionOffset(offset);
    }

    private void RandomRowCol()
    {
        if (FillType == ArrayFillType.Random)
        {// 随机填充情况下，行、列排布也随机
            RowLayout = (ArrayLayoutType)UnityEngine.Random.Range(1, 3);
            ColLayout = (ArrayLayoutType)UnityEngine.Random.Range(1, 3);
        }
    }

    private void CreateArrayChildren()
    {
        bool isSelfTarget = (_originAnim.Target == null || _originAnim.Target == _originAnim.gameObject);
        Transform targetParent = (_originAnim.Target == null ? null : _originAnim.Target.transform.parent);
        LayoutParent = LayoutParent == null ? targetParent : LayoutParent;

        RandomRowCol();

        CreateChildIndex(Count, FillType);

        CaclSizeDelta(isSelfTarget);

        _originAnim.ArrayAnim.IsChild = true;

        // 拷贝动画对象和动画组件
        for (int i = 0; i < Count; ++i)
        {
            GameObject newGo = GameObject.Instantiate(_originAnim.gameObject, _originAnim.transform.parent, true);
            GameObject newTarget = newGo;
            if (!isSelfTarget && _originAnim.Target != null)
            {
                newTarget = GameObject.Instantiate(_originAnim.Target.gameObject, LayoutParent, true);
            }
            else
            {
                newTarget.transform.SetParent(LayoutParent, true);
            }

            _childrenTarget.Add(newTarget);

            UITweenAnimation newUIAnim = newGo.GetComponentInChildren<UITweenAnimation>();
            newUIAnim.ArrayAnim.AsArray = false;
            newUIAnim.Target = _childrenTarget[i];
            newUIAnim.InitTween();

            SetPositionOffset(i, newUIAnim);

            newGo.SetActive(true);

            _children.Add(newUIAnim);

        }

        _originAnim.ArrayAnim.IsChild = false;
    }

    private void CreateSpecifyArrayChildren()
    {
        if (null == SpecifyChildren || SpecifyChildren.Count <= 0) return;

        bool isSelfTarget = (_originAnim.Target == null || _originAnim.Target == _originAnim.gameObject);
        Transform targetParent = (_originAnim.Target == null ? null : _originAnim.Target.transform.parent);
        LayoutParent = LayoutParent == null ? targetParent : LayoutParent;

        RandomRowCol();

        for (int i = SpecifyChildren.Count - 1; i >= 0; --i)
        {
            if (null == SpecifyChildren[i])
            {
                SpecifyChildren.RemoveAt(i);
            }
        }

        Count = SpecifyChildren.Count;

        CreateChildIndex(Count, FillType);

        CaclSizeDelta(isSelfTarget);

        // 拷贝动画对象和动画组件
        for (int i = 0; i < SpecifyChildren.Count; ++i)
        {
            UITweenAnimation childAnim = SpecifyChildren[i];
            childAnim.transform.SetParent(LayoutParent, true);
            childAnim.Target = childAnim.Target == null ? childAnim.gameObject : childAnim.Target;
            _childrenTarget.Add(childAnim.Target);
            childAnim.ArrayAnim.AsArray = false;
            childAnim.InitTween();

            SetPositionOffset(i, childAnim);

            childAnim.gameObject.SetActive(true);

            _children.Add(childAnim);
        }

        _originAnim.ArrayAnim.IsChild = false;
    }

    public void CreateArray(UITweenAnimation tweenAnim)
    {
        if (!AsArray) return;

        //Interval = 1.0f / (float)Count;
        _useTimeline = (null != Timeline && Timeline.length > 0 && Timeline[Timeline.length - 1].time >= 1);

        CurArrayTime = 0;
        CurArrayRate = 0;
        _originAnim = tweenAnim;

        if (null == _childrenTarget)
        {
            _childrenTarget = new List<GameObject>();
        }
        if (null == _children)
        {
            _children = new List<UITweenAnimation>();
        }
        if (null == _childrenPosition)
        {
            _childrenPosition = new List<Vector2>();
        }

        ReleaseArray();

        if (UseSpecify)
        {
            CreateSpecifyArrayChildren();
        }
        else
        {
            CreateArrayChildren();
        }
    }

    private bool IsSpecifyChildren(UITweenAnimation anim)
    {
        return SpecifyChildren != null && anim != null && SpecifyChildren.Contains(anim);
    }

    public void ReleaseArray()
    {
        if (null != _childrenTarget)
        {
            //Debug.LogErrorFormat("_childrenTarget.Count={0}, child.count={1}", _childrenTarget.Count, _children.Count);
            for (int i = 0; i < _childrenTarget.Count; ++i)
            {
                if (null != _children && _children.Count > i)
                {
                    bool isSpecify = IsSpecifyChildren(_children[i]);

                    if (null != _children[i]
                        && null != _childrenTarget[i]
                        && _children[i].gameObject != _childrenTarget[i].gameObject)
                    {
                        UITweenRunner.Remove(_children[i].Tween);

                        if (null != _children[i].gameObject && !isSpecify)
                        {
                            GameObject.DestroyImmediate(_children[i].gameObject);
                        }
                    }

                    if (null != _childrenTarget[i] && null != _childrenTarget[i].gameObject && !isSpecify)
                    {
                        GameObject.DestroyImmediate(_childrenTarget[i].gameObject);
                    }
                }
            }
            _children.Clear();
            _childrenTarget.Clear();
        }
        if (null != _childrenPosition)
        {
            _childrenPosition.Clear();
        }
        if (null != _childrenIndex)
        {
            _childrenIndex.Clear();
        }
        if (!UseSpecify && null != SpecifyChildren)
        {
            SpecifyChildren.Clear();
        }
    }

    private void OnUpdateChild(int index, float rate)
    {
        float activeRate = ((float)index * Interval) / ArrayDuration;
        bool isActive = rate >= activeRate;

        GameObject childTarget = _childrenTarget[index];
        UITweenAnimation child = _children[index];

        if (null == childTarget || null == child) return;

        childTarget.SetActive(isActive);
        child.gameObject.SetActive(isActive);

        if (isActive)
        {
            float startTime = (float)index * Interval;
            float childTime = CurArrayTime - startTime;
            childTime = childTime >= 0 ? childTime : 0;
            child.OnUpdate(child.Tween, childTime);
        }
    }

    public void OnUpdate(float rate)
    {
        if (null != _originAnim.Target && _originAnim.Target.gameObject.activeSelf)
        {
            _originAnim.Target.gameObject.SetActive(false);
        }

        float time = rate;
        if (_useTimeline)
        {
            time = Timeline.Evaluate(rate);
        }

        for (int i = 0; i < _children.Count; ++i)
        {
            OnUpdateChild(i, time);
        }
    }

    public void Release()
    {
        _originAnim = null;

        ReleaseArray();

        _childrenTarget = null;
        _children = null;
    }

    public void OnStart()
    {
        for (int i = 0; i < _children.Count; ++i)
        {
            UITweenAnimation child = _children[i];
            if (null != child)
            {
                child.OnStart(child.Tween);
            }
        }
    }

    public void OnStop()
    {
        for (int i = 0; i < _children.Count; ++i)
        {
            UITweenAnimation child = _children[i];
            if (null != child)
            {
                child.OnStop(child.Tween);
            }
        }
    }
}