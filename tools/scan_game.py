#!/usr/bin/env python3
"""Read-only Unity/Stationeers inventory. Python standard library only."""
import argparse, collections, hashlib, json, mmap, sqlite3, struct, zipfile
from pathlib import Path

# ECMA-335 metadata table layouts. s/b/g are heap indices; integers are table indices.
CODES = {
 'ResolutionScope':(2,[0,26,35,1]),'TypeDefOrRef':(2,[2,1,27]),
 'HasConstant':(2,[4,8,23]),'HasCustomAttribute':(5,[6,4,1,2,8,9,10,0,14,23,20,17,26,27,32,35,38,39,40,42,44,43]),
 'HasFieldMarshal':(1,[4,8]),'HasDeclSecurity':(2,[2,6,32]),'MemberRefParent':(3,[2,1,26,6,27]),
 'HasSemantics':(1,[20,23]),'MethodDefOrRef':(1,[6,10]),'MemberForwarded':(1,[4,6]),
 'Implementation':(2,[38,35,39]),'CustomAttributeType':(3,[6,10]),'TypeOrMethodDef':(1,[2,6])}
LAYOUTS = [
 ['u2','s','g','g','g'],['ResolutionScope','s','s'],['u4','s','s','TypeDefOrRef',4,6],[4],['u2','s','b'],[6],
 ['u4','u2','u2','s','b',8],[8],['u2','u2','s'],[2,'TypeDefOrRef'],['MemberRefParent','s','b'],
 ['u2','HasConstant','b'],['HasCustomAttribute','CustomAttributeType','b'],['HasFieldMarshal','b'],['u2','HasDeclSecurity','b'],
 ['u2','u4',2],['u4',4],['b'],[2,20],[20],['u2','s','TypeDefOrRef'],[2,23],[23],['u2','s','b'],
 ['u2',6,'HasSemantics'],[2,'MethodDefOrRef','MethodDefOrRef'],['s'],['b'],['u2','MemberForwarded','s',26],['u4',4],
 ['u4','u4'],['u4'],['u4','u2','u2','u2','u2','u4','b','s','s'],['u4'],['u4','u4','u4'],
 ['u2','u2','u2','u2','u4','b','s','s','b'],['u4',35],['u4','u4','u4',35],['u4','s','b'],
 ['u4','u4','s','s','Implementation'],['u4','u4','s','Implementation'],[2,2],['u2','u2','TypeOrMethodDef','s'],['MethodDefOrRef','b'],[42,'TypeDefOrRef']]

def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for chunk in iter(lambda:f.read(4*1024*1024),b''): h.update(chunk)
 return h.hexdigest()

