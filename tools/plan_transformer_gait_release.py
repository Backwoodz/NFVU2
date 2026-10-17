"""Find a collision-reviewed withdrawal path for the upper side armor."""
from pathlib import Path
import sys,heapq,itertools,json
sys.argv.extend(['--no-clearance','--articulated-armor'])
source=Path(__file__).with_name('preview_transformer_locomotion.py').read_text()
exec(source[:source.index('\nvisible=')])
helpers=(ROOT/'tools/fit_transformer_paths.py').read_text()
exec(helpers[helpers.index('def geometry('):helpers.index('\nscene.frame_set(1)')])
groups=defaultdict(list)
for obj in bpy.data.collections['01 Original 350Z surfaces'].objects:groups[obj['module']].append(obj)
for obj in bpy.data.collections['02 Authored mechanism - BLOCKOUT'].objects:
    if obj.get('continuous_unsegmented'):groups[obj.name].append(obj)
groups['RIDER']=[o for o in bpy.data.collections['05 Rider - scale reference, NOT game avatar'].objects if o.type=='MESH']
gait(0,0)
geometry_data={k:geometry(v) for k,v in groups.items()}
fixed={k:placed(v,np.zeros(3)) for k,v in geometry_data.items()}
obstacles=[k for k in groups if k not in ('sill1_L','sill1_R')]
baseline={(s,k):hits(fixed[s],fixed[k]) for s in ('sill1_L','sill1_R') for k in obstacles}
def depth(a,b,contacts):
    if not contacts:return 0.
    ia,ib=np.array(list(contacts)).T
    da=np.einsum('ijk,ik->ij',a[1][ia]-b[1][ib,0,None],b[2][ib])
    db=np.einsum('ijk,ik->ij',b[1][ib]-a[1][ia,0,None],a[2][ia])
    return float(np.minimum.reduce([-da.min(axis=1),da.max(axis=1),-db.min(axis=1),db.max(axis=1)]).max())
baseline_depth={(s,k):depth(fixed[s],fixed[k],contacts) for (s,k),contacts in baseline.items()}
cache={}
def collisions(p):
    p=tuple(round(x,5) for x in p)
    if p not in cache:
        count=0
        for side,sign in (('L',1),('R',-1)):
            name='sill1_'+side;shape=placed(geometry_data[name],np.array(p)*[sign,1,1])
            for k in obstacles:
                contacts=hits(shape,fixed[k])
                if not contacts:continue
                if not baseline[name,k] or depth(shape,fixed[k],contacts)>baseline_depth[name,k]+.0015:count+=len(contacts)
        cache[p]=count
    return cache[p]
directions=list(itertools.product((-1,0,1),repeat=3));directions.remove((0,0,0))
print('INITIAL DIRECTIONS',[(v,collisions(tuple(x*.01 for x in v))) for v in directions],flush=True)
step=.04;target=(10,0,0);start=(0,0,0)
front=[(0,0,start)];distance={start:0};previous={}
found=None
while front and len(distance)<16000:
    _,cost,node=heapq.heappop(front)
    if cost>distance[node]+1e-7:continue
    if node==target:found=node;break
    for d in directions:
        next_node=tuple(a+b for a,b in zip(node,d))
        if not (-3<=next_node[0]<=15 and -10<=next_node[1]<=10 and -5<=next_node[2]<=12):continue
        new=cost+sum(x*x for x in d)**.5
        if new>=distance.get(next_node,1e9):continue
        if any(collisions(tuple((a+b*u)*step for a,b in zip(node,d))) for u in (.25,.5,.75,1)):continue
        distance[next_node]=new;previous[next_node]=node
        heuristic=sum((a-b)**2 for a,b in zip(next_node,target))**.5
        heapq.heappush(front,(new+heuristic,new,next_node))
    if len(distance)%500==0:print('SEARCH',len(distance),len(cache),flush=True)
path=[]
if found:
    while found!=start:path.append([v*step for v in found]);found=previous[found]
    path.append([0,0,0]);path.reverse()
(OUT/'gait-release-path.json').write_text(json.dumps(dict(path=path,visited=len(distance),tested=len(cache)),indent=2))
print('RELEASE PATH',path,'visited',len(distance),'tested',len(cache),flush=True)
