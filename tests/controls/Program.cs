using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static void Main() {
        Func<string,string> de=s=>InterfaceText.Translate(s,false),en=s=>InterfaceText.Translate(s,true);
        var xbox=string.Join("\n",VehicleHintContent.Build(true,false,false,false,false,"F9",de));
        Check(xbox.Contains("name=\"button_lb\">  Geschütz feuern")&&xbox.Contains("name=\"button_x\">  Nitro")&&xbox.Contains("name=\"button_a\">  Handbremse"),"Xbox ground hints match physical turret, nitro and handbrake bindings");
        var ps=string.Join("\n",VehicleHintContent.Build(true,true,false,false,false,"F9",en));
        Check(ps.Contains("<sprite=\"ps5\" name=\"button_l1\">  Fire turret")&&ps.Contains("name=\"button_square\">  Nitro")&&ps.Contains("name=\"button_cross\">  Handbrake")&&!ps.Contains("xbox"),"PS5 uses native L1, square and cross glyph names");
        var flying=string.Join("\n",VehicleHintContent.Build(true,false,true,false,false,"F9",en));
        Check(flying.Contains("name=\"button_lb\">  Descend")&&flying.Contains("name=\"button_rb\">  Ascend")&&!flying.Contains("Fire turret")&&flying.Contains("Turret: automatic"),"flight hints replace manual turret with altitude controls and automatic fire");
        Check(flying.Contains("name=\"button_b\">  Land & exit"),"flight exit hint describes landing first");
        var menu=string.Join("\n",VehicleHintContent.Build(true,true,true,true,false,"F9",en));
        Check(menu.Contains("Close menu")&&menu.Contains("Category")&&menu.Contains("Camera")&&!menu.Contains("Nitro")&&!menu.Contains("Fire turret"),"open menu hints show only available interface and camera actions");
        var keyboard=string.Join("\n",VehicleHintContent.Build(false,false,true,false,false,"F10",en));
        Check(!keyboard.Contains("<sprite")&&keyboard.Contains("F10  Mod menu")&&keyboard.Contains("Shift  Ascend")&&keyboard.Contains("Space  Descend"),"keyboard flight hints use actual keys and configured menu shortcut");
        Check(!string.Join("\n",VehicleHintContent.Build(true,false,false,false,true,"F9",de)).Contains("Nitro"),"H2 ground hints omit unavailable nitro");
        Check(string.Join("\n",VehicleHintContent.Build(true,true,true,false,true,"F9",de)).Contains("Nitro"),"H2 flight hints retain available nitro");
        Check(ControlRules.Axis(.1f,.16f)==0&&ControlRules.Axis(-.1f,.16f)==0,"stick drift is neutral in both directions");
        Check(ControlRules.Axis(1,.16f)==1&&ControlRules.Axis(-1,.16f)==-1,"full stick retains full steering range");
        Check(ControlRules.Axis(.58f,.16f)>.49f&&ControlRules.Axis(.58f,.16f)<.51f,"partial stick remains analog");
        Check(ControlRules.Axis(float.NaN,.16f)==0&&ControlRules.Axis(float.PositiveInfinity,.16f)==0,"invalid stick values cannot reach physics");
        ControlRules.Pedals(.4f,0,10,out float throttle,out float brake);
        Check(Math.Abs(throttle-.4f)<.001&&brake==0,"RT/R2 applies proportional forward throttle");
        ControlRules.Pedals(0,.3f,10,out throttle,out brake);
        Check(throttle==0&&Math.Abs(brake-.3f)<.001,"LT/L2 first applies proportional brakes");
        ControlRules.Pedals(0,.3f,0,out throttle,out brake);
        Check(throttle<0&&brake==0,"LT/L2 reverses after stopping");
        ControlRules.Pedals(.7f,0,-5,out throttle,out brake);
        Check(throttle==0&&Math.Abs(brake-.7f)<.001,"RT/R2 brakes reverse travel before forward drive");
        var gate=new ReleaseGate();gate.Block();
        Check(!gate.Allow(true)&&!gate.Allow(true),"held gas/nitro cannot resume after a menu or focus interruption");
        Check(gate.Allow(false)&&gate.Allow(true),"release permits fresh control input");
        Check(ControlRules.CycleMode(1,1,false)==2&&ControlRules.CycleMode(2,1,false)==3,"350Z cycles road drift and flight");
        Check(ControlRules.CycleMode(1,1,true)==3&&ControlRules.CycleMode(3,-1,true)==1,"H2 skips unavailable drift both ways");
        Check(ControlRules.CycleMode(3,1,false)==0&&ControlRules.CycleMode(0,-1,false)==3,"mode cycling wraps consistently");
        Check(ControlRules.AutomaticTurret(true,true,true,false),"flight always uses auto aim even if parked guard is disabled");
        Check(!ControlRules.AutomaticTurret(false,true,true,true),"ground driving remains manual");
        Check(ControlRules.AutomaticTurret(false,false,true,true),"existing parked guard remains available");
        Check(!ControlRules.AutomaticTurret(false,false,false,true),"fresh unoccupied cars do not fire");
        Check(ControlRules.MenuMove(0,-1,5)==4&&ControlRules.MenuMove(4,1,5)==0,"controller menu focus wraps");
        Check(ControlRules.MenuMove(0,1,0)==0,"empty or disabled menu is safe");
        Check(InterfaceText.Translate("Scheibentönung   75 %",true)=="Window tint   75 %","English dynamic tuning label retains the numeric value");
        Check(InterfaceText.Translate("Fahrmodus",false)=="Fahrmodus","German mode preserves authored text");
        Check(InterfaceText.Translate("Kofferraum-Neon",true)=="Trunk neon","specific labels translate before general fragments");
        Check(InterfaceText.Translate("X: Nitro · B: aussteigen",true)=="X: Nitro · B: exit","Xbox hint keeps button names");
        Check(InterfaceText.Translate("Quadrat: Nitro · Kreis: aussteigen",true)=="Square: Nitro · Circle: exit","PS5 hint uses matching button names");
        Check(InterfaceText.Translate("Grün",true)=="Green"&&InterfaceText.Translate("Straße",true)=="Road","Unicode translation keys remain intact");
        Check(InterfaceText.Translate("Nissan 350Z",true)=="Nissan 350Z","vehicle identifiers are not rewritten");
        Console.WriteLine(checks+" controls checks passed. Controller hardware, IMGUI focus, native capture and flight landing require a game test.");
    }
}