def clr(p):
 data=p.read_bytes()
 def u(fmt,o): return struct.unpack_from('<'+fmt,data,o)[0]
 if data[:2]!=b'MZ': raise ValueError('Not PE')
 pe=u('I',0x3c); assert data[pe:pe+4]==b'PE\0\0'
 n=u('H',pe+6); opt=pe+24; size=u('H',pe+20); magic=u('H',opt)
 dd=opt+(96 if magic==0x10b else 112); cli=u('I',dd+14*8)
 if not cli: return None
 sections=[]
 for i in range(n):
  o=opt+size+i*40; sections.append((u('I',o+12),max(u('I',o+8),u('I',o+16)),u('I',o+20)))
 def offset(rva):
  for va,sz,raw in sections:
   if va<=rva<va+sz:return raw+rva-va
  raise ValueError('RVA outside sections')
 co=offset(cli); md=offset(u('I',co+8)); assert data[md:md+4]==b'BSJB'
 vl=u('I',md+12); pos=(md+16+vl+3)&~3; streams={}; count=u('H',pos+2); pos+=4
 for _ in range(count):
  rel=u('I',pos); length=u('I',pos+4); start=pos+8; end=data.index(0,start)
  name=data[start:end].decode(); streams[name]=(md+rel,length); pos=(end+4)&~3
 ts=streams.get('#~',streams.get('#-'))[0]; heap=data[ts+6]; valid=u('Q',ts+8); pos=ts+24; counts={}
 for t in range(64):
  if valid>>t&1:counts[t]=u('I',pos); pos+=4
 def width(c):
  if c=='u2':return 2
  if c=='u4':return 4
  if c in ('s','g','b'):return 4 if heap & {'s':1,'g':2,'b':4}[c] else 2
  if isinstance(c,int):return 4 if counts.get(c,0)>=65536 else 2
  bits,tables=CODES[c]; return 4 if max(counts.get(t,0) for t in tables)>=(1<<(16-bits)) else 2
 offsets={}; sizes={}
 for t,num in sorted(counts.items()):
  if t>=len(LAYOUTS):raise ValueError('Unsupported metadata table '+str(t))
  offsets[t]=pos; sizes[t]=sum(width(c) for c in LAYOUTS[t]); pos+=num*sizes[t]
 def rows(t):
  for i in range(counts.get(t,0)):
   o=offsets[t]+i*sizes[t]; vals=[]
   for c in LAYOUTS[t]:
    w=width(c); vals.append(u('I' if w==4 else 'H',o));o+=w
   yield vals
 def string(i):
  o=streams['#Strings'][0]+i; return data[o:data.index(0,o)].decode('utf-8',errors='replace')
 def blob(i):
  o=streams['#Blob'][0]+i; lead=data[o]
  if lead<128:sz=lead;o+=1
  elif lead<192:sz=((lead&63)<<8)|data[o+1];o+=2
  else:sz=((lead&31)<<24)|(data[o+1]<<16)|(data[o+2]<<8)|data[o+3];o+=4
  return data[o:o+sz].hex()
 rawtypes=list(rows(2)); names=[(string(t[2])+'.' if string(t[2]) else '')+string(t[1]) for t in rawtypes]
 nested=dict(rows(41))
 def fullname(i,seen=None):
  if i in nested:
   seen=set() if seen is None else seen
   if i in seen:raise ValueError('Nested type cycle')
   seen.add(i); return fullname(nested[i],seen)+'+'+string(rawtypes[i-1][1])
  return names[i-1]
 names=[fullname(i+1) for i in range(len(rawtypes))]
 refs=[(string(t[2])+'.' if string(t[2]) else '')+string(t[1]) for t in rows(1)]
 def basetype(v):
  if not v:return None
  tag=v&3; idx=v>>2
  return names[idx-1] if tag==0 else refs[idx-1] if tag==1 else 'TypeSpec:'+str(idx)
 fields=[{'token':hex(0x04000000+i),'flags':r[0],'name':string(r[1]),'signature_hex':blob(r[2])} for i,r in enumerate(rows(4),1)]
 methods=[{'token':hex(0x06000000+i),'rva':r[0],'implementation_flags':r[1],'flags':r[2],'name':string(r[3]),'signature_hex':blob(r[4])} for i,r in enumerate(rows(6),1)]
 types=[]
 for i,t in enumerate(rawtypes):
  nxt=rawtypes[i+1] if i+1<len(rawtypes) else [0,0,0,0,len(fields)+1,len(methods)+1]
  types.append({'token':hex(0x02000000+i+1),'name':names[i],'flags':t[0],'base':basetype(t[3]),'fields':fields[t[4]-1:nxt[4]-1],'methods':methods[t[5]-1:nxt[5]-1]})
 refsassembly=[{'name':string(r[6]),'version':'.'.join(map(str,r[:4]))} for r in rows(35)]
 asm=list(rows(32)); aname=string(asm[0][7]) if asm else p.stem
 return {'name':aname,'module_name':string(next(rows(0))[1]),'types':types,'type_count':len(types),'method_count':len(methods),'field_count':len(fields),'references':refsassembly,'metadata_tables':counts}

