//#define UITWEEN_ANIMATION_DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

using ArrayLayoutType = UIArrayAnimation.ArrayLayoutType;
using ArrayFillType = UIArrayAnimation.ArrayFillType;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;
using UnityEditor.Experimental.SceneManagement;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CanEditMultipleObjects]
[CustomEditor(typeof(UITweenAnimation))]
public class UITweenAnimationEditor : UnityEditor.Editor
{
    private bool _selfAnimFoldout = true;
    private bool _arrayAnimFoldout = true;
    private bool _sequenceFoldout = true;
    private UITweenAnimation _uiTweenAnimation = null;

    private bool _autoRecord = false;
    private bool _animUpdated = false;

    private float _lastCheckTime = 0;
    private float _checkInterval = 0.5f;

    private static List<string> ArrayAnimEffectsName = new List<string>() { "棋盘格横向填充", "棋盘格纵向填充", "棋盘格随机方向填充" };
    private static List<string> ArrayAnimAlignName = new List<string> { "左下对齐", "左上对齐", "右上对齐", "右下对齐" };


    private static string[] CombinationName = new string[] { "无", "动画队列", "动画阵列" };
    private int _combinationIndex = 0;

    private static string[] ArrayChildrenTypeName = new string[] { "克隆自身", "指定列表" };
    private int _childrenTypeIndex = 0;
    private static ShaderPropertyNamesPreset NamesMapping;
    private const string NamesPresetPath = "Assets/Editor/UI/Component/UITweenAnimationEditor/ShaderPropertyNames.asset";

    [MenuItem("Tools/配置/创建UITweenAnimation配置")]
    static void CreateShaderPropertyPreset()
    {
        ShaderPropertyNamesPreset asset = ScriptableObject.CreateInstance<ShaderPropertyNamesPreset>();
        AssetDatabase.CreateAsset(asset, NamesPresetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    static UITweenAnimationEditor()
    {
        // 打开Prefab编辑界面回调
        //PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        // Prefab被保存之前回调
        PrefabStage.prefabSaving += OnPrefabSaving;
        // Prefab被保存之后回调
        PrefabStage.prefabSaved += OnPrefabSaved;
        // 关闭Prefab编辑界面回调
        //PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    static void OnPrefabSaving(GameObject go)
    {
        UITweenAnimation[] anims = go.GetComponentsInChildren<UITweenAnimation>();
        if (null != anims)
        {
            for (int i = 0; i < anims.Length; ++i)
            {
                Debug.Log($"保存prefab前还原颜色");
                anims[i].ColorAnim.ResetColor();
            }
        }
    }

    static void OnPrefabSaved(GameObject go)
    {
    }

    static void OnSceneSaving(Scene scene, string path)
    {
        UITweenAnimation[] anims = GameObject.FindObjectsOfType<UITweenAnimation>();
        if (null != anims)
        {
            for (int i = 0; i < anims.Length; ++i)
            {
                Debug.Log($"保存场景前还原颜色");
                anims[i].ColorAnim.ResetColor();
            }
        }
    }

    private void OnEnable()
    {
        _uiTweenAnimation = (UITweenAnimation)serializedObject.targetObject;
        UITweenEditorRunner.ResetTime();
        //Debug.LogError(Application.dataPath.Replace("/Assets", "/") + NamesPresetPath);
        if (null == NamesMapping
            && File.Exists(Application.dataPath.Replace("/Assets", "/") + NamesPresetPath))
        {
            NamesMapping = AssetDatabase.LoadAssetAtPath<ShaderPropertyNamesPreset>(NamesPresetPath);
        }
    }

    private void OnDisable()
    {
        if (target == null)
        {// 组件被删除target会被置为null
            _uiTweenAnimation.ReleaseTween();
        }
    }

    private void OnDestroy()
    {
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject.targetObjects.Length <= 1)
        {
            bool changed = false;

            _selfAnimFoldout = EditorGUILayout.Foldout(_selfAnimFoldout, "自身动画");
            if (_selfAnimFoldout)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUI.BeginDisabledGroup(_uiTweenAnimation.ArrayAnim.UseSpecify);

                // 缩进一级
                EditorGUI.indentLevel++;
                DrawPropertyByRelative("Duration", "时长");
                DrawPropertyByRelative("Delay", "延迟");
                DrawPropertyByRelative("Loops", "循环");
                DrawPropertyByRelative("AutoPlay", "自动播放");
                DrawPropertyByRelative("Name", "名字");
                DrawPropertyByRelative("IgnoreTimeScale", "忽略TimeScale");

                if (_uiTweenAnimation.IsArrayAnim() && !_uiTweenAnimation.ArrayAnim.UseSpecify)
                {// 阵列动画克隆自身时，强制目标对象为自身
                    _uiTweenAnimation.Target = null;
                }
                EditorGUI.BeginDisabledGroup(_uiTweenAnimation.IsArrayAnim() && !_uiTweenAnimation.ArrayAnim.UseSpecify);
                DrawPropertyByRelative("Target", "目标对象");
                EditorGUI.EndDisabledGroup();

                changed &= EditorGUI.EndChangeCheck();

                changed &= DrawRectTransAnim();
                changed &= DrawColorAnim();
                changed &= DrawParticleAnim();
                changed &= DrawMaterialAnim();
                // 重置缩进
                EditorGUI.indentLevel--;

                EditorGUI.EndDisabledGroup();
            }

            SetCombinationType();

            DrawPropertyByRelative("ArrayAnim.IsChild", "阵列子对象");
            DrawPropertyByRelative("ArrayAnim.ArrayName", "阵列分组");

            if (_uiTweenAnimation.AsSequence)
            {
                _sequenceFoldout = EditorGUILayout.Foldout(_sequenceFoldout, "动画队列");
                if (_sequenceFoldout)
                {
                    // 缩进一级
                    EditorGUI.indentLevel++;
    #if UITWEEN_ANIMATION_DEBUG
                    EditorGUI.BeginDisabledGroup(true);
                    DrawPropertyByRelative("AsSequence", "按队列播放");
                    EditorGUI.EndDisabledGroup();
    #endif
                    DrawPropertyByRelative("SequenceName", "队列分组");
                    DrawPropertyByRelative("SequenceIndex", "队列顺序");
                    DrawPropertyByRelative("ReverseSequence", "倒退播放队列");
                    DrawPropertyByRelative("ReverseDuration", "倒退播放时长");

                    DrawSequenceTimeline();

                    // 重置缩进
                    EditorGUI.indentLevel--;
                }
            }

            if (_uiTweenAnimation.IsArrayAnim())
            {
                _arrayAnimFoldout = EditorGUILayout.Foldout(_arrayAnimFoldout, "动画阵列");
                if (_arrayAnimFoldout)
                {
                    // 缩进一级
                    EditorGUI.indentLevel++;
                    DrawArrayAnim();
                    // 重置缩进
                    EditorGUI.indentLevel--;
                }
            }

            if (changed)
            {
                RefreshSelected();
            }

            serializedObject.ApplyModifiedProperties();
        }
        DrawFunctionButtons();
    }

    void SetCombinationType()
    {
        if (_uiTweenAnimation.IsArrayAnim())
        {
            _combinationIndex = 2;
        }
        else if (_uiTweenAnimation.AsSequence)
        {
            _combinationIndex = 1;
        }
        else
        {
            _combinationIndex = 0;
        }

        EditorGUIUtility.labelWidth = 80;
        EditorGUI.BeginChangeCheck();
        _combinationIndex = EditorGUILayout.Popup("组合动画", _combinationIndex, CombinationName);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
            _uiTweenAnimation.CreateTween();
        }
        EditorGUIUtility.labelWidth = 0;
        switch (_combinationIndex)
        {
            case 0:
                _uiTweenAnimation.AsSequence = false;
                _uiTweenAnimation.ArrayAnim.AsArray = false;
                _uiTweenAnimation.ArrayAnim.UseSpecify = false;
                break;
            case 1:
                _uiTweenAnimation.AsSequence = true;
                _uiTweenAnimation.ArrayAnim.AsArray = false;
                _uiTweenAnimation.ArrayAnim.UseSpecify = false;
                break;
            case 2:
                _uiTweenAnimation.AsSequence = false;
                _uiTweenAnimation.ArrayAnim.AsArray = true;
                break;
            default:
                _uiTweenAnimation.AsSequence = false;
                _uiTweenAnimation.ArrayAnim.AsArray = false;
                _uiTweenAnimation.ArrayAnim.UseSpecify = false;
                break;
        }
    }

