using System;
namespace Rachel350Z {
// Only local sky presentation changes: native network time and world age keep running.
internal sealed class DriftSessionEnvironment : IDisposable {
    readonly EnvMan environment;
    readonly ZDOMan world;
    readonly bool previousOverride;
    readonly float previousTime;
    readonly string previousWeather;
    bool disposed;
    public bool IsCurrent=>!disposed&&environment!=null&&environment==EnvMan.instance&&world==ZDOMan.instance;
    public DriftSessionEnvironment(EnvMan environment,ZDOMan world) {
        this.environment=environment;this.world=world;
        previousOverride=environment.m_debugTimeOfDay;previousTime=environment.m_debugTime;
        previousWeather=environment.m_debugEnv;
    }
    public void Tick() {
        if(!IsCurrent)return;
        environment.m_debugTimeOfDay=true;environment.m_debugTime=DriftSessionRules.NightFraction;
        environment.m_debugEnv=DriftSessionRules.Weather;
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        if(environment==null)return;
        // Restore the captured instance only, even when a new world's EnvMan exists.
        environment.m_debugTimeOfDay=previousOverride;environment.m_debugTime=previousTime;
        environment.m_debugEnv=previousWeather;
    }
}
}
