using System;
using System.Collections.Generic;
using Rachel350Z;
namespace UnityEngine {
    public static class Mathf {
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Repeat(float value,float length)=>value-(float)Math.Floor(value/length)*length;
    }
}
public class EnvSetup {public string m_name;}
public class EnvMan {
    public static EnvMan instance;
    public bool m_debugTimeOfDay;public float m_debugTime=.25f,m_transitionDuration=2,DayFraction=.5f;
    public string m_debugEnv="";
    public List<EnvSetup> m_environments=new List<EnvSetup>{new EnvSetup{m_name="ThunderStorm"},new EnvSetup{m_name="Clear"}};
    public float GetDayFraction()=>DayFraction;
}
public class ZDOMan {public static ZDOMan instance;}
static class Program {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static void Main() {
        var env=EnvMan.instance=new EnvMan();var world=ZDOMan.instance=new ZDOMan();
        var weather=new HordeWeather(env,world);weather.End();
        Check(!env.m_debugTimeOfDay&&env.m_debugEnv=="","idle cleanup does not change ordinary world weather");
        weather.Begin();Check(env.m_debugTimeOfDay&&Near(env.m_debugTime,.5f)&&env.m_debugEnv=="ThunderStorm"&&env.m_transitionDuration==8,"start queues storm without snapping time");
        weather.Tick(0);weather.Tick(-1);weather.Tick(float.NaN);weather.Tick(float.PositiveInfinity);
        Check(Near(env.m_debugTime,.5f),"pause and invalid deltas do not advance transition");
        weather.Tick(4);Check(Near(env.m_debugTime,.75f),"halfway to night is evening");
        weather.Tick(4);Check(Near(env.m_debugTime,0),"transition wraps smoothly to midnight");
        weather.Tick(100);Check(Near(env.m_debugTime,0)&&env.DayFraction==.5f,"night holds without changing native world clock");
        weather.End();Check(env.m_debugEnv=="Clear"&&Near(env.m_debugTime,0),"end queues clear skies without instant daylight");
        weather.Tick(4);Check(Near(env.m_debugTime,.25f),"return passes through sunrise");
        weather.End();weather.Tick(4);Check(Near(env.m_debugTime,.5f),"repeated cleanup does not restart the return transition");
        weather.Tick(100);Check(env.m_debugTimeOfDay&&Near(env.m_debugTime,.5f)&&env.m_debugEnv=="Clear","day and clear skies remain after the run");
        weather.Begin();weather.Tick(2);float interrupted=env.m_debugTime;
        weather.End();Check(Near(env.m_debugTime,interrupted),"early abort starts from currently displayed time");
        weather.Tick(2);float returnTime=env.m_debugTime;
        weather.Begin();Check(Near(env.m_debugTime,returnTime)&&env.m_debugEnv=="ThunderStorm","quick restart does not jump to an old time");
        weather.Dispose();weather.Dispose();Check(!env.m_debugTimeOfDay&&env.m_debugEnv==""&&env.m_debugTime==.25f&&env.m_transitionDuration==2,"session exit restores original settings once");
        weather.Begin();weather.Tick(4);Check(!env.m_debugTimeOfDay,"disposed weather cannot affect the world");
        env.m_debugTimeOfDay=true;env.m_debugTime=.7f;env.m_debugEnv="Rain";env.m_transitionDuration=5;
        weather=new HordeWeather(env,world);weather.Begin();weather.Tick(8);weather.End();weather.Tick(8);weather.Dispose();
        Check(env.m_debugTimeOfDay&&Near(env.m_debugTime,.7f)&&env.m_debugEnv=="Rain"&&env.m_transitionDuration==5,"pre-existing custom overrides also restore on exit");
        weather=new HordeWeather(env,world);weather.Begin();EnvMan.instance=new EnvMan();ZDOMan.instance=new ZDOMan();
        weather.Tick(8);weather.End();weather.Dispose();
        Check(!weather.IsCurrent&&!EnvMan.instance.m_debugTimeOfDay&&EnvMan.instance.m_debugEnv=="","departed session never writes the new environment");
        env=EnvMan.instance;env.m_environments.Clear();weather=new HordeWeather(env,ZDOMan.instance);
        bool rejected=false;try{weather.Begin();}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&!env.m_debugTimeOfDay&&env.m_debugEnv=="","missing weather presets fail before changing settings");
        Console.WriteLine(checks+" horde weather lifecycle checks passed (native weather rendering requires in-game verification).");
    }
}