    void PlaySelected()
    {
        foreach (UITweenAnimation anim in targets.Select(obj => obj as UITweenAnimation))
        {
            anim.CreateTween();
            UITweenRunner.Play(anim.Tween);
        }
    }

    void StopSelected()
    {
        foreach (UITweenAnimation anim in targets.Select(obj => obj as UITweenAnimation))
        {
            UITweenRunner.Stop(anim.Tween);
        }
    }

    void RefreshSelected()
    {
        foreach (UITweenAnimation anim in targets.Select(obj => obj as UITweenAnimation))
        {
            anim.InitTween();
        }
    }

    void ReleaseArraySelected()
    {
        foreach (UITweenAnimation anim in targets.Select(obj => obj as UITweenAnimation))
        {
            if (anim.IsArrayAnim())
            {
                anim.ArrayAnim.ReleaseArray();
            }
        }
    }


    void DrawFunctionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("► 播放"))
        {
            _autoRecord = false;
            PlaySelected();
        }
        if (GUILayout.Button("■ 停止"))
        {
            StopSelected();
        }

        if (GUILayout.Button("☣ 刷新"))
        {
            RefreshSelected();
        }

        EditorGUI.BeginDisabledGroup(!_uiTweenAnimation.IsArrayAnim() || targets.Length > 1);
        if (GUILayout.Button("☒ 清空阵列"))
        {
            ReleaseArraySelected();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }

    void DrawPropertyByRelative(string relative, string showName)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(relative), new GUIContent(showName));
    }

    void DrawVector3CurveProperty(SerializedProperty spVector3Curve
        , string showName
        , string enableName
        , string curveName
        , string typeName
        , string easeName)
    {
        SerializedProperty spEnable = spVector3Curve.FindPropertyRelative(enableName);
        SerializedProperty spCurve = spVector3Curve.FindPropertyRelative(curveName);
        SerializedProperty spCurveType = spVector3Curve.FindPropertyRelative(typeName);

        EditorGUILayout.BeginHorizontal("HelpBox");

        spEnable.boolValue = GUILayout.Toggle(spEnable.boolValue, showName);

        EditorGUI.BeginDisabledGroup(!spEnable.boolValue);

        EditorGUIUtility.labelWidth = 40;
        EditorGUILayout.PropertyField(spCurveType, new GUIContent("类型"));
        if (spCurveType.intValue == (int)UICurveType.AnimCurve)
        {
            spCurve.animationCurveValue = EditorGUILayout.CurveField(spCurve.animationCurveValue);
        }
        else if (spCurveType.intValue == (int)UICurveType.EaseCurve)
        {
            SerializedProperty spEaseCurve = spVector3Curve.FindPropertyRelative(easeName);
            SerializedProperty spECType = spEaseCurve.FindPropertyRelative("EaseType");
            SerializedProperty spECCurve = spEaseCurve.FindPropertyRelative("Curve");
            SerializedProperty spECStart = spEaseCurve.FindPropertyRelative("Start");
            SerializedProperty spECEnd = spEaseCurve.FindPropertyRelative("End");

            int lastType = spECType.intValue;
            EditorGUILayout.PropertyField(spECType, new GUIContent("走势"));
            if (lastType != spECType.intValue)
            {
                spECCurve.animationCurveValue = EasingCurve.EasingAnimationCurve.EaseToAnimationCurve((EasingCurve.EasingFunctions.Ease)spECType.intValue);
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(spECCurve, new GUIContent(""), GUILayout.MaxWidth(60));
            EditorGUI.EndDisabledGroup();
            spECStart.floatValue = EditorGUILayout.FloatField(spECStart.floatValue, GUILayout.MaxWidth(60));
            spECEnd.floatValue = EditorGUILayout.FloatField(spECEnd.floatValue, GUILayout.MaxWidth(60));
        }
        EditorGUIUtility.labelWidth = 0;

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }

    void DrawVector3Curve(string relative, string showName)
    {
        SerializedProperty spVector3Curve = serializedObject.FindProperty(relative);

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label(showName);

        DrawVector3CurveProperty(spVector3Curve, "X", "EnableX", "X", "CurveTypeX", "ECX");
        DrawVector3CurveProperty(spVector3Curve, "Y", "EnableY", "Y", "CurveTypeY", "ECY");
        DrawVector3CurveProperty(spVector3Curve, "Z", "EnableZ", "Z", "CurveTypeZ", "ECZ");

        EditorGUILayout.EndHorizontal();
    }

    private Vector3 GetVertor3Curve(Vector3 origin, string relative, float time)
    {
        SerializedProperty spVector3Curve = serializedObject.FindProperty(relative);
        SerializedProperty spEnableX = spVector3Curve.FindPropertyRelative("EnableX");
        SerializedProperty spEnableY = spVector3Curve.FindPropertyRelative("EnableY");
        SerializedProperty spEnableZ = spVector3Curve.FindPropertyRelative("EnableZ");
        SerializedProperty spCurveX = spVector3Curve.FindPropertyRelative("X");
        SerializedProperty spCurveY = spVector3Curve.FindPropertyRelative("Y");
        SerializedProperty spCurveZ = spVector3Curve.FindPropertyRelative("Z");

        float x = (spEnableX.boolValue && spCurveX.animationCurveValue.length > 0) ? spCurveX.animationCurveValue.Evaluate(time) : origin.x;
        float y = (spEnableY.boolValue && spCurveY.animationCurveValue.length > 0) ? spCurveY.animationCurveValue.Evaluate(time) : origin.y;
        float z = (spEnableZ.boolValue && spCurveZ.animationCurveValue.length > 0) ? spCurveZ.animationCurveValue.Evaluate(time) : origin.z;
        return new Vector3(x, y, z);
    }

    private void UpdateTransformForce(float curTime)
    {
        SerializedProperty spCurTime = serializedObject.FindProperty("CurTime");
        SerializedProperty spDelay = serializedObject.FindProperty("Delay");
        SerializedProperty spCurDelay = serializedObject.FindProperty("CurDelay");
        spCurTime.floatValue = curTime;
        spCurDelay.floatValue = spDelay.floatValue;

        GameObject target = _uiTweenAnimation.Target == null ? _uiTweenAnimation.gameObject : _uiTweenAnimation.Target;
        RectTransform rectTrans = target.GetComponent<RectTransform>();

        Vector3 pos = GetVertor3Curve(rectTrans.anchoredPosition3D, "TransformAnim.CurvePosition", curTime);
        Vector3 rot = GetVertor3Curve(UITransformAnimation.GetLocalEulerAngles(rectTrans), "TransformAnim.CurveRotation", curTime);
        Vector3 scale = GetVertor3Curve(rectTrans.localScale, "TransformAnim.CurveScale", curTime);

        rectTrans.anchoredPosition3D = pos;
        UITransformAnimation.SetLocalEulerAngles(rectTrans, rot);
        rectTrans.localScale = scale;

        _animUpdated = true;

        serializedObject.ApplyModifiedProperties();

        _uiTweenAnimation.OnUpdate(_uiTweenAnimation.Tween, curTime + spDelay.floatValue);
    }

    private void DrawSelfAnimTimeline()
    {
        SerializedProperty spCurTime = serializedObject.FindProperty("CurTime");
        SerializedProperty spDuration = serializedObject.FindProperty("Duration");

        EditorGUI.BeginDisabledGroup(Application.isPlaying || _uiTweenAnimation.IsArrayAnim());
        EditorGUIUtility.labelWidth = 40;
        float newCurTime = EditorGUILayout.Slider("时间", spCurTime.floatValue, 0, spDuration.floatValue);
        EditorGUIUtility.labelWidth = 0;
        EditorGUI.EndDisabledGroup();

        if (newCurTime != spCurTime.floatValue)
        {
            if (!Application.isPlaying 
                && !_uiTweenAnimation.IsArrayAnim()
                && (null == _uiTweenAnimation.Tween || !_uiTweenAnimation.Tween.IsRun()))
            {
                UpdateTransformForce(newCurTime);
            }
        }
    }

    private void DrawSequenceTimeline()
    {
        UITweenSequence seq = UITweenRunner.GetSequence(_uiTweenAnimation.SequenceName);
        if (null != seq)
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying || !_uiTweenAnimation.AsSequence);
            EditorGUIUtility.labelWidth = 80;
            float newCurTime = 0;
            if (_uiTweenAnimation.ReverseSequence)
            {
                newCurTime = EditorGUILayout.Slider("倒退队列时间", seq.ReverseTime, 0, seq.ReverseDuration);
                if (!seq.IsRun() && seq.ReverseTime != newCurTime)
                {
                    seq.DoUpdateChildren(newCurTime);
                }
            }
            else
            {
                newCurTime = EditorGUILayout.Slider("队列时间", seq.Time, 0, seq.Duration);
                if (!seq.IsRun() && seq.Time != newCurTime)
                {
                    seq.DoUpdateChildren(newCurTime);
                }
            }
            EditorGUIUtility.labelWidth = 0;
            EditorGUI.EndDisabledGroup();
        }
    }


    private RectTransform GetAnimationTarget()
    {
        GameObject animTarget = _uiTweenAnimation.Target != null ? _uiTweenAnimation.Target : _uiTweenAnimation.gameObject;
        return animTarget.GetComponent<RectTransform>();
    }

    private static bool Approximately(float a, float b, float precision = 0.0001f)
    {
        return (Mathf.Abs(a - b) <= precision);
    }

    private void RecordToAnimation()
    {
        RectTransform trans = GetAnimationTarget();

        Vector3 pos = trans.anchoredPosition3D;
        float posX = _uiTweenAnimation.TransformAnim.CurvePosition.EnableX ? _uiTweenAnimation.TransformAnim.CurvePosition.X.Evaluate(_uiTweenAnimation.CurTime) : pos.x;
        posX = _uiTweenAnimation.TransformAnim.CurvePosition.X.length <= 0 ? float.MinValue : posX; // 如果曲线本身没有keyframe，强制写入（因为没有AnimationCurve长度为0的情况返回是0）
        float posY = _uiTweenAnimation.TransformAnim.CurvePosition.EnableY ? _uiTweenAnimation.TransformAnim.CurvePosition.Y.Evaluate(_uiTweenAnimation.CurTime) : pos.y;
        posY = _uiTweenAnimation.TransformAnim.CurvePosition.Y.length <= 0 ? float.MinValue : posY;
        float posZ = _uiTweenAnimation.TransformAnim.CurvePosition.EnableZ ? _uiTweenAnimation.TransformAnim.CurvePosition.Z.Evaluate(_uiTweenAnimation.CurTime) : pos.z;
        posZ = _uiTweenAnimation.TransformAnim.CurvePosition.Z.length <= 0 ? float.MinValue : posZ;
        //Vector3 curvePos = new Vector3(posX, posY, posZ);

        Vector3 euler = UITransformAnimation.GetLocalEulerAngles(trans);
        float rotX = _uiTweenAnimation.TransformAnim.CurveRotation.EnableX ? _uiTweenAnimation.TransformAnim.CurveRotation.X.Evaluate(_uiTweenAnimation.CurTime) : euler.x;
        rotX = _uiTweenAnimation.TransformAnim.CurveRotation.X.length <= 0 ? float.MinValue : rotX;
        float rotY = _uiTweenAnimation.TransformAnim.CurveRotation.EnableY ? _uiTweenAnimation.TransformAnim.CurveRotation.Y.Evaluate(_uiTweenAnimation.CurTime) : euler.y;
        rotY = _uiTweenAnimation.TransformAnim.CurveRotation.Y.length <= 0 ? float.MinValue : rotY;
        float rotZ = _uiTweenAnimation.TransformAnim.CurveRotation.EnableZ ? _uiTweenAnimation.TransformAnim.CurveRotation.Z.Evaluate(_uiTweenAnimation.CurTime) : euler.z;
        rotZ = _uiTweenAnimation.TransformAnim.CurveRotation.Z.length <= 0 ? float.MinValue : rotZ;
        //Vector3 curveEuler = new Vector3(rotX, rotY, rotZ);

        Vector3 scale = trans.localScale;
        float scaleX = _uiTweenAnimation.TransformAnim.CurveScale.EnableX ? _uiTweenAnimation.TransformAnim.CurveScale.X.Evaluate(_uiTweenAnimation.CurTime) : scale.x;
        scaleX = _uiTweenAnimation.TransformAnim.CurveScale.X.length <= 0 ? float.MinValue : scaleX;
        float scaleY = _uiTweenAnimation.TransformAnim.CurveScale.EnableY ? _uiTweenAnimation.TransformAnim.CurveScale.Y.Evaluate(_uiTweenAnimation.CurTime) : scale.y;
        scaleY = _uiTweenAnimation.TransformAnim.CurveScale.Y.length <= 0 ? float.MinValue : scaleY;
        float scaleZ = _uiTweenAnimation.TransformAnim.CurveScale.EnableZ ? _uiTweenAnimation.TransformAnim.CurveScale.Z.Evaluate(_uiTweenAnimation.CurTime) : scale.z;
        scaleZ = _uiTweenAnimation.TransformAnim.CurveScale.Z.length <= 0 ? float.MinValue : scaleZ;
        //Vector3 curveScale = new Vector3(scaleX, scaleY, scaleZ);

        const float precision = 0.001f;
        const float eulerPre = 0.01f;
        if (!Approximately(trans.anchoredPosition3D.x, posX, precision)
            || !Approximately(trans.anchoredPosition3D.y, posY, precision)
            || !Approximately(trans.anchoredPosition3D.z, posZ, precision)
            || !Approximately(euler.x, rotX, eulerPre)
            || !Approximately(euler.y, rotY, eulerPre)
            || !Approximately(euler.z, rotZ, eulerPre)
            || !Approximately(trans.localScale.x, scaleX, precision)
            || !Approximately(trans.localScale.y, scaleY, precision)
            || !Approximately(trans.localScale.z, scaleZ, precision))
        {
            float rate = _uiTweenAnimation.Duration > 0 ? _uiTweenAnimation.CurTime / _uiTweenAnimation.Duration : 0;
            _uiTweenAnimation.TransformAnim.Record(trans.GetComponent<RectTransform>(), rate);
        }
    }

    private void CheckModify()
    {
        if (Event.current.type != EventType.MouseDown)
        {
            if (Time.realtimeSinceStartup - _lastCheckTime > _checkInterval)
            {
                _lastCheckTime = Time.realtimeSinceStartup;

                RecordToAnimation();
            }
        }
    }

    private bool DrawRectTransAnim()
    {
        _animUpdated = false;
        bool changedCurve = false;
        EditorGUI.BeginChangeCheck();
        DrawVector3Curve("TransformAnim.CurvePosition", "位移");
        DrawVector3Curve("TransformAnim.CurveRotation", "旋转");
        DrawVector3Curve("TransformAnim.CurveScale", "缩放");
        changedCurve = EditorGUI.EndChangeCheck();

        if (changedCurve)
        {
            _lastCheckTime = Time.realtimeSinceStartup;
            
            UpdateTransformForce(_uiTweenAnimation.CurTime);
        }

        EditorGUI.BeginDisabledGroup(Application.isPlaying || _uiTweenAnimation.IsArrayAnim());
        bool newAuto = GUILayout.Toggle(_autoRecord, "自动记录");
        if (newAuto && _autoRecord != newAuto)
        {
            UpdateTransformForce(_uiTweenAnimation.CurTime);
        }
        _autoRecord = newAuto;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.BeginHorizontal();

        DrawSelfAnimTimeline();

        if (GUILayout.Button("同步到曲线", GUILayout.MaxWidth(80)))
        {
            RecordToAnimation();
        }
        EditorGUILayout.EndHorizontal();

        if (!changedCurve)
        {
            if (_autoRecord && !_animUpdated)
            {
                CheckModify();
            }
        }
        return changedCurve;
    }

    private bool DrawColorAnim()
    {
        EditorGUI.BeginChangeCheck();
        SerializedProperty spEnableColor = serializedObject.FindProperty("ColorAnim.EnableColor");
        SerializedProperty spCover = serializedObject.FindProperty("ColorAnim.Cover");
        SerializedProperty spAutoReset = serializedObject.FindProperty("ColorAnim.AutoReset");
        SerializedProperty spGradientColor = serializedObject.FindProperty("ColorAnim.GradientColor");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("颜色");
        bool lastEnableColor = spEnableColor.boolValue;
        spEnableColor.boolValue = GUILayout.Toggle(spEnableColor.boolValue, "改变颜色");
        EditorGUI.BeginDisabledGroup(!spEnableColor.boolValue);
        spCover.boolValue = GUILayout.Toggle(spCover.boolValue, "覆盖颜色");
        spAutoReset.boolValue = GUILayout.Toggle(spAutoReset.boolValue, "自动重置");
        if (GUILayout.Button("添加子对象"))
        {
            _uiTweenAnimation.ColorAnim.InitGraphicComps(null != _uiTweenAnimation.Target ? _uiTweenAnimation.Target : _uiTweenAnimation.gameObject, true);
            EditorUtility.SetDirty(target);
        }
        if (GUILayout.Button("A通道全选/全取消"))
        {
            if (null != _uiTweenAnimation.ColorAnim.UICompInfos)
            {
                bool value = false;
                for (int i = 0; i < _uiTweenAnimation.ColorAnim.UICompInfos.Count; ++i)
                {
                    if (i == 0)
                    {
                        value = !_uiTweenAnimation.ColorAnim.UICompInfos[i].AlphaOnly;
                    }
                    _uiTweenAnimation.ColorAnim.UICompInfos[i].AlphaOnly = value;
                }
                EditorUtility.SetDirty(target);
            }
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 80;
        EditorGUILayout.PropertyField(spGradientColor, new GUIContent("颜色变化"));
        EditorGUIUtility.labelWidth = 0;
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        SerializedProperty spUICompInfos = serializedObject.FindProperty("ColorAnim.UICompInfos");
        if (EditorGUILayout.PropertyField(spUICompInfos, new GUIContent("UI对象"), false))
        {
            spUICompInfos.arraySize = EditorGUILayout.DelayedIntField("Size", spUICompInfos.arraySize);

            EditorGUI.indentLevel++;
            for (int i = 0, size = spUICompInfos.arraySize; i < size; ++i)
            {
                SerializedProperty spElement = spUICompInfos.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 90;
                EditorGUILayout.PropertyField(spElement.FindPropertyRelative("Original"), new GUIContent("自身颜色"));
                EditorGUIUtility.labelWidth = 70;
                EditorGUILayout.PropertyField(spElement.FindPropertyRelative("UIComp"), new GUIContent("UI组件"));
                EditorGUIUtility.labelWidth = 100;
                EditorGUILayout.PropertyField(spElement.FindPropertyRelative("AlphaOnly"), new GUIContent("只影响A通道"));
                EditorGUIUtility.labelWidth = 0;
                if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
                {
                    spUICompInfos.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        bool changed = EditorGUI.EndChangeCheck();
        if (changed)
        {
            serializedObject.ApplyModifiedProperties();

            if (lastEnableColor != spEnableColor.boolValue)
            {
                if (spEnableColor.boolValue)
                {
                    _uiTweenAnimation.ColorAnim.Init(null != _uiTweenAnimation.Target ? _uiTweenAnimation.Target : _uiTweenAnimation.gameObject);
                }
                else
                {
                    _uiTweenAnimation.ColorAnim.ResetColor();
                }
            }
        }

        return changed;
    }

    private bool DrawParticleAnim()
    {
        EditorGUI.BeginChangeCheck();

        SerializedProperty spEnableParticle = serializedObject.FindProperty("ParticleAnim.EnableParticle");
        //SerializedProperty spPlayTime = serializedObject.FindProperty("ParticleAnim.PlayTime");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("粒子");
        spEnableParticle.boolValue = GUILayout.Toggle(spEnableParticle.boolValue, "播放粒子");
        EditorGUI.BeginDisabledGroup(!spEnableParticle.boolValue);
        //DrawPropertyByRelative("ParticleAnim.PlayTime", "播放时机");
        //GUILayout.Space(30);
        //GUILayout.Label("播放时机");
        //spPlayTime.floatValue = EditorGUILayout.FloatField(spPlayTime.floatValue);

        if (GUILayout.Button("添加子对象"))
        {
            SerializedProperty spTarget = serializedObject.FindProperty("Target");
            GameObject animTarget = spTarget.objectReferenceValue as GameObject;
            if (animTarget == null)
            {
                animTarget = ((UITweenAnimation)serializedObject.targetObject).gameObject;
            }
            ParticleSystem[] particles = animTarget.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particles.Length; ++i)
            {
                UIParticleAnimation.UIParticleCfg cfg = new UIParticleAnimation.UIParticleCfg();
                cfg.Particle = particles[i];
                ((UITweenAnimation)serializedObject.targetObject).ParticleAnim.ParticlesCfg.Add(cfg);
            }
            EditorUtility.SetDirty(target);
        }
        EditorGUILayout.EndHorizontal();

        SerializedProperty spParticlesCfg = serializedObject.FindProperty("ParticleAnim.ParticlesCfg");
        if (EditorGUILayout.PropertyField(spParticlesCfg, new GUIContent("粒子"), false))
        {
            spParticlesCfg.arraySize = EditorGUILayout.DelayedIntField("Size", spParticlesCfg.arraySize);

            EditorGUI.indentLevel++;
            for (int i = 0, size = spParticlesCfg.arraySize; i < size; ++i)
            {
                //EditorGUI.indentLevel++;
                SerializedProperty spElement = spParticlesCfg.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 60;
                EditorGUILayout.PropertyField(spElement.FindPropertyRelative("Particle"), new GUIContent("粒子"));
                EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField(spElement.FindPropertyRelative("PlayTime"), new GUIContent("播放时机"));
                EditorGUIUtility.labelWidth = 0;
                if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
                {
                    spParticlesCfg.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                //EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndDisabledGroup();

        return EditorGUI.EndChangeCheck();
    }

    private List<List<string>> _nameFilters = new List<List<string>>();
    private int _propertyIndex = 0;
    private int _paramIndex = 0;

    private void CheckNameFiltersByPropertyCount(int size)
    {
        if (_nameFilters.Count > size)
        {
            for (int i = _nameFilters.Count - 1; i > size; --i)
            {
                _nameFilters.RemoveAt(i);
            }
        }
        if (_nameFilters.Count < size)
        {
            for (int i = _nameFilters.Count; i < size; ++i)
            {
                _nameFilters.Add(new List<string>());
            }
        }
    }

    private void CheckNameFiltersByParamCount(int size)
    {
        List<string> filters = _nameFilters[_propertyIndex];
        if (filters.Count > size)
        {
            for (int i = filters.Count - 1; i > size; --i)
            {
                filters.RemoveAt(i);
            }
        }
        if (filters.Count < size)
        {
            for (int i = filters.Count; i < size; ++i)
            {
                filters.Add("");
            }
        }
    }

    private bool DrawMaterialAnim()
    {
        EditorGUI.BeginChangeCheck();

        SerializedProperty spMatProps = serializedObject.FindProperty("MaterialAnim.MaterialProperties");
        if (EditorGUILayout.PropertyField(spMatProps, new GUIContent("UI材质动画"), false))
        {
            EditorGUI.indentLevel++;
            spMatProps.arraySize = EditorGUILayout.DelayedIntField("Size", spMatProps.arraySize);

            CheckNameFiltersByPropertyCount(spMatProps.arraySize);

            EditorGUI.indentLevel++;
            for (int i = 0, size = spMatProps.arraySize; i < size; ++i)
            {
                _propertyIndex = i;
                bool isRemove = !DrawMaterialArrayProperty(spMatProps, i);
                if (isRemove)
                {
                    break;
                }
                EditorGUILayout.Separator();
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        SerializedProperty spRendererMatProps = serializedObject.FindProperty("MaterialAnim.RendererMaterialProperties");
        if (EditorGUILayout.PropertyField(spRendererMatProps, new GUIContent("Mesh材质动画"), false))
        {
            EditorGUI.indentLevel++;
            spRendererMatProps.arraySize = EditorGUILayout.DelayedIntField("Size", spRendererMatProps.arraySize);

            CheckNameFiltersByPropertyCount(spRendererMatProps.arraySize);

            EditorGUI.indentLevel++;
            for (int i = 0, size = spRendererMatProps.arraySize; i < size; ++i)
            {
                _propertyIndex = i;
                bool isRemove = !DrawRendererMaterialArrayProperty(spRendererMatProps, i);
                if (isRemove)
                {
                    break;
                }
                EditorGUILayout.Separator();
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        bool changed = EditorGUI.EndChangeCheck();
        if (changed)
        {
            _uiTweenAnimation.MaterialAnim.ResetMaterial();
        }
        return changed;
    }

    private bool DrawMaterialArrayProperty(SerializedProperty spPropArray, int i)
    {
        SerializedProperty spProp = spPropArray.GetArrayElementAtIndex(i);
        SerializedProperty spUIComp = spProp.FindPropertyRelative("UIComp");
        SerializedProperty spMat = spProp.FindPropertyRelative("Mat");
        SerializedProperty spParams = spProp.FindPropertyRelative("Params");

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 90;
        Graphic lastGraph = spUIComp.objectReferenceValue as Graphic;
        EditorGUILayout.PropertyField(spUIComp, new GUIContent("UI组件"));
        Graphic uiGraph = spUIComp.objectReferenceValue as Graphic;
        if (lastGraph != uiGraph)
        {
            if (null != lastGraph)
            {
                lastGraph.material = spMat.objectReferenceValue as Material;
            }
            spMat.objectReferenceValue = (null != uiGraph ? uiGraph.material : null);
            if (null != uiGraph)
            {
                uiGraph.material = GameObject.Instantiate(spMat.objectReferenceValue as Material);
            }
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(spMat, new GUIContent("材质"));
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
        {
            spPropArray.DeleteArrayElementAtIndex(i);
            return false;
        }
        EditorGUIUtility.labelWidth = 0;
        EditorGUILayout.EndHorizontal();

        if (EditorGUILayout.PropertyField(spParams, new GUIContent("参数"), false))
        {
            EditorGUI.indentLevel++;
            spParams.arraySize = EditorGUILayout.DelayedIntField("Size", spParams.arraySize);

            CheckNameFiltersByParamCount(spParams.arraySize);
            for (int j = 0, paramsSize = spParams.arraySize; j < paramsSize; ++j)
            {
                _paramIndex = j;
                DrawMaterialParam(spMat, spParams.GetArrayElementAtIndex(j));
                EditorGUILayout.Separator();
            }

            EditorGUI.indentLevel--;
        }
        return true;
    }

    private bool DrawRendererMaterialArrayProperty(SerializedProperty spPropArray, int i)
    {
        SerializedProperty spProp = spPropArray.GetArrayElementAtIndex(i);
        SerializedProperty spRendererComp = spProp.FindPropertyRelative("RendererComp");
        SerializedProperty spMat = spProp.FindPropertyRelative("Mat");
        SerializedProperty spParams = spProp.FindPropertyRelative("Params");

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 90;
        Renderer lastRenderer = spRendererComp.objectReferenceValue as Renderer;
        EditorGUILayout.PropertyField(spRendererComp, new GUIContent("Renderer组件"));
        Renderer renderer = spRendererComp.objectReferenceValue as Renderer;
        if (lastRenderer != renderer)
        {
            if (null != lastRenderer)
            {
                lastRenderer.material = spMat.objectReferenceValue as Material;
            }
            spMat.objectReferenceValue = (null != renderer ? renderer.sharedMaterial : null);
            if (null != renderer)
            {
                renderer.material = GameObject.Instantiate(spMat.objectReferenceValue as Material);
            }
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(spMat, new GUIContent("材质"));
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
        {
            spPropArray.DeleteArrayElementAtIndex(i);
            return false;
        }
        EditorGUIUtility.labelWidth = 0;
        EditorGUILayout.EndHorizontal();

        if (EditorGUILayout.PropertyField(spParams, new GUIContent("参数"), false))
        {
            EditorGUI.indentLevel++;
            spParams.arraySize = EditorGUILayout.DelayedIntField("Size", spParams.arraySize);

            CheckNameFiltersByParamCount(spParams.arraySize);
            for (int j = 0, paramsSize = spParams.arraySize; j < paramsSize; ++j)
            {
                _paramIndex = j;
                DrawMaterialParam(spMat, spParams.GetArrayElementAtIndex(j));
                EditorGUILayout.Separator();
            }

            EditorGUI.indentLevel--;
        }
        return true;
    }

    private string[] GetPropertyNames(Material mat)
    {
        int count = (null == mat || null == mat.shader) ? 0 :mat.shader.GetPropertyCount();
        string[] names = new string[count];
        for (int i = 0; i < count; ++i)
        {
            names[i] = mat.shader.GetPropertyName(i);
        }
        return names;
    }

    private void DrawMaterialPropertyName(SerializedProperty spMat, SerializedProperty spParam, SerializedProperty spName, SerializedProperty spTexST, SerializedProperty spType)
    {
        string[] names = GetPropertyNames(spMat.objectReferenceValue as Material);
        int instanceID = spParam.GetHashCode();
        EditorGUIUtility.labelWidth = 100;
        EditorGUILayout.BeginHorizontal();

        Material mat = spMat.objectReferenceValue as Material;
        string filter = _nameFilters[_propertyIndex][_paramIndex];
        string[] showNames = names.Where(name => name.Contains(filter)).ToArray();
        int index = null != mat ? Array.FindIndex(showNames, 0, showNames.Length, name => name == spName.stringValue) : -1;

        string[] displayName = new string[showNames.Length];
        for (int i = 0; i < displayName.Length; ++i)
        {// 转换名字
            displayName[i] = NamesMapping != null ? NamesMapping.GetDisplayName(mat.shader.name, showNames[i]) : showNames[i];
        }

        index = EditorGUILayout.Popup("属性", index, displayName);

        if (spType.intValue == (int)ShaderPropertyType.Texture)
        {// 纹理参数才显示该选线   _ST 的变量来表示纹理的缩放（Scale => S）和偏移（Translation => T)
            spTexST.boolValue = GUILayout.Toggle(spTexST.boolValue, "纹理的_ST");
        }

        spName.stringValue = index >= 0 && index < showNames.Length ? showNames[index] : spName.stringValue;

        _nameFilters[_propertyIndex][_paramIndex] = EditorGUILayout.TextField(filter);

        if (GUILayout.Button("确定", GUILayout.MaxWidth(80)))
        {
            spName.stringValue = null != showNames && showNames.Length > 0 ? showNames[0] : spName.stringValue;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 0;

        index = null != mat ? Array.FindIndex(names, 0, names.Length, name => name == spName.stringValue) : -1;
        if (index >= 0 && index < mat.shader.GetPropertyCount())
        {
            spType.intValue = (int)mat.shader.GetPropertyType(index);
        }
        else
        {
            spType.intValue = (int)ShaderPropertyType.Texture;
        }
    }

    private void DrawMaterialParam(SerializedProperty spMat, SerializedProperty spParam)
    {
        EditorGUI.indentLevel++;
        SerializedProperty spName = spParam.FindPropertyRelative("Name");
        SerializedProperty spTexST = spParam.FindPropertyRelative("TexST");
        SerializedProperty spType = spParam.FindPropertyRelative("PropertyType");
        SerializedProperty spColor = spParam.FindPropertyRelative("Color");
        SerializedProperty spCurve0 = spParam.FindPropertyRelative("Curve0");
        SerializedProperty spCurve1 = spParam.FindPropertyRelative("Curve1");
        SerializedProperty spCurve2 = spParam.FindPropertyRelative("Curve2");
        SerializedProperty spCurve3 = spParam.FindPropertyRelative("Curve3");

        DrawMaterialPropertyName(spMat, spParam, spName, spTexST, spType);

        switch (spType.intValue)
        {
            case (int)ShaderPropertyType.Color:
                EditorGUILayout.PropertyField(spColor, new GUIContent("颜色"));
                break;
            case (int)ShaderPropertyType.Vector:
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 90;
                EditorGUILayout.PropertyField(spCurve0, new GUIContent("X"));
                EditorGUILayout.PropertyField(spCurve1, new GUIContent("Y"));
                EditorGUILayout.PropertyField(spCurve2, new GUIContent("Z"));
                EditorGUILayout.PropertyField(spCurve3, new GUIContent("W"));
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();
                break;
            case (int)ShaderPropertyType.Float:
            case (int)ShaderPropertyType.Range:
                EditorGUILayout.PropertyField(spCurve0, new GUIContent("数值"));
                break;
            case (int)ShaderPropertyType.Texture:
                if (!spTexST.boolValue)
                {
                    EditorGUILayout.LabelField(new GUIContent("不支持！"));
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 90;
                    EditorGUILayout.PropertyField(spCurve0, new GUIContent("X"));
                    EditorGUILayout.PropertyField(spCurve1, new GUIContent("Y"));
                    EditorGUILayout.PropertyField(spCurve2, new GUIContent("Z"));
                    EditorGUILayout.PropertyField(spCurve3, new GUIContent("W"));
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUILayout.EndHorizontal();
                }

                break;
        }
        EditorGUI.indentLevel--;
    }

    private void GetAnimEffectsIndex(ref int effectIndex, ref int alignIndex)
    {
        SerializedProperty spRowLayout = serializedObject.FindProperty("ArrayAnim.RowLayout");
        SerializedProperty spColLayout = serializedObject.FindProperty("ArrayAnim.ColLayout");
        SerializedProperty spFillType = serializedObject.FindProperty("ArrayAnim.FillType");

        effectIndex = spFillType.intValue - 1;

        if (spRowLayout.intValue == (int)ArrayLayoutType.Increase && spColLayout.intValue == (int)ArrayLayoutType.Increase)
        {
            alignIndex = 0;
        }
        else if (spRowLayout.intValue == (int)ArrayLayoutType.Increase && spColLayout.intValue == (int)ArrayLayoutType.Decrease)
        {
            alignIndex = 1;
        }
        else if (spRowLayout.intValue == (int)ArrayLayoutType.Decrease && spColLayout.intValue == (int)ArrayLayoutType.Decrease)
        {
            alignIndex = 2;
        }
        else if (spRowLayout.intValue == (int)ArrayLayoutType.Decrease && spColLayout.intValue == (int)ArrayLayoutType.Increase)
        {
            alignIndex = 3;
        }
    }

    public void SetAnimEffectsByIndex(int effectIndex, int alignIndex)
    {
        SerializedProperty spRowLayout = serializedObject.FindProperty("ArrayAnim.RowLayout");
        SerializedProperty spColLayout = serializedObject.FindProperty("ArrayAnim.ColLayout");
        SerializedProperty spFillType = serializedObject.FindProperty("ArrayAnim.FillType");
        spFillType.intValue = effectIndex + 1;
        if (alignIndex == 0)
        {
            spColLayout.intValue = (int)ArrayLayoutType.Increase;
            spRowLayout.intValue = (int)ArrayLayoutType.Increase;
        }
        else if (alignIndex == 1)
        {
            spRowLayout.intValue = (int)ArrayLayoutType.Increase;
            spColLayout.intValue = (int)ArrayLayoutType.Decrease;
        }
        else if (alignIndex == 2)
        {
            spRowLayout.intValue = (int)ArrayLayoutType.Decrease;
            spColLayout.intValue = (int)ArrayLayoutType.Decrease;
        }
        else if (alignIndex == 3)
        {
            spRowLayout.intValue = (int)ArrayLayoutType.Decrease;
            spColLayout.intValue = (int)ArrayLayoutType.Increase;
        }
    }

    private void DrawArrayAnimEffects()
    {
        int effectIndex = 0;
        int alignIndex = 0;
        GetAnimEffectsIndex(ref effectIndex, ref alignIndex);
        effectIndex = EditorGUILayout.Popup("阵列动画", effectIndex, ArrayAnimEffectsName.ToArray());
        if (effectIndex < 2)
        {
            alignIndex = EditorGUILayout.Popup("对齐方式", alignIndex, ArrayAnimAlignName.ToArray());
        }

        SetAnimEffectsByIndex(effectIndex, alignIndex);

#if UITWEEN_ANIMATION_DEBUG
        EditorGUI.BeginDisabledGroup(true);
        DrawPropertyByRelative("ArrayAnim.RowLayout", "行排列方向");
        DrawPropertyByRelative("ArrayAnim.ColLayout", "列排列方向");
        DrawPropertyByRelative("ArrayAnim.FillType", "填充方式");
        EditorGUI.EndDisabledGroup();
#endif
    }

    private void AddSpecifyChildren(RectTransform selectedObj)
    {
        if (null != selectedObj)
        {
            UITweenAnimation[] anims = selectedObj.GetComponentsInChildren<UITweenAnimation>();
            for (int i = 0; i < anims.Length; ++i)
            {
                if (anims[i] != _uiTweenAnimation
                    && anims[i].ArrayAnim.ArrayName == _uiTweenAnimation.ArrayAnim.ArrayName
                    && !_uiTweenAnimation.ArrayAnim.SpecifyChildren.Contains(anims[i]))
                {
                    _uiTweenAnimation.ArrayAnim.SpecifyChildren.Add(anims[i]);
                }
            }
        }
    }


    private void DrawArrayAnim()
    {
        SerializedProperty spAsArray = serializedObject.FindProperty("ArrayAnim.AsArray");

#if UITWEEN_ANIMATION_DEBUG
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(spAsArray, new GUIContent("生成阵列"));
        EditorGUI.EndDisabledGroup();
#endif

        if (spAsArray.boolValue)
        {
            DrawArrayAnimEffects();

#if UITWEEN_ANIMATION_DEBUG
            EditorGUI.BeginDisabledGroup(true);
            DrawPropertyByRelative("ArrayAnim.Spacing", "间隔");
            DrawPropertyByRelative("ArrayAnim.LayoutSizeDelta", "父节点大小");
            DrawPropertyByRelative("ArrayAnim.TargetSizeDelta", "目标大小");
            EditorGUI.EndDisabledGroup();
#endif

            DrawPropertyByRelative("ArrayAnim.LayoutParent", "阵列范围(父级)");
            DrawPropertyByRelative("ArrayAnim.Timeline", "时间线");

            EditorGUI.BeginDisabledGroup(true);
            DrawPropertyByRelative("ArrayAnim.ArrayDuration", "时长");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();

            SerializedProperty spLayoutParent = serializedObject.FindProperty("ArrayAnim.LayoutParent");
            SerializedProperty spUseSpecify = serializedObject.FindProperty("ArrayAnim.UseSpecify");
            _childrenTypeIndex = spUseSpecify.boolValue ? 1 : 0;
            _childrenTypeIndex = EditorGUILayout.Popup("阵列子集", _childrenTypeIndex, ArrayChildrenTypeName);
            spUseSpecify.boolValue = _childrenTypeIndex > 0; //GUILayout.Toggle(spUseSpecify.boolValue, "指定列表");

            EditorGUI.BeginDisabledGroup(!spUseSpecify.boolValue || spLayoutParent.objectReferenceValue == null);
            if (GUILayout.Button("添加列表", GUILayout.MaxWidth(80)))
            {
                AddSpecifyChildren(spLayoutParent.objectReferenceValue as RectTransform);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(!spUseSpecify.boolValue);
            DrawPropertyByRelative("ArrayAnim.SpecifyChildren", "列表");
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(spUseSpecify.boolValue);
            DrawPropertyByRelative("ArrayAnim.Count", "生成总数");
            EditorGUI.EndDisabledGroup();

            DrawPropertyByRelative("ArrayAnim.Interval", "生成间隔");
            DrawPropertyByRelative("ArrayAnim.ColSize", "列数");
            DrawPropertyByRelative("ArrayAnim.RowSize", "行数");

            SerializedProperty spArrayTime = serializedObject.FindProperty("ArrayAnim.CurArrayTime");
            SerializedProperty spArrayDuration = serializedObject.FindProperty("ArrayAnim.ArrayDuration");

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUIUtility.labelWidth = 80;
            float newCurTime = EditorGUILayout.Slider("阵列时间", spArrayTime.floatValue, 0, spArrayDuration.floatValue);
            EditorGUIUtility.labelWidth = 0;
            EditorGUI.EndDisabledGroup();

            if (newCurTime != spArrayTime.floatValue)
            {
                spArrayTime.floatValue = newCurTime;
                if (!Application.isPlaying
                    && (null == _uiTweenAnimation.Tween || !_uiTweenAnimation.Tween.IsRun()))
                {
                    if (null == _uiTweenAnimation.ArrayAnim.OriginAnim)
                    {
                        _uiTweenAnimation.CreateTween();
                    }
                    _uiTweenAnimation.OnUpdate(_uiTweenAnimation.Tween, newCurTime);
                }
            }
        }
    }

}
