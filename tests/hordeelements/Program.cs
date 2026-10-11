using System;
using System.Reflection;
using UnityEngine;
using Rachel350Z;
class Program {
 static int checks;static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static Character Enemy(float x,bool marked=true) {
  var go=new GameObject();var enemy=go.AddComponent<Character>();enemy.point=new Vector3(x,0,0);
  if(marked)go.AddComponent<HordeEnemyMarker>();go.AddComponent<HordeElementStatus>();go.AddComponent<Collider>();go.AddComponent<MonsterAI>();return enemy;
 }
 static void Main() {
  var owner=Enemy(-10,false);owner.player=true;
  var first=Enemy(0);var second=Enemy(2);var third=Enemy(4);var fourth=Enemy(6);var fifth=Enemy(8);
  var lightningBody=first.gameObject.AddComponent<MeshRenderer>();lightningBody.block.SetColor("_Color",new Color(.2f,.3f,.4f));
  var tame=Enemy(.1f);tame.tame=true;var player=Enemy(.2f);player.player=true;var unmarked=Enemy(.3f,false);var dead=Enemy(.4f);dead.dead=true;
  var combat=new HordeElementCombat();var impact=new RaycastHit{collider=first.GetComponent<Collider>(),point=first.point};
  combat.Hit(impact,owner,HordePact.Storm);
  Check(lightningBody.block.colors["_Color"].b==.4f&&!lightningBody.block.colors.ContainsKey("_EmissionColor"),"lightning preserves enemy materials while retaining arc effects");
  Check(first.hits==1&&second.hits==1&&third.hits==1&&fourth.hits==1&&fifth.hits==0,"one hit chains to exactly three distinct additional targets");
  Check(first.lightning==18&&second.lightning<first.lightning&&third.lightning<second.lightning,"native lightning damage falls with each jump");
  Check(tame.hits+player.hits+unmarked.hits+dead.hits==0,"chain excludes tame, player, unrelated and dead actors");
  combat.Hit(impact,owner,HordePact.Storm);Check(first.hits==1,"rapid rounds share one chain cooldown");
  combat.Tick(.41f);var wall=new GameObject().AddComponent<Collider>();Physics.covers=new[]{new RaycastHit{collider=wall}};
  combat.Hit(impact,owner,HordePact.Storm);Check(first.hits==2&&second.hits==1,"solid cover blocks a chain jump");
  Physics.covers=Array.Empty<RaycastHit>();combat.Tick(.41f);
  second.point=third.point=fourth.point=fifth.point=new Vector3(20,0,0);
  combat.Hit(impact,owner,HordePact.Storm);Check(first.hits==3&&second.hits==1,"chain cannot jump beyond eight metres");
  for(int i=0;i<30;i++){combat.Tick(.5f);combat.Hit(impact,owner,HordePact.Storm);}
  Check(LineRenderer.All.Count<=16,"lightning visual pool stays bounded across many shots");
  combat.Tick(.5f);Check(LineRenderer.All.TrueForAll(x=>!x.enabled),"arcs disappear after their short lifetime");
  int before=UnityEngine.Object.Destroyed;combat.Dispose();Check(UnityEngine.Object.Destroyed>before,"cleanup destroys arc objects and material");
  var body=first.gameObject.AddComponent<SkinnedMeshRenderer>();var original=new Color(.2f,.3f,.4f);body.block.SetColor("_Color",original);
  var ice=first.GetComponent<HordeElementStatus>();ice.Freeze();
  Check(ice.Frozen&&first.m_speed==0&&first.m_flyFastSpeed==0&&first.animationFreeze>0,"ice freezes ground, flying movement and attack animation");
  Check(body.block.colors["_Color"].b>body.block.colors["_Color"].r,"frozen body gets a blue-white material override");
  var prefix=typeof(HordeFrozenAI).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);var ai=first.GetComponent<MonsterAI>();var args=new object[]{ai,true};
  Check(!(bool)prefix.Invoke(null,args)&&!(bool)args[1]&&ai.stopped==1,"frozen AI cannot acquire a new attack or move");
  ice.Tick(1.3f);Check(!ice.Frozen&&first.m_speed==4&&first.m_flyFastSpeed==12,"thaw restores original movement speeds");
  Check(body.block.colors["_Color"].r==original.r&&!body.block.colors.ContainsKey("_EmissionColor"),"thaw restores original material properties exactly");
  ice.Freeze();Check(!ice.Frozen,"repeated rounds cannot bypass thaw immunity");
  var boss=Enemy(40);var bossIce=boss.GetComponent<HordeElementStatus>();bossIce.Boss=true;bossIce.Freeze();
  Check(!bossIce.Frozen&&Math.Abs(boss.m_speed-2.6f)<.001f&&boss.animationFreeze==0,"boss is slowed by 35 percent without locking attacks");
  typeof(HordeElementStatus).GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bossIce,null);
  Check(boss.m_speed==4&&boss.m_flyFastSpeed==12,"early cleanup restores boss movement too");
  Console.WriteLine(checks+" elemental combat checks passed (controlled native API doubles).");
 }
}
