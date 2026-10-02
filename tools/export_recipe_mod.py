#!/usr/bin/env python3
"""Export one XML recipe override as a native Stationeers mod; never edit source."""
import argparse, copy, json, re, zipfile
from pathlib import Path
from decimal import Decimal
import xml.etree.ElementTree as E
from scan_game import digest

def export(root,source,section,prefab,changes,out,name):
 if not re.fullmatch(r'[A-Za-z0-9_-]+',name):raise ValueError('Mod name must be a safe folder name')
 root=root.resolve();source=(root/source).resolve()
 if not source.is_relative_to(root):raise ValueError('Source must be inside game root')
 out=out.resolve()
 if out.is_relative_to(root/'rocketstation_Data') or out.is_relative_to(root/'BepInEx'):raise ValueError('Output must be outside game directories')
 if out.exists():raise ValueError('Output directory already exists')
 before=digest(source);doc=E.parse(source).getroot();matches=[r for s in doc if s.tag==section for r in s.findall('RecipeData') if r.findtext('PrefabName')==prefab]
 if len(matches)!=1:raise ValueError(f'Expected one recipe, found {len(matches)}')
 record=copy.deepcopy(matches[0]);recipe=record.find('Recipe');journal=[]
 for field,value in changes.items():
  if not re.fullmatch(r'[A-Za-z][A-Za-z0-9]*',field):raise ValueError('Only direct numeric recipe fields supported')
  decimal=Decimal(value)
  if not decimal.is_finite() or decimal<0:raise ValueError('Value must be finite and nonnegative')
  nodes=recipe.findall(field)
  if len(nodes)!=1 or len(nodes[0]):raise ValueError('Unknown or nested recipe field: '+field)
  journal.append({'field':field,'old':nodes[0].text,'new':value});nodes[0].text=value
 override=E.Element('GameData');E.SubElement(override,section).append(record)
 mod=out/name;(mod/'GameData').mkdir(parents=True);(mod/'About').mkdir()
 E.indent(override);E.ElementTree(override).write(mod/'GameData'/source.name,encoding='utf-8',xml_declaration=True)
 about=E.Element('ModMetadata')
 for k,v in {'Name':name,'Author':'StationeersGF','Version':'0.1.0','Description':'Selective recipe override exported from source game data.'}.items():E.SubElement(about,k).text=v
 E.indent(about);E.ElementTree(about).write(mod/'About/About.xml',encoding='utf-8',xml_declaration=True)
 provenance={'source_path':source.relative_to(root).as_posix(),'source_sha256':before,'section':section,'prefab':prefab,'changes':journal,'runtime_tested':False,'load_order_note':'The shipped ExampleMod readme warns duplicate recipes are discarded; set mod load order and verify the active recipe in game.'}
 (mod/'About/extraction.json').write_text(json.dumps(provenance,indent=2)+'\n')
 with zipfile.ZipFile(out/(name+'.zip'),'w',zipfile.ZIP_DEFLATED) as z:
  for f in sorted(mod.rglob('*')):
   if f.is_file():z.write(f,f.relative_to(out))
 assert digest(source)==before,'Source changed during export'
 print(json.dumps(provenance,indent=2))

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--game-root',type=Path,default=Path('.'));p.add_argument('--source',type=Path,required=True);p.add_argument('--section',required=True);p.add_argument('--prefab',required=True);p.add_argument('--set',action='append',required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--name',required=True);a=p.parse_args()
 changes={}
 for v in a.set:
  k,value=v.split('=',1)
  if k in changes:raise ValueError('Duplicate change: '+k)
  changes[k]=value
 export(a.game_root,a.source,a.section,a.prefab,changes,a.output,a.name)
