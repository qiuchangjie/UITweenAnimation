using System;
using UnityEngine;
using System.Reflection;

[Serializable]
public class UITransformAnimation : IUIAnimationBase
{
    public Vector3Curve CurvePosition = new Vector3Curve(false, false, false);
    public Vector3Curve CurveRotation = new Vector3Curve(false, false, false);
    public Vector3Curve CurveScale = new Vector3Curve(false, false, false);

    private RectTransform _target;

    public void Init(GameObject target)
    {
        _target = target.GetComponent<RectTransform>();
    }

    public void SetPositionOffset(Vector2 offset)
    {
        if (_target != null)
        {
            _target.anchoredPosition3D = _target.anchoredPosition3D + new Vector3(offset.x, offset.y, 0);
        }
        CurvePosition.SetOffset(offset);
    }

    public Vector3 GetPosition(float rate)
    {
        Vector3 position = _target.anchoredPosition3D;
        position.x = CurvePosition.EvaluateX(position.x, rate);
        position.y = CurvePosition.EvaluateY(position.y, rate);
        position.z = CurvePosition.EvaluateZ(position.z, rate);
        return position;
    }

    public Vector3 GetRotation(float rate)
    {
        Vector3 localEuler = GetLocalEulerAngles(_target);
        localEuler.x = CurveRotation.EvaluateX(localEuler.x, rate);
        localEuler.y = CurveRotation.EvaluateY(localEuler.y, rate);
        localEuler.z = CurveRotation.EvaluateZ(localEuler.z, rate);

        return localEuler;
    }

    public Vector3 GetScale(float rate)
    {
        Vector3 localScale = _target.localScale;
        localScale.x = CurveScale.EvaluateX(localScale.x, rate);
        localScale.y = CurveScale.EvaluateY(localScale.y, rate);
        localScale.z = CurveScale.EvaluateZ(localScale.z, rate);

        return localScale;
    }

    public void OnUpdate(float curRate)
    {
        if (null != _target)
        {
            _target.anchoredPosition3D = GetPosition(curRate);
            SetLocalEulerAngles(_target, GetRotation(curRate));
            _target.localScale = GetScale(curRate);
        }
    }

    public void Release()
    {
        _target = null;
    }

    private void SetCurveValue(AnimationCurve curve, float time, float value, float precision)
    {
        //if (curve.Evaluate(time) == value)
        if (curve.length > 0 && Mathf.Abs(curve.Evaluate(time) - value) <= precision)
        {
            return;
        }

        bool exist = false;
        int keyIndex = 0;
        Keyframe[] frames = curve.keys;
        Keyframe newFrame = new Keyframe(time, value, 0, 0);
        Keyframe preFrame = newFrame;
        for (int i = 0; i < frames.Length; ++i)
        {
            if (frames[i].time < time)
            {
                preFrame = frames[i];
            }

            else if (frames[i].time == time)
            {
                frames[i].value = value;
                exist = true;
                keyIndex = i;
                break;
            }
        }

        if (exist)
        {
            curve.keys = frames;
        }
        else
        {
            if (frames.Length > 0)
            {
                newFrame.inTangent = preFrame.inTangent;
                newFrame.outTangent = preFrame.outTangent;
                newFrame.inWeight = preFrame.inWeight;
                newFrame.outWeight = preFrame.outWeight;
                newFrame.weightedMode = preFrame.weightedMode;
            }
            keyIndex = curve.AddKey(newFrame);
        }

#if UNITY_EDITOR
        UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, UnityEditor.AnimationUtility.TangentMode.Linear);
        UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, UnityEditor.AnimationUtility.TangentMode.Linear);
        if (keyIndex - 1 >= 0)
        {
            UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve, keyIndex - 1, UnityEditor.AnimationUtility.TangentMode.Linear);
            //curve.SmoothTangents(keyIndex - 1, 1);
        }
        if (keyIndex + 1 < curve.length)
        {
            UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve, keyIndex + 1, UnityEditor.AnimationUtility.TangentMode.Linear);
            //curve.SmoothTangents(keyIndex + 1, 1);
        }

        curve.MoveKey(keyIndex, curve[keyIndex]);
#endif
    }

    private Vector3Curve RecordToCurve(Vector3Curve curve, Vector3 vec, float time, float precision = 0.0001f)
    {
        if (curve.EnableX)
        {
            if (null == curve.X)
            {
                curve.X = new AnimationCurve();
            }
            SetCurveValue(curve.X, time, vec.x, precision);
        }
        if (curve.EnableY)
        {
            if (null == curve.Y)
            {
                curve.Y = new AnimationCurve();
            }
            SetCurveValue(curve.Y, time, vec.y, precision);
        }
        if (curve.EnableZ)
        {
            if (null == curve.Z)
            {
                curve.Z = new AnimationCurve();
            }
            SetCurveValue(curve.Z, time, vec.z, precision);
        }
        return curve;
    }

    private static object[] _getParameters = new object[1];
    public static Vector3 GetLocalEulerAngles(Transform trans)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 为了保证获取Inspector界面上的值，需要通过反射的方法调用内部方法
            Vector3 vect3 = Vector3.zero;
            MethodInfo mth = typeof(Transform).GetMethod("GetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo pi = typeof(Transform).GetProperty("rotationOrder", BindingFlags.Instance | BindingFlags.NonPublic);
            object rotationOrder = null;
            if (pi != null)
            {
                rotationOrder = pi.GetValue(trans, null);
            }
            if (mth != null)
            {
                _getParameters[0] = rotationOrder;
                object retVector3 = mth.Invoke(trans, _getParameters);
                vect3 = (Vector3)retVector3;
            }
            return vect3;
        }
        else
        {
            return trans.localEulerAngles;
        }
#else
        return trans.localEulerAngles;
#endif
    }

    private static object[] _setParameters = new object[2];
    public static void SetLocalEulerAngles(Transform trans, Vector3 angles)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 为了保证赋的角度能够在Inspector界面显示，需要通过反射的方法调用内部方法
            MethodInfo mth = typeof(Transform).GetMethod("SetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo pi = typeof(Transform).GetProperty("rotationOrder", BindingFlags.Instance | BindingFlags.NonPublic);
            object rotationOrder = null;
            if (pi != null)
            {
                rotationOrder = pi.GetValue(trans, null);
            }
            if (mth != null)
            {
                _setParameters[0] = angles;
                _setParameters[1] = rotationOrder;
                mth.Invoke(trans, _setParameters);
            }
        }
        else
        {
            trans.localEulerAngles = angles;
        }
#else
        trans.localEulerAngles = angles;
#endif
    }

    public void Record(RectTransform rectTrans, float rate)
    {
        CurvePosition = RecordToCurve(CurvePosition, rectTrans.anchoredPosition3D, rate);
        CurveRotation = RecordToCurve(CurveRotation, GetLocalEulerAngles(rectTrans), rate);
        CurveScale = RecordToCurve(CurveScale, rectTrans.localScale, rate);
    }

    public void OnStart()
    {
    }

    public void OnStop()
    {
    }
}