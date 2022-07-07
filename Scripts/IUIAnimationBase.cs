
using System;
using UnityEngine;
using EasingCurve;

public enum UICurveType
{
    AnimCurve = 0,
    EaseCurve = 1,
}

[Serializable]
public struct EaseCurve
{
    public EasingFunctions.Ease EaseType;
    public AnimationCurve Curve;
    public float Start;
    public float End;
}

[Serializable]
public struct Vector3Curve
{
    public bool EnableX;
    public UICurveType CurveTypeX;
    public AnimationCurve X;
    public EaseCurve ECX;

    public bool EnableY;
    public UICurveType CurveTypeY;
    public AnimationCurve Y;
    public EaseCurve ECY;

    public bool EnableZ;
    public UICurveType CurveTypeZ;
    public AnimationCurve Z;
    public EaseCurve ECZ;

    public Vector3Curve(bool enableX, bool enableY, bool enableZ)
    {
        EnableX = enableX;
        X = new AnimationCurve();
        CurveTypeX = UICurveType.AnimCurve;
        ECX.EaseType = EasingFunctions.Ease.EaseInQuad;
        ECX.Curve = EasingAnimationCurve.EaseToAnimationCurve(ECX.EaseType);
        ECX.Start = 0;
        ECX.End = 1;

        EnableY = enableY;
        Y = new AnimationCurve();
        CurveTypeY = UICurveType.AnimCurve;
        ECY.EaseType = EasingFunctions.Ease.EaseInQuad;
        ECY.Curve = EasingAnimationCurve.EaseToAnimationCurve(ECX.EaseType);
        ECY.Start = 0;
        ECY.End = 1;

        EnableZ = enableZ;
        Z = new AnimationCurve();
        CurveTypeZ = UICurveType.AnimCurve;
        ECZ.EaseType = EasingFunctions.Ease.EaseInQuad;
        ECZ.Curve = EasingAnimationCurve.EaseToAnimationCurve(ECX.EaseType);
        ECZ.Start = 0;
        ECZ.End = 1;
    }
    public void SetOffset(Vector2 offset)
    {
        if (null != X)
        {
            Keyframe[] keys = X.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i].value = keys[i].value + offset.x;
            }
            X.keys = keys;

            ECX.Start += offset.x;
            ECX.End += offset.x;
        }
        if (null != Y)
        {
            Keyframe[] keys = Y.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i].value = keys[i].value + offset.y;
            }
            Y.keys = keys;

            ECY.Start += offset.y;
            ECY.End += offset.y;
        }
    }

    /// <summary>
    /// 由于历史原因（兼容旧的动画配置数据），只能分别实现X、Y、Z的取值函数
    /// </summary>
    /// <param name="defaultValue"></param>
    /// <param name="rate"></param>
    /// <returns></returns>
    public float EvaluateX(float defaultValue, float rate)
    {
        float value = defaultValue;
        if (EnableX)
        {
            if (CurveTypeX == UICurveType.AnimCurve)
            {
                value = X.Evaluate(rate);
            }
            else if (CurveTypeX == UICurveType.EaseCurve)
            {
                value = ECX.Start + (ECX.End - ECX.Start) * ECX.Curve.Evaluate(rate);
            }
        }
        return value;
    }

    public float EvaluateY(float defaultValue, float rate)
    {
        float value = defaultValue;
        if (EnableY)
        {
            if (CurveTypeY == UICurveType.AnimCurve)
            {
                value = Y.Evaluate(rate);
            }
            else if (CurveTypeY == UICurveType.EaseCurve)
            {
                value = ECY.Start + (ECY.End - ECY.Start) * ECY.Curve.Evaluate(rate);
            }
        }
        return value;
    }

    public float EvaluateZ(float defaultValue, float rate)
    {
        float value = defaultValue;
        if (EnableZ)
        {
            if (CurveTypeZ == UICurveType.AnimCurve)
            {
                value = Z.Evaluate(rate);
            }
            else if (CurveTypeZ == UICurveType.EaseCurve)
            {
                value = ECZ.Start + (ECZ.End - ECZ.Start) * ECZ.Curve.Evaluate(rate);
            }
        }
        return value;
    }
}

public interface IUIAnimationBase
{
    void Init(GameObject target);
    void OnStart();
    void OnUpdate(float rate);
    void OnStop();
    void Release();
}
