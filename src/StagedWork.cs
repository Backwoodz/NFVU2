using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace Rachel350Z {
// Advances only on the caller's thread. A step is indivisible; the budget is soft.
public sealed class StagedWork : IDisposable {
    public const int NextFrame=1;
    IEnumerator<int> steps;
    public double SlowestStepMilliseconds {get;private set;}
    public StagedWork(IEnumerable<int> work){steps=work.GetEnumerator();}
    public bool Tick(double budgetMilliseconds) {
        if(steps==null)return false;
        var frame=Stopwatch.StartNew();
        do {
            double before=frame.Elapsed.TotalMilliseconds;
            bool more;
            try {more=steps.MoveNext();}
            catch {Dispose();throw;}
            SlowestStepMilliseconds=Math.Max(SlowestStepMilliseconds,frame.Elapsed.TotalMilliseconds-before);
            if(!more){Dispose();return false;}
            if(steps.Current==NextFrame)break;
        }while(frame.Elapsed.TotalMilliseconds<budgetMilliseconds);
        return true;
    }
    public void Dispose(){var previous=steps;steps=null;previous?.Dispose();}
    public static IEnumerable<int> Optional(IEnumerable<int> work,Action<Exception> failed) {
        using(var steps=work.GetEnumerator())while(true) {
            bool more;
            try {more=steps.MoveNext();}
            catch(Exception error){failed(error);yield break;}
            if(!more)yield break;
            yield return steps.Current;
        }
    }
}
}
