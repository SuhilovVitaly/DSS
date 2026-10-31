/* Archived policies and deterministic replay of the original experiment. */
(function(root){'use strict';
const R=typeof module!=='undefined'&&module.exports?require('./replay-rules-v1.js'):root.HexReplayRulesV1;
const scenarios={baseline:['current','current'],focus:['focus','current'],raid:['raid','current'],defence:['guard','raid']};
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

function build(row){
 if(row.rulesVersion===2)return (typeof module!=='undefined'&&module.exports?require('./replay-v2.js'):root.HexReplayV2).build(row);
 const [a,b]=scenarios[row.scenario],policies=row.swap?{player:b,ai:a}:{player:a,ai:b};
 let state=[...R.initialUnits(),...R.initialCarriers()],result=null,round=0,shotsCount=0,damage=0,hits=0,blocked=0;
 const rng=random(row.seed),frames=[];
 for(round=1;round<=100&&!result;round++){
  const orders=Object.fromEntries(state.filter(R.isOperational).map(u=>[u.id,plan(u,state,round,policies[u.side])]));
  frames.push({state,round,label:'Планирование',orders,shots:[]});
  const moved=R.simulate(state,orders);
  moved.frames.forEach((f,i)=>{blocked+=f.blockedIds.length;frames.push({state:f.units,round,label:'Движение '+(i+1)+'/9',orders,shots:[],blockedIds:f.blockedIds});});state=moved.units;
  if(R.hasFireTargets(state)){
   const orders={...fireOrders(state,'player',policies.player),...fireOrders(state,'ai',policies.ai)};
   frames.push({state,round,label:'Прицеливание',fireOrders:orders,shots:[]});
   const fire=R.resolveFire(state,orders,rng);state=fire.units;shotsCount+=fire.shots.length;damage+=fire.shots.reduce((n,s)=>n+s.damage,0);hits+=fire.shots.filter(s=>s.hit).length;
   frames.push({state,round,label:'Залп',shots:fire.shots});
  }else frames.push({state,round,label:'Нет целей — стрельба пропущена',shots:[]});
  result=R.battleResult(state);
 }
 const actual=result===null?'timeout':result==='draw'?'draw':result===row.focal?'win':'loss';
 const verified=actual===row.result&&round-1===row.rounds&&shotsCount===row.shots&&damage===row.damage&&hits===row.hits&&blocked===row.blocked;
 if(!verified)throw Error('Архивный бой не совпал с записью');
 frames.push({state,round:round-1,label:result===null?'Лимит 100 раундов':result==='draw'?'Ничья':result==='player'?'Победа голубых':'Победа красных',shots:[],result:actual});
 return {frames,policies,verified,result:actual};
}
const api={build};if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.HexReplay=api;
})(globalThis);
