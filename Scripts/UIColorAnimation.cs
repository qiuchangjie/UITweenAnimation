using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UIColorAnimation : IUIAnimationBase
{
    [Serializable]
    public class UIGraphicInfo
    {
        public Color Original;
        public Graphic UIComp;
        public bool AlphaOnly;

        public UIGraphicInfo(Graphic graphic)
        {
            UIComp = graphic;
            Original = UIComp.color;
        }

        public void ResetColor()
        {
            UpdateColor(Original);
        }

        public void UpdateColor(Color targetColor)
        {
            if (null != UIComp)
            {
                if (AlphaOnly)
                {
                    Color color = UIComp.color;
                    color.a = targetColor.a;
                    UIComp.color = color;
                }
                else
                {
                    UIComp.color = targetColor;
                }
            }
        }
    }

    public bool EnableColor;
    public bool Cover;
    public bool AutoReset;
    public Gradient GradientColor;

    public List<UIGraphicInfo> UICompInfos;

    public void InitGraphicComps(GameObject target, bool recursively)
    {
        if (null == target) return;

        if (recursively)
        {
            Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; ++i)
            {
                AddCompInfo(graphics[i]);
            }
        }
        else
        {
            AddCompInfo(target.GetComponent<Graphic>());
        }
    }

    private void AddCompInfo(Graphic graphic)
    {
        if (null == UICompInfos)
        {
            UICompInfos = new List<UIGraphicInfo>();
        }

        if (null != graphic)
        {
            bool isExist = false;
            for (int i = 0; i < UICompInfos.Count; ++i)
            {
                if (UICompInfos[i].UIComp == graphic)
                {
                    isExist = true;
                    break;
                }
            }
            if (!isExist)
            {
                UICompInfos.Add(new UIGraphicInfo(graphic));
            }
        }
    }

    public void Init(GameObject target)
    {
        if (EnableColor)
        {
            //Release();
            InitGraphicComps(target, false);
        }
    }

    public void ResetColor()
    {
        if (null == UICompInfos) return;
        for (int i = 0; i < UICompInfos.Count; ++i)
        {
            UICompInfos[i].ResetColor();
        }
    }

    public void UpdateSelfColor()
    {
        if (null == UICompInfos) return;
        for (int i = 0; i < UICompInfos.Count; ++i)
        {
            if (UICompInfos[i].UIComp)
            {
                UICompInfos[i].Original = UICompInfos[i].UIComp.color;
                UICompInfos[i].UpdateColor(UICompInfos[i].UIComp.color);
            }
        }
    }

    /// <summary>
    /// 修改单个项目的颜色
    /// </summary>
    /// <param name="uIComp"></param>
    public void ChangeUICompColor(Graphic uIComp)
    {
        if (null == UICompInfos) return;
        for (int i = 0; i < UICompInfos.Count; ++i)
        {
            if (uIComp == UICompInfos[i].UIComp)
            {
                UICompInfos[i].UIComp = uIComp;
                UICompInfos[i].Original = uIComp.color;
                break;
            }
        }
    }

    public void Release()
    {
        ResetColor();
        /*
        if (null != UICompInfos)
        {
            UICompInfos.Clear();
            UICompInfos = null;
        }
        */
    }

    private Color CalcTargetColor(Color origin, Color gradient)
    {
        Color target = gradient;
        if (!Cover)
        {
            target.r = origin.r * gradient.r;
            target.g = origin.g * gradient.g;
            target.b = origin.b * gradient.b;
        }

        target.a = origin.a * gradient.a;
        return target;
    }

    public void OnUpdate(float rate)
    {
        if (EnableColor)
        {
            if (null == UICompInfos) return;
            for (int i = 0; i < UICompInfos.Count; ++i)
            {
                UIGraphicInfo info = UICompInfos[i];
                info.UpdateColor(CalcTargetColor(info.Original, GradientColor.Evaluate(rate)));
            }
        }
    }

    public void OnStart()
    {
    }

    public void OnStop()
    {
        if (AutoReset)
        {
            ResetColor();
        }
    }
}
