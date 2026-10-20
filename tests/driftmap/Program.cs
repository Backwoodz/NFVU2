using System;
using System.IO;
using Rachel350Z;
using UnityEngine;

static class Program {
    static int checks;
    static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
    static Color32[] pixels;
    static Color32 At(float x,float z) {
        int px=(int)((x-DriftCircuitMapDrawing.MinX)/DriftCircuitMapDrawing.WorldSize*DriftCircuitMapDrawing.Size);
        int py=(int)((z-DriftCircuitMapDrawing.MinZ)/DriftCircuitMapDrawing.WorldSize*DriftCircuitMapDrawing.Size);
        return pixels[py*DriftCircuitMapDrawing.Size+px];
    }
    static void Main() {
        pixels=DriftCircuitMapDrawing.Pixels();
        Check(pixels.Length==512*512,"bounded shared texture");
        foreach(var p in DriftCircuitLayout.Samples) {
            var color=At(p.X,p.Z);Check(color.a==255&&color.r>200||color.a==255&&color.g>200,"entire actual centreline is visible, including closed seam");
        }
        foreach(var p in DriftCircuitPlan.PaintStamps) {
            Check(p.X-p.Radius>DriftCircuitMapDrawing.MinX&&p.X+p.Radius<DriftCircuitMapDrawing.MinX+512&&p.Z-p.Radius>DriftCircuitMapDrawing.MinZ&&p.Z+p.Radius<DriftCircuitMapDrawing.MinZ+512,"texture contains pavement without cropping");
            if(p.DistanceAlong<0)Check(At(p.X,p.Z).a>200,"pit apron and approach included");
        }
        Check(At(0,0).a==0&&At(280,100).a==0,"spawn and course interior stay transparent");
        var start=At(96,24);Check(start.g>220&&start.r<100,"start finish stripe distinguishes the seam");
        int soft=0;foreach(var p in pixels)if(p.a>0&&p.a<255)soft++;
        Check(soft>100,"antialiased border");
        foreach(int size in new[]{2048,256,2049})foreach(float metres in new[]{12f,64f})
        foreach(float zoom in new[]{.002f,.01f,.1f,1f})foreach(float aspect in new[]{1f,1.6f}) {
            float width=240*aspect,height=240;
            double origin=(double)(size/2)/size,span=(double)size*metres;
            float uvX=(float)(origin+96/span-zoom*aspect/2),uvY=(float)(origin+24/span-zoom/2);
            var placement=DriftCircuitMapDrawing.Project(size,metres,uvX,uvY,zoom*aspect,zoom,width,height);
            Check(placement.Visible,"start remains visible at every zoom and map aspect");
            foreach(var p in DriftCircuitLayout.Samples) {
                double actualX=placement.X+(p.X-DriftCircuitMapDrawing.MinX)/512*placement.Width;
                double actualY=placement.Y+(p.Z-DriftCircuitMapDrawing.MinZ)/512*placement.Height;
                double nativeX=(origin+p.X/span-uvX)/(zoom*aspect)*width;
                double nativeY=(origin+p.Z/span-uvY)/zoom*height;
                Check(Math.Abs(actualX-nativeX)<.004&&Math.Abs(actualY-nativeY)<.004,"matches native projection under pan, aspect and zoom");
            }
        }
        Check(!DriftCircuitMapDrawing.Project(2048,12,0,0,.001f,.001f,240,240).Visible,"remote map view hides course");
        // Course centre can be outside the viewport while part of the road is visible.
        var edgeView=DriftCircuitMapDrawing.Project(2048,12,.5f+345f/24576,.5f+180f/24576,40f/24576,40f/24576,240,240);
        Check(edgeView.Visible,"do not use centre-only native pin culling");
        foreach(float invalid in new[]{0,-1,float.NaN,float.PositiveInfinity}) {
            Check(!DriftCircuitMapDrawing.Project(2048,invalid,.5f,.5f,.01f,.01f,240,240).Visible,"invalid pixel size");
            Check(!DriftCircuitMapDrawing.Project(2048,12,.5f,.5f,invalid,.01f,240,240).Visible,"invalid UV size");
        }
        Check(!DriftCircuitMapDrawing.Project(2048,12,float.NaN,.5f,.01f,.01f,240,240).Visible,"invalid UV origin");
        Check(!DriftCircuitMapDrawing.Project(0,12,.5f,.5f,.01f,.01f,240,240).Visible,"uninitialised map");
        Console.WriteLine(checks+" drift map geometry, raster and native-projection checks passed.");
    }
}
