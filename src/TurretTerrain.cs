using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rachel350Z {
// Single-player crater edits use the game's terrain compiler, including saving,
// grass removal, collision rebuilding and the native eight-metre depth limit.
public static class TurretTerrain {
    static readonly MethodInfo Operation=typeof(TerrainComp).GetMethod("DoOperation",BindingFlags.Instance|BindingFlags.NonPublic,
        null,new[]{typeof(Vector3),typeof(Vector3),typeof(TerrainOp.Settings)},null);
    public static bool Excavate(RaycastHit hit) {
        var surface=hit.collider==null?null:hit.collider.GetComponentInParent<Heightmap>();
        if(surface==null)return false; // A wall, creature or rock is not a terrain hit.
        if(Operation==null)throw new MissingMethodException("TerrainComp.DoOperation API unavailable");
        Vector3 point=hit.point;
        // Use the affected grid vertex too: on slopes it can be below the triangle hit.
        if(surface.GetWorldHeight(point,out float gridHeight))point.y=Mathf.Min(point.y,gridHeight);
        var settings=new TerrainOp.Settings {
            m_raise=true,m_raiseRadius=TurretBlastRules.CraterRadius,m_raiseDelta=-TurretBlastRules.CraterDepth,
            m_raisePower=1.5f,m_square=false,m_level=false,m_smooth=false,
            m_paintCleared=true,m_paintRadius=TurretBlastRules.CraterRadius,m_paintType=TerrainModifier.PaintType.Dirt,
            m_paintHeightCheck=false,m_halfOffset=false
        };
        var maps=new List<Heightmap>();Heightmap.FindHeightmap(point,settings.GetRadius(),maps);
        bool changed=false;
        foreach(var map in maps) {
            var compiler=map.GetAndCreateTerrainCompiler();
            if(compiler==null||!compiler.IsOwner())continue;
            Operation.Invoke(compiler,new object[]{point,Vector3.zero,settings});changed=true;
        }
        return changed;
    }
}
}