def unity_asset(p):
 with p.open('rb') as f, mmap.mmap(f.fileno(),0,access=mmap.ACCESS_READ) as d:
  version=struct.unpack_from('>I',d,8)[0]
  if not 9<=version<=22:raise ValueError('Unsupported serialized asset version')
  endian='<' if d[16]==0 else '>'; cursor=48 if version>=22 else 20
  def read(fmt):
   nonlocal cursor
   value=struct.unpack_from(endian+fmt,d,cursor)[0];cursor+=struct.calcsize(fmt);return value
  end=d.find(b'\0',cursor); unity=d[cursor:end].decode();cursor=end+1
  platform=read('i'); tree=bool(read('B')); nt=read('i'); types=[]
  for _ in range(nt):
   cid=read('i')
   if version>=16:read('B')
   if version>=17:read('h')
   if version>=13:
    if (version<16 and cid<0) or (version>=16 and cid==114):cursor+=16
    cursor+=16
   if tree:
    if version<12:raise ValueError('Legacy type tree unsupported')
    nodes=read('i'); strings=read('i');cursor+=nodes*(32 if version>=19 else 24)+strings
   if tree and version>=21:
    deps=read('i');cursor+=deps*4
   types.append(cid)
  data_offset=struct.unpack_from('>Q',d,32)[0] if version>=22 else struct.unpack_from('>I',d,12)[0]
  objects=read('i'); records=[]; decode_errors=[]; histogram=collections.Counter(); serialized_bytes=collections.Counter()
  if version<17: return {'unity_version':unity,'serialized_version':version,'platform':platform,'type_tree':tree,'types':types,'object_count':objects,'object_table_decoded':False}
  for _ in range(objects):
   cursor=(cursor+3)&~3;pid=read('q');start=read('q' if version>=22 else 'I');size=read('I');tid=read('i')
   cid=types[tid];histogram[cid]+=1;serialized_bytes[cid]+=size
   absolute=data_offset+start
   if absolute<0 or absolute+size>len(d):raise ValueError('Object range outside file')
   record={'path_id':pid,'class_id':cid,'offset':absolute,'size':size}
   if cid in (1,115):
    try:
     q=absolute
     def ptr():
      nonlocal q
      value={'file_id':struct.unpack_from(endian+'i',d,q)[0],'path_id':struct.unpack_from(endian+'q',d,q+4)[0]};q+=12;return value
     def text():
      nonlocal q
      length=struct.unpack_from(endian+'i',d,q)[0];q+=4
      if not 0<=length<=size or q+length>absolute+size:raise ValueError('String outside object')
      value=d[q:q+length].decode('utf-8');q=(q+length+3)&~3;return value
     if cid==1:
      count=struct.unpack_from(endian+'i',d,q)[0];q+=4
      if not 0<=count<10000 or q+count*12+4>absolute+size:raise ValueError('Invalid component count')
      record['components']=[ptr() for _ in range(count)];record['layer']=struct.unpack_from(endian+'I',d,q)[0];q+=4;record['name']=text()
     else:
      record['name']=text();record['execution_order']=struct.unpack_from(endian+'i',d,q)[0];q+=20
      record['class_name']=text();record['namespace']=text();record['assembly_name']=text()
    except Exception as e:decode_errors.append({'path_id':pid,'class_id':cid,'error':str(e)})
   records.append(record)
  return {'unity_version':unity,'serialized_version':version,'platform':platform,'type_tree':tree,'types':types,'object_count':objects,'object_table_decoded':True,'class_counts':dict(histogram),'class_serialized_bytes':dict(serialized_bytes),'objects':records,'name_decode_errors':decode_errors}

def xmlnode(e):
 return {'tag':e.tag,'attributes':dict(e.attrib),'text':(e.text or '').strip(),'children':[xmlnode(c) for c in e]}

