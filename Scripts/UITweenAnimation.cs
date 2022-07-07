using System;
using System.Collections.Generic;
using UnityEngine;

public class UITweenAnimation : MonoBehaviour, IUITweenEvents
{
    private readonly static Dictionary<string, List<UITweenAnimation>> AllAnimations = new Dictionary<string, List<UITweenAnimation>>();

    public float Duration = 1;
    public float Delay = 0;
    public int Loops = 1;
    public bool AutoPlay;
    public GameObject Target;
    public string Name;
    public bool IgnoreTimeScale = false;

    public UITransformAnimation TransformAnim;
    public UIColorAnimation ColorAnim;
    public UIParticleAnimation ParticleAnim;
    public UIMaterialAnimation MaterialAnim;

    public bool AsSequence = false; // 按照队列播放
    public string SequenceName = ""; // 队列Name
    public int SequenceIndex = -1;
    public bool ReverseSequence;
    public float ReverseDuration = 1;
    public const int DEFAULT_SEQ_INDEX = -1;

    public UIArrayAnimation ArrayAnim; // 阵列排布动画

    public float CurTime;
    public float CurDelay;
    public float CurRate;

    private UITween _tween = new UITween();
    public float DelayDuration;

    public Action OnStartFunc = null;
    public Action OnStopFunc = null;

    public UITween Tween
    {
        get { return _tween; }
    }

    /// <summary>
    /// 播放指定名字的所有UI动画
    /// </summary>
    /// <param name="name">动画名字</param>
    public static void PlayAll(string name)
    {
        List<UITweenAnimation> anims = null;
        if (AllAnimations.TryGetValue(name, out anims))
        {
            for (int i = 0; i < anims.Count; ++i)
            {
                anims[i].Replay();
            }
        }
    }

    /// <summary>
    /// 播放UI动画队列
    /// </summary>
    /// <param name="seqName">队列名</param>
    /// <param name="reverse">是否倒退播放</param>
    public static void PlaySequence(string seqName, bool reverse)
    {
        foreach (var pair in AllAnimations)
        {
            List<UITweenAnimation> anims = pair.Value;
            for (int i = 0; i < anims.Count; ++i)
            {
                if (anims[i].AsSequence && anims[i].SequenceName.Equals(seqName))
                {
                    anims[i].ReverseSequence = reverse;
                    anims[i].CreateTween();
                    UITweenRunner.Play(anims[i].Tween);
                    break;
                }
            }
        }
    }

    public void AddToRunner()
    {
        if (ArrayAnim.IsChild) return;

        if (AsSequence)
        {
            UITweenAnimation[] anims = this.GetComponents<UITweenAnimation>();
            if (!Application.isPlaying)
            {// 运行状态下不能这么简单粗暴
                anims = this.transform.root.GetComponentsInChildren<UITweenAnimation>();
            }

            HashSet<int> existIndex = new HashSet<int>();
            for (int i = 0; i < anims.Length; ++i)
            {
                if (anims[i].SequenceIndex >= 0)
                {
                    existIndex.Add(anims[i].SequenceIndex);
                }
            }

            int index = 0;
            for (int i = 0; i < anims.Length; ++i)
            {
                if (!anims[i].AsSequence
                    || anims[i].SequenceName != this.SequenceName)
                {
                    continue;
                }

                anims[i].InitTween();

                if (anims[i].SequenceIndex < 0)
                {
                    while (existIndex.Contains(index))
                    {
                        ++index;
                    }
                    anims[i].Tween.Index = index;
                    existIndex.Add(index);
                }
                else
                {
                    anims[i].Tween.Index = anims[i].SequenceIndex;
                }
                anims[i].ReverseSequence = ReverseSequence;
                anims[i].ReverseDuration = ReverseDuration;
                UITweenRunner.Add(anims[i].Tween, anims[i].AsSequence, anims[i].SequenceName, ReverseSequence, ReverseDuration);
            }
        }
        else
        {
            UITweenRunner.Add(_tween, AsSequence, SequenceName, false, 0);
        }
    }

    public bool IsArrayAnim()
    {
        return (null != ArrayAnim && ArrayAnim.AsArray && !ArrayAnim.IsChild);
    }

    public void InitTween()
    {
        GameObject targetGo = this.gameObject;
        if (null != Target)
        {
            targetGo = Target;
        }

        TransformAnim.Init(targetGo);
        ColorAnim.Init(targetGo);
        ParticleAnim.Init(targetGo);
        ParticleAnim.SetDuration(Duration);
        ArrayAnim.Init(targetGo);
        MaterialAnim.Init(targetGo);

        DelayDuration = Delay + Duration;
        CurTime = 0;
        CurDelay = 0;
        CurRate = 0;

        if (null == _tween)
        {
            _tween = new UITween(DelayDuration, Loops, this);
        }

        if (IsArrayAnim())
        {
            AsSequence = false;

            if (ArrayAnim.UseSpecify)
            {
                ArrayAnim.ArrayDuration = 0;
                for (int i = 0; i < ArrayAnim.SpecifyChildren.Count; ++i)
                {// 累加所有阵列子对象
                    float childDur = i * 1.0f * ArrayAnim.Interval + ArrayAnim.SpecifyChildren[i].Delay + ArrayAnim.SpecifyChildren[i].Duration;
                    ArrayAnim.ArrayDuration = Mathf.Max(childDur, ArrayAnim.ArrayDuration);
                }
            }
            else
            {
                ArrayAnim.ArrayDuration = (float)(ArrayAnim.Count - 1) * ArrayAnim.Interval + DelayDuration;
            }

            _tween.Duration = ArrayAnim.ArrayDuration;
            _tween.Loops = 1;

            ArrayAnim.CreateArray(this);
        }
        else
        {
            _tween.Duration = DelayDuration;
            _tween.Loops = Loops;
        }
        _tween.IgnoreTimeScale = IgnoreTimeScale;
        _tween.TweenEvents = this;
        _tween.Reset();

        UITweenRunner.Remove(_tween);
    }

