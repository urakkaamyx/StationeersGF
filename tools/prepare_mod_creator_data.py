#!/usr/bin/env python3
"""Build the desktop tool's bundled catalog and game artwork from extracted sources."""
import collections,json,re,sqlite3,zipfile
from pathlib import Path
import xml.etree.ElementTree as E
import UnityPy
root=Path('.');out=root/'src/StationeersModCreator.Desktop/Content';out.mkdir(parents=True,exist_ok=True)
con=sqlite3.connect('analysis/stationeers.sqlite');recipes=json.load(open('analysis/recipes.json'))
mapping={
'AutolatheRecipes':('Autolathe','StructureAutolathe','FABRICATION'),
'ElectronicsPrinterRecipes':('Electronics Printer','StructureElectronicsPrinter','FABRICATION'),
'HydraulicPipeBenderRecipes':('Hydraulic Pipe Bender','StructureHydraulicPipeBender','FABRICATION'),
'ToolManufactoryRecipes':('Tool Manufactory','StructureToolManufactory','FABRICATION'),
'RocketManufactoryRecipes':('Rocket Manufactory','StructureRocketManufactory','FABRICATION'),
'SecurityPrinterRecipes':('Security Printer','StructureSecurityPrinter','FABRICATION'),
'TerraformingManufactoryRecipes':('Terraforming Manufactory','StructureTerraformingManufactory','FABRICATION'),
'FurnaceRecipes':('Furnace','StructureFurnace','SMELTING'),
'AdvancedFurnaceRecipes':('Advanced Furnace','StructureAdvancedFurnace','SMELTING'),
'ArcFurnaceRecipes':('Arc Furnace','StructureArcFurnace','SMELTING'),
'CentrifugeRecipes':('Centrifuge','StructureCentrifuge','PROCESSING'),
'ChemistryRecipes':('Chemistry Station','ApplianceChemistryStation','PROCESSING'),
'PackagingMachineRecipes':('Packaging Machine','AppliancePackagingMachine','PROCESSING'),
'AutomatedOvenRecipes':('Automated Oven','StructureAutomatedOven','FOOD'),
'MicrowaveRecipes':('Microwave','ApplianceMicrowave','FOOD'),
'IngotRecipes':('Reagent Definitions','ItemIronIngot','DEFINITIONS')}
def label(name):return re.sub(r'(?<=[a-z0-9])(?=[A-Z])',' ',re.sub(r'^(ItemKit|Item|Structure|Appliance)','',name))
def node(x):
 el=E.Element(x['tag'],x['attributes']);el.text=x['text'] or None
 for c in x['children']:el.append(node(c))
 return el
asset_hashes={a['source']['path']:a['source']['sha256'] for a in json.load(open('analysis/assets.json'))}
assets={};prefabs=[];allprefabs={}
for row in con.execute('select source_path,path_id,class_name,prefab_name,record_json from prefabs order by source_path,path_id'):
 source,pid,cls,name,raw=row
 if name not in allprefabs:allprefabs[name]=(source,pid,cls,json.loads(raw))
for name,(source,pid,cls,data) in allprefabs.items():
 thumbnail=data.get('Thumbnail',{});assetkey=name+'.png'
 if thumbnail.get('m_PathID'):assets[name]=(source,thumbnail['m_FileID'],thumbnail['m_PathID'])
 fields=[]
 supported={'SurfaceAreaScale':'SurfaceAreaScale','ThermodynamicsScale':'ThermodynamicsScale','SolarHeatingScale':'SolarHeatingScale','ShatterTemperature':'shatterTemperature','FlashpointTemperature':'flashpointTemperature','AutoignitionTemperature':'autoignitionTemperature','BurnTime':'BurnTime','EnergyReleasedWhenBurning':'EnergyReleasedWhenBurned','MaxQuantity':'MaxQuantity'}
 for target,field in supported.items():
  value=data.get(field)
  if not isinstance(value,(int,float)):continue
  fields.append({'name':target,'value':str(value),'modType':'StackableModData' if target=='MaxQuantity' else 'ThingModData'})
 prefabs.append({'name':name,'displayName':label(name),'className':cls,'asset':assetkey,'sourcePath':source,'sourceHash':asset_hashes[source],'pathId':pid,'attributes':fields})
rows=[]
for r in recipes:
 el=node(r['record']);fields=[]
 def walk(e,path):
  for c in e:
   part=path+'/'+c.tag if path else c.tag
   if len(c):walk(c,part)
   else:
    try:float(c.text)
    except (ValueError,TypeError):continue
    group='PROCESS' if part in ['Time','Energy'] else 'CONDITIONS' if '/' in part else 'MATERIALS'
    fields.append({'path':part,'label':label(c.tag),'value':c.text,'group':group})
 walk(el.find('Recipe'),'')
 rows.append({'id':r['id'],'prefab':r['prefab'],'displayName':label(r['prefab']),'section':r['section'],'sourceFile':Path(r['source_path']).name,'sourceHash':r['source_sha256'],'sourcePath':r['source_path'],'originalXml':E.tostring(el,encoding='unicode'),'asset':r['prefab']+'.png','fields':fields})
machines=[{'id':section,'name':v[0],'prefab':v[1],'category':v[2],'asset':v[1]+'.png','recipeCount':sum(r['section']==section for r in recipes)} for section,v in mapping.items()]
errors=[];exported=[]
with zipfile.ZipFile(out/'artwork.zip','w',zipfile.ZIP_DEFLATED) as z:
 for source in sorted({v[0] for v in assets.values()}):
  env=UnityPy.load(source);file=next(iter(env.files.values()))
  for name,(src,fid,pid) in assets.items():
   if src!=source:continue
   try:
    from UnityPy.classes import PPtr
    data=PPtr(m_FileID=fid,m_PathID=pid,assetsfile=file).deref_parse_as_object();import io
    buf=io.BytesIO();data.image.save(buf,format='PNG');z.writestr(name+'.png',buf.getvalue());exported.append(name)
   except Exception as e:errors.append({'prefab':name,'error':str(e)})
  print(json.dumps({'source':source,'artwork_exported':len(exported),'errors':len(errors)}),flush=True)
 for p in (root/'rocketstation_Data/StreamingAssets/Images/SpaceMapImages/Planets').glob('*.png'):z.write(p,'planets/'+p.name)
(out/'catalog.json').write_text(json.dumps({'snapshot':'2026-10-02','unityVersion':'2022.3.62f3','machines':machines,'recipes':rows,'prefabs':prefabs},ensure_ascii=False,separators=(',',':')))
(out/'artwork-manifest.json').write_text(json.dumps({'exported':len(exported),'errors':errors},indent=2))
print(json.dumps({'recipes':len(rows),'machines':len(machines),'prefabs':len(prefabs),'artwork':len(exported),'errors':errors[:10],'artwork_bytes':(out/'artwork.zip').stat().st_size},indent=2))
