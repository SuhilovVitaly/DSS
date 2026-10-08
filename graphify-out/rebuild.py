"""Re-export the audited extraction; refuses changed source bytes.
For changed sources, run the documented Graphify AST/semantic extraction first.
Usage from repo root: python graphify-out/rebuild.py
"""
from pathlib import Path
import hashlib,json,re,importlib.metadata
from graphify.build import build_from_json
from graphify.cluster import cluster,score_all
from graphify.analyze import god_nodes,surprising_connections,suggest_questions
from graphify.diagnostics import diagnose_extraction
from graphify.export import to_json
from graphify.report import generate

def main():
    root=Path(__file__).resolve().parent.parent
    out=root/'graphify-out'
    manifest=json.loads((out/'manifest.json').read_text(encoding='utf-8'))
    changed=[p for p,h in manifest['inputs'].items() if not (root/p).is_file() or hashlib.sha256((root/p).read_bytes()).hexdigest()!=h]
    if changed: raise SystemExit('Source hashes changed; refresh extraction first: '+str(changed))
    extraction=json.loads((out/'extraction.json').read_text(encoding='utf-8'))
    graph=build_from_json(extraction,root=root,directed=False)
    if graph.number_of_nodes()==0: raise SystemExit('Empty graph cannot replace an existing graph')
    communities=cluster(graph)
    cohesion=score_all(graph,communities)
    # Deterministic initial labels from central declarations, human-reviewed below.
    labels={}
    for key,ids in communities.items():
        ranked=sorted(ids,key=lambda n:(bool(graph.nodes[n].get('source_file')),graph.degree(n),str(n)),reverse=True)
        label=graph.nodes[ranked[0]].get('label',ranked[0])
        label=re.sub(r'\.(cs|md)$','',label)
        label=re.sub(r'([a-z0-9])([A-Z])',r'\1 \2',label)
        words=re.findall(r'[A-Za-zА-Яа-я0-9]+',label)
        labels[key]=' '.join(words[:4]) if len(words)>1 else ((words[0] if words else 'Repository')+' References')
    labelpath=out/'community-labels.json'
    # Existing curated labels can be reused only for the identical node partition.
    partition_hash=hashlib.sha256(json.dumps(communities,sort_keys=True).encode()).hexdigest()
    if labelpath.exists():
        saved=json.loads(labelpath.read_text(encoding='utf-8'))
        if saved.get('partitionSha256')==partition_hash: labels={int(k):v for k,v in saved['labels'].items()}
    labelpath.write_text(json.dumps({'partitionSha256':partition_hash,'labels':labels},indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    if not to_json(graph,communities,str(out/'graph.json'),built_at_commit=manifest['sourceBase'],community_labels=labels):
        raise SystemExit('Graph shrink guard refused export')
    diagnostics=diagnose_extraction(extraction,directed=False,root=root)
    (out/'diagnostics.json').write_text(json.dumps(diagnostics,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    gods=god_nodes(graph);surprises=surprising_connections(graph,communities)
    questions=suggest_questions(graph,communities,labels)
    report=generate(graph,communities,cohesion,labels,gods,surprises,manifest['scope'],{'input':0,'output':0},str(root),suggested_questions=questions,built_at_commit=manifest['sourceBase'])
    report=re.sub(r'^- Token cost:.*$', '- Semantic token usage: unavailable from host; not measured. Schema zeros are placeholders, not a zero-cost claim.',report,flags=re.M)
    report=report.replace('- Built from commit: `'+manifest['sourceBase'][:8]+'`','- Source base: `'+manifest['sourceBase'][:8]+'` plus final US9 TK2 working-tree texts; manifest SHA256 hashes identify actual inputs.')
    report=report.replace('- Run `git rev-parse HEAD` and compare to check if the graph is stale.','- Validate input hashes with `python graphify-out/rebuild.py`; commit equality alone is insufficient for this build.')
    report=report.replace('- Run `graphify update .` after code changes (no API cost).','- After source changes refresh AST/semantic extraction with the documented scope; semantic usage is unmeasured.')
    report+='\n## EP-0004 provenance and limitations\n\nSource base '+manifest['sourceBase']+' plus final US9 TK2 working-tree text; manifest hashes define actual inputs. '+str(len(manifest['inputs']))+' inputs. Media/build/generated outputs and four self-referential run/status documents excluded. Semantic token usage unmeasured. AST references are navigation, not runtime proof. Unlocated type/import references are explicitly tagged. Graph is undirected: parallel/reverse edges collapse; extraction.json retains original directions, confidence and evidence. HTML intentionally omitted for >5000 symbols; JSON/report/CLI remain available. Native80FPS acceptance remains OPEN.\n'
    report+='\nIntegrity: '+str(diagnostics['dangling_endpoint_edges'])+' dangling and '+str(diagnostics['missing_endpoint_edges'])+' missing endpoints; '+str(diagnostics['self_loop_edges'])+' self-loops. Health warning: '+str(diagnostics['undirected_same_endpoint_collapsed_edges'])+' original edges collapse in the undirected export; all original evidence remains in extraction.json. '+str(extraction['sourceValidation']['unlocatedAstReferences'])+' reference nodes lack a verified declaration location.\n'
    report+='\n[Manifest](manifest.json) · [Diagnostics](diagnostics.json) · [Verification](verification.json) · [Extraction](extraction.json).\n'
    (out/'GRAPH_REPORT.md').write_text(report,encoding='utf-8')
    (out/'community-cohesion.json').write_text(json.dumps(cohesion,indent=2)+'\n',encoding='utf-8')
    summaries=[]
    for key,ids in communities.items():
        ranked=sorted(ids,key=lambda n:graph.degree(n),reverse=True)[:4]
        summaries.append({'id':key,'size':len(ids),'label':labels[key],'top':[graph.nodes[n].get('label',n) for n in ranked]})
    (root/'TestResults/graph-communities.json').write_text(json.dumps(summaries,ensure_ascii=False,indent=2),encoding='utf-8')
    verification={'inputHashesVerified':len(manifest['inputs']),'changedInputs':changed,'nodes':graph.number_of_nodes(),'edges':graph.number_of_edges(),'communities':len(communities),'graphifyVersion':importlib.metadata.version('graphifyy'),'graphSha256':hashlib.sha256((out/'graph.json').read_bytes()).hexdigest(),'extractionSha256':hashlib.sha256((out/'extraction.json').read_bytes()).hexdigest(),'sourceValidation':extraction['sourceValidation'],'nativeAcceptance':'OPEN; FPS target measured failed','semanticTokenUsage':'unmeasured'}
    traces=[]
    pairs=[('aibasegenerator_generate','aitradeplacementvalidator_validate'),('environmentfieldgenerator_generate','environmentfieldgenerator_validateworld'),('pointofinterestgenerator_generate','pointofinterestgenerator_validateworld'),('fullmaprenderevidencetests_rendernevercallssession','throwingconnection')]
    for caller,callee in pairs:
        matches=[e for e in extraction['edges'] if e['source'].endswith(caller) and e['target'].endswith(callee) and e['relation']=='calls']
        if not matches: raise SystemExit('Missing expected navigation edge: '+caller+' -> '+callee)
        for edge in matches:
            assert graph.has_edge(edge['source'],edge['target'])
            traces.append(edge)
    verification['verifiedNavigationEdges']=traces
    verification['navigationLimit']='These edges are parser evidence with retained confidence, not independent runtime proof.'
    (out/'verification.json').write_text(json.dumps(verification,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps(verification,ensure_ascii=False))
    print('Health:',{k:v for k,v in diagnostics.items() if k.endswith('edges') or k.endswith('nodes')})

if __name__=='__main__': main()
