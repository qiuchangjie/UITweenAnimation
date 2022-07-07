using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITweenSequence : IUITween
{
    public float Duration;
    public float Time;
    public bool ReversePlay;
    public float ReverseTime;
    public float ReverseDuration;
    public float ReverseScale;
    private Dictionary<int, List<UITween>> _children;
    private Dictionary<int, float> _childDuration;
    private List<int> _childIndex;
    private UITweenState _state;

    public UITweenSequence()
    {
        Reset();
    }

    public void AddChild(UITween tween)
    {
        if (null == tween) return;

        if (null == _children)
        {
            _children = new Dictionary<int, List<UITween>>();
        }
        if (null == _childIndex)
        {
            _childIndex = new List<int>();
        }
        if (null == _childDuration)
        {
            _childDuration = new Dictionary<int, float>();
        }

        if (!_children.ContainsKey(tween.Index))
        {
            _children.Add(tween.Index, new List<UITween>());
        }

        if (!_children[tween.Index].Contains(tween))
        {
            _children[tween.Index].Add(tween);
        }
    }

    public void RemoveChild(UITween child)
    {
        if (null != _children)
        {
            foreach (var pair in _children)
            {
                if (pair.Value.Contains(child))
                {
                    pair.Value.Remove(child);
                }
            }
        }

        RefreshDuration();
    }

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

    public void Reset()
    {
        _state = UITweenState.Ready;
        Time = 0;
        ReverseTime = 0;
    }

    public void Release()
    {
        _children.Clear();
        _childDuration.Clear();
        _childIndex.Clear();
    }

    private void RefreshDuration()
    {
        _childIndex.Clear();
        _childIndex.AddRange(_children.Keys);
        _childIndex.Sort();

        _childDuration.Clear();
        Duration = 0;
        foreach (var pair in _children)
        {
            List<UITween> tweens = pair.Value;
            float max = 0;
            for (int i = 0; i < tweens.Count; ++i)
            {
                max = Mathf.Max(tweens[i].Duration, max);
                tweens[i].Reset();
            }
            Duration += max;
            _childDuration[pair.Key] = max;
        }

        ReverseScale = Duration / ReverseDuration;
    }

    public void Play()
    {
        Time = 0;
        _state = UITweenState.Run;
        ReverseTime = 0;
        ReverseScale = Duration / ReverseDuration;

        RefreshDuration();
    }

    public void Stop()
    {
        Time = Duration;
        _state = UITweenState.Stop;
    }

    public void Pause()
    {
        _state = UITweenState.Pause;
    }

    public void Resume()
    {
        _state = UITweenState.Run;
    }

    public void Rewind()
    {
        Time = 0;
        ReverseTime = 0;
        _state = UITweenState.Ready;
    }

    public bool IsChild(UITween tween)
    {
        if (null != _children)
        {
            foreach (var pair in _children)
            {
                if (pair.Value.Contains(tween))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private float OnTickChild(int i, float deltaTime, float timeScale)
    {
        int index = _childIndex[i];
        if (_children.ContainsKey(index))
        {
            List<UITween> tweens = _children[index];
            for (int j = 0; j < tweens.Count; ++j)
            {
                UITween tween = tweens[j];
                if (!tween.IsStop())
                {
                    if (!tween.IsRun())
                    {
                        tween.Play();
                    }

                    tween.Tick(deltaTime, timeScale);
                }
            }
        }

        return _childDuration[index];
    }

    public void Tick(float deltaTime, float timeScale)
    {
        if (_state == UITweenState.Run)
        {
            Time = Mathf.Clamp(Time + deltaTime * timeScale, 0, Duration);
            float passDuration = 0;
            if (ReversePlay)
            {
                ReverseTime = Time;
                for (int i = _childIndex.Count - 1; i >= 0; --i)
                {
                    if (Time * ReverseScale < passDuration)
                    {
                        break;
                    }
                    passDuration += OnTickChild(i, deltaTime * ReverseScale, timeScale);
                }

                if (ReverseTime >= ReverseDuration)
                {
                    Stop();
                }
            }
            else
            {
                for (int i = 0; i < _childIndex.Count; ++i)
                {
                    if (Time < passDuration)
                    {
                        break;
                    }
                    passDuration += OnTickChild(i, deltaTime, timeScale);
                }

                if (Time >= Duration)
                {
                    Stop();
                }
            }
        }
    }

    private void DoUpdateChild(int i, ref float timeLeft)
    {
        int index = _childIndex[i];
        if (timeLeft >= 0)
        {
            if (_children.ContainsKey(index))
            {
                List<UITween> tweens = _children[index];
                for (int j = 0; j < tweens.Count; ++j)
                {
                    UITween tween = tweens[j];
                    tween.TweenEvents.OnUpdate(tween, timeLeft);
                }
            }
        }

        timeLeft = Mathf.Clamp(timeLeft - _childDuration[index], 0, timeLeft);
    }

    public void DoUpdateChildren(float time)
    {
        if (null == _childIndex || null == _children || null == _childDuration) return;

        this.Time = time;
        float timeLeft = time;
        if (ReversePlay)
        {
            this.ReverseTime = this.Time;
            timeLeft = this.ReverseTime * ReverseScale;
            for (int i = _childIndex.Count - 1; i >= 0; --i)
            {
                DoUpdateChild(i, ref timeLeft);
            }
        }
        else
        {
            for (int i = 0; i < _childIndex.Count; ++i)
            {
                DoUpdateChild(i, ref timeLeft);
            }
        }
    }
}
