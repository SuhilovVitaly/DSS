/* Standalone prototype: all resources are local; no build step is required. */
(() => {
  'use strict';
  const R=window.HexRules,$=id=>document.getElementById(id),S=40/3,DX=Math.sqrt(3)*S;
  const BLUE='#57dce5',RED='#ef7970',WHITE='#edf4fa',MUTED='#8fa6b9';
  const names={F:'ШАГ',L:'↶ 60°',R:'60° ↷'},short={F:'Ш',L:'Л',R:'П'};
  const xy=p=>({x:DX*(p.q+p.r/2),y:1.5*S*p.r});
  const hex=(x,y)=>Array.from({length:6},(_,i)=>{const a=(60*i-30)*Math.PI/180;return `${x+S*Math.cos(a)},${y+S*Math.sin(a)}`;}).join(' ');
  let replay=null;
  let units,carriers,round,phase,selected,orders,aiOrders,logs,paths,camera,token=0,substep=0,hoverCell=null,fireOrders,aiFireOrders,shots=[],winner=null;
  const entities=()=>[...units,...carriers];
  function updateEntities(next){units=next.filter(u=>u.type!=='carrier');carriers=next.filter(u=>u.type==='carrier');}
  const living=side=>units.filter(u=>R.isOperational(u)&&(!side||u.side===side));
  const totalSteps=()=>Math.max(0,...living().map(u=>u.budget));
  const unit=()=>units.find(u=>u.id===selected);
  function ensureSelected(){if(!living('player').some(u=>u.id===selected))selected=living('player')[0]?.id||units.find(u=>u.side==='player').id;}
  function savedPlan(u){return R.validatePlan(u,orders[u.id])?R.tracePlan(u,orders[u.id]):null;}
  function options(u){
    const base=R.reachablePlans(u),saved=savedPlan(u);
    if(!saved||base.some(p=>p.actions===saved.actions))return base;
    return base.map(p=>R.key(p.end)===R.key(saved.end)&&p.end.heading===saved.end.heading?saved:p);
  }
  function groupsFor(u){const groups=new Map();for(const p of options(u)){const k=R.key(p.end);if(!groups.has(k))groups.set(k,[]);groups.get(k).push(p);}return groups;}
  function addLog(message){logs.push(`Раунд ${round}: ${message}`);if(logs.length>100)logs.shift();}
  function prepare(){
    aiOrders={};const snapshot=entities();
    for(const u of living('ai')){
      let best=null;
      for(const budget of R.speedChoices(u)){
        const candidate=R.withFireMode(R.withSpeed(u,budget),u.type==='heavy'?'power':'precise'),actions=R.chooseAIPlan(candidate,snapshot,round),end=R.tracePlan(candidate,actions).end;
        const enemies=snapshot.filter(e=>e.side!==u.side&&R.isAlive(e));
        const score=Math.min(...enemies.map(e=>R.distance(end,e)*10+(R.distance(R.advance(end,'F'),e)-R.distance(end,e))*2-(R.canFireAt(end,e)?3:0)));
        if(!best||score<best.score)best={candidate,actions,score};
      }
      units=units.map(e=>e.id===u.id?best.candidate:e);aiOrders[u.id]=best.actions;
    }
    ensureSelected();hoverCell=null;orders=Object.fromEntries(living('player').map(u=>[u.id,'F'.repeat(u.budget)]));paths=[];shots=[];fireOrders={};aiFireOrders={};
  }
  function resizeCamera(){const width=Math.max(1,$('board').clientWidth),height=Math.max(1,$('board').clientHeight);camera.w=width*camera.scale;camera.h=height*camera.scale;camera.x=camera.cx-camera.w/2;camera.y=camera.cy-camera.h/2;$('zoomValue').textContent=`${Math.round(100/camera.scale)}%`;}
  function fitCamera(extra=[]){
    const points=[...(living().length?living():units),...carriers,...extra];if(phase==='movement')for(const u of living('player'))points.push(...R.reachablePlans(u).map(p=>p.end));
    const all=points.map(xy),minX=Math.min(...all.map(p=>p.x))-45,maxX=Math.max(...all.map(p=>p.x))+45,minY=Math.min(...all.map(p=>p.y))-45,maxY=Math.max(...all.map(p=>p.y))+45;
    camera={cx:(minX+maxX)/2,cy:(minY+maxY)/2,scale:Math.max(1,(maxX-minX)/Math.max(1,$('board').clientWidth),(maxY-minY)/Math.max(1,$('board').clientHeight))};resizeCamera();
  }
  function reset(){window.HexReplayControls?.stop();replay=null;token++;$('mainAction').hidden=false;units=R.initialUnits().map(u=>R.withFireMode(u,'precise'));carriers=R.initialCarriers();round=1;phase='movement';selected='a1';logs=[];substep=0;winner=null;prepare();fitCamera();addLog('Точный режим: 90%, урон 1–2. Мощный: 60%, урон 2–4. Выбирайте скорость каждого дрона. Корпус носителя: 15.');render();}
  function endpointArrow(x,y,heading,color,actions,interactive=false){
    const a=(heading-90)*Math.PI/180,ux=Math.cos(a),uy=Math.sin(a),end={x:x+S*.61*ux,y:y+S*.61*uy},b={x:x+S*.35*ux,y:y+S*.35*uy};
    return `<g ${interactive?`data-end-plan="${actions}" data-heading="${heading}" style="cursor:pointer;pointer-events:all"`:'pointer-events="none"'}><circle cx="${end.x}" cy="${end.y}" r="${S*.24}" fill="transparent"/><path d="M${x},${y} L${b.x},${b.y}" stroke="${color}" stroke-width="1" pointer-events="none"/><polygon points="${end.x},${end.y} ${b.x-1.6*uy},${b.y+1.6*ux} ${b.x+1.6*uy},${b.y-1.6*ux}" fill="${color}" pointer-events="none"/>${interactive?`<title>Курс ${heading}°</title>`:''}</g>`;
  }
  function planLine(plan,extra=''){return `<polyline class="route ${extra}" points="${plan.points.map(p=>{const z=xy(p);return `${z.x},${z.y}`;}).join(' ')}"/>`;}
  function drawBoard(){
    const canPlan=phase==='movement'&&R.isOperational(unit()),ends=canPlan?groupsFor(unit()):new Map();let scene='';
    const rMin=Math.floor(camera.y/(1.5*S))-1,rMax=Math.ceil((camera.y+camera.h)/(1.5*S))+1;
    for(let r=rMin;r<=rMax;r++){
      const qMin=Math.floor(camera.x/DX-r/2)-1,qMax=Math.ceil((camera.x+camera.w)/DX-r/2)+1;
      for(let q=qMin;q<=qMax;q++){const {x,y}=xy({q,r}),available=ends.has(`${q},${r}`),fireZone=phase==='shooting'&&R.inFireArc(unit(),{q,r});scene+=`<polygon points="${hex(x,y)}" class="cell${available?' target':''}${fireZone?' fire-zone':''}" data-cell="${q},${r}"/>`;}
    }
    for(const c of carriers){const {x,y}=xy(c),color=c.side==='player'?BLUE:RED;scene+=`<g class="carrier ${c.side}" data-carrier="${c.id}" style="pointer-events:all;cursor:pointer;opacity:${R.isAlive(c)?1:.3}" transform="translate(${x},${y}) rotate(${c.heading-90})"><title>${c.name}: корпус ${c.hp}/15${R.isAlive(c)?"":" · уничтожен"}</title><polygon points="-25,-10 -18,-15 -6,-15 -2,-11 13,-10 26,0 13,10 -2,11 -6,15 -18,15 -25,10 -21,0" fill="#182d3e" stroke="${color}" stroke-width="1.2"/><path d="M-16,-9 H7 L15,0 7,9 H-16 Z M-16,-4 H6 M-16,4 H6 M-7,-13 V13" fill="none" stroke="${color}" stroke-width=".8"/><path d="M-25,-7 L-32,-5 -25,-3 M-25,3 L-32,5 -25,7" fill="${color}"/></g>`;}
    if(replay?.frame.orders&&replay.frame.label==='Планирование')for(const u of living()){const actions=replay.frame.orders[u.id];if(!actions)continue;const rules=replay.session.row.rulesVersion===2?window.HexReplayRulesV2:window.HexReplayRulesV1,plan=rules.tracePlan(u,actions),p=xy(plan.end);scene+=`<g pointer-events="none" style="--route-color:${u.side==='player'?BLUE:RED}">${planLine(plan,'archive-route')}${endpointArrow(p.x,p.y,plan.end.heading,u.side==='player'?BLUE:RED,actions)}</g>`;}
    if(canPlan){
      for(const u of living('player')){
        const plan=savedPlan(u);if(!plan)continue;const end=xy(plan.end),active=u.id===selected;
        scene+=`<g data-saved-unit="${u.id}" data-saved-plan="${plan.actions}" pointer-events="none" opacity="${active?1:.65}">${planLine(plan,active?'selected':'')}${endpointArrow(end.x,end.y,plan.end.heading,active?WHITE:BLUE,plan.actions)}</g>`;
      }
      scene+='<g id="hoverRoute" pointer-events="none"></g>';
    }
    if(!canPlan)for(const p of paths){const a=xy(p.from),b=xy(p.to);scene+=`<path d="M${a.x},${a.y} L${b.x},${b.y}" stroke="${p.side==='player'?'#57dce54d':'#ef79704d'}" stroke-width=".8" fill="none" stroke-dasharray="2 3"/>`;}
    if(phase==='shooting'){const target=entities().find(u=>u.id===fireOrders[selected]);if(target&&R.canFireAt(unit(),target)){const a=xy(unit()),b=xy(target);scene+=`<path class="aim-line" d="M${a.x},${a.y} L${b.x},${b.y}"/>`;}}
    if(replay?.frame.fireOrders)for(const [id,targetId] of Object.entries(replay.frame.fireOrders)){const shooter=units.find(u=>u.id===id),target=entities().find(u=>u.id===targetId);if(!shooter||!target)continue;const a=xy(shooter),b=xy(target);scene+=`<path class="aim-line" style="stroke:${shooter.side==='player'?BLUE:RED}" d="M${a.x},${a.y} L${b.x},${b.y}"/>`;}
    for(const shot of shots){const a=xy(shot.from),b=xy(shot.to);scene+=`<path class="shot ${shot.side}${shot.hit?'': ' miss'}" data-shot="${shot.shooterId}" d="M${a.x},${a.y} L${b.x},${b.y}"/>`;}
    for(const u of units.filter(u=>!R.isAlive(u))){const {x,y}=xy(u);scene+=`<g class="wreck" data-destroyed="${u.id}"><title>${u.name}: уничтожен</title><path d="M${x-5},${y-5} L${x+5},${y+5} M${x-5},${y+5} L${x+5},${y-5}"/></g>`;}
    for(const u of units.filter(R.isAlive)){
      const {x,y}=xy(u),color=u.side==='player'?BLUE:RED,shape=u.type==='heavy'?'-23,-11 -7,-11 -13,-18 2,-13 26,0 2,13 -13,18 -7,11 -23,11 -17,0':'-19,-5 -12,-13 8,-6 24,0 8,6 -12,13 -19,5 -13,0';
      const targetable=phase==='shooting'&&R.canFireAt(unit(),u);
      scene+=`<g class="unit ${u.side}${canPlan&&u.id===selected?' selected':''}${targetable?' valid-target':''}${targetable&&fireOrders[selected]===u.id?' aim-target':''}" data-unit="${u.id}"><title>${u.name} · корпус ${u.hp}/${R.maxHp(u)} · ход ${u.budget}</title><polygon class="ring" points="${hex(x,y)}"/><g id="sprite-${u.id}" transform="translate(${x},${y}) rotate(${u.heading-90})"><g class="glyph" transform="scale(.34)"><polygon points="${shape}" fill="#132a3a" opacity="${u.disabled?.3:1}" stroke="${color}" stroke-width="1"/><path d="M-9,0 H13 M-5,-4 H4 M-5,4 H4" stroke="${color}" stroke-width="2"/><path d="M-23,-3 L-29,0 L-23,3" fill="${color}"/></g></g></g>`;
    }
    if(canPlan)scene+='<g id="hoverPreview" pointer-events="none"></g>';
    $('board').setAttribute('viewBox',`${camera.x} ${camera.y} ${camera.w} ${camera.h}`);$('board').innerHTML=scene;paintHover();
    if(replay)interpolateReplay(replay.frame,replay.session.frames[replay.session.index+1],replay.session.progress);
  }
  function paintHover(){
    const overlay=$('hoverPreview');if(!overlay)return;
    const saved=$('board').querySelector(`[data-saved-unit="${selected}"]`);if(saved)saved.setAttribute('opacity','1');
    $('board').querySelectorAll('.hovered').forEach(el=>el.classList.remove('hovered'));
    const occupied=living('player').some(u=>R.key(u)===hoverCell),candidates=occupied?[]:(groupsFor(unit()).get(hoverCell)||[]);
    if(!candidates.length){overlay.innerHTML='';$('hoverRoute').innerHTML='';$('fieldHint').textContent='Наведи мышь на затемнённый гекс. Клик выбирает клетку; курс можно выбрать справа.';return;}
    const plan=candidates.find(p=>p.actions===orders[selected])||candidates[0],end=xy(plan.end);
    if(saved)saved.setAttribute('opacity','.22');
    $('hoverRoute').innerHTML=planLine(plan,'selected').replace('<polyline','<polyline data-preview-plan="'+plan.actions+'"');
    overlay.innerHTML=candidates.map(p=>endpointArrow(end.x,end.y,p.end.heading,p.actions===plan.actions?WHITE:BLUE,p.actions,true)).join('');
    const cell=$('board').querySelector(`[data-cell="${hoverCell}"]`);if(cell)cell.classList.add('hovered');
    $('fieldHint').textContent=`Гекс ${hoverCell.replace(',', ' : ')} · курс ${plan.end.heading}° · доступно курсов: ${candidates.length}`;
  }
  function pointerWorld(event){
    const point=$('board').createSVGPoint();point.x=event.clientX;point.y=event.clientY;const matrix=$('board').getScreenCTM();if(!matrix)return null;
    return point.matrixTransform(matrix.inverse());
  }
  function pointerCell(event){
    const p=pointerWorld(event);if(!p)return null;const r=p.y/(1.5*S),q=p.x/DX-r/2;let x=Math.round(q),z=Math.round(r),y=Math.round(-q-r);
    const xd=Math.abs(x-q),yd=Math.abs(y+q+r),zd=Math.abs(z-r);if(xd>yd&&xd>zd)x=-y-z;else if(zd>=yd)z=-x-y;return `${x},${z}`;
  }
  function friendlyUnderPointer(event){
    const occupant=living('player').find(u=>R.key(u)===pointerCell(event));return occupant?$('board').querySelector(`[data-unit="${occupant.id}"]`):null;
  }
  function clearUnitHover(){$('board').querySelectorAll('.hover-unit').forEach(el=>el.classList.remove('hover-unit'));}
  $('board').addEventListener('pointermove',event=>{
    if(phase!=='movement')return;clearUnitHover();const own=friendlyUnderPointer(event);
    if(own){hoverCell=null;paintHover();own.classList.add('hover-unit');const u=units.find(u=>u.id===own.dataset.unit);$('fieldHint').textContent=`${u.name} · ${u.type==='heavy'?'тяжёлый':'лёгкий'} · ход ${u.budget}. Нажми, чтобы выбрать.`;return;}
    const next=pointerCell(event);if(next!==hoverCell){hoverCell=next;paintHover();}
  });
  $('board').addEventListener('pointerleave',()=>{clearUnitHover();hoverCell=null;paintHover();});
  $('board').addEventListener('wheel',event=>{
    if(!camera||event.deltaY===0)return;event.preventDefault();
    const anchor=pointerWorld(event);if(!anchor)return;
    const fx=(anchor.x-camera.x)/camera.w,fy=(anchor.y-camera.y)/camera.h;
    const pixels=event.deltaY*(event.deltaMode===1?16:event.deltaMode===2?$('board').clientHeight:1);
    const scale=Math.max(.25,Math.min(Math.max(2,camera.scale),camera.scale*Math.exp(Math.max(-400,Math.min(400,pixels))*.0015)));
    if(scale===camera.scale)return;
    camera.scale=scale;resizeCamera();camera.cx=anchor.x+(.5-fx)*camera.w;camera.cy=anchor.y+(.5-fy)*camera.h;resizeCamera();
    $('board').setAttribute('viewBox',`${camera.x} ${camera.y} ${camera.w} ${camera.h}`);
    if(phase==='executing')return; // Keep existing animated sprite transforms intact.
    hoverCell=phase==='movement'?pointerCell(event):null;drawBoard();
  },{passive:false});
  function render(){
    if(replay){renderReplay();return;}
    $('speedLegend').textContent='Тяжёлый: 4/6 · лёгкий: 6/9';$('fast').closest('label').hidden=false;
    const u=unit(),editing=phase==='movement'&&R.isOperational(u),busy=phase==='executing',aiming=phase==='shooting',draft=orders[selected]||'',groups=editing?groupsFor(u):new Map(),chosen=editing?savedPlan(u):null,sameCell=chosen?(groups.get(R.key(chosen.end))||[]):[];
    $('phaseTitle').textContent=`РАУНД ${round} · ${editing?'ДВИЖЕНИЕ':busy?`ДВИЖЕНИЕ ${substep}/${totalSteps()}`:phase==='finished'?'БОЙ ЗАВЕРШЁН':'СТРЕЛЬБА'}`;
    const gun=u.gun||R.COURSE_GUN;
    $('mainAction').hidden=false;$('gunModePanel').hidden=false;$('gunMode').value=u.fireMode||'precise';$('gunMode').disabled=!(editing||aiming)||!R.isOperational(u);
    $('moveSpeed').innerHTML=R.speedChoices(u).map(n=>`<option value="${n}" ${n===u.budget?'selected':''}>${n} действий${n===R.speedChoices(u)[0]?' · малый ход':' · полный ход'}</option>`).join('');
    $('unitInfo').innerHTML=`<h2>${u.name} <span class="hp">${u.hp}/${R.maxHp(u)}</span></h2><div class="meta">${u.type==='heavy'?'Тяжёлый':'Лёгкий'} · ход ${u.budget} · курс ${u.heading}°</div><div class="help">Курсовая: ${gun.range} гексов · ±${gun.halfAngle}° · урон ${gun.damageMin}–${gun.damageMax} · ${Math.round(gun.hitChance*100)}%</div>`;
    $('carriers').innerHTML=carriers.map(c=>`<div class="carrier-card ${c.side}"><b>${c.name} · ${c.hp}/15</b><small>${c.side==='player'?'Ваш носитель':'Носитель ИИ'} · неподвижен</small><small>${R.isAlive(c)?"20 гексов за исходной линией тяжёлых":"Уничтожен · дроны отключены"}</small></div>`).join('');
    $('planner').hidden=!editing;
    const sorted=[...groups.entries()].sort((a,b)=>a[1][0].end.r-b[1][0].end.r||a[1][0].end.q-b[1][0].end.q);
    $('targetCell').innerHTML='<option value="">Выберите клетку на поле или из списка</option>'+sorted.map(([k,list])=>`<option value="${k}" ${chosen&&R.key(chosen.end)===k?'selected':''}>Гекс ${k.replace(',', ' : ')} · курсов: ${list.length}</option>`).join('');
    $('headingChoices').hidden=!editing||sameCell.length<2;
    $('headingChoices').innerHTML='<div class="sectiontitle">КОНЕЧНЫЙ КУРС</div><div class="edit">'+sameCell.map(p=>`<button data-heading-plan="${p.actions}" class="${p.actions===draft?'heading-selected':''}">${p.end.heading}°</button>`).join('')+'</div>';
    $('plans').innerHTML=sameCell.map(p=>`<button class="plan${p.actions===draft?' selected':''}" data-plan="${p.actions}"><span>${p.actions.split('').map(a=>short[a]).join(' ')}</span><span class="finish">${p.end.heading}°</span></button>`).join('');
    $('actionSlots').innerHTML=Array.from({length:u.budget},(_,i)=>`<div class="slot${draft[i]?'':' empty'}"><small>${i+1}</small>${draft[i]?names[draft[i]]:'—'}</div>`).join('');
    const full=draft.length>=u.budget,draftEnd=editing?R.tracePlan(u,draft).end:u,stepsLeft=Math.max(0,R.TURN_STRAIGHT_STEPS-R.straightSinceTurn(draftEnd));
    $('forward').disabled=!editing||full;$('left').disabled=$('right').disabled=!editing||full||draft.length===0||stepsLeft>0;$('undo').disabled=!editing||draft.length<=1;$('clear').disabled=!editing;
    $('draftHelp').textContent=`${draft.length}/${u.budget} действий. ${full?'План готов. «Сначала» — ручной ввод.':stepsLeft?`До поворота: вперёд ещё ${stepsLeft}.`:'Поворот доступен.'}`;
    $('fleet').innerHTML=units.filter(u=>u.side==='player').map(u=>{const target=entities().find(t=>t.id===fireOrders[u.id]);return `<button class="unitcard${(editing||aiming)&&selected===u.id?' selected':''}${R.isAlive(u)?'':' destroyed'}" data-select="${u.id}" ${busy||!R.isOperational(u)?'disabled':''}><b>${u.name} · ${u.hp}/${R.maxHp(u)}</b><small>${u.type==='heavy'?'Тяжёлый':'Лёгкий'} · ход ${u.budget}</small><small>${!R.isAlive(u)?'Уничтожен':u.disabled?'Отключён: носитель уничтожен':aiming?(target?`→ ${target.name}`:'Не стрелять'):`Курс ${u.heading}° · ${editing?`${(orders[u.id]||'').length}/${u.budget}`:'ход завершён'}`}</small></button>`;}).join('');
    const ready=living('player').every(u=>R.validatePlan(u,orders[u.id]));
    $('mainAction').textContent=editing?'ВЫПОЛНИТЬ ДВИЖЕНИЕ':busy?'ИСПОЛНЕНИЕ…':aiming?'ОТКРЫТЬ ОГОНЬ':phase==='finished'?'НОВЫЙ БОЙ':'СЛЕДУЮЩИЙ ХОД';$('mainAction').disabled=busy||(editing&&!ready);$('reset').disabled=busy;$('fitView').disabled=busy;
    $('status').textContent=busy?'Конфликтующие шаги отменяются до движения.':editing?(ready?'Все планы готовы. ИИ выбрал свои приказы.':'Дополните планы живых дронов.'):aiming?'Выберите цели. Все выстрелы разрешатся одновременно.':phase==='finished'?(winner==='player'?'Победа! У противника нет боеспособных дронов.':winner==='ai'?'Поражение. У вас нет боеспособных дронов.':'Ничья: у обеих сторон нет боеспособных дронов.'):`Выстрелов: ${shots.length}. Игрок: ${living('player').length} · ИИ: ${living('ai').length}.`;
    $('firePlanner').hidden=!aiming;
    const targets=aiming?R.fireTargets(u,entities()):[];
    $('fireTargets').innerHTML=targets.map(t=>`<button class="fire-target${fireOrders[selected]===t.id?' selected':''}" data-fire-target="${t.id}"><span>${t.name} · ${t.hp}/${R.maxHp(t)}</span><span>${R.distance(u,t)} гексов · −${gun.damageMin}–${gun.damageMax} · ${Math.round(gun.hitChance*100)}%</span></button>`).join('')+`<button class="fire-target${fireOrders[selected]==null?' selected':''}" data-fire-target="">Не стрелять</button>`;
    $('fireHint').textContent=targets.length?'Выберите цель и режим. Один выстрел за фазу; сводка учитывает промахи и случайный урон.':'В секторе курсовой пушки нет доступных целей.';
    $('log').innerHTML=logs.map(line=>`<div>${line}</div>`).join('');$('log').scrollTop=$('log').scrollHeight;
    $('phaseOverlay').hidden=phase!=='fireResult'&&phase!=='finished';$('phaseOverlay').innerHTML=phase==='fireResult'||phase==='finished'?`<div class="phasecard"><h2>${phase==='finished'?'БОЙ ЗАВЕРШЁН':'ЗАЛП РАЗРЕШЁН'}</h2><p>${$('status').textContent}</p></div>`:'';
    renderSalvo(aiming?fireOrders:null,'player');
    drawBoard();
  }
  function renderSalvo(assigned,side){
    $('salvoPanel').hidden=!assigned;
    if(!assigned)return;
    const forecast=R.salvoForecast(entities(),assigned,side);
    $('salvoSummary').innerHTML=forecast.length?forecast.map(g=>`<div class="salvo-card"><b>${g.targetName} · корпус ${g.hp}</b><div>${g.count} ${g.count===1?'ствол':'ствола'} · ${g.shooters.join(', ')}</div><div>Ожидаемый урон: <strong>${g.expected.toFixed(2)}</strong> · диапазон 0–${g.max}</div><div>Вероятность уничтожения: <strong>${(g.killChance*100).toFixed(1)}%</strong></div></div>`).join(''):'<div class="help">Нет назначенных выстрелов.</div>';
  }
  function renderReplay(){
    const {frame,session}=replay;
    const v2=session.row.rulesVersion===2;
    $('speedLegend').textContent=v2?'Архив v2: тяжёлый 4/6 · лёгкий 6/9':'Архив v1: тяжёлый 6 · лёгкий 9';$('fast').closest('label').hidden=true;
    $('phaseTitle').textContent=`ПРОСМОТР · РАУНД ${frame.round} · ${frame.label}`;
    const policyName={current:'текущий ИИ',focus:'сосредоточенный огонь',raid:'налёт на носитель',guard:'защита носителя'};
    $('unitInfo').innerHTML=`<h2>Архивный бой №${session.number}</h2><div class="help">Голубые: ${policyName[session.policies.player]} · красные: ${policyName[session.policies.ai]}</div>`;
    $('mainAction').hidden=true;$('planner').hidden=true;$('firePlanner').hidden=true;$('gunModePanel').hidden=true;$('phaseOverlay').hidden=true;
    $('status').textContent=(v2?'Новые правила: выбор скорости; точный 90%, 1–2; мощный 60%, 2–4. ':'Исходные правила: ход 6/9, попадание 75%, урон 1–3. ')+'Совпадение с результатом эксперимента проверено.';
    $('reset').disabled=false;$('fitView').disabled=false;
    $('carriers').innerHTML=carriers.map(c=>`<div class="carrier-card ${c.side}"><b>${c.name} · ${c.hp}/15</b><small>${R.isAlive(c)?'Носитель': 'Уничтожен · дроны отключены'}</small></div>`).join('');
    $('fleet').innerHTML=units.map(u=>`<div class="unitcard ${u.side}${R.isAlive(u)?'':' destroyed'}"><b>${u.name} · ${u.hp}/${R.maxHp(u)}</b><small>${u.side==='player'?'Голубые':'Красные'} · ${!R.isAlive(u)?'уничтожен':u.disabled?'отключён':`ход ${u.budget} · курс ${u.heading}°`}</small>${v2&&R.isAlive(u)?`<small>${u.fireMode==='power'?'Мощный · 60% · 2–4':'Точный · 90% · 1–2'}</small>`:''}</div>`).join('');
    const assigned=frame.fireOrders||(frame.shots.length?Object.fromEntries(frame.shots.map(s=>[s.shooterId,s.targetId])):null);
    // Forecast refers to pre-shot hull, never the already damaged result frame.
    if(frame.fireOrders)renderSalvo(assigned);else $('salvoPanel').hidden=true;
    $('log').innerHTML=frame.shots.length?frame.shots.map(s=>`<div>${s.shooterName} → ${s.targetName}: ${s.hit?`−${s.damage}`:'промах'}</div>`).join(''):`<div>${frame.label}${frame.blockedIds?.length?` · отменено шагов: ${frame.blockedIds.length}`:''}</div>`;
    drawBoard();
  }
  function interpolateReplay(from,to,f){
    if(!to||!to.label.startsWith('Движение')||to.round!==from.round)return;
    for(const b of to.state){const a=from.state.find(u=>u.id===b.id),element=$(`sprite-${b.id}`);if(!a||!element)continue;const p=xy(a),q=xy(b),angle=((b.heading-a.heading+540)%360)-180;element.setAttribute('transform',`translate(${p.x+(q.x-p.x)*f},${p.y+(q.y-p.y)*f}) rotate(${a.heading-90+angle*f})`);}
  }
  function select(id){if(phase==='executing'||!living('player').some(u=>u.id===id))return;hoverCell=null;selected=id;render();}
  function choose(actions){if(phase!=='movement'||!R.validatePlan(unit(),actions))return;orders[selected]=actions;render();}
  $('board').addEventListener('click',event=>{
    if(phase==='shooting'){const target=event.target.closest('[data-unit],[data-carrier]');if(target){const u=entities().find(u=>u.id===(target.dataset.unit||target.dataset.carrier));if(u.side==='player')select(u.id);else chooseShot(u.id);}return;}
    const own=friendlyUnderPointer(event);if(own){select(own.dataset.unit);return;}if(event.target.closest('[data-unit]')||phase!=='movement')return;
    const direct=event.target.closest('[data-end-plan]');if(direct){choose(direct.dataset.endPlan);return;}
    const list=groupsFor(unit()).get(pointerCell(event))||[];if(list.length){const i=list.findIndex(p=>p.actions===orders[selected]);choose(list[(i+1)%list.length].actions);}
  });
  $('targetCell').onchange=()=>{const list=groupsFor(unit()).get($('targetCell').value)||[];if(list.length)choose(list[0].actions);};
  $('plans').addEventListener('click',event=>{const b=event.target.closest('[data-plan]');if(b)choose(b.dataset.plan);});
  $('headingChoices').addEventListener('click',event=>{const b=event.target.closest('[data-heading-plan]');if(b)choose(b.dataset.headingPlan);});
  $('fleet').addEventListener('click',event=>{const b=event.target.closest('[data-select]');if(b)select(b.dataset.select);});
  function chooseShot(id){if(phase!=='shooting')return;const target=entities().find(u=>u.id===id);if(id!=null&&(!target||!R.canFireAt(unit(),target)))return;fireOrders[selected]=id;render();}
  $('fireTargets').addEventListener('click',event=>{const b=event.target.closest('[data-fire-target]');if(b)chooseShot(b.dataset.fireTarget||null);});
  function append(action){if(phase!=='movement')return;const p=orders[selected];if(p.length>=unit().budget||action!=='F'&&(p.length===0||!R.canTurn(R.tracePlan(unit(),p).end)))return;orders[selected]+=action;render();}
  $('forward').onclick=()=>append('F');$('left').onclick=()=>append('L');$('right').onclick=()=>append('R');
  $('undo').onclick=()=>{if(phase==='movement'&&orders[selected].length>1){orders[selected]=orders[selected].slice(0,-1);render();}};
  $('clear').onclick=()=>{if(phase==='movement'){orders[selected]='F';render();}};
  $('moveSpeed').onchange=()=>{
    if(phase!=='movement')return;const id=selected;
    units=units.map(u=>u.id===id?R.withSpeed(u,Number($('moveSpeed').value)):u);
    orders[id]='F'.repeat(unit().budget);hoverCell=null;addLog(`${unit().name}: ход ${unit().budget}, план заменён на прямой.`);render();
  };
  $('gunMode').onchange=()=>{if(!['movement','shooting'].includes(phase))return;units=units.map(u=>u.id===selected?R.withFireMode(u,$('gunMode').value):u);render();};
  function animate(from,to,duration,myToken){return new Promise(resolve=>{const started=performance.now();function frame(now){
    if(myToken!==token){resolve(false);return;}const f=Math.min(1,(now-started)/duration),ease=f*f*(3-2*f);
    for(const b of to){const a=from.find(u=>u.id===b.id);if(!a)continue;const p=xy(a),q=xy(b),angle=((b.heading-a.heading+540)%360)-180,element=$(`sprite-${b.id}`);if(element)element.setAttribute('transform',`translate(${p.x+(q.x-p.x)*ease},${p.y+(q.y-p.y)*ease}) rotate(${a.heading-90+angle*ease})`);}
    if(f<1)requestAnimationFrame(frame);else resolve(true);
  }requestAnimationFrame(frame);});}
  async function execute(){
    if(phase!=='movement')return;const allOrders={...orders,...aiOrders};if(!living().every(u=>R.validatePlan(u,allOrders[u.id])))return;
    phase='executing';const myToken=++token;paths=[];addLog('Началось одновременное движение.');
    for(let i=0;i<totalSteps();i++){
      substep=i+1;render();const before=units,frame=R.step(entities(),allOrders,i),fast=$('fast').checked;
      for(const c of frame.conflicts)addLog(`${c.names.join(' и ')}: клетка будет занята — шаг отменён.`);
      if(!await animate(before,frame.units,fast?35:140,myToken))return;
      for(let j=0;j<before.length;j++)if(R.key(before[j])!==R.key(frame.units[j]))paths.push({from:before[j],to:frame.units[j],side:before[j].side});updateEntities(frame.units);
    }
    if(!R.hasFireTargets(entities())){addLog('Ни у одной стороны нет целей — стрельба пропущена.');nextMovement();return;}
    phase='shooting';hoverCell=null;ensureSelected();fireOrders=Object.fromEntries(living('player').map(u=>[u.id,R.chooseFireTarget(u,entities())]));aiFireOrders=Object.fromEntries(living('ai').map(u=>[u.id,R.chooseFireTarget(u,entities())]));addLog('Движение завершено. Выберите цели для курсовых пушек.');render();
  }
  function shoot(){
    if(phase!=='shooting')return;const result=R.resolveFire(entities(),{...fireOrders,...aiFireOrders});updateEntities(result.units);shots=result.shots;
    for(const shot of shots)addLog(`${shot.shooterName} → ${shot.targetName}: ${shot.hit?`попадание, −${shot.damage}`:'промах'}.`);
    if(!shots.length)addLog('Нет выстрелов: цели не выбраны или вне секторов.');
    for(const id of result.destroyedIds)addLog(`${entities().find(u=>u.id===id).name}: уничтожен.${id.startsWith('carrier-')?' Его дроны отключены.':''}`);
    winner=R.battleResult(entities());phase=winner?'finished':'fireResult';ensureSelected();render();
  }
  function nextMovement(){round++;phase='movement';prepare();addLog('Началось планирование нового хода.');render();}
  $('mainAction').onclick=()=>{if(phase==='movement')execute();else if(phase==='shooting')shoot();else if(phase==='finished')reset();else if(phase==='fireResult')nextMovement();};
  $('reset').onclick=reset;
  $('fitView').onclick=()=>{if(phase==='executing')return;hoverCell=null;fitCamera();render();};
  document.addEventListener('keydown',event=>{if(replay||event.target.matches('input,button,select'))return;if(event.key==='Tab'){event.preventDefault();const list=living('player'),i=list.findIndex(u=>u.id===selected);if(list.length)select(list[(i+(event.shiftKey?list.length-1:1))%list.length].id);}if(event.key==='Enter'&&!$('mainAction').disabled)$('mainAction').click();});
  reset();
  window.HexReplayControls.init({
    enter(){token++;phase='replay';hoverCell=null;},
    show(frame,session){replay={frame,session};updateEntities(frame.state);round=frame.round;phase='replay';orders=frame.orders||{};fireOrders=frame.fireOrders||{};aiFireOrders={};shots=frame.shots;paths=[];render();},
    interpolate:interpolateReplay,
    fit(){const points=replay.session.frames.flatMap(f=>f.state);fitCamera(points);drawBoard();},
    exit:reset
  });
  new ResizeObserver(()=>{if(!camera)return;resizeCamera();hoverCell=null;drawBoard();}).observe($('board'));
})();