    public void CreateTween()
    {
        if (Application.isPlaying
            && AsSequence
            && null != this._tween
            && UITweenRunner.IsSequenceChild(this.SequenceName, this._tween))
        {// 运行状态下为了避免重复操作造成UI卡顿，提前结束
            return;
        }

        //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        //watch.Restart();

        InitTween();
        AddToRunner();

        //watch.Stop();
        //Debug.LogError($"{Name} CreateTween use {watch.Elapsed.TotalMilliseconds}ms");
    }

    public void ReleaseTween()
    {
        if (null != _tween)
        {
            UITweenRunner.Remove(_tween);
            _tween.Release();
        }
        _tween = null;

        TransformAnim.Release();
        ColorAnim.Release();
        ArrayAnim.Release();
        MaterialAnim.Release();
    }

    public void Play()
    {
        UITweenRunner.Play(_tween);
    }

    public void Replay()
    {
        _tween.ResetState();
        UITweenRunner.Play(_tween);
    }

    public void Stop()
    {
        UITweenRunner.Stop(_tween);
    }

    private void AddToStaticCache()
    {
        List<UITweenAnimation> anims = null;
        if (!AllAnimations.TryGetValue(Name, out anims))
        {
            anims = new List<UITweenAnimation>();
            AllAnimations.Add(Name, anims);
        }
        
        if (!anims.Contains(this))
        {
            anims.Add(this);
        }
    }

    private void DelFromStaticCache()
    {
        List<UITweenAnimation> anims = null;
        if (AllAnimations.TryGetValue(Name, out anims))
        {
            anims.Remove(this);
        }
    }

    void Awake()
    {
        CreateTween();
        AddToStaticCache();
    }
    void Start()
    {
    }

    void OnEnable()
    {
        //Debug.LogError($"OnEnable AutoPlay={AutoPlay} Name={name}");
        if (AutoPlay)
        {
            Replay();
        }
    }

    void OnDisable()
    {
        
    }

    void OnDestroy()
    {
        ReleaseTween();
        OnStartFunc = null;
        OnStopFunc = null;
        DelFromStaticCache();
    }

    public void OnReady(IUITween t)
    {
        if (AsSequence || (null != ArrayAnim && ArrayAnim.IsChild))
        {
            TransformAnim.OnUpdate(0);
        }
        ColorAnim.OnUpdate(0);
        ParticleAnim.OnUpdate(0);
        MaterialAnim.OnUpdate(0);
    }

    public void OnStart(IUITween t)
    {
        if (IsArrayAnim() && !ArrayAnim.UseSpecify)
        {
            this.gameObject.SetActive(false);
        }

        if (IsArrayAnim())
        {
            ArrayAnim.OnStart();
        }
        else
        {
            TransformAnim.OnStart();
            ColorAnim.OnStart();
            ParticleAnim.OnStart();
            MaterialAnim.OnStart();
        }
        if (null != OnStartFunc)
        {
            OnStartFunc();
        }
    }

    public void OnStop(IUITween t)
    {
        if (IsArrayAnim())
        {
            ArrayAnim.OnStop();
        }
        else
        {
            TransformAnim.OnStop();
            ColorAnim.OnStop();
            ParticleAnim.OnStop();
            MaterialAnim.OnStop();
        }
        if (null != OnStopFunc)
        {
            OnStopFunc();
        }
    }

    public void OnPause(IUITween t)
    {
    }

    public void OnResume(IUITween t)
    {
    }

    public void OnRewind(IUITween t)
    {
    }

    private bool IsReversePlay()
    {
        return AsSequence && ReverseSequence;
    }
    public void OnUpdate(IUITween t, float time)
    {
        if (IsArrayAnim())
        {
            ArrayAnim.CurArrayTime = time;
            ArrayAnim.CurArrayRate = (ArrayAnim.ArrayDuration > 0 ? ArrayAnim.CurArrayTime / ArrayAnim.ArrayDuration : 0);

            float rate = IsReversePlay() ? (Mathf.Clamp01(1 - ArrayAnim.CurArrayRate)) : ArrayAnim.CurArrayRate;
            ArrayAnim.OnUpdate(rate);
        }
        else
        {
            //Debug.LogError($"time={time} Name={Name}");
            if (time < Delay)
            {
                CurDelay = time;
                CurTime = 0;
                CurRate = 0;
            }
            else
            {
                CurDelay = Delay;
                CurTime = time - CurDelay;
                CurRate = (Duration > 0 ? CurTime / Duration : 0);
            }
            float rate = IsReversePlay() ? (Mathf.Clamp01(1 - CurRate)) : CurRate;

            TransformAnim.OnUpdate(rate);
            ColorAnim.OnUpdate(rate);
            ParticleAnim.OnUpdate(rate);
            MaterialAnim.OnUpdate(rate);
        }
    }

    /// <summary>
    /// 刷新所有的项颜色
    /// </summary>
    public void OnAnimUpdateColor()
    {
        ColorAnim.UpdateSelfColor();
    }
}
