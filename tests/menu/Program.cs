using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Rachel350Z;
class MenuTests {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static void Main(string[] args) {
        var position=new MenuPlacement(16,16);
        position.Begin(100,40);position.Move(300,140,1920,1080,740,560);
        Check(position.X==216&&position.Y==116,"drag preserves grab offset without jumping");
        position.Move(3000,2000,1920,1080,740,560);
        Check(position.X==1180&&position.Y==520,"drag keeps entire menu inside right and bottom edges");
        position.Move(-300,-200,1920,1080,740,560);
        Check(position.X==0&&position.Y==0,"drag cannot hide header above or left of screen");
        position.End();position.Move(500,500,1920,1080,740,560);
        Check(position.X==0&&position.Y==0&&!position.Dragging,"release or close stops movement");
        position=new MenuPlacement(1100,500);position.Constrain(800,600,740,560);
        Check(position.X==60&&position.Y==40,"saved position stays reachable after resolution decrease");
        position=new MenuPlacement(16,16);position.Begin(40,30);position.Move(140,130,800,600,370,280);
        Check(position.X==116&&position.Y==116,"scaled menu follows physical mouse distance");
        position=new MenuPlacement(float.NaN,float.PositiveInfinity);
        Check(position.X==16&&position.Y==16,"invalid saved coordinates restore default position");
        string catalog=Path.Combine(args[0],"menu.json");
        if(!File.Exists(catalog)){Console.WriteLine("Optional menu media absent; runtime will use text controls.");return;}
        var data=MenuData.Load(catalog);
        Check(data.icons.Count(t=>t!=null)==10&&data.icons[9]==null,"ten original icons and custom turret fallback load in runtime parser");
        Check(data.titles[5]=="Felgen"&&data.titles[10]=="Garage","original German titles survive import and runtime deserialization");
        foreach(var role in new[]{"navigate","accept","back"}) {
            var wav=WavPcm16.Read(File.OpenRead(Path.Combine(args[0],role+".wav")));
            Check(wav.Samples.Length>100&&wav.Samples.Any(s=>Math.Abs(s)>.05f)&&wav.Samples.Length<wav.SampleRate,"short non-silent original "+role+" clip accepted by Unity PCM input reader");
        }
        string temp=Path.Combine(Path.GetTempPath(),"nfvu2-menu-"+Guid.NewGuid()+".json");
        try {
            foreach(var bad in new[]{"null","{}","{\"schemaVersion\":1,\"icons\":[],\"titles\":[]}"}) {
                File.WriteAllText(temp,bad);bool rejected=false;try{MenuData.Load(temp);}catch(InvalidDataException){rejected=true;}
                Check(rejected,"malformed catalog rejected before texture allocation");
            }
            data.icons[0].rgba=new byte[4];File.WriteAllText(temp,JsonConvert.SerializeObject(data));
            bool invalid=false;try{MenuData.Load(temp);}catch(InvalidDataException){invalid=true;}
            Check(invalid,"truncated original pixel payload rejected");
        }finally{if(File.Exists(temp))File.Delete(temp);}
        Console.WriteLine(checks+" menu asset checks passed; layout and playback require an in-game check.");
    }
}
