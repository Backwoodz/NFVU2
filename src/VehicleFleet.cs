using System;
using System.Collections;
using System.Collections.Generic;
namespace Rachel350Z {
// Keeps the UI/driver selection stable while visiting every parked and driven car.
public sealed class VehicleFleet<T> : IEnumerable<T> where T:class {
    readonly List<T> items=new List<T>();
    public T Current {get;set;}
    public const int Capacity=4;
    public int Count=>items.Count;
    public VehicleFleet(T empty){Current=empty;}
    public void Add(T item) {
        if(item==null||items.Contains(item))throw new ArgumentException("Duplicate or null vehicle");
        if(items.Count>=Capacity)throw new InvalidOperationException("Garage is full");
        items.Add(item);
    }
    public bool Contains(T item)=>items.Contains(item);
    // Removing a car must not switch Current while its resources are still being disposed.
    public bool Remove(T item)=>items.Remove(item);
    public T Nearest(Func<T,float> squaredDistance) {
        T closest=null;float distance=float.PositiveInfinity;
        foreach(var item in items){float next=squaredDistance(item);if(next>=0&&next<distance){distance=next;closest=item;}}
        return closest;
    }
    public void Visit(Action action) {
        var selected=Current;
        try {foreach(var item in items.ToArray()){if(!items.Contains(item))continue;Current=item;action();}}
        finally {Current=selected;}
    }
    public IEnumerator<T> GetEnumerator()=>items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}
}
