/* Frozen v2 experiment policies; both sides choose speed and gun mode. */
(function(root){'use strict';
const R=typeof module!=='undefined'&&module.exports?require('./replay-rules-v2.js'):root.HexReplayRulesV2;
const scenarios={baseline:['current','current'],focus:['focus','current'],raid:['raid','current'],defence:['guard','raid']};
const templates=new Map();
function options(u){
 const k=[u.budget,u.heading,R.straightSinceTurn(u)].join(':');
 if(!templates.has(k))templates.set(k,R.reachablePlans({...u,id:k,q:0,r:0}).map(p=>({actions:p.actions,end:{q:p.end.q,r:p.end.r,heading:p.end.heading}})));
 return templates.get(k).map(p=>({actions:p.actions,end:{...u,q:u.q+p.end.q,r:u.r+p.end.r,heading:p.end.heading}}));
}
function routeForBudget(u,state,round,policy){
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
function random(seed){let x=seed>>>0;return()=>{x+=0x6d2b79f5;let t=x;t=Math.imul(t^(t>>>15),t|1);t^=t+Math.imul(t^(t>>>7),t|61);return((t^(t>>>14))>>>0)/4294967296;};}


function routeScore(end,state,policy){
 const enemies=state.filter(e=>e.side!==end.side&&R.isAlive(e)),home=state.find(e=>e.side===end.side&&e.type==='carrier');
 const carrier=enemies.find(e=>e.type==='carrier');
 const target=policy==='raid'&&carrier?carrier:policy==='guard'?[...enemies].sort((a,b)=>R.distance(a,home)-R.distance(b,home)||a.id.localeCompare(b.id))[0]:[...enemies].sort((a,b)=>R.distance(end,a)-R.distance(end,b)||a.id.localeCompare(b.id))[0];
 const distance=R.distance(end,target),facing=R.distance(R.advance(end,'F'),target)-distance;
 const spacing=state.filter(e=>e.id!==end.id&&R.isAlive(e)&&R.key(e)===R.key(end)).length;
 const exposure=state.filter(e=>e.side!==end.side&&R.isOperational(e)&&R.canFireAt(e,end)).length;
 return distance*10+facing*2+spacing*4-(R.canFireAt(end,target)?(policy==='current'||policy==='focus'?3:5):0)+(policy==='guard'?exposure*3:0);
}
function movement(state,round,policies){
 const chosen=new Map(),orders={};
 for(const u of state.filter(R.isOperational)){
  let best=null;
  for(const budget of R.speedChoices(u)){
   const candidate=R.withSpeed(u,budget),actions=routeForBudget(candidate,state,round,policies[u.side]),score=routeScore(R.tracePlan(candidate,actions).end,state,policies[u.side]);
   if(!best||score<best.score)best={unit:candidate,actions,score};
  }
  chosen.set(u.id,best.unit);orders[u.id]=best.actions;
 }
 return {state:state.map(u=>chosen.get(u.id)||u),orders};
}
function bestMode(u,target){
 let best=null;
 for(const mode of ['precise','power']){
  const candidate=R.withFireMode(u,mode),[forecast]=R.salvoForecast([candidate,target],{[u.id]:target.id});
  const score=forecast.killChance*100+forecast.expected;
  if(!best||score>best.score)best={unit:candidate,score};
 }
 return best.unit;
}
function aiming(state,policies){
 const orders={},chosen=new Map();
 for(const side of ['player','ai']){
  const assigned=new Map(),policy=policies[side];
  for(const u of state.filter(u=>u.side===side&&R.isOperational(u))){
   const targets=R.fireTargets(u,state);
   if(policy==='focus'||policy==='guard')targets.sort((a,b)=>Math.max(.1,a.hp-(assigned.get(a.id)||0))-Math.max(.1,b.hp-(assigned.get(b.id)||0))||a.id.localeCompare(b.id));
   if(policy==='raid')targets.sort((a,b)=>(a.type==='carrier'?0:1)-(b.type==='carrier'?0:1)||R.distance(u,a)-R.distance(u,b));
   const target=targets[0];orders[u.id]=target?.id??null;
   if(target){const candidate=bestMode(u,target);chosen.set(u.id,candidate);const g=candidate.gun;assigned.set(target.id,(assigned.get(target.id)||0)+g.hitChance*(g.damageMin+g.damageMax)/2);}
  }
 }
 return {state:state.map(u=>chosen.get(u.id)||u),orders};
}
function run(input,{capture=false,verify=false}={}){
 const [a,b]=scenarios[input.scenario],policies=input.swap?{player:b,ai:a}:{player:a,ai:b};
 let state=[...R.initialUnits(),...R.initialCarriers()],result=null;
 const rng=random(input.seed),frames=[],row={rulesVersion:2,scenario:input.scenario,seed:input.seed,swap:input.swap,focal:input.swap?'ai':'player',rounds:0,firstFire:null,noFireRounds:0,shots:0,hits:0,damage:0,carrierDamage:0,blocked:0,firstLoss:null,firstLossWinner:null,speeds:{heavySlow:0,heavyFull:0,lightSlow:0,lightFull:0},modes:{precise:{shots:0,hits:0,damage:0},power:{shots:0,hits:0,damage:0}}};
 for(let round=1;round<=100&&!result;round++){
  const move=movement(state,round,policies);state=move.state;
  for(const u of state.filter(R.isOperational))row.speeds[u.type+(u.budget===R.speedChoices(u)[0]?'Slow':'Full')]++;
  if(capture)frames.push({state,round,label:'Планирование',orders:move.orders,shots:[]});
  const moved=R.simulate(state,move.orders);
  for(let i=0;i<moved.frames.length;i++){const f=moved.frames[i];row.blocked+=f.blockedIds.length;if(capture)frames.push({state:f.units,round,label:'Движение '+(i+1)+'/'+moved.frames.length,orders:move.orders,shots:[],blockedIds:f.blockedIds});}
  state=moved.units;
  if(!R.hasFireTargets(state)){row.noFireRounds++;if(capture)frames.push({state,round,label:'Нет целей — стрельба пропущена',shots:[]});}
  else{
   row.firstFire??=round;const aim=aiming(state,policies);state=aim.state;
   if(capture)frames.push({state,round,label:'Прицеливание',fireOrders:aim.orders,shots:[]});
   const fire=R.resolveFire(state,aim.orders,rng);row.shots+=fire.shots.length;
   for(const shot of fire.shots){const u=state.find(u=>u.id===shot.shooterId),mode=row.modes[u.fireMode];mode.shots++;mode.hits+=Number(shot.hit);mode.damage+=shot.damage;row.hits+=Number(shot.hit);row.damage+=shot.damage;if(state.find(u=>u.id===shot.targetId).type==='carrier')row.carrierDamage+=shot.damage;}
   const lost=fire.destroyedIds.map(id=>state.find(u=>u.id===id));
   if(!row.firstLoss&&lost.length){const sides=new Set(lost.map(u=>u.side));row.firstLoss=sides.size===2?'both':lost[0].side;}
   state=fire.units;if(capture)frames.push({state,round,label:'Залп',shots:fire.shots});
  }
  row.rounds=round;result=R.battleResult(state);
 }
 row.result=result===null?'timeout':result==='draw'?'draw':result===row.focal?'win':'loss';
 row.carriersLost=state.filter(u=>u.type==='carrier'&&!R.isAlive(u)).length;row.dronesDestroyed=state.filter(u=>u.type!=='carrier'&&!R.isAlive(u)).length;
 row.dronesDisabled=state.filter(u=>u.type!=='carrier'&&R.isAlive(u)&&u.disabled).length;row.survivors=state.filter(u=>u.side===row.focal&&R.isOperational(u)).length;
 row.firstLossWinner=result&&result!=='draw'&&row.firstLoss&&row.firstLoss!=='both'?result!==row.firstLoss:null;
 if(verify)for(const key of Object.keys(row))if(JSON.stringify(row[key])!==JSON.stringify(input[key]))throw Error('Новый архив не совпал: '+key);
 if(capture)frames.push({state,round:row.rounds,label:result===null?'Лимит 100 раундов':result==='draw'?'Ничья':result==='player'?'Победа голубых':'Победа красных',shots:[],result:row.result});
 return {row,frames,policies,verified:verify,result:row.result};
}
const api={run,build:row=>run(row,{capture:true,verify:true}),movement,aiming};if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.HexReplayV2=api;
})(globalThis);
