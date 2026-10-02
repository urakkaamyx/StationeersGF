#!/usr/bin/env python3
"""Selective UnityPy export and independent object-table verification."""
import argparse, collections, hashlib, json, re
from pathlib import Path
import UnityPy
from scan_game import digest, clr


def verify(index):
 report={'unitypy_version':UnityPy.__version__,'assets':[]}
 for asset in json.loads(index.read_text()):
  path=Path(asset['source']['path']);env=UnityPy.load(str(path))
  objs=list(env.objects);ids={o.path_id for o in objs};counts=dict(collections.Counter(o.type.value for o in objs))
  expected={o['path_id'] for o in asset['objects']};expected_counts={int(k):v for k,v in asset['class_counts'].items()}
  name_mismatches=[]
  for record in asset['objects']:
   if record['class_id'] not in (1,115):continue
   obj=next(iter(env.files.values())).objects.get(record['path_id'])
   if obj is None:raise ValueError('Missing object')
   name=obj.peek_name()
   if name!=record.get('name'):name_mismatches.append(record['path_id'])
  entry={'source':str(path),'count':len(objs),'path_ids_match':ids==expected,'class_counts_match':counts==expected_counts,'name_mismatches':name_mismatches}
  report['assets'].append(entry);print(json.dumps(entry),flush=True)
 report['passed']=all(x['path_ids_match'] and x['class_counts_match'] and not x['name_mismatches'] for x in report['assets'])
 Path('analysis/unitypy_validation.json').write_text(json.dumps(report,indent=2)+'\n')
 if not report['passed']:raise SystemExit('UnityPy cross-check failed')


def configure_generator(env,managed):
 from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
 first=next(iter(env.objects));generator=TypeTreeGenerator(first.assets_file.unity_version)
 assemblies={}
 for dll in sorted(managed.glob('*.dll')):
  metadata=clr(dll)
  if metadata is None:continue
  identity=metadata['module_name']
  if identity not in assemblies or dll.name==identity:assemblies[identity]=dll
 for identity,dll in sorted(assemblies.items()):generator.load_dll(dll.read_bytes())
 env.typetree_generator=generator
 return generator


def export(source,out,kind,path_id,limit,managed):
 env=UnityPy.load(str(source));out.mkdir(parents=True,exist_ok=True)
 if managed:configure_generator(env,managed)
 manifest={'source':str(source),'sha256':digest(source),'unitypy_version':UnityPy.__version__,'exports':[],'errors':[]}
 for obj in env.objects:
  if obj.type.name!=kind or path_id is not None and obj.path_id!=path_id:continue
  if len(manifest['exports'])>=limit:break
  try:
   if kind=='MonoBehaviour':
    data=obj.parse_as_dict();name=data.get('m_Name','');payload=json.dumps(data,indent=2).encode();ext='.json'
   else:
    data=obj.parse_as_object();name=getattr(data,'m_Name','');payload=None;ext={'Texture2D':'.png','Sprite':'.png','Mesh':'.obj','TextAsset':'.bin'}[kind]
   safe=re.sub(r'[^A-Za-z0-9_.-]+','_',name)[:100] or 'unnamed';dest=out/(str(obj.path_id)+'_'+safe+ext)
   if kind in ('Texture2D','Sprite'):data.image.save(dest)
   elif kind=='Mesh':dest.write_text(data.export(),newline='')
   elif kind=='TextAsset':dest.write_bytes(data.m_Script.encode('utf-8',errors='surrogateescape') if isinstance(data.m_Script,str) else data.m_Script)
   else:dest.write_bytes(payload)
   manifest['exports'].append({'path_id':obj.path_id,'type':kind,'name':name,'file':dest.name})
  except Exception as e:manifest['errors'].append({'path_id':obj.path_id,'error':str(e)})
 (out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest,indent=2))
 if manifest['errors'] or not manifest['exports']:raise SystemExit(1)

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);s=p.add_subparsers(dest='command',required=True)
 v=s.add_parser('verify');v.add_argument('--index',type=Path,default=Path('analysis/assets.json'))
 e=s.add_parser('export');e.add_argument('source',type=Path);e.add_argument('--output',type=Path,required=True);e.add_argument('--type',choices=['Texture2D','Sprite','Mesh','TextAsset','MonoBehaviour'],required=True);e.add_argument('--path-id',type=int);e.add_argument('--limit',type=int,default=1);e.add_argument('--managed',type=Path)
 args=p.parse_args()
 if args.command=='verify':verify(args.index)
 else:export(args.source,args.output,args.type,args.path_id,args.limit,args.managed)
