using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static void Contacts() {
        var m=new TransformerFootstepEffectModel();
        Check(m.Observe(true,false,15,19)==0,"first observed counters establish baseline instead of replaying parked history");
        Check(m.Observe(true,false,16,19)==1,"one true left footfall creates one visual contact");
        Check(m.Observe(true,false,16,20)==2,"right footfall independent of left timing");
        bool idleSilent=true;for(int i=0;i<100;i++)idleSilent&=m.Observe(true,false,16,20)==0;
        Check(idleSilent,"one hundred idle observations cannot synthesize any footprint");
        Check(m.Observe(true,false,17,21)==3,"simultaneous actual landings retain both contacts");
        Check(m.Observe(true,true,18,22)==0,"pause focus and streaming suppress new visual emission");
        Check(m.Observe(true,false,18,22)==0,"resume cannot replay contacts consumed while blocked");
        Check(m.Observe(false,false,25,29)==0,"inactive car mode updates counter baseline without creating a print");
        Check(m.Observe(true,false,25,29)==0,"active robot restore does not replay a car-state counter change");
        Check(m.Observe(true,false,28,32)==3,"short frame gap emits at most one current contact per foot");
        Check(m.Observe(true,false,100,200)==0,"large counter jump is treated as restore instead of historical burst");
        Check(m.Observe(true,false,0,0)==0,"counter reset does not emit either foot");
        Check(m.Observe(true,false,1,0)==1,"next actual footfall works normally after reset");
        m.Observe(false,false,uint.MaxValue,uint.MaxValue);
        Check(m.Observe(true,false,0,0)==3,"uint wrap still represents exactly one real contact per foot");
    }
    static void Pool() {
        var m=new TransformerFootstepEffectModel();
        bool invisible=true;for(int i=0;i<TransformerFootstepEffectModel.Capacity;i++)invisible&=m.Alpha(i)==0;
        Check(invisible,"every unallocated slot is invisible");
        int first=m.Allocate();Check(first==0&&m.Alpha(first)==1,"fresh footprint starts fully visible");
        m.Tick(18,false);Check(m.Alpha(first)==1,"ground mark retains full contrast for eighteen seconds");
        m.Tick(3,false);Check(Near(m.Alpha(first),.5f),"footprint fades smoothly during final six seconds");
        m.Tick(100,true);Check(Near(m.Alpha(first),.5f),"pause or streaming freeze does not consume footprint lifetime");
        m.Tick(0,false);m.Tick(-1,false);m.Tick(float.NaN,false);m.Tick(float.PositiveInfinity,false);
        Check(Near(m.Alpha(first),.5f),"invalid or zero frame time cannot poison or expire mark ages");
        m.Tick(3,false);Check(m.Alpha(first)==0,"expired footprint disappears without persistent world state");
        bool sequential=true;for(int i=1;i<TransformerFootstepEffectModel.Capacity;i++)sequential&=m.Allocate()==i;
        Check(sequential,"bounded pool visits each existing slot in order before reuse");
        Check(m.Allocate()==0&&m.Alpha(0)==1,"full pool reuses oldest allocated slot rather than growing resources");
        for(int i=0;i<10000;i++)m.Allocate();
        m.Tick(24,false);
        bool expired=true;for(int i=0;i<TransformerFootstepEffectModel.Capacity;i++)expired&=m.Alpha(i)==0;
        Check(expired,"all pooled marks age out after ten thousand allocations");
    }
    static void Main(){Contacts();Pool();Console.WriteLine(checks+" transformer footstep effect model checks passed; surface and render need native visual review.");}
}
