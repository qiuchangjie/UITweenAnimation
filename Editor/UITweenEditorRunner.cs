using UnityEditor;
using UnityEngine;

public class UITweenEditorRunner : EditorUpdatable
{
    private static float _deltaTime = 1.0f / 30.0f;
    private static float _lastTime = 0;
    private static float _timeScale = 1;

    public static void ResetTime()
    {
        _lastTime = 0;
    }

    public override void Start()
    {
    }

    public override void Update()
    {
        //Debug.LogErrorFormat("startupTime={0}, _lastTime={1}", Time.realtimeSinceStartup, _lastTime);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (Time.realtimeSinceStartup - _lastTime >= _deltaTime)
            {
                _lastTime = Time.realtimeSinceStartup;

                UITweenRunner.OnTick(_deltaTime, _timeScale);
            }
        }
#endif
    }

    public override void OnPlayModeStateChanged(PlayModeStateChange modeState)
    {
        if (modeState == PlayModeStateChange.ExitingEditMode)
        {
            UITweenRunner.Cleanup();
        }
    }
}
