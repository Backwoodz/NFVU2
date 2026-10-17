using System;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static void Main() {
        Check(ExhaustSmokeRules.Rate(true,0,0,false)==6,"running idle emits a small bounded stream");
        Check(ExhaustSmokeRules.Rate(false,0,0,false)==0,"parked engine off emits nothing");
        Check(ExhaustSmokeRules.Rate(true,0,0,true)==0,"menu focus or streaming block stops emission");
        Check(ExhaustSmokeRules.Rate(true,3,0,false)==0&&ExhaustSmokeRules.Rate(true,30,0,false)==0,"driving speed suppresses idle vapour");
        Check(ExhaustSmokeRules.Rate(true,0,1,false)==0&&ExhaustSmokeRules.Rate(true,0,-1,false)==0,"throttle suppresses idle vapour in either direction");
        Check(ExhaustSmokeRules.Rate(true,1,0,false)>0&&ExhaustSmokeRules.Rate(true,1,0,false)==ExhaustSmokeRules.Rate(true,-1,0,false),"slow coasting tapers continuously in either direction");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            if(ExhaustSmokeRules.Rate(true,invalid,0,false)!=0||ExhaustSmokeRules.Rate(true,0,invalid,false)!=0)throw new Exception("invalid input emits smoke");
        Check(true,"invalid velocity or controls never feed invalid particle rates");
        Console.WriteLine(checks+" idle exhaust checks passed; native particles require an in-game visual check.");
    }
}
