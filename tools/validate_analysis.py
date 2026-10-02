#!/usr/bin/env python3
"""Independent consistency checks plus a real native-mod export round trip."""
import json,sqlite3,tempfile,zipfile
from pathlib import Path
import xml.etree.ElementTree as E
from export_recipe_mod import export
from scan_game import digest

def main():
 summary=json.load(open('analysis/summary.json'));checks={};assert not summary['errors'];checks['parse_errors_zero']=True
 with sqlite3.connect('analysis/stationeers.sqlite') as db:
  assert db.execute('pragma integrity_check').fetchone()[0]=='ok';checks['sqlite_integrity']=True
  for table,key in [('recipes','recipe_count'),('types','type_count'),('methods','method_count'),('fields','field_count'),('asset_objects','serialized_object_count')]:assert db.execute('select count(*) from '+table).fetchone()[0]==summary[key]
 checks['database_counts_match']=True
 recipes=json.load(open('analysis/recipes.json'));count=0
 for p in Path('rocketstation_Data').rglob('*.xml'):count+=len(E.parse(p).getroot().findall('./*/RecipeData/Recipe'))
 assert count==len(recipes);assert len({x['id'] for x in recipes})==len(recipes);checks['xml_recipe_count_independent']=True
 for a in json.load(open('analysis/assemblies.json')):
  assert sum(len(t['methods']) for t in a['types'])==a['method_count'];assert sum(len(t['fields']) for t in a['types'])==a['field_count']
 checks['managed_member_ownership']=True
 v=json.load(open('analysis/unitypy_validation.json'));assert v['passed'];checks['unitypy_independent_asset_validation']=True
 source=Path('rocketstation_Data/StreamingAssets/Data/autolathe.xml');before=digest(source)
 with tempfile.TemporaryDirectory() as td:
  out=Path(td)/'override';export(Path('.'),source,'AutolatheRecipes','ItemIronFrames',{'Iron':'5'},out,'ValidationOnly')
  with zipfile.ZipFile(out/'ValidationOnly.zip') as z:
   tree=E.fromstring(z.read('ValidationOnly/GameData/autolathe.xml'));assert len(tree.findall('./AutolatheRecipes/RecipeData'))==1;assert tree.findtext('./AutolatheRecipes/RecipeData/Recipe/Iron')=='5';E.fromstring(z.read('ValidationOnly/About/About.xml'))
  for field,value in [('Iron','NaN'),('Iron','-1'),('UnknownField','1')]:
   try:export(Path('.'),source,'AutolatheRecipes','ItemIronFrames',{field:value},Path(td)/field/value,'Rejected')
   except ValueError:pass
   else:raise AssertionError('Invalid override accepted')
 assert digest(source)==before;checks['recipe_mod_zip_and_source_preservation']=True;checks['invalid_override_rejection']=True
 if Path('analysis/prefab_summary.json').exists():
  ps=json.load(open('analysis/prefab_summary.json'));assert all(e['class_name']=='Assets.Scripts.Objects.Book' and 'Expected to read' in e['error'] for e in ps['errors']);entries=json.load(open('analysis/prefabs.json'));assert len(entries)==ps['decoded_Thing_components'];assert any(e['prefab_name']=='StructureAutolathe' for e in entries);checks['decoded_prefab_consistency']=True
 import py_compile
 for script in Path('tools').glob('*.py'):py_compile.compile(str(script),doraise=True)
 checks['python_compile']=True
 report={'passed':True,'checks':checks,'runtime_tested':False,'known_prefab_decode_errors':json.load(open('analysis/prefab_summary.json'))['errors'] if Path('analysis/prefab_summary.json').exists() else []};Path('analysis/validation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
