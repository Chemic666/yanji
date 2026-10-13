import sqlite3,json,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'artifacts/v021-implementation'
before=json.loads((root/'before/canonical.json').read_text(encoding='utf-8-sig'))
after=json.loads((root/'final/canonical.json').read_text(encoding='utf-8-sig'))
def normalized(rows):
    return sorted([{k:v for k,v in r.items() if k!='CreatedAt'} for r in rows],key=lambda r:r['Id'])
assert normalized(before)==normalized(after),'Raw record/part facts differ'
db=sqlite3.connect(f'file:{root / "before-db-3/yanji.sqlite"}?mode=ro',uri=True)
schema={r[0]:[x[1] for x in db.execute('pragma table_info("'+r[0]+'")')] for r in db.execute("select name from sqlite_master where type='table' and name not like 'sqlite_%'")}
result={'Status':'PASS','RecordCount':len(before),'Comparison':'All record fields and parts except database-generated CreatedAt; audit timestamps not byte-identical','CanonicalSha256':hashlib.sha256(json.dumps(normalized(before),sort_keys=True).encode()).hexdigest(),'PrecisionSamples':[r['Parts'][0]['DurationMs'] for r in sorted(before,key=lambda r:r['Id']) if r['Id'].startswith('precision-')],'Schema':schema}
def clean(value):
    if isinstance(value,dict):return {k:clean(v) for k,v in value.items() if k not in ['CreatedAt','created','at']}
    if isinstance(value,list):return [clean(v) for v in value]
    return value
def tables(folder):
    with sqlite3.connect(f'file:{folder / "yanji.sqlite"}?mode=ro',uri=True) as c:
        facts={}
        for table,columns in schema.items():
            rows=[]
            for row in c.execute('select * from "'+table+'"'):
                values={}
                for name,value in zip(columns,row):
                    if name in ['created','at'] or (table=='revisions' and name=='id'):continue
                    if name in ['payload','before_json','after_json'] and value:
                        try:value=clean(json.loads(value))
                        except ValueError:pass
                    values[name]=value
                rows.append(json.dumps(values,sort_keys=True,ensure_ascii=False))
            facts[table]=sorted(rows)
        return facts
left=tables(root/'before-db-3');right=tables(root/'final-db')
assert left==right,'Normalized all-table facts differ'
result['AllTableFactsEqual']=True
result['ExcludedGeneratedFields']=['sessions.created','revisions.id','revisions.at','CreatedAt inside revision JSON']
(root/'facts-comparison.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False))

