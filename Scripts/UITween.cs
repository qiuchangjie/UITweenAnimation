using System;
using UnityEngine;

public interface IUITweenEvents
{
    void OnReady(IUITween t);
    void OnStart(IUITween t);
    void OnStop(IUITween t);
    void OnPause(IUITween t);
    void OnResume(IUITween t);
    void OnRewind(IUITween t);
    void OnUpdate(IUITween t, float time);
}

public enum UITweenState
{
    Ready,
    Run,
    Pause,
    Stop,
}

public interface IUITween
{
    bool IsStop();
    bool IsRun();
    void Reset();
    void Release();
    void Play();
    void Stop();
    void Pause();
    void Resume();
    void Rewind();
    void Tick(float deltaTime, float timeScale);
}

[Serializable]
public class UITween : IUITween
{
    public float Duration;
    public int Loops;
    public float Time;
    public int Index;
    public bool IgnoreTimeScale;

    public IUITweenEvents TweenEvents;

    private UITweenState _state;
    private int _loopTime;

    public UITweenState State
    {
        get { return _state; }
    }

    public bool IsStop()
    {
        return _state == UITweenState.Stop;
    }

    public bool IsRun()
    {
        return _state == UITweenState.Run;
    }

    public UITween()
    {
        Reset();
    }

    public UITween(float duration, int loops, IUITweenEvents events)
    {
        Time = 0;
        Duration = duration;
        Loops = loops;
        TweenEvents = events;
        _loopTime = 0;
        IgnoreTimeScale = false;
        _state = UITweenState.Ready;

        if (null != TweenEvents)
        {
            TweenEvents.OnReady(this);
        }
    }

    public void Reset()
    {
        _loopTime = 0;
        _state = UITweenState.Ready;

        if (null != TweenEvents)
        {
            TweenEvents.OnReady(this);
        }
    }

    public void Release()
    {
        TweenEvents = null;
    }

    public void Play()
    {
        Time = 0;
        _loopTime = 0;
        _state = UITweenState.Run;

        if (null != TweenEvents)
        {
            TweenEvents.OnStart(this);
            TweenEvents.OnUpdate(this, Time);
        }
    }

    public void Stop()
    {
        Time = Duration;
        _loopTime = Loops > 0 ? Loops : _loopTime;
        _state = UITweenState.Stop;

        if (null != TweenEvents)
        {
            TweenEvents.OnUpdate(this, Time);
            TweenEvents.OnStop(this);
        }
    }

    public void Pause()
    {
        _state = UITweenState.Pause;

        if (null != TweenEvents)
        {
            TweenEvents.OnUpdate(this, Time);
            TweenEvents.OnPause(this);
        }
    }

    public void Resume()
    {
        _state = UITweenState.Run;

        if (null != TweenEvents)
        {
            TweenEvents.OnUpdate(this, Time);
            TweenEvents.OnResume(this);
        }
    }

    public void Rewind()
    {
        Time = 0;
        _loopTime = 0;
        _state = UITweenState.Ready;

        if (null != TweenEvents)
        {
            TweenEvents.OnUpdate(this, Time);
            TweenEvents.OnRewind(this);
        }
    }

    public void ResetState()
    {
        Time = 0;
        _loopTime = 0;
        _state = UITweenState.Ready;
    }

    private bool IsLoopEnd()
    {
        return (Loops >= 0 && _loopTime >= (Loops - 1));
    }

    public void Tick(float deltaTime, float timeScale)
    {
        if (_state == UITweenState.Run)
        {
            if (null != TweenEvents)
            {
                TweenEvents.OnUpdate(this, Time);
            }

            if (IgnoreTimeScale)
            {
                Time = Mathf.Clamp(Time + deltaTime, 0, Duration);
            }
            else
            {
                Time = Mathf.Clamp(Time + deltaTime * timeScale, 0, Duration);
            }

            if (Time >= Duration)
            {
                if (IsLoopEnd())
                {
                    Stop();
                }
                else
                {
                    Time = 0;
                    ++_loopTime;
                }
            }
        }
    }
}
