#!/usr/bin/env python3
"""Prepare native definition catalogs; never alter source game files."""
from pathlib import Path
import xml.etree.ElementTree as E
import hashlib,json,re
root=Path('.');stream=root/'rocketstation_Data/StreamingAssets';rows=[]
categories={'World':'Worlds','StartCondition':'Starting conditions','StartLocation':'Starting conditions','Spawn':'Spawn packages','DifficultySetting':'Difficulty','WeatherEvent':'Weather','OreVein':'Mining','Minables':'Mining','DeepMinables':'Mining','LifeRequirements':'Plant requirements','CustomPlant':'Plants','CustomSeed':'Plants','WorldObjective':'Objectives','Objective':'Objectives','RoomTypeRule':'Room rules','ContactSlot':'Trading','Trader':'Trading','Buy':'Trading','Sell':'Trading','SpaceMap':'Space maps','CelestialBody':'Celestial bodies','Blueprint':'Blueprints','GHGIndex':'Terraforming','Icon':'Artwork references','ItemReplacement':'Replacements'}
# Only policies traced in the snapshot's managed loader are writable initially.
verified={'StartCondition','StartLocation','Spawn','DifficultySetting','World','WeatherEvent','OreVein','LifeRequirements'}
wrappers={'WorldSettings','DifficultySettings','CelestialBodies'}
for path in sorted(list((stream/'Data').glob('*.xml'))+list((stream/'Worlds').rglob('*.xml'))):
 data=path.read_bytes();doc=E.fromstring(data);sha=hashlib.sha256(data).hexdigest()
 for index,top in enumerate(doc):
  items=list(top) if top.tag in wrappers else [top]
  for childIndex,n in enumerate(items):
   if n.tag not in categories:continue
   key=n.get('Id') or n.get('Name') or n.get('RoomType') or n.get('Gas')
   if not key:continue
   asset='LanderMkII.png' if n.tag in ['StartCondition','Spawn'] else 'planets/StatMars.png'
   dyn=next(n.iter('DynamicThing'),None);item=next(n.iter('Item'),None)
   if dyn is not None:asset=dyn.get('Id','LanderMkII')+'.png'
   elif item is not None:asset=item.get('Id','ItemIronFrames')+'.png'
   if n.tag=='World':asset='planets/'+{'Mars2':'StatMars','Lunar':'StatMoon','Europa3':'StatEuropa','Venus':'StatVenus','Vulcan':'StatVulkan','Vulcan2':'StatVulkan','MimasHerschel':'StatMimas'}.get(key,'StatMars')+'.png'
   if n.tag=='Spawn' and not (n.findall('DynamicThing') or n.findall('Item')):asset='ItemEvaSuit.png'
   rows.append({'key':str(path)+'|'+str(index)+'|'+str(childIndex),'id':key,'tag':n.tag,'category':categories[n.tag],'wrapper':top.tag if top.tag in wrappers else '', 'sourcePath':str(path),'sourceHash':sha,'xml':E.tostring(n,encoding='unicode'),'asset':asset,'canExport':n.tag in verified,'policy':'Exact definition replacement — loader registers a whole object by ID.' if n.tag in verified else 'Inspect only: this domain requires additional loader/resource verification.'})
out=root/'src/StationeersModCreator.Desktop/Content/native-catalog.json';out.write_text(json.dumps(rows,separators=(',',':')))
print(json.dumps({'definitions':len(rows),'writable':sum(r['canExport'] for r in rows),'categories':sorted({r['category'] for r in rows})}))
