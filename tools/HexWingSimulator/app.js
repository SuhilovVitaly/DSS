/* Standalone prototype. No network, packages, build step or external assets. */
(() => {
  'use strict';
  const R=window.HexRules,$=id=>document.getElementById(id),S=40,DX=Math.sqrt(3)*S;
  const BLUE='#57dce5',RED='#ef7970',WHITE='#edf4fa',MUTED='#8fa6b9';
  const names={F:'ШАГ',L:'↶ 60°',R:'60° ↷'},short={F:'Ш',L:'Л',R:'П'};
  const xy=p=>({x:DX*(p.q+p.r/2),y:1.5*S*p.r});
  const hex=(x,y)=>Array.from({length:6},(_,i)=>{const a=(60*i-30)*Math.PI/180;return `${x+S*Math.cos(a)},${y+S*Math.sin(a)}`;}).join(' ');
  const text=(x,y,value,color=MUTED,size=12)=>`<text x="${x}" y="${y}" text-anchor="middle" fill="${color}" font-size="${size}">${value}</text>`;
  let units,round,phase,selected,orders,aiOrders,logs,paths,camera,token=0,substep=0,hoverCell=null;
  function unit(){return units.find(u=>u.id===selected);}
  function addLog(message){logs.push(`Раунд ${round}: ${message}`);if(logs.length>100)logs.shift();}
  function prepare(){hoverCell=null;orders=Object.fromEntries(units.filter(u=>u.side==='player').map(u=>[u.id,'F'.repeat(u.budget)]));aiOrders=Object.fromEntries(units.filter(u=>u.side==='ai').map(u=>[u.id,R.chooseAIPlan(u,units,round)]));paths=[];}
  function reset(){token++;units=R.initialUnits();round=1;phase='movement';selected='a1';logs=[];substep=0;prepare();fitCamera();addLog('Выберите траектории четырём дронам. По умолчанию все летят прямо.');render();}
  function fitCamera(){const p=[...units];if(phase==='movement')for(const u of units.filter(u=>u.side==='player'))p.push(...R.enumeratePlans(u).map(v=>v.end));const a=p.map(xy);const minX=Math.min(...a.map(p=>p.x))-110,maxX=Math.max(...a.map(p=>p.x))+110,minY=Math.min(...a.map(p=>p.y))-110,maxY=Math.max(...a.map(p=>p.y))+110;camera={x:minX,y:minY,w:Math.max(620,maxX-minX),h:Math.max(430,maxY-minY)};}
  function endpointArrow(x,y,heading,color,actions,interactive=false){const a=(heading-90)*Math.PI/180,ux=Math.cos(a),uy=Math.sin(a),end={x:x+24*ux,y:y+24*uy},b={x:x+14*ux,y:y+14*uy};return `<g ${interactive?`data-end-plan="${actions}" data-heading="${heading}" style="cursor:pointer;pointer-events:all"`:'pointer-events="none"'}><circle cx="${end.x}" cy="${end.y}" r="11" fill="transparent"/><path d="M${x},${y} L${b.x},${b.y}" stroke="${color}" stroke-width="2" pointer-events="none"/><polygon points="${end.x},${end.y} ${b.x-4*uy},${b.y+4*ux} ${b.x+4*uy},${b.y-4*ux}" fill="${color}" pointer-events="none"/>${interactive?`<title>Курс ${heading}°</title>`:''}</g>`;}
  function drawBoard(){
    const current=unit(),canPlan=phase==='movement',options=canPlan?R.enumeratePlans(current):[];
    const ends=new Map();options.forEach(p=>{const k=R.key(p.end);if(!ends.has(k))ends.set(k,{...p.end,plans:[],label:String.fromCharCode(65+ends.size)});ends.get(k).plans.push(p);});
    let scene='';
    // Generate only visible cells, with no artificial movement boundary.
    const rMin=Math.floor(camera.y/(1.5*S))-1,rMax=Math.ceil((camera.y+camera.h)/(1.5*S))+1;
    for(let r=rMin;r<=rMax;r++){const qMin=Math.floor(camera.x/DX-r/2)-1,qMax=Math.ceil((camera.x+camera.w)/DX-r/2)+1;for(let q=qMin;q<=qMax;q++){const {x,y}=xy({q,r}),end=ends.get(`${q},${r}`);scene+=`<polygon points="${hex(x,y)}" class="cell${end?' target':''}" data-cell="${q},${r}"/>`;}}
    if(canPlan){
      // Show one saved order for every friendly drone, never all alternative routes.
      for(const u of units.filter(u=>u.side==='player')){
        const plan=R.enumeratePlans(u).find(p=>p.actions===orders[u.id]);if(!plan)continue;
        const end=xy(plan.end),active=u.id===selected;
        scene+=`<g data-saved-unit="${u.id}" pointer-events="none" opacity="${active?1:.65}"><polyline class="route${active?' selected':''}" data-saved-plan="${plan.actions}" points="${plan.points.map(p=>{const z=xy(p);return `${z.x},${z.y}`;}).join(' ')}"/>${endpointArrow(end.x,end.y,plan.end.heading,active?WHITE:BLUE,plan.actions)}</g>`;
      }
      scene+='<g id="hoverRoute" pointer-events="none"></g>';
    }
    if(!canPlan)for(const p of paths){const a=xy(p.from),b=xy(p.to);scene+=`<path d="M${a.x},${a.y} L${b.x},${b.y}" stroke="${p.side==='player'?'#57dce54d':'#ef79704d'}" stroke-width="2" fill="none" stroke-dasharray="4 5"/>`;}
    for(const u of units){const {x,y}=xy(u),color=u.side==='player'?BLUE:RED;const shape=u.type==='heavy'?'-23,-11 -7,-11 -13,-18 2,-13 26,0 2,13 -13,18 -7,11 -23,11 -17,0':'-19,-5 -12,-13 8,-6 24,0 8,6 -12,13 -19,5 -13,0';scene+=`<g class="unit ${u.side}${u.id===selected?' selected':''}" data-unit="${u.id}"><polygon class="ring" points="${hex(x,y)}"/><g id="sprite-${u.id}" transform="translate(${x},${y}) rotate(${u.heading-90})"><polygon points="${shape}" fill="#132a3a" stroke="${color}"/><path d="M-9,0 H13 M-5,-4 H4 M-5,4 H4" stroke="${color}" stroke-width="1.4"/><path d="M-23,-3 L-29,0 L-23,3" fill="${color}"/></g>${text(x,y+32,`${u.name} · ${u.budget}`,color,12)}</g>`;}
    const shared=new Map();for(const u of units){const k=R.key(u);shared.set(k,(shared.get(k)||0)+1);}for(const [k,count] of shared)if(count>1){const [q,r]=k.split(',').map(Number),{x,y}=xy({q,r});scene+=`<circle cx="${x+25}" cy="${y-25}" r="12" fill="#f0be77"/>${text(x+25,y-21,count,'#07111b',13)}`;}
    if(canPlan)scene+='<g id="hoverPreview" pointer-events="none"></g>';
    $('board').setAttribute('viewBox',`${camera.x} ${camera.y} ${camera.w} ${camera.h}`);$('board').innerHTML=scene;
    paintHover();
  }
  function paintHover(){
    const overlay=$('hoverPreview');if(!overlay)return;
    const saved=$('board').querySelector(`[data-saved-unit="${selected}"]`);if(saved)saved.setAttribute('opacity','1');
    $('board').querySelectorAll('.hovered').forEach(el=>el.classList.remove('hovered'));
    const occupiedByFriendly=units.some(u=>u.side==='player'&&R.key(u)===hoverCell);
    const candidates=occupiedByFriendly?[]:R.enumeratePlans(unit()).filter(p=>R.key(p.end)===hoverCell);
    if(!candidates.length){overlay.innerHTML='';$('hoverRoute').innerHTML='';$('fieldHint').textContent='Наведи мышь на затемнённый гекс, чтобы увидеть траекторию';return;}
    const plan=candidates.find(p=>p.actions===orders[selected])||candidates[0],end=xy(plan.end);
    if(saved)saved.setAttribute('opacity','.22');
    const headings=new Map();for(const p of candidates)if(!headings.has(p.end.heading)||p.actions===plan.actions)headings.set(p.end.heading,p);
    $('hoverRoute').innerHTML=`<polyline class="route selected" data-preview-plan="${plan.actions}" points="${plan.points.map(p=>{const z=xy(p);return `${z.x},${z.y}`;}).join(' ')}"/>`;
    overlay.innerHTML=[...headings.values()].map(p=>endpointArrow(end.x,end.y,p.end.heading,p.actions===plan.actions?WHITE:BLUE,p.actions,true)).join('');
    const cell=$('board').querySelector(`[data-cell="${hoverCell}"]`);if(cell)cell.classList.add('hovered');
    $('fieldHint').textContent=`Предпросмотр: ${plan.actions.split('').map(a=>short[a]).join(' → ')} · курс ${plan.end.heading}°${candidates.length>1?' · клик переключает варианты':' · клик выбирает план'}`;
  }
  function pointerCell(event){
    const point=$('board').createSVGPoint();point.x=event.clientX;point.y=event.clientY;
    const matrix=$('board').getScreenCTM();if(!matrix)return null;
    const p=point.matrixTransform(matrix.inverse()),r=p.y/(1.5*S),q=p.x/DX-r/2;
    let x=Math.round(q),z=Math.round(r),y=Math.round(-q-r);
    const xd=Math.abs(x-q),yd=Math.abs(y+q+r),zd=Math.abs(z-r);
    if(xd>yd&&xd>zd)x=-y-z;else if(zd>=yd)z=-x-y;
    return `${x},${z}`;
  }
  function friendlyUnderPointer(event){
    // The WHOLE friendly hex has priority, including empty space around its sprite.
    const cell=pointerCell(event),occupant=units.find(u=>u.side==='player'&&R.key(u)===cell);
    if(occupant)return $('board').querySelector(`[data-unit="${occupant.id}"]`);
    // Also cover the label/outline if they protrude outside that hex.
    for(const element of document.elementsFromPoint(event.clientX,event.clientY)){
      const own=element.closest('.unit.player');if(own&&$('board').contains(own))return own;
    }
    return null;
  }
  function clearUnitHover(){$('board').querySelectorAll('.hover-unit').forEach(el=>el.classList.remove('hover-unit'));}
  $('board').addEventListener('pointermove',event=>{
    if(phase!=='movement')return;clearUnitHover();const own=friendlyUnderPointer(event);
    if(own){hoverCell=null;paintHover();own.classList.add('hover-unit');$('fieldHint').textContent='Нажми на дрон, чтобы выбрать его. Сохранённые приказы остаются.';return;}
    const next=pointerCell(event);if(next!==hoverCell){hoverCell=next;paintHover();}
  });
  $('board').addEventListener('pointerleave',()=>{clearUnitHover();hoverCell=null;paintHover();});
  function render(){
    const u=unit(),editing=phase==='movement',busy=phase==='executing',options=R.enumeratePlans(u),draft=orders[selected]||'';
    $('phaseTitle').textContent=`РАУНД ${round} · ${editing?'ДВИЖЕНИЕ':busy?`ДВИЖЕНИЕ ${substep}/4`:'СТРЕЛЬБА'}`;
    $('fieldHint').textContent=editing?'Наведи мышь на затемнённый гекс, чтобы увидеть траекторию':busy?'Обе стороны выполняют приказы одновременно':'Движение завершено. Стрельба пока без расчётов.';
    $('unitInfo').innerHTML=`<h2>${u.name}</h2><div class="meta">${u.type==='heavy'?'Тяжёлый':'Лёгкий'} · ход ${u.budget} · курс ${u.heading}°</div>`;
    const chosen=options.find(p=>p.actions===draft),sameCell=chosen?options.filter(p=>R.key(p.end)===R.key(chosen.end)):[],headingOptions=new Map();
    for(const p of sameCell)if(!headingOptions.has(p.end.heading)||p.actions===draft)headingOptions.set(p.end.heading,p);
    $('headingChoices').hidden=!editing||headingOptions.size<2;
    $('headingChoices').innerHTML='<div class="sectiontitle">КОНЕЧНЫЙ КУРС В ВЫБРАННОЙ КЛЕТКЕ</div><div class="edit">'+[...headingOptions.values()].map(p=>`<button data-heading-plan="${p.actions}" class="${p.actions===draft?'heading-selected':''}">${p.end.heading}°</button>`).join('')+'</div>';
    $('planner').hidden=!editing;
    const cells=new Map();for(const p of options){const k=R.key(p.end);if(!cells.has(k))cells.set(k,String.fromCharCode(65+cells.size));}
    $('plans').innerHTML=options.map(p=>`<button class="plan${p.actions===draft?' selected':''}${draft.length<u.budget&&p.actions.startsWith(draft)?' preview':''}" data-plan="${p.actions}"><span>${p.actions.split('').map(v=>short[v]).join(' → ')}</span><span class="finish">${cells.get(R.key(p.end))} / ${p.end.heading}°</span></button>`).join('');
    $('actionSlots').innerHTML=Array.from({length:u.budget},(_,i)=>`<div class="slot${draft[i]?'':' empty'}">${draft[i]?names[draft[i]]:'—'}</div>`).join('');
    const full=draft.length>=u.budget,lastTurn=/[LR]$/.test(draft);$('forward').disabled=!editing||full;$('left').disabled=$('right').disabled=!editing||full||draft.length===0||lastTurn;$('undo').disabled=!editing||draft.length<=1;$('clear').disabled=!editing;
    $('draftHelp').textContent=`${draft.length}/${u.budget} действий. ${full?'План готов. Для ручного ввода нажми «Сначала».':lastTurn?'Теперь нужен шаг вперёд.':'Выбери следующее действие.'}`;
    $('fleet').innerHTML=units.filter(u=>u.side==='player').map(u=>`<button class="unitcard${selected===u.id?' selected':''}" data-select="${u.id}" ${busy?'disabled':''}><b>${u.name}</b><small>${u.type==='heavy'?'Тяжёлый':'Лёгкий'} · ход ${u.budget}</small><small>${editing?(orders[u.id]||'').split('').map(v=>short[v]).join(' → '):`Курс ${u.heading}°`}</small></button>`).join('');
    const ready=units.filter(u=>u.side==='player').every(u=>R.validatePlan(u,orders[u.id]));
    $('mainAction').textContent=editing?'ВЫПОЛНИТЬ ДВИЖЕНИЕ':busy?'ИСПОЛНЕНИЕ…':'ЗАВЕРШИТЬ СТРЕЛЬБУ';$('mainAction').disabled=busy||(editing&&!ready);$('reset').disabled=busy;$('fitView').disabled=busy;
    $('status').textContent=busy?'Проверка столкновений после каждого действия.':editing?(ready?'Все четыре плана готовы. ИИ уже выбрал свои приказы.':'Дополните планы всех четырёх дронов.'):'Фаза-заглушка: выстрелов, урона и потерь нет.';
    $('log').innerHTML=logs.map(line=>`<div>${line}</div>`).join('');$('log').scrollTop=$('log').scrollHeight;
    $('phaseOverlay').hidden=phase!=='shooting';$('phaseOverlay').innerHTML=phase==='shooting'?'<div class="phasecard"><h2>ФАЗА СТРЕЛЬБЫ</h2><p>Пока заглушка: никто не стреляет.<br>Заверши фазу, чтобы перейти к следующему ходу.</p></div>':'';
    drawBoard();
  }
  function select(id){if(phase==='executing'||!units.some(u=>u.id===id&&u.side==='player'))return;hoverCell=null;selected=id;render();}
  function choose(actions){if(phase!=='movement'||!R.validatePlan(unit(),actions))return;orders[selected]=actions;render();}
  $('board').addEventListener('click',e=>{const own=friendlyUnderPointer(e);if(own){select(own.dataset.unit);return;}if(e.target.closest('[data-unit]'))return;if(phase!=='movement')return;const direct=e.target.closest('[data-end-plan]');if(direct){choose(direct.dataset.endPlan);return;}const cell=e.target.closest('[data-cell]');if(cell){const list=R.enumeratePlans(unit()).filter(p=>R.key(p.end)===cell.dataset.cell);if(list.length){const i=list.findIndex(p=>p.actions===orders[selected]);choose(list[(i+1)%list.length].actions);}}});
  $('plans').addEventListener('click',e=>{const b=e.target.closest('[data-plan]');if(b)choose(b.dataset.plan);});
  $('headingChoices').addEventListener('click',e=>{const b=e.target.closest('[data-heading-plan]');if(b)choose(b.dataset.headingPlan);});
  $('fleet').addEventListener('click',e=>{const b=e.target.closest('[data-select]');if(b)select(b.dataset.select);});
  function append(a){if(phase!=='movement')return;const p=orders[selected];if(p.length>=unit().budget||a!=='F'&&(p.length===0||/[LR]$/.test(p)))return;orders[selected]+=a;render();}
  $('forward').onclick=()=>append('F');$('left').onclick=()=>append('L');$('right').onclick=()=>append('R');$('undo').onclick=()=>{if(phase==='movement'&&orders[selected].length>1){orders[selected]=orders[selected].slice(0,-1);render();}};$('clear').onclick=()=>{if(phase==='movement'){orders[selected]='F';render();}};
  function animate(from,to,duration,myToken){return new Promise(resolve=>{const started=performance.now();function frame(now){if(myToken!==token){resolve(false);return;}const f=Math.min(1,(now-started)/duration),ease=f*f*(3-2*f);for(const b of to){const a=from.find(u=>u.id===b.id),p=xy(a),q=xy(b),angle=((b.heading-a.heading+540)%360)-180;const el=$(`sprite-${b.id}`);if(el)el.setAttribute('transform',`translate(${p.x+(q.x-p.x)*ease},${p.y+(q.y-p.y)*ease}) rotate(${a.heading-90+angle*ease})`);}if(f<1)requestAnimationFrame(frame);else resolve(true);}requestAnimationFrame(frame);});}
  async function execute(){
    if(phase!=='movement')return;const allOrders={...orders,...aiOrders};if(!units.every(u=>R.validatePlan(u,allOrders[u.id])))return;
    phase='executing';const myToken=++token;paths=[];addLog('Началось одновременное движение.');
    for(let i=0;i<4;i++){
      substep=i+1;render();const before=units,frame=R.step(before,allOrders,i),fast=$('fast').checked;
      for(const c of frame.conflicts)addLog(`${c.names.join(' и ')}: клетка будет занята — шаг отменён.`);
      if(!await animate(before,frame.units,fast?70:440,myToken))return;
      for(let j=0;j<before.length;j++)if(R.key(before[j])!==R.key(frame.units[j]))paths.push({from:before[j],to:frame.units[j],side:before[j].side});
      units=frame.units;
    }
    phase='shooting';addLog('Движение завершено. Фаза стрельбы — заглушка.');render();
  }
  $('mainAction').onclick=()=>{if(phase==='movement')execute();else if(phase==='shooting'){round++;phase='movement';prepare();addLog('Началось планирование нового хода.');render();}};
  $('reset').onclick=reset;
  $('fitView').onclick=()=>{if(phase==='executing')return;hoverCell=null;fitCamera();render();};
  document.addEventListener('keydown',e=>{if(e.target.matches('input,button'))return;if(e.key==='Tab'){e.preventDefault();const list=units.filter(u=>u.side==='player'),i=list.findIndex(u=>u.id===selected);select(list[(i+(e.shiftKey?3:1))%4].id);}if(e.key==='Enter'&&!$('mainAction').disabled)$('mainAction').click();});
  reset();
})();
