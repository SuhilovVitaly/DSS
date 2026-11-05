/* Reproducible experiments against the same rules used by the browser.
   node simulate-battles.cjs ABSOLUTE_OUTPUT_DIRECTORY [pairsPerScenario=250]
   Four scenarios, each played twice per seed with swapped starting sides. */
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),R=require('./replay-rules-v1.js');
const output=process.argv[2],pairs=Number(process.argv[3]||250),limit=100;
if(!output||!path.isAbsolute(output)||!Number.isInteger(pairs)||pairs<1)throw Error('Absolute output directory and positive integer pairs required');
fs.mkdirSync(output,{recursive:true});
const scenarios=[['baseline','current','current'],['focus','focus','current'],['raid','raid','current'],['defence','guard','raid']];
const templates=new Map();
function options(u){
 const k=[u.budget,u.heading,R.straightSinceTurn(u)].join(':');
 if(!templates.has(k))templates.set(k,R.reachablePlans({...u,id:k,q:0,r:0}).map(p=>({actions:p.actions,end:{q:p.end.q,r:p.end.r,heading:p.end.heading}})));
 return templates.get(k).map(p=>({actions:p.actions,end:{...u,q:u.q+p.end.q,r:u.r+p.end.r,heading:p.end.heading}}));
}
function plan(u,state,round,policy){
 if(policy==='current'||policy==='focus')return R.chooseAIPlan(u,state,round);
 const enemies=state.filter(e=>e.side!==u.side&&R.isAlive(e)),enemyCarrier=enemies.find(e=>e.type==='carrier'),home=state.find(e=>e.side===u.side&&e.type==='carrier');
 let best,score=Infinity;
 for(const p of options(u)){
  const target=policy==='raid'&&enemyCarrier?enemyCarrier:[...enemies].sort((a,b)=>R.distance(a,home)-R.distance(b,home)||a.id.localeCompare(b.id))[0];
  const d=R.distance(p.end,target),ahead=R.advance(p.end,'F'),facing=R.distance(ahead,target)-d;
  const fire=R.canFireAt(p.end,target)?1:0;
  const exposure=enemies.filter(e=>R.isOperational(e)&&R.canFireAt(e,p.end)).length;
  const spacing=state.filter(e=>e.id!==u.id&&R.isAlive(e)&&R.key(e)===R.key(p.end)).length;
  const value=d*10+facing*2-fire*5+spacing*4+(policy==='guard'?exposure*3:0);
  if(value<score){score=value;best=p;}
 }
 return best.actions;
}
function fireOrders(state,side,policy){
 const orders={},assigned=new Map();
 for(const u of state.filter(u=>u.side===side&&R.isOperational(u))){
  let targets=R.fireTargets(u,state);
  if(policy==='focus'||policy==='guard')targets.sort((a,b)=>Math.max(.1,a.hp-(assigned.get(a.id)||0))-Math.max(.1,b.hp-(assigned.get(b.id)||0))||a.id.localeCompare(b.id));
  if(policy==='raid')targets.sort((a,b)=>(a.type==='carrier'?0:1)-(b.type==='carrier'?0:1)||R.distance(u,a)-R.distance(u,b));
  const t=targets[0];orders[u.id]=t?.id??null;if(t)assigned.set(t.id,(assigned.get(t.id)||0)+1.5);
 }
 return orders;
}
function random(seed){let x=seed>>>0;return()=>{x+=0x6d2b79f5;let t=x;t=Math.imul(t^(t>>>15),t|1);t^=t+Math.imul(t^(t>>>7),t|61);return((t^(t>>>14))>>>0)/4294967296;};}
function battle(name,a,b,seed,swap){
 const focal=swap?'ai':'player',policies=swap?{player:b,ai:a}:{player:a,ai:b};
 let state=[...R.initialUnits(),...R.initialCarriers()],result=null;
 const rng=random(seed),row={scenario:name,seed,swap,focal,rounds:0,firstFire:null,noFireRounds:0,shots:0,hits:0,damage:0,carrierDamage:0,blocked:0,firstLoss:null,firstLossWinner:null};
 for(let round=1;round<=limit&&!result;round++){
  const orders=Object.fromEntries(state.filter(R.isOperational).map(u=>[u.id,plan(u,state,round,policies[u.side])]));
  const moved=R.simulate(state,orders);state=moved.units;row.blocked+=moved.frames.reduce((n,f)=>n+f.blockedIds.length,0);
  assert.equal(new Set(state.filter(R.isAlive).map(R.key)).size,state.filter(R.isAlive).length);
  if(!R.hasFireTargets(state))row.noFireRounds++;else{
   row.firstFire??=round;
   const fire=R.resolveFire(state,{...fireOrders(state,'player',policies.player),...fireOrders(state,'ai',policies.ai)},rng);
   row.shots+=fire.shots.length;row.hits+=fire.shots.filter(s=>s.hit).length;row.damage+=fire.shots.reduce((n,s)=>n+s.damage,0);
   row.carrierDamage+=fire.shots.filter(s=>state.find(u=>u.id===s.targetId).type==='carrier').reduce((n,s)=>n+s.damage,0);
   const lost=fire.destroyedIds.map(id=>state.find(u=>u.id===id));
   if(!row.firstLoss&&lost.length){const sides=new Set(lost.map(u=>u.side));row.firstLoss=sides.size===2?'both':lost[0].side;}
   state=fire.units;
  }
  row.rounds=round;result=R.battleResult(state);
 }
 row.result=result===null?'timeout':result==='draw'?'draw':result===focal?'win':'loss';
 row.carriersLost=state.filter(u=>u.type==='carrier'&&!R.isAlive(u)).length;
 row.dronesDestroyed=state.filter(u=>u.type!=='carrier'&&!R.isAlive(u)).length;
 row.dronesDisabled=state.filter(u=>u.type!=='carrier'&&R.isAlive(u)&&u.disabled).length;
 row.survivors=state.filter(u=>u.side===focal&&R.isOperational(u)).length;
 row.firstLossWinner=result&&result!=='draw'&&row.firstLoss&&row.firstLoss!=='both'?result!==row.firstLoss:null;
 return row;
}
const rows=[];
for(const [name,a,b] of scenarios){for(let seed=1;seed<=pairs;seed++)for(const swap of [false,true])rows.push(battle(name,a,b,seed,swap));console.log(name+': '+rows.length+' battles complete');}
function summary(rows){const sum=k=>rows.reduce((n,r)=>n+r[k],0),quantile=(xs,q)=>xs.sort((a,b)=>a-b)[Math.floor((xs.length-1)*q)]??null;const resolved=rows.filter(r=>r.result!=='timeout'),first=rows.filter(r=>r.firstLossWinner!==null);return {battles:rows.length,wins:rows.filter(r=>r.result==='win').length,losses:rows.filter(r=>r.result==='loss').length,draws:rows.filter(r=>r.result==='draw').length,timeouts:rows.length-resolved.length,meanRounds:sum('rounds')/rows.length,medianResolvedRounds:quantile(resolved.map(r=>r.rounds),.5),p90ResolvedRounds:quantile(resolved.map(r=>r.rounds),.9),meanFirstFire:rows.filter(r=>r.firstFire!==null).reduce((n,r)=>n+r.firstFire,0)/rows.filter(r=>r.firstFire!==null).length,noFireRoundFraction:sum('noFireRounds')/sum('rounds'),shots:sum('shots'),hits:sum('hits'),damage:sum('damage'),carrierDamage:sum('carrierDamage'),carrierLossBattles:rows.filter(r=>r.carriersLost>0).length,dronesDestroyed:sum('dronesDestroyed'),dronesDisabled:sum('dronesDisabled'),meanBlockedActions:sum('blocked')/rows.length,firstLossPredictsWinner:{eligible:first.length,opponentOfFirstLossWins:first.filter(r=>r.firstLossWinner).length},winsByStartingSide:{player:rows.filter(r=>r.focal==='player'&&r.result==='win').length,ai:rows.filter(r=>r.focal==='ai'&&r.result==='win').length}};}
const report={rules:{roundLimit:limit,pairsPerScenario:pairs,damage:[1,3],hitChance:.75,carrierHull:15},method:'Fixed initial deployment. Four deterministic policies; only fire uses seeded PRNG. Each seed is replayed with focal policy on each starting side. Timeout is unresolved, not draw. Custom policies use current positions, never enemy pending orders. Carrier is a one-hex stationary collision obstacle.',total:summary(rows),scenarios:Object.fromEntries(scenarios.map(([name,a,b])=>[name,{policies:[a,b],...summary(rows.filter(r=>r.scenario===name))}]))};
fs.writeFileSync(path.join(output,'battles.json'),JSON.stringify(rows));fs.writeFileSync(path.join(output,'statistics.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
