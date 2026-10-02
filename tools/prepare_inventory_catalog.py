import UnityPy,json,dnfile,zlib,hashlib,re
from pathlib import Path
import argparse
parser=argparse.ArgumentParser(description="Extract inventory prefab metadata without changing game files.")
parser.add_argument("game_root",type=Path)
parser.add_argument("--output",type=Path,default=Path("src/StationeersModCreator.Desktop/Content/inventory-catalog.json"))
parser.add_argument("--slot-source",type=Path,required=True,help="Local decompilation of Assets.Scripts.Objects.Slot, used to read its XmlEnum names")
args=parser.parse_args();root=args.game_root/'rocketstation_Data';out=args.output
out.parent.mkdir(parents=True,exist_ok=True)
source=json.loads((out.parent/'catalog.json').read_text());env=UnityPy.load(str(root/'resources.assets'))
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
gen=TypeTreeGenerator(next(iter(env.objects)).assets_file.unity_version);seen=set()
for dll in sorted((root/'Managed').glob('*.dll')):
 p=dnfile.dnPE(str(dll),clr_lazy_load=True)
 if not p.net:continue
 name=str(p.net.mdtables.Module.rows[0].Name)
 if name in seen:continue
 gen.load_dll(dll.read_bytes());seen.add(name)
env.typetree_generator=gen;objects=next(x for x in env.objects if x.path_id==112574).assets_file.objects
keys=['LeftHand','RightHand','Helmet','Glasses','Uniform','Suit','Back','Belt','Brain','Lungs','Liver','Stomach','Heart','Battery','Tool','AirTank','WasteTank','Filter','LifeSupport','Propellent','CreditCard','GasCanister','Cartridge','DataDisk','LiquidCanister','Organ','Circuitboard','Circuit','Crate','Portables','SuitMod']
hashes={zlib.crc32(s.encode()):s for s in keys}
classes=re.findall(r'\[XmlEnum\("([^"]+)"\)\]',args.slot_source.read_text())
rows=[];errors=[]
for p in source['prefabs']:
 if not p['sourcePath'].endswith('/resources.assets'):errors.append({'prefab':p['name'],'reason':'External asset file not extracted'});continue
 try:
  d=objects[p['pathId']].parse_as_dict();slots=[]
  for i,s in enumerate(d.get('Slots',[])):
   h=s.get('StringHash',0);key=s.get('StringKey') or hashes.get(h&0xffffffff,'');loc=s['Location'];attachment=''
   if loc['m_PathID'] and loc['m_FileID']==0:
    t=objects[loc['m_PathID']].parse_as_dict();g=objects[t['m_GameObject']['m_PathID']].parse_as_dict();attachment=g['m_Name']
   slots.append({'index':i,'key':key,'stringHash':h,'type':s['Type'],'typeName':classes[s['Type']],'specificPrefabHashes':s.get('SpecificTypePrefabHashes',[]),'isLocked':bool(s.get('IsLocked')),'isInteractable':bool(s.get('IsInteractable')),'isSwappable':bool(s.get('IsSwappable')),'locationPathId':loc['m_PathID'],'attachmentName':attachment})
  rows.append({'name':p['name'],'pathId':p['pathId'],'className':p['className'],'prefabHash':d.get('PrefabHash',0),'slotType':d.get('SlotType',0),'slotTypeName':classes[d.get('SlotType',0)],'maxQuantity':d.get('MaxQuantity'),'slots':slots})
 except Exception as e:errors.append({'prefab':p['name'],'reason':str(e)})
result={'assetHash':hashlib.file_digest((root/'resources.assets').open('rb'),'sha256').hexdigest(),'assemblyHash':hashlib.file_digest((root/'Managed/Assembly-CSharp.dll').open('rb'),'sha256').hexdigest(),'slotClasses':classes,'prefabs':rows,'errors':errors};out.write_text(json.dumps(result,separators=(',',':')))
print(json.dumps({'prefabs':len(rows),'containers':sum(bool(x['slots']) for x in rows),'errors':errors[:10]},indent=2))
for p in rows:
 if p['name'] in ['Character','ItemEvaSuit','ItemEmergencyEvaSuit','UniformOrangeJumpSuit','ItemJetpackBasic','ItemToolBelt','ItemMiningBelt','ItemHardSuit','ItemHardJetpack']:print(json.dumps(p))
