using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITweenRunner : MonoSingleton<UITweenRunner>
{
    public struct UITweenInfo
    {
        public UITween Tween;
        public bool AsSequence;
        public string SequenceName;

        public UITweenInfo(UITween t, bool asSeq, string seqName)
        {
            Tween = t;
            AsSequence = asSeq;
            SequenceName = seqName;
        }
    }

    [SerializeField]
    private static List<UITween> _tweens = new List<UITween>();
    [SerializeField]
    private static Dictionary<string, UITweenSequence> _sequence = new Dictionary<string, UITweenSequence>();

    private static Dictionary<UITween, UITweenInfo> _tweenInfos = new Dictionary<UITween, UITweenInfo>();

    public override void Dispose()
    {
        Cleanup();
    }
    void Awake()
    {
    }
    void Start()
    {
    }

    public void Update()
    {
        if (Application.isPlaying)
        {
            OnTick(Time.unscaledDeltaTime, Time.timeScale);
        }
    }

    public static void OnTick(float deltaTime, float timeScale)
    {
        for (int i = 0; i < _tweens.Count; ++i)
        {
            UITween tween = _tweens[i];
            if (tween.IsStop())
            {
                continue;
            }
            if (tween.IsRun())
            {
                tween.Tick(deltaTime, timeScale);
            }
        }

        // sequence
        foreach (KeyValuePair<string, UITweenSequence> pair in _sequence)
        {
            UITweenSequence tweenSeq = pair.Value;
            if (tweenSeq.IsStop())
            {
                continue;
            }

            if (tweenSeq.IsRun())
            {
                tweenSeq.Tick(deltaTime, timeScale);
            }
        }
    }

    public static void Cleanup()
    {
        _tweens.Clear();
        _tweenInfos.Clear();
        _sequence.Clear();
    }

    private static void PlaySequence(string sequenceName)
    {
        if (_sequence.ContainsKey(sequenceName))
        {
            _sequence[sequenceName].Play();
        }
    }

    private static void StopSequence(string sequenceName)
    {
        if (_sequence.ContainsKey(sequenceName))
        {
            _sequence[sequenceName].Stop();
        }
    }

    public static UITweenSequence GetSequence(string sequenceName)
    {
        UITweenSequence seq = null;
        _sequence.TryGetValue(sequenceName, out seq);
        return seq;
    }

    public static bool IsSequenceChild(string sequenceName, UITween tween)
    {
        UITweenSequence seq = GetSequence(sequenceName);
        if (null != seq)
        {
            return seq.IsChild(tween);
        }
        return false;
    }

    public static void Play(UITween tween)
    {
        if (Instance) ; // 仅仅为了运行时初始化单例

        //Debug.LogError($"containskey={_tweenInfos.ContainsKey(tween)}");
        if (_tweenInfos.ContainsKey(tween))
        {
            UITweenInfo info = _tweenInfos[tween];
            if (info.AsSequence)
            {
                PlaySequence(info.SequenceName);
            }
            else
            {
                info.Tween.Play();
            }
        }
    }

    public static void Stop(UITween tween)
    {
        if (_tweenInfos.ContainsKey(tween))
        {
            UITweenInfo info = _tweenInfos[tween];
            if (info.AsSequence)
            {
                StopSequence(info.SequenceName);
            }
            else
            {
                info.Tween.Stop();
            }
        }
    }

    public static bool Exist(UITween tween)
    {
        return ((null != _tweenInfos) && _tweenInfos.ContainsKey(tween));
    }

    public static void Add(UITween tween, bool asSequence, string sequenceName, bool reverseSeq, float reverseDuration)
    {
        if (Exist(tween)) return;

        UITweenInfo info = new UITweenInfo(tween, asSequence, sequenceName);
        _tweenInfos.Add(tween, info);

        if (info.AsSequence)
        {
            UITweenSequence seq = null;
            if (!_sequence.TryGetValue(info.SequenceName, out seq))
            {
                seq = new UITweenSequence();
                _sequence.Add(info.SequenceName, seq);
            }
            seq.ReversePlay = reverseSeq;
            seq.ReverseDuration = reverseDuration;
            seq.AddChild(tween);

        }
        else
        {
            _tweens.Add(tween);
        }
    }

    public static void Remove(UITween tween)
    {
        UITweenInfo info;
        if (_tweenInfos.TryGetValue(tween, out info))
        {
            if (info.AsSequence 
                && _sequence.ContainsKey(info.SequenceName))
            {
                _sequence[info.SequenceName].RemoveChild(tween);
            }
            else
            {
                _tweens.Remove(tween);
            }

            _tweenInfos.Remove(tween);
        }
        else
        {
            _tweens.Remove(tween);
        }
    }
}
