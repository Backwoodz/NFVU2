using System;
using UnityEngine;
namespace Rachel350Z {
// Local presentation override: never advance the saved world's clock.
public sealed class HordeWeather : IDisposable {
    public const float TransitionSeconds=8;
    readonly EnvMan environment;
    readonly ZDOMan world;
    readonly bool previousTimeOverride;
    readonly float previousTime,previousDuration;
    readonly string previousWeather;
    float start,distance,elapsed=TransitionSeconds;
    bool inHorde,disposed;
    public bool IsCurrent=>!disposed&&environment!=null&&environment==EnvMan.instance&&world==ZDOMan.instance;
    public HordeWeather(EnvMan environment,ZDOMan world) {
        this.environment=environment;this.world=world;
        previousTimeOverride=environment.m_debugTimeOfDay;previousTime=environment.m_debugTime;
        previousWeather=environment.m_debugEnv;previousDuration=environment.m_transitionDuration;
    }
    public void Begin() {
        if(!IsCurrent)return;
        if(!HasWeather("ThunderStorm")||!HasWeather("Clear"))throw new InvalidOperationException("Horde weather needs native ThunderStorm and Clear environments.");
        inHorde=true;Transition("ThunderStorm",0);
    }
    bool HasWeather(string name)=>environment.m_environments.Exists(env=>env.m_name==name);
    public void End() {
        if(!inHorde||!IsCurrent)return;
        inHorde=false;Transition("Clear",.5f);
    }
    void Transition(string weather,float target) {
        start=Mathf.Repeat(environment.m_debugTimeOfDay?environment.m_debugTime:environment.GetDayFraction(),1);
        distance=Mathf.Repeat(target-start,1);elapsed=0;
        environment.m_debugTime=start;environment.m_debugTimeOfDay=true;
        // EnvMan queues and blends its native sky, fog, rain, wind and storm FX.
        environment.m_transitionDuration=TransitionSeconds;environment.m_debugEnv=weather;
    }
    public void Tick(float dt) {
        if(!IsCurrent||elapsed>=TransitionSeconds||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
        elapsed=Mathf.Min(TransitionSeconds,elapsed+dt);
        float t=elapsed/TransitionSeconds;t=t*t*(3-2*t);
        environment.m_debugTime=Mathf.Repeat(start+distance*t,1);
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        if(environment==null)return;
        // Restore only the captured environment instance, never the next world's.
        environment.m_debugTimeOfDay=previousTimeOverride;environment.m_debugTime=previousTime;
        environment.m_debugEnv=previousWeather;environment.m_transitionDuration=previousDuration;
    }
}
}
