"""Fresh offline Graphify AST/Markdown + fresh host-agent document/EP-0014 semantic rebuild.
Never executes the game or external APIs. Run with graphify 0.9.66.
--prepare-ast warms the structural cache; --force permits intentional shrink.
"""
from pathlib import Path
from collections import Counter
from datetime import datetime, timezone
import argparse, hashlib, json, os, re, subprocess, sys
from urllib.parse import unquote
from graphify.extract import collect_files, extract
from graphify.build import build_from_json
from graphify.cluster import cluster, score_all
from graphify.analyze import god_nodes, surprising_connections, suggest_questions
from graphify.export import to_json, to_html
from graphify.report import generate
from graphify.diagnostics import diagnose_extraction
from graphify.detect import save_manifest
R=Path(__file__).resolve().parents[2]
O=Path(os.environ.get('DSS_GRAPH_OUTPUT') or ('D:/DeepSpaceSaga/artifacts/validation/graphify/'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S')))
O.mkdir(parents=True,exist_ok=True)
EP='Board/EP-0010-combat-reports'
OBSERVED_EP='Board/EP-0011-short-fixes'
POINT_EP='Board/EP-0012-combat-rebuild'
MODULE_EP='Board/EP-0009-battle'
TUNING_EP='Board/EP-0013-debug-tuning-panel'
PANELS_EP='Board/EP-0014-module-panels'
EXCLUDED={PANELS_EP+'/ExecutionStatus.md':'publication ledger updated after graph',
 PANELS_EP+'/PublicationStatus.md':'publication ledger updated after graph',
 PANELS_EP+'/DocumentationImpact.md':'impact audit updated after graph',
 PANELS_EP+'/FinalValidation.md':'validation audit updated after graph',
 TUNING_EP+'/ExecutionStatus.md':'publication/evidence ledger updated alongside graph',
 TUNING_EP+'/PublicationStatus.md':'publication ledger updated alongside graph',
 TUNING_EP+'/DocumentationImpact.md':'final impact audit updated alongside graph',
 TUNING_EP+'/FinalValidation.md':'final evidence audit updated alongside graph',
 'Documentation/06-Tooling/GraphifyRebuild.md':'describes generated graph itself',
 'Board/EP-0012-combat-rebuild/EP-0012-US-0020-bugs/ExecutionStatus.md':'US20 evidence updated alongside graph',
 'Documentation/README.md':'navigation index updated alongside graph',
 'Documentation/06-Tooling/Graphify.md':'describes generated graph itself',
 EP+'/ImplementationStatus.md':'execution status updated alongside graph',
 OBSERVED_EP+'/ImplementationStatus.md':'execution status updated alongside graph',
 POINT_EP+'/ImplementationStatus.md':'execution status updated alongside graph',
 POINT_EP+'/PublicationStatus.md':'publication ledger updated alongside graph',
}
def dump(n,v): (O/n).write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def txt(p): return (R/p).read_text(encoding='utf-8-sig')
def slug(s):
 v=re.sub('[^a-z0-9]+','_',s.lower()).strip('_')
 if any(ord(c)>127 for c in s):v+='_'+hashlib.sha256(s.encode('utf-8')).hexdigest()[:12]
 return v.strip('_') or 'unnamed'
def nid(p,e): return slug(str(Path(p).with_suffix('')))+'_'+slug(e)
def main():
 a=argparse.ArgumentParser(description=__doc__); a.add_argument('--prepare-ast',action='store_true'); a.add_argument('--dry-run',action='store_true'); a.add_argument('--force',action='store_true'); args=a.parse_args()
 code=sorted(p for p in collect_files(R,root=R) if p.suffix in ('.cs','.py') and 'graphify-out' not in p.parts)
 docs=sorted(set((R/'Documentation').rglob('*.md')) | {p for ep in [MODULE_EP, EP, OBSERVED_EP, POINT_EP, TUNING_EP, PANELS_EP] for p in (R/ep).rglob('*.md')} | {R/'AGENTS.md',R/'CLAUDE.md'} | set((R/'tools').rglob('README.md')) | set((R/'tools').rglob('EP0013.md')))
 docs=[p for p in docs if not {'bin','obj'} & set(p.parts)]
 docs=[p for p in docs if p.relative_to(R).as_posix() not in EXCLUDED and ('evidence' not in p.relative_to(R).parts or p.relative_to(R).as_posix() in {POINT_EP+'/evidence/balance.md',POINT_EP+'/evidence/native.md'})]
 print(f'Corpus: {len(code)} code files, {len(docs)} Markdown files; aggregated HTML above 5000 nodes.',flush=True)
 ast=extract(code,cache_root=O/'cache',root=R,parallel=False)
 if ast.get('failed_sources'):raise ValueError('AST failed sources: '+str(ast['failed_sources']))
 if args.prepare_ast: print(len(ast['nodes']),len(ast['edges'])); return
 ns=list(ast['nodes']); es=list(ast['edges']); containers={}; semantic_files=[]; headings=links=0
 for e in es:e.setdefault('confidence_score',1.0 if e.get('confidence')=='EXTRACTED' else .85)
 def node(p,label,e='file',kind='document',line=1,**extra):
  n=dict(id=nid(p,e),label=label,file_type=kind,source_file=p,source_location=f'L{line}',_origin='dss_structure',**extra);ns.append(n);return n['id']
 def edge(s,t,rel,p,line=1,conf='EXTRACTED',rationale=None):
  e=dict(source=s,target=t,relation=rel,confidence=conf,confidence_score=1.0 if conf=='EXTRACTED' else .85,source_file=p,source_location=f'L{line}',weight=1.0,_origin='dss_structure' if rel in ('contains','references') else 'dss_curated_semantic')
  if rationale:e['rationale']=rationale
  es.append(e)
 for p in code+docs:
  path=p.relative_to(R).as_posix();containers[path]=node(path,path,kind='code' if p in code else 'document')
 for n in ast['nodes']:
  p=str(n.get('source_file','')).replace('\\','/')
  if p in containers: edge(containers[p],n['id'],'contains',p)
 for p in docs:
  path=p.relative_to(R).as_posix(); lines=txt(path).splitlines(); front={}
  if lines and lines[0]=='---':
   for line in lines[1:]:
    if line=='---':break
    m=re.match(r'^(source_url|captured_at|author|contributor):\s*(.+)$',line)
    if m:front[m[1]]=m[2].strip('\"\x27')
  if front:
   next(n for n in ns if n['id']==containers[path]).update(front)
  for i,line in enumerate(lines,1):
   m=re.match(r'^#{1,6}\s+(.+)',line)
   if m:
    h=node(path,m[1].strip(),'heading_'+m[1],line=i,**front);edge(containers[path],h,'contains',path,i);headings+=1
   for m in re.finditer(r'(?<!!)\[[^\]]+\]\(([^)]+)\)',line):
    target=unquote(m[1].strip('<>').split('#')[0])
    if not target or re.match(r'^[a-z]+:',target,re.I):continue
    try:relative=(p.parent/target).resolve().relative_to(R).as_posix()
    except ValueError:continue
    if relative in containers:edge(containers[path],containers[relative],'references',path,i);links+=1
 evidence=[
 ('src/DeepSpaceSaga.Contracts/BattleReportSnapshot.cs','CombatReportBatchSnapshot','Immutable combat report batch','contract'),
 ('src/DeepSpaceSaga.Engine/SimulationEngine.CombatReports.cs','outbox overflow','Bounded authoritative report outbox','outbox'),
 ('src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportRange.cs','CombatReportRangeTargets','Authoritative range eligibility','range'),
 ('src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportIdentity.cs','CaptureReportIdentity','Captured reporter and visible participant identity','identity'),
 ('src/DeepSpaceSaga.Engine/SimulationEngine.CombatReportPersistence.cs','_combatReportBaseline = _combatReportSequence','Restored baseline and silent contacts','save_restore'),
 ('src/DeepSpaceSaga.Engine/Scenario/CombatReportsSaveValidation.cs','Validate','Combat report save validation','save_validation'),
 ('src/DeepSpaceSaga.Engine.LocalClient/SnapshotMailbox.cs','MergeReports','Report retention across snapshot coalescing','mailbox'),
 ('src/DeepSpaceSaga.Client/SnapshotBuffer.cs','_combatReports.Receive(snapshot.CombatReports)','Reports received before older world state rejection','buffer'),
 ('src/DeepSpaceSaga.Client/CombatReportInbox.cs','Do not acknowledge across a gap','Contiguous delivery and bounded deduplication','inbox'),
 ('src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportStore.cs','HistoryLimit = 200','Latest 200 UI reports','history'),
 ('src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportFormatter.cs','Format','Localized captured report facts','formatter'),
 ('src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatReportAssets.cs','Prepare','Prepared illustration and portrait resources','assets'),
 ('src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CombatReportPanel.cs','OnMouse','Accordion panel and focused input','panel'),
 ('src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.CombatReports.cs','DisposeCombatReports','Report preparation and lifecycle disposal','screen')]
 mechanisms={}
 for p,token,label,key in evidence:
  line=next((i for i,s in enumerate(txt(p).splitlines(),1) if token in s),None)
  if line is None:raise ValueError('Semantic evidence changed: '+p+': '+token)
  n=node(p,label,'mechanism_'+key,'rationale',line,rationale='Selective reviewed mechanism grounded in this source; navigation evidence only.'); ns[-1]['_origin']='dss_curated_semantic';mechanisms[key]=n;semantic_files.append(p);edge(containers[p],n,'implements',p,line)
 flow=[('outbox','contract'),('range','outbox'),('identity','outbox'),('save_validation','save_restore'),('save_restore','outbox'),('outbox','mailbox'),('mailbox','buffer'),('buffer','inbox'),('inbox','history'),('history','formatter'),('formatter','panel'),('assets','panel'),('screen','history'),('screen','assets'),('screen','panel')]
 for s,t in flow:
  p=next(x[0] for x in evidence if x[3]==s);edge(mechanisms[s],mechanisms[t],'shares_data_with',p,conf='INFERRED',rationale='Reviewed EP-0010 architecture mapping, not a measured execution trace.')
 spec='Documentation/04-Engineering/CombatReports.md'
 if spec in containers:
  for key in mechanisms:edge(containers[spec],mechanisms[key],'conceptually_related_to',spec,conf='INFERRED',rationale='Selective implementation mapping to the report specification.')
 hyper=[dict(id='ep0010_lossless_delivery',label='Combat report delivery pipeline',nodes=[mechanisms[k] for k in ('outbox','mailbox','buffer','inbox','history')],relation='participate_in',confidence='INFERRED',confidence_score=.85,source_file='src/DeepSpaceSaga.Client/SnapshotBuffer.cs',source_location='L1')]
 semantic_dir=Path(os.environ.get('DSS_GRAPH_SEMANTICS',str(O.parent)))
 semantic_paths=sorted(semantic_dir.glob('graph-semantic-*.json'))
 if not semantic_paths:raise ValueError('Fresh host-agent semantic fragments required in DSS_GRAPH_SEMANTICS')
 fragment=dict(nodes=[],input_sha256={},chunks=len(semantic_paths))
 for chunk_index,semantic_path in enumerate(semantic_paths):
  chunk=json.loads(semantic_path.read_text(encoding='utf-8-sig'))
  def relative(source):
   p=Path(source)
   return p.resolve().relative_to(R).as_posix() if p.is_absolute() else p.as_posix()
  for source,expected in chunk['input_sha256'].items():
   actual=Path(source) if Path(source).is_absolute() else R/source
   if sha(actual)!=expected:raise ValueError('Semantic input changed; re-extract: '+source)
   if actual.resolve().is_relative_to(R):fragment['input_sha256'][relative(source)]=expected
  mapping={n['id']:f'dss_host_{chunk_index}_'+n['id'] for n in chunk['nodes']}
  for n in chunk['nodes']:
   n=dict(n);n['id']=mapping[n['id']];n['source_file']=relative(n['source_file']);n['_origin']='dss_dss_host_semantic'
   if n.get('file_type') not in {'code','document','paper','image','rationale','concept'}:raise ValueError('Invalid semantic node type')
   ns.append(n);fragment['nodes'].append(n)
   if n['source_file'] in containers:edge(containers[n['source_file']],n['id'],'conceptually_related_to',n['source_file'],int(str(n.get('source_location') or 'L1')[1:]),conf='INFERRED',rationale='Fresh host-reviewed source concept; navigation, not runtime evidence.')
  for e in chunk['edges']:
   e=dict(e);e['source']=mapping.get(e['source'],e['source']);e['target']=mapping.get(e['target'],e['target']);e['source_file']=relative(e['source_file']);e['_origin']='dss_dss_host_semantic'
   if e['confidence']=='EXTRACTED' and e['confidence_score']!=1.0:raise ValueError('Invalid extracted confidence')
   es.append(e)
  for h in chunk.get('hyperedges',[]):
   h=dict(h);h['id']=f'dss_host_{chunk_index}_'+h['id'];h['nodes']=[mapping.get(n,n) for n in h['nodes']];h['source_file']=relative(h['source_file']);hyper.append(h)
 ns=list({n['id']:n for n in ns}.values());ids={n['id'] for n in ns};pruned=[e for e in es if e.get('source') not in ids or e.get('target') not in ids];es=[e for e in es if e.get('source') in ids and e.get('target') in ids and e['source']!=e['target']]
 unlocated=[];bad=[];missing=[];linecounts={}
 for n in ns+es:
  p=n.get('source_file');loc=n.get('source_location')
  if not p:
   if 'id' in n:unlocated.append(n['id'])
   continue
  if not (R/p).is_file():missing.append(p);continue
  if p not in linecounts:linecounts[p]=max(1,len(txt(p).splitlines()))
  m=re.match(r'^L(\d+)',str(loc))
  if m and not 1<=int(m[1])<=linecounts[p]:bad.append(dict(source_file=p,source_location=loc))
 if bad or missing:raise ValueError(f'Invalid source evidence: {len(missing)} missing, {len(bad)} invalid locations: {bad[:3]}')
 ex=dict(nodes=ns,edges=es,hyperedges=hyper,input_tokens=None,output_tokens=None,token_usage_status='unmeasured host-agent extraction');G=build_from_json(ex,root=R,directed=False)
 if not G:raise ValueError('Refusing empty graph')
 # Canonicalization can merge a file container and its sole declaration, turning
 # their original contains edge into a self-loop. Audit and remove those only
 # from the derived graph; extraction.json retains the original relationships.
 canonical_selfloops=[(s,t) for s,t in G.edges() if s==t]
 G.remove_edges_from(canonical_selfloops)
 if args.dry_run:
  print(json.dumps(dict(nodes=G.number_of_nodes(),edges=G.number_of_edges(),missing_sources=len(missing),invalid_locations=len(bad),pruned_unresolved=len(pruned),unlocated_references=len(unlocated),canonical_selfloops_pruned=len(canonical_selfloops))),flush=True);return
 c=cluster(G);co=score_all(G,c);labels={}
 for k,members in c.items():
  counts=Counter(str(G.nodes[n].get('source_file','')).replace('\\','/').split('/')[1] for n in members if '/' in str(G.nodes[n].get('source_file','')))
  family=counts.most_common(1)[0][0] if counts else ''
  readable={'DeepSpaceSaga.Engine':'Engine simulation','DeepSpaceSaga.Client':'Client interface','DeepSpaceSaga.Contracts':'Snapshot contracts','DeepSpaceSaga.Engine.LocalClient':'Local session transport','DeepSpaceSaga.Client.Tests':'Client verification','DeepSpaceSaga.Engine.Tests':'Engine verification','EP-0010-combat-reports':'Combat report planning','EP-0012-combat-rebuild':'Point combat contract','EP-0009-battle':'Historical module combat','04-Engineering':'Engineering documentation','02-FirstRelease':'Release requirements','01-Requirements':'Gameplay requirements'}
  labels[k]=readable.get(family,family.replace('DeepSpaceSaga.','').replace('-',' ') or 'Shared references')+f' {k}'
 head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,text=True).strip()
 if not to_json(G,c,str(O/'graph.json'),force=args.force,built_at_commit=head,community_labels=labels):raise SystemExit('Fresh graph shrank: inspect scope and use --force only if intentional.')
 if not to_html(G,c,str(O/'graph.html'),community_labels=labels,node_limit=5000):raise SystemExit('HTML export failed')
 html=(O/'graph.html').read_text(encoding='utf-8')
 note='<aside id="dss-graph-scope" style="position:fixed;bottom:8px;right:8px;max-width:440px;z-index:9999;background:#17212b;color:#e7edf3;padding:10px;border:1px solid #668095;font:12px sans-serif">DSS navigation only. Fresh AST and Markdown structure; fresh host-agent document/EP-0014 semantics. Aggregated community view; full data in graph.json. Does not prove gameplay, FPS, or human acceptance.</aside>'
 html=re.sub(r'(<body[^>]*>)',lambda m:m[1]+note,html,count=1)
 (O/'graph.html').write_text(html,encoding='utf-8')
 hashes={p.relative_to(R).as_posix():sha(p) for p in code+docs};d=dict(total_files=len(hashes),total_words=sum(len(txt(p).split()) for p in hashes),files=dict(code=[p.relative_to(R).as_posix() for p in code],document=[p.relative_to(R).as_posix() for p in docs]))
 report=generate(G,c,co,labels,god_nodes(G),surprising_connections(G,c),d,dict(input=0,output=0),str(R),suggested_questions=suggest_questions(G,c,labels),built_at_commit=head)
 report=re.sub(r'- Token cost:.*', '- Token cost: unmeasured host-agent usage (see cost.json)', report)
 report=report.replace('Run `graphify update .` after code changes (no API cost).','Rebuild this selected corpus with `tools/graphify/rebuild.py`; AST/Markdown extraction is offline and curated semantic evidence tokens are revalidated.')
 prefix='# DSS fresh structural and host-agent semantic graph\n\nFresh AST covers selected code. Markdown headings/links are structural evidence. Fresh host-agent fragments cover the explicit source hashes listed in semantic-coverage.json; coverage is selective and recorded per run. Remaining Board/code semantics are selective, including validated EP10 mechanisms. Historical EP12 fragments are replaced. No image extraction or external API runs. Token usage is unmeasured. Navigation does not establish gameplay, FPS, physical input, audio or human acceptance. HTML aggregates communities; JSON contains the complete graph. Recorded HEAD is pre-export; hashes describe the actual working tree, including concurrent documentation curation.\n\n'
 prefix+='Reproduce with tools/graphify/rebuild.py and the fresh DSS_GRAPH_SEMANTICS fragments; choose DSS_GRAPH_OUTPUT outside the repository. --dry-run validates without exporting.\n\n'
 prefix+=f'Audit: {len(pruned)} unresolved AST endpoint edges were explicitly pruned; {len(unlocated)} reference-only symbols have no local declaration location. Canonicalization produced {len(canonical_selfloops)} self-loops, removed from the derived graph. The undirected export combines some relations and canonicalizes equivalent nodes; extraction.json retains original non-loop relationships. See diagnostics.json and the dedicated audit sidecars before interpreting absence of a path.\n\n'
 (O/'GRAPH_REPORT.md').write_text(prefix+report,encoding='utf-8');dump('extraction.json',ex);dump('.graphify_labels.json',labels);dump('community-cohesion.json',co);dump('diagnostics.json',diagnose_extraction(ex,directed=False,root=R));dump('pruned-unresolved-edges.json',dict(reason='AST references without declared endpoints; not invented as facts',edges=pruned));dump('unlocated-references.json',dict(reason='Symbols without local declaration location',nodes=unlocated))
 stamp=datetime.now(timezone.utc).isoformat();dump('input-evidence.json',dict(head=head,working_tree=True,captured_at=stamp,inputs=hashes,exclusions=EXCLUDED,excluded_directories=['src/graphify-out','Board/*/evidence','**/bin','**/obj'],policy='No game/API execution; navigation is not acceptance'));dump('source-manifest.json',dict(algorithm='sha256',files=hashes,excluded=EXCLUDED))
 # Only AST inputs receive native Graphify stamps. Selective documents remain
 # unstamped for semantic updates, rather than pretending full semantic coverage.
 dump('manifest.json',{})
 save_manifest({'code':[str(p) for p in code]},manifest_path=str(O/'manifest.json'),kind='ast',root=R,scan_corpus=[str(p) for p in code],clear_semantic=[str(p) for p in code])
 dump('semantic-coverage.json',dict(mode='Fresh AST + Markdown structure + fresh host-agent document/EP-0014 semantics',code_files=len(code),markdown_files=len(docs),doc_heading_occurrences=headings,resolved_markdown_links=links,curated_mechanisms=len(mechanisms)+len(fragment["nodes"]),curated_source_files=sorted(set(semantic_files)|set(fragment["input_sha256"])),host_semantic_chunks=fragment["chunks"],host_semantic_inputs=len(fragment["input_sha256"]),limitations=['Host semantic coverage is selective; exact current inputs are listed in curated_source_files','No image vision extraction','INFERRED architecture mappings are not runtime traces','Undirected graph collapses some relations; extraction.json retains them','Generated status and graph docs excluded to avoid circular hashes','Historical semantic fragments intentionally replaced'],token_usage=dict(input=None,output=None,status='unmeasured host-agent extraction')))
 counts=dict(nodes=G.number_of_nodes(),edges=G.number_of_edges(),communities=len(c),extraction_nodes=len(ns),extraction_edges=len(es),code_files=len(code),markdown_files=len(docs),pruned_unresolved_edges=len(pruned),canonical_selfloops_pruned=len(canonical_selfloops),unlocated_reference_nodes=len(unlocated));dump('counts.json',counts);dump('cost.json',dict(run=stamp,input_tokens=None,output_tokens=None,status='unmeasured',backend='Host-agent semantics; offline AST/Markdown'));(O/'.graphify_root').write_text(str(R),encoding='utf-8');(O/'.graphify_python').write_text(sys.executable,encoding='utf-8')
 v=dict(head=head,working_tree=True,input_hashes_verified=len(hashes),semantic_input_hashes_verified=len(fragment['input_sha256']),missing_source_files=0,out_of_range_locations=0,dangling_edges=0,self_loops=0,**counts);v['artifact_sha256']={p.name:sha(p) for p in sorted(O.iterdir()) if p.is_file() and p.name!='verification.json'};dump('verification.json',v);print(json.dumps(counts),flush=True)
if __name__=='__main__':main()
