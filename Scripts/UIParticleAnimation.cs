using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UIParticleAnimation : IUIAnimationBase
{
    [Serializable]
    public class UIParticleCfg
    {
        public ParticleSystem Particle;
        public float PlayTime;
        [HideInInspector]
        public float PlayRate;
        [HideInInspector]
        public bool Played;
    }

    public bool EnableParticle;
    public List<UIParticleCfg> ParticlesCfg;

    private float _duration;

    public void SetDuration(float duration)
    {
        _duration = duration;
    }

    public void Init(GameObject target)
    {
        if (null != ParticlesCfg)
        {
            for (int i = 0; i < ParticlesCfg.Count; ++i)
            {
                UIParticleCfg cfg = ParticlesCfg[i];
                cfg.PlayRate = (0 == _duration ? 0 : cfg.PlayTime / _duration);
                cfg.Played = false;
            }
        }
    }

    public void OnStart()
    {
    }

    public void OnStop()
    {
    }

    public void OnUpdate(float rate)
    {
        if (null != ParticlesCfg)
        {
            for (int i = 0; i < ParticlesCfg.Count; ++i)
            {
                UIParticleCfg cfg = ParticlesCfg[i];
                if (!cfg.Played && rate >= cfg.PlayRate)
                {
                    cfg.Particle.Play();
                    cfg.Played = true;
                }
                else if (cfg.Played && rate < cfg.PlayRate)
                {
                    cfg.Particle.Stop();
                    cfg.Played = false;
                }

                if (!Application.isPlaying)
                {
                    if (cfg.Played)
                    {
                        float curTime = rate * _duration;
                        cfg.Particle.Simulate(curTime - cfg.PlayTime);
                    }
                }
            }
        }
    }

    public void Release()
    {
        if (null != ParticlesCfg)
        {
            ParticlesCfg.Clear();
            ParticlesCfg = null;
        }
    }
}
