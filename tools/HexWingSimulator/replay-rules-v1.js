/* Frozen rules of the original 2000-battle experiment. Do not change for live balance. */
(function (root) {
  'use strict';
  const DIRECTIONS = {30:[1,-1],90:[1,0],150:[0,1],210:[-1,1],270:[-1,0],330:[0,-1]};
  const COURSE_GUN=Object.freeze({range:12,halfAngle:30,damageMin:1,damageMax:3,hitChance:.75}),HULL=Object.freeze({light:6,heavy:9,carrier:15}),TURN_STRAIGHT_STEPS=2;
  const maxHp=unit=>unit.maxHp??HULL[unit.type]??HULL.light;
  const isAlive=unit=>(unit.hp??maxHp(unit))>0;
  const isOperational=unit=>isAlive(unit)&&unit.type!=='carrier'&&!unit.disabled;
  function applyCarrierLoss(units){const lost=new Set(units.filter(u=>u.type==='carrier'&&!isAlive(u)).map(u=>u.side));return units.map(u=>u.type!=='carrier'&&lost.has(u.side)?{...u,disabled:true}:{...u});}
  const key = p => `${p.q},${p.r}`;
  const distance = (a,b) => (Math.abs(a.q-b.q)+Math.abs(a.r-b.r)+Math.abs(a.q+a.r-b.q-b.r))/2;
  const straightSinceTurn = unit => unit.straightSinceTurn??TURN_STRAIGHT_STEPS;
  const canTurn = unit => straightSinceTurn(unit)>=TURN_STRAIGHT_STEPS;
  function advance(unit, action) {
    const next = {...unit};
    if(action==='F'){const d=DIRECTIONS[next.heading];next.q+=d[0];next.r+=d[1];next.straightSinceTurn=Math.min(TURN_STRAIGHT_STEPS,straightSinceTurn(unit)+1);}
    else if(action==='L'||action==='R'){
      if(!canTurn(unit))throw new Error('Между поворотами нужны два шага вперёд');
      next.heading=(next.heading+(action==='L'?300:60))%360;next.straightSinceTurn=0;
    }
    else throw new Error('Неизвестное действие');
    return next;
  }
  function validatePlan(unit, plan) {
    if(typeof plan!=='string'||plan.length!==unit.budget||plan[0]!=='F'||!/^[FLR]+$/.test(plan))return false;
    try{tracePlan(unit,plan);return true;}catch{return false;}
  }
  function enumeratePlans(unit) {
    const plans=[];
    function visit(state,seq,points){
      if(seq.length===unit.budget){plans.push({actions:seq,end:state,points});return;}
      for(const action of ['F','L','R']){
        if((seq.length===0&&action!=='F')||(action!=='F'&&!canTurn(state)))continue;
        const next=advance(state,action);
        visit(next,seq+action,action==='F'?[...points,{q:next.q,r:next.r}]:points);
      }
    }
    visit({...unit},'',[{q:unit.q,r:unit.r}]);return plans;
  }
  function tracePlan(unit,actions) {
    if(typeof actions!=='string'||actions.length>unit.budget||!/^[FLR]*$/.test(actions)||(actions&&actions[0]!=='F'))throw new Error('Неверная последовательность');
    let end={...unit};const points=[{q:unit.q,r:unit.r}];
    for(const action of actions){end=advance(end,action);if(action==='F')points.push({q:end.q,r:end.r});}
    return {actions,end,points};
  }
  const reachableCache=new Map();
  function reachablePlans(unit) {
    const cacheKey=[unit.id,unit.q,unit.r,unit.heading,unit.budget,straightSinceTurn(unit),unit.hp??maxHp(unit),maxHp(unit)].join(':');
    if(reachableCache.has(cacheKey))return reachableCache.get(cacheKey);
    let states=[{actions:'',end:{...unit}}];
    const better=(a,b)=>{const turns=s=>s.replaceAll('F','').length;return turns(a)<turns(b)||(turns(a)===turns(b)&&a<b);};
    for(let i=0;i<unit.budget;i++){
      const next=new Map();
      for(const state of states)for(const action of ['F','L','R']){
        if((i===0&&action!=='F')||(action!=='F'&&!canTurn(state.end)))continue;
        const end=advance(state.end,action),actions=state.actions+action;
        const k=[end.q,end.r,end.heading,straightSinceTurn(end)].join(':');
        if(!next.has(k)||better(actions,next.get(k).actions))next.set(k,{actions,end});
      }
      states=[...next.values()];
    }
    const poses=new Map();for(const state of states){const k=[state.end.q,state.end.r,state.end.heading].join(':');if(!poses.has(k)||better(state.actions,poses.get(k).actions))poses.set(k,state);}
    const plans=[...poses.values()].map(p=>tracePlan(unit,p.actions)).sort((a,b)=>a.actions<b.actions?-1:a.actions>b.actions?1:0);
    if(reachableCache.size>=64)reachableCache.delete(reachableCache.keys().next().value);
    reachableCache.set(cacheKey,plans);return plans;
  }
  function initialUnits() {
    const unit=(id,name,side,type,col,r,heading)=>({id,name,side,type,budget:type==='heavy'?6:9,q:3*(col-Math.floor(r/2)),r:3*r,heading,straightSinceTurn:TURN_STRAIGHT_STEPS,hp:HULL[type],maxHp:HULL[type],gun:{...COURSE_GUN}});
    return [unit('a1','Вера','player','heavy',3,3,90),unit('a2','Юн','player','heavy',3,7,90),unit('a3','Марта','player','light',2,4,90),unit('a4','Олег','player','light',2,6,90),unit('b1','Т-1','ai','heavy',13,3,270),unit('b2','Т-2','ai','heavy',13,7,270),unit('b3','Л-1','ai','light',14,4,270),unit('b4','Л-2','ai','light',14,6,270)];
  }
  function initialCarriers(){
    const drones=initialUnits();
    return ['player','ai'].map(side=>{
      const line=drones.filter(u=>u.side===side&&u.type==='heavy');
      const anchor={q:line.reduce((n,u)=>n+u.q,0)/line.length,r:line.reduce((n,u)=>n+u.r,0)/line.length};
      const heading=line[0].heading,d=DIRECTIONS[heading];
      return {id:side==='player'?'carrier-a':'carrier-b',name:side==='player'?'Скиталец':'Чёрный кит',type:'carrier',hp:15,maxHp:15,side,heading,q:anchor.q-20*d[0],r:anchor.r-20*d[1],formationAnchor:anchor,offset:20};
    });
  }
  function chooseAIPlan(unit, units, round) {
    const enemies=units.filter(u=>u.side!==unit.side&&isAlive(u));
    if(!isOperational(unit)||!enemies.length)return 'F'.repeat(unit.budget||0);
    const options=reachablePlans(unit);
    let best=options[0],score=Infinity;
    for(let i=0;i<options.length;i++){const p=options[i];
      let target=enemies[0];
      for(const e of enemies)if(distance(p.end,e)<distance(p.end,target))target=e;
      const ahead=advance(p.end,'F');
      const facing=distance(ahead,target)-distance(p.end,target);
      // Use only current positions, never the human player's pending orders.
      const spacing=units.filter(u=>u.id!==unit.id&&u.side===unit.side&&key(u)===key(p.end)).length;
      const tie=(i+(round+Number(unit.id.slice(1))))%options.length;
      const value=distance(p.end,target)*10+facing*2+spacing*4+tie*.001;
      if(value<score){score=value;best=p;}
    }
    return best.actions;
  }
  function inFireArc(shooter,point){
    const gun=shooter.gun||COURSE_GUN,range=distance(shooter,point);
    if(!isOperational(shooter)||range===0||range>gun.range)return false;
    const dq=point.q-shooter.q,dr=point.r-shooter.r;
    const bearing=Math.atan2(Math.sqrt(3)*(dq+dr/2),-1.5*dr)*180/Math.PI;
    const delta=((bearing-shooter.heading+540)%360)-180;
    return Math.abs(delta)<=gun.halfAngle+1e-9;
  }
  function canFireAt(shooter,target){return shooter.id!==target.id&&shooter.side!==target.side&&isAlive(target)&&inFireArc(shooter,target);}
  function fireTargets(shooter,units){return units.filter(u=>canFireAt(shooter,u)).sort((a,b)=>distance(shooter,a)-distance(shooter,b)||(a.id<b.id?-1:a.id>b.id?1:0));}
  function chooseFireTarget(shooter,units){return fireTargets(shooter,units)[0]?.id??null;}
  function hasFireTargets(units){return units.some(u=>isAlive(u)&&fireTargets(u,units).length>0);}
  function resolveFire(units,orders,random=Math.random){
    units=applyCarrierLoss(units);
    const byId=new Map(units.map(u=>[u.id,u])),shots=[],damage=new Map();
    if(byId.size!==units.length)throw new Error('Повторяющийся ID');
    // Validate and collect all shots from the SAME pre-fire snapshot.
    for(const shooter of units.filter(isOperational)){
      const targetId=orders[shooter.id];if(targetId==null)continue;
      const target=byId.get(targetId);if(!target||!canFireAt(shooter,target))throw new Error(`Недопустимая цель для ${shooter.name}`);
      const gun=shooter.gun||COURSE_GUN;
      shots.push({shooterId:shooter.id,targetId:target.id,shooterName:shooter.name,targetName:target.name,side:shooter.side,hitChance:gun.hitChance,damageMin:gun.damageMin,damageMax:gun.damageMax,from:{q:shooter.q,r:shooter.r},to:{q:target.q,r:target.r}});
    }
    const roll=()=>{const value=random();if(!Number.isFinite(value)||value<0||value>=1)throw new Error('Неверный случайный бросок');return value;};
    for(const shot of shots){
      shot.hit=roll()<shot.hitChance;
      shot.damage=shot.hit?shot.damageMin+Math.floor(roll()*(shot.damageMax-shot.damageMin+1)):0;
      damage.set(shot.targetId,(damage.get(shot.targetId)||0)+shot.damage);
    }
    const result=applyCarrierLoss(units.map(u=>({...u,hp:Math.max(0,(u.hp??maxHp(u))-(damage.get(u.id)||0))})));
    return {units:result,shots,destroyedIds:result.filter(u=>!isAlive(u)&&isAlive(byId.get(u.id))).map(u=>u.id)};
  }
  function battleResult(units){const state=applyCarrierLoss(units),player=state.some(u=>u.side==='player'&&isOperational(u)),ai=state.some(u=>u.side==='ai'&&isOperational(u));return player&&ai?null:player?'player':ai?'ai':'draw';}
  function preventCollisions(before, proposed) {
    if(new Set(before.map(u=>u.id)).size!==before.length)throw new Error('Повторяющийся ID');
    if(new Set(before.map(key)).size!==before.length)throw new Error('Начальная клетка уже занята двумя дронами');
    const previous=new Map(before.map(u=>[u.id,u]));
    const result=proposed.map(u=>({...u})),blocked=new Set(),conflicts=[];
    if(result.length!==before.length||new Set(result.map(u=>u.id)).size!==result.length||result.some(u=>!previous.has(u.id)))throw new Error('Несогласованные дроны');
    for(;;){
      const groups=new Map();for(const u of result){const k=key(u);if(!groups.has(k))groups.set(k,[]);groups.get(k).push(u);}
      const occupied=[...groups.values()].filter(g=>g.length>1);if(!occupied.length)break;
      const cancel=new Set();
      for(const group of occupied){
        const moving=group.filter(u=>key(u)!==key(previous.get(u.id)));
        if(!moving.length)throw new Error('Не удалось предотвратить занятие клетки');
        for(const u of moving)cancel.add(u.id);
        conflicts.push({cell:{q:group[0].q,r:group[0].r},ids:group.map(u=>u.id),names:group.map(u=>u.name),blockedIds:moving.map(u=>u.id)});
      }
      // Cancel simultaneously, then recheck followers whose target is no longer vacated.
      // Every pass blocks at least one additional mover, so at most N passes are needed.
      for(const u of result)if(cancel.has(u.id)){const old=previous.get(u.id);u.q=old.q;u.r=old.r;blocked.add(u.id);}
    }
    return {units:result,proposed:proposed.map(u=>({...u})),blockedIds:[...blocked],conflicts};
  }
  function step(units, orders, index) {
    const state=applyCarrierLoss(units),active=state.filter(isAlive),proposed=active.map(u=>{
      if(!isOperational(u))return {...u};
      const plan=orders[u.id];
      if(typeof plan!=='string'||plan.length!==u.budget||plan[0]!=='F'||!/^[FLR]+$/.test(plan)||(index===0&&!validatePlan(u,plan)))throw new Error(`Неверный план ${u.name}`);
      return index<u.budget?advance(u,plan[index]):{...u};
    });
    const result=preventCollisions(active,proposed),byId=new Map(result.units.map(u=>[u.id,u]));
    return {...result,units:state.map(u=>byId.get(u.id)||{...u})};
  }
  function simulate(units,orders){let current=applyCarrierLoss(units);const frames=[];for(let i=0;i<Math.max(0,...current.filter(isOperational).map(u=>u.budget));i++){const frame=step(current,orders,i);frames.push(frame);current=frame.units;}return {units:current,frames};}
  const api={DIRECTIONS,COURSE_GUN,HULL,maxHp,TURN_STRAIGHT_STEPS,isAlive,isOperational,applyCarrierLoss,key,distance,straightSinceTurn,canTurn,advance,validatePlan,enumeratePlans,tracePlan,reachablePlans,initialUnits,initialCarriers,chooseAIPlan,inFireArc,canFireAt,fireTargets,chooseFireTarget,hasFireTargets,resolveFire,battleResult,preventCollisions,step,simulate};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.HexReplayRulesV1=api;
})(typeof globalThis!=='undefined'?globalThis:this);
