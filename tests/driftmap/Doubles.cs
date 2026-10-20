namespace UnityEngine {
public struct Color {
    public float r,g,b,a;
    public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
    public static Color clear=>new Color(0,0,0,0);
}
public struct Color32 {
    public byte r,g,b,a;
    public static implicit operator Color32(Color c)=>new Color32{r=Byte(c.r),g=Byte(c.g),b=Byte(c.b),a=Byte(c.a)};
    static byte Byte(float n)=>(byte)System.Math.Round(System.Math.Max(0,System.Math.Min(1,n))*255);
}
}
