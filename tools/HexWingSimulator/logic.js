(function (root) {
  'use strict';
  const DIRECTIONS = {30:[1,-1],90:[1,0],150:[0,1],210:[-1,1],270:[-1,0],330:[0,-1]};
  const key = p => `${p.q},${p.r}`;
  const distance = (a,b) => (Math.abs(a.q-b.q)+Math.abs(a.r-b.r)+Math.abs(a.q+a.r-b.q-b.r))/2;
  function advance(unit, action) {
    const next = {...unit};
    if(action==='F'){const d=DIRECTIONS[next.heading];next.q+=d[0];next.r+=d[1];}
    else if(action==='L')next.heading=(next.heading+300)%360;
    else if(action==='R')next.heading=(next.heading+60)%360;
    else throw new Error('Неизвестное действие');
    return next;
  }
  function validatePlan(unit, plan) {
    return typeof plan==='string' && plan.length===unit.budget && plan[0]==='F' && /^[FLR]+$/.test(plan) && !/[LR][LR]/.test(plan);
  }
  function enumeratePlans(unit) {
    const plans=[];
    function visit(state,seq,points){
      if(seq.length===unit.budget){plans.push({actions:seq,end:state,points});return;}
      for(const action of ['F','L','R']){
        if((seq.length===0&&action!=='F')||(action!=='F'&&/[LR]$/.test(seq)))continue;
        const next=advance(state,action);
        visit(next,seq+action,action==='F'?[...points,{q:next.q,r:next.r}]:points);
      }
    }
    visit({...unit},'',[{q:unit.q,r:unit.r}]);return plans;
  }
  function initialUnits() {
    const unit=(id,name,side,type,col,r,heading)=>({id,name,side,type,budget:type==='heavy'?3:4,q:col-Math.floor(r/2),r,heading});
    return [unit('a1','Вера','player','heavy',3,3,90),unit('a2','Юн','player','heavy',3,7,90),unit('a3','Марта','player','light',2,4,90),unit('a4','Олег','player','light',2,6,90),unit('b1','Т-1','ai','heavy',13,3,270),unit('b2','Т-2','ai','heavy',13,7,270),unit('b3','Л-1','ai','light',14,4,270),unit('b4','Л-2','ai','light',14,6,270)];
  }
  function chooseAIPlan(unit, units, round) {
    const enemies=units.filter(u=>u.side!==unit.side);
    const options=enumeratePlans(unit);
    let best=options[0],score=Infinity;
    for(const p of options){
      let target=enemies[0];
      for(const e of enemies)if(distance(p.end,e)<distance(p.end,target))target=e;
      const ahead=advance(p.end,'F');
      const facing=distance(ahead,target)-distance(p.end,target);
      // Use only current positions, never the human player's pending orders.
      const spacing=units.filter(u=>u.id!==unit.id&&u.side===unit.side&&key(u)===key(p.end)).length;
      const tie=(options.indexOf(p)+(round+Number(unit.id.slice(1))))%options.length;
      const value=distance(p.end,target)*10+facing*2+spacing*4+tie*.001;
      if(value<score){score=value;best=p;}
    }
    return best.actions;
  }
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
    const proposed=units.map(u=>{if(!validatePlan(u,orders[u.id]))throw new Error(`Неверный план ${u.name}`);return index<u.budget?advance(u,orders[u.id][index]):{...u};});
    return preventCollisions(units,proposed);
  }
  function simulate(units,orders){let current=units.map(u=>({...u}));const frames=[];for(let i=0;i<Math.max(...units.map(u=>u.budget));i++){const frame=step(current,orders,i);frames.push(frame);current=frame.units;}return {units:current,frames};}
  const api={DIRECTIONS,key,distance,advance,validatePlan,enumeratePlans,initialUnits,chooseAIPlan,preventCollisions,step,simulate};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.HexRules=api;
})(typeof globalThis!=='undefined'?globalThis:this);
