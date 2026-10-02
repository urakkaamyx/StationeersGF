#!/usr/bin/env python3
"""Search extracted Stationeers data without loading game binaries."""
import argparse,json,sqlite3
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--db',type=Path,default=Path('analysis/stationeers.sqlite'));p.add_argument('kind',choices=['recipes','types','methods','objects','prefabs']);p.add_argument('search');p.add_argument('--limit',type=int,default=30);a=p.parse_args()
queries={'recipes':('recipes','prefab'),'types':('types','name'),'methods':('methods','name'),'objects':('asset_objects','name'),'prefabs':('prefabs','prefab_name')}
table,col=queries[a.kind]
with sqlite3.connect(a.db.resolve().as_uri()+'?mode=ro',uri=True) as db:
 db.row_factory=sqlite3.Row
 for r in db.execute(f'SELECT * FROM {table} WHERE {col} LIKE ? LIMIT ?',('%'+a.search+'%',a.limit)):print(json.dumps(dict(r),ensure_ascii=False))
