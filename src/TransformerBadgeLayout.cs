namespace Rachel350Z {
// Rear view, read left to right. Coordinates are metres in the hatch face.
public struct TransformerBadgeStroke {
    public float Ax,Ay,Bx,By;
    public TransformerBadgeStroke(float ax,float ay,float bx,float by){Ax=ax;Ay=ay;Bx=bx;By=by;}
}
public static class TransformerBadgeLayout {
    public const float CenterY=2.80f,Scale=.65f,TubeWidth=.020f,ChannelWidth=.034f;
    public static readonly TransformerBadgeStroke[] Strokes={
        new TransformerBadgeStroke(-.41f,.12f,-.21f,.12f),
        new TransformerBadgeStroke(-.43f,.10f,-.43f,.02f),
        new TransformerBadgeStroke(-.41f,0,-.21f,0),
        new TransformerBadgeStroke(-.19f,-.02f,-.19f,-.10f),
        new TransformerBadgeStroke(-.41f,-.12f,-.21f,-.12f),
        new TransformerBadgeStroke(-.12f,.12f,-.12f,-.12f),
        new TransformerBadgeStroke(-.10f,-.12f,.12f,-.12f),
        new TransformerBadgeStroke(.19f,.12f,.31f,0),
        new TransformerBadgeStroke(.43f,.12f,.31f,0),
        new TransformerBadgeStroke(.31f,0,.31f,-.12f)
    };
}
}
