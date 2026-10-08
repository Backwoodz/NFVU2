using System;
using Rachel350Z;
using UnityEngine;
class TerrainTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Main(){
  var map=new Heightmap();var adjacent=new Heightmap();Heightmap.maps.Add(map);Heightmap.maps.Add(adjacent);
  Check(!TurretTerrain.Excavate(new RaycastHit()),"no ray hit never edits terrain");
  Check(!TurretTerrain.Excavate(new RaycastHit{collider=new Collider()}),"wall rock or character hit never digs through to underlying ground");
  Check(map.compiler.operations==0&&adjacent.compiler.operations==0,"blocked ground leaves both tiles untouched");
  var hit=new RaycastHit{collider=new Collider{map=map},point=new Vector3(2,12,3)};
  Check(TurretTerrain.Excavate(hit),"direct terrain hit invokes native operation");
  Check(map.compiler.operations==1&&adjacent.compiler.operations==1,"crater crossing a tile seam updates both tiles");
  Check(map.compiler.point.y==10,"slope operation uses lower grid height so each hit can dig");
  var settings=map.compiler.settings;
  Check(settings.m_raise&&settings.m_raiseDelta<0&&!settings.m_level&&!settings.m_smooth,"crater only lowers terrain and does not level or smooth surroundings");
  Check(settings.m_paintCleared&&!settings.m_square&&Heightmap.queriedRadius==settings.m_raiseRadius,"round crater clears grass within its own radius");
  TurretTerrain.Excavate(hit);Check(map.compiler.operations==2,"repeated ground hits are not suppressed by a global cooldown");
  map.compiler.owner=false;adjacent.compiler.owner=false;
  Check(!TurretTerrain.Excavate(hit)&&map.compiler.operations==2,"non-owned compilers are not modified");
  Console.WriteLine(checks+" terrain adapter checks passed with doubles; native terrain saving and mesh rebuilding require an in-game test.");
 }
}
