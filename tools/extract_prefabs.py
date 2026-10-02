#!/usr/bin/env python3
"""Decode Thing-derived Unity components and link them to source recipes."""
import collections,json,sqlite3
from pathlib import Path
import UnityPy
from unity_assets import configure_generator

assets=json.load(open('analysis/assets.json'));assemblies=json.load(open('analysis/assemblies.json'))
main=next(a for a in assemblies if a['name']=='Assembly-CSharp');known={'Assets.Scripts.Objects.Thing'}
while True:
 previous=len(known)
 for t in main['types']:
  if t['base'] in known:known.add(t['name'])
 if previous==len(known):break
errors=[];missing_scripts=[];entries=[];script_counts=collections.Counter();decoded=0;skipped=0
with sqlite3.connect('analysis/stationeers.sqlite') as db, Path('analysis/prefab_components.jsonl').open('w') as records:
 db.execute('DROP TABLE IF EXISTS prefabs');db.execute('CREATE TABLE prefabs(source_path TEXT,path_id INTEGER,class_name TEXT,prefab_name TEXT,prefab_hash INTEGER,power_raw REAL,record_json TEXT,PRIMARY KEY(source_path,path_id))');db.execute('CREATE INDEX prefab_name ON prefabs(prefab_name)')
 for asset in assets:
  if not asset['class_counts'].get('114',asset['class_counts'].get(114,0)):continue
  env=UnityPy.load(asset['source']['path']);configure_generator(env,Path('rocketstation_Data/Managed'))
  count=0
  for obj in env.objects:
   if obj.type.name!='MonoBehaviour':continue
   fullname=None
   try:
    head=obj.parse_monobehaviour_head()
    if head.m_Script.m_PathID==0:
     missing_scripts.append({'source_path':asset['source']['path'],'path_id':obj.path_id});continue
    script=head.m_Script.deref_parse_as_object()
    fullname=(script.m_Namespace+'.' if script.m_Namespace else '')+script.m_ClassName
    script_counts[fullname]+=1
    if fullname not in known:skipped+=1;continue
    data=obj.parse_as_dict();decoded+=1;count+=1
    entry={'source_path':asset['source']['path'],'source_sha256':asset['source']['sha256'],'path_id':obj.path_id,'class_name':fullname,'prefab_name':data.get('PrefabName'),'prefab_hash':data.get('PrefabHash'),'power_raw':data.get('UsedPower'),'field_count':len(data)}
    records.write(json.dumps({'identity':entry,'data':data},ensure_ascii=False)+'\n');entries.append(entry)
    db.execute('INSERT INTO prefabs VALUES(?,?,?,?,?,?,?)',(entry['source_path'],entry['path_id'],fullname,entry['prefab_name'],entry['prefab_hash'],entry['power_raw'],json.dumps(data)))
   except Exception as e:errors.append({'source_path':asset['source']['path'],'path_id':obj.path_id,'class_name':fullname,'error':str(e)})
  db.commit();print(json.dumps({'source':asset['source']['path'],'Thing_components':count,'errors_so_far':len(errors)}),flush=True)
 recipes=json.load(open('analysis/recipes.json'));names={x['prefab_name'] for x in entries if x['prefab_name']}
 unresolved=[{'recipe_id':r['id'],'prefab':r['prefab']} for r in recipes if r['prefab'] not in names]
 summary={'Thing_derived_metadata_types':len(known),'decoded_Thing_components':decoded,'distinct_prefab_names':len(names),'other_components_skipped':skipped,'recipe_records':len(recipes),'recipes_with_decoded_prefab':len(recipes)-len(unresolved),'unresolved_recipe_prefabs':unresolved,'errors':errors,'missing_scripts':missing_scripts,'scope':'All nine indexed serialized files; Thing inheritance via direct TypeDef/TypeRef metadata. Generic TypeSpec inheritance is not decoded.'}
 Path('analysis/prefabs.json').write_text(json.dumps(entries,indent=2)+'\n');Path('analysis/prefab_summary.json').write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps({k:v for k,v in summary.items() if k not in ['errors','unresolved_recipe_prefabs']},indent=2))
 if errors:raise SystemExit(1)