def scan(root,out):
 import xml.etree.ElementTree as ET
 out.mkdir(parents=True,exist_ok=True); report={'files':[],'assemblies':[],'assets':[],'xml':[],'recipes':[],'zip_examples':[],'errors':[]}
 game=root/'rocketstation_Data'; sources=[]
 for p in sorted(root.rglob('*')):
  if not p.is_file() or any(v in ('.git','analysis','tools','docs') for v in p.relative_to(root).parts):continue
  # Interrupted git-lfs temporary copies are not source game files.
  if any(v.startswith('.') for v in p.relative_to(root).parts):continue
  rel=p.relative_to(root).as_posix();sz=p.stat().st_size
  with p.open('rb') as f:head=f.read(160)
  rec={'path':rel,'size':sz,'extension':p.suffix.lower(),'lfs_pointer':head.startswith(b'version https://git-lfs.github.com/spec/v1')}
  report['files'].append(rec)
  if rec['lfs_pointer']:report['errors'].append({'path':rel,'error':'Unmaterialized LFS object'});continue
  if p.suffix.lower() in ('.dll','.xml','.assets','.exe','.zip') or p.name.startswith('level') or p.name=='globalgamemanagers':
   rec['sha256']=digest(p);sources.append(rec)
  if p.suffix.lower()=='.dll':
   try:
    a=clr(p)
    if a:a['source']=rec;report['assemblies'].append(a)
   except Exception as e:report['errors'].append({'path':rel,'stage':'clr','error':str(e)})
  if p.suffix=='.assets' or (p.parent==game and (p.name.startswith('level') and '.' not in p.name or p.name=='globalgamemanagers')):
   try:a=unity_asset(p);a['source']=rec;report['assets'].append(a)
   except Exception as e:report['errors'].append({'path':rel,'stage':'unity_asset','error':str(e)})
  if p.suffix.lower()=='.xml':
   try:
    tree=ET.parse(p).getroot();report['xml'].append({'source':rec,'document':xmlnode(tree),'elements':sum(1 for _ in tree.iter())})
    for section in tree:
     for i,r in enumerate(section.findall('RecipeData'),1):
      name=r.findtext('PrefabName');recipe=r.find('Recipe')
      if recipe is None:continue
      report['recipes'].append({'id':rel+'#'+section.tag+'/RecipeData['+str(i)+']','source_path':rel,'source_sha256':rec['sha256'],'section':section.tag,'prefab':name,'data':xmlnode(recipe),'record':xmlnode(r)})
   except Exception as e:report['errors'].append({'path':rel,'stage':'xml','error':str(e)})
  if p.suffix=='.zip':
   try:
    with zipfile.ZipFile(p) as z:
     report['zip_examples'].append({'source':rec,'entries':[{'path':i.filename,'size':i.file_size} for i in z.infolist()],'text_files':{i.filename:z.read(i).decode('utf-8-sig') for i in z.infolist() if i.filename.endswith(('.xml','.cs','.txt','.json')) and i.file_size<1000000}})
   except Exception as e:report['errors'].append({'path':rel,'stage':'zip','error':str(e)})
 stats={'file_count':len(report['files']),'game_bytes':sum(x['size'] for x in report['files']),'managed_assembly_count':len(report['assemblies']),'type_count':sum(x['type_count'] for x in report['assemblies']),'method_count':sum(x['method_count'] for x in report['assemblies']),'field_count':sum(x['field_count'] for x in report['assemblies']),'xml_count':len(report['xml']),'recipe_count':len(report['recipes']),'asset_count':len(report['assets']),'serialized_object_count':sum(x['object_count'] for x in report['assets']),'errors':report['errors']}
 report['summary']=stats
 for key in ['files','assemblies','assets','xml','recipes','zip_examples','summary']:(out/(key+'.json')).write_text(json.dumps(report[key],indent=2,ensure_ascii=False)+'\n')
 dbpath=out/'stationeers.sqlite'
 if dbpath.exists():dbpath.unlink()
 with sqlite3.connect(dbpath) as db:
  db.executescript('CREATE TABLE sources(path TEXT PRIMARY KEY,sha256 TEXT,size INTEGER);CREATE TABLE recipes(id TEXT PRIMARY KEY,prefab TEXT,section TEXT,source_path TEXT,record_json TEXT);CREATE TABLE types(assembly TEXT,token TEXT,name TEXT,base TEXT,source_path TEXT,PRIMARY KEY(assembly,token));CREATE TABLE methods(assembly TEXT,type_token TEXT,token TEXT,name TEXT,rva INTEGER,flags INTEGER,signature_hex TEXT);CREATE TABLE fields(assembly TEXT,type_token TEXT,token TEXT,name TEXT,flags INTEGER,signature_hex TEXT);CREATE TABLE xml_documents(source_path TEXT PRIMARY KEY,document_json TEXT);CREATE TABLE asset_objects(source_path TEXT,path_id INTEGER,class_id INTEGER,name TEXT,offset INTEGER,size INTEGER,record_json TEXT,PRIMARY KEY(source_path,path_id));CREATE INDEX asset_name ON asset_objects(name);CREATE INDEX recipe_prefab ON recipes(prefab);CREATE INDEX type_name ON types(name);CREATE INDEX method_name ON methods(name);')
  db.executemany('INSERT INTO sources VALUES(?,?,?)',[(x['path'],x['sha256'],x['size']) for x in sources])
  db.executemany('INSERT INTO recipes VALUES(?,?,?,?,?)',[(x['id'],x['prefab'],x['section'],x['source_path'],json.dumps(x['record'])) for x in report['recipes']])
  db.executemany('INSERT INTO xml_documents VALUES(?,?)',[(x['source']['path'],json.dumps(x['document'])) for x in report['xml']])
  for asset in report['assets']:
   db.executemany('INSERT INTO asset_objects VALUES(?,?,?,?,?,?,?)',[(asset['source']['path'],o['path_id'],o['class_id'],o.get('name'),o['offset'],o['size'],json.dumps(o)) for o in asset.get('objects',[])])
  for a in report['assemblies']:
   for t in a['types']:
    db.execute('INSERT INTO types VALUES(?,?,?,?,?)',(a['name'],t['token'],t['name'],t['base'],a['source']['path']))
    db.executemany('INSERT INTO methods VALUES(?,?,?,?,?,?,?)',[(a['name'],t['token'],m['token'],m['name'],m['rva'],m['flags'],m['signature_hex']) for m in t['methods']])
    db.executemany('INSERT INTO fields VALUES(?,?,?,?,?,?)',[(a['name'],t['token'],f['token'],f['name'],f['flags'],f['signature_hex']) for f in t['fields']])
 print(json.dumps(stats,indent=2))

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--game-root',type=Path,default=Path('.'));p.add_argument('--output',type=Path,default=Path('analysis'));args=p.parse_args();scan(args.game_root.resolve(),args.output.resolve())
