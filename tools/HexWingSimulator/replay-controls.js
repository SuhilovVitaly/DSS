/* Playback clock is independent of the interactive game. No simulation runs while playing. */
(function(root){'use strict';
 const $=id=>document.getElementById(id),scenarioNames={baseline:'Текущий / текущий',focus:'Фокус / текущий',raid:'Налёт / текущий',defence:'Защита / налёт'},results={win:'победа тактики',loss:'поражение тактики',draw:'ничья',timeout:'лимит 100 раундов'};
 let session=null,adapter,last=0,raf=0;
 function stop(){cancelAnimationFrame(raf);raf=0;if(session)session.playing=false;last=0;}
 function filters(){
  const rows=root.HexBattleCatalog.map((row,i)=>({row,i})).filter(({row})=>(!$('replayVersion').value||String(row.rulesVersion||1)===$('replayVersion').value)&&(!$('replayScenario').value||row.scenario===$('replayScenario').value)&&(!$('replayResult').value||($('replayResult').value==='carrier'?row.carriersLost>0:row.result===$('replayResult').value)));
  $('replayBattle').innerHTML=rows.map(({row,i})=>`<option value="${i}">№${i+1} · v${row.rulesVersion||1} · ${scenarioNames[row.scenario]} · ${results[row.result]} · ${row.rounds} р. · ${row.focal==='player'?'голубые':'красные'}</option>`).join('');
  $('archiveCount').textContent=`Найдено ${rows.length} из ${root.HexBattleCatalog.length}. Исход указан для первой тактики.`;$('loadReplay').disabled=!rows.length;
 }
 function controls(){if(!session)return;const s=session;$('replayPlay').textContent=s.playing?'Ⅱ Пауза':'▶ Смотреть';$('replaySeek').max=s.frames.length-1;$('replaySeek').value=s.index;$('replayPosition').textContent=`Кадр ${s.index+1}/${s.frames.length} · ${s.playing?'Просмотр':'Пауза'} · ${s.row.rulesVersion===2?'v2: выбор скорости и режима':'v1: 75%, урон 1–3'}`;$('replayBack').disabled=s.index===0;$('replayNext').disabled=s.index===s.frames.length-1;}
 function show(){adapter.show(session.frames[session.index],session);controls();adapter.interpolate(session.frames[session.index],session.frames[session.index+1],session.progress);}
 function duration(){const label=session.frames[session.index+1]?.label||'';return label.startsWith('Движение')?420:label==='Залп'?1000:750;}
 function tick(now){
  if(!session?.playing)return;
  if(last)session.progress+=Math.min(100,now-last)*Number($('replaySpeed').value)/duration();last=now;
  while(session.progress>=1&&session.index<session.frames.length-1){session.progress-=1;session.index++;show();}
  if(session.index===session.frames.length-1){stop();session.progress=0;controls();return;}
  adapter.interpolate(session.frames[session.index],session.frames[session.index+1],session.progress);raf=requestAnimationFrame(tick);
 }
 function pause(){stop();controls();}
 function seek(index){pause();session.index=Math.max(0,Math.min(session.frames.length-1,index));session.progress=0;show();}
 function load(){
  stop();const index=Number($('replayBattle').value),row=root.HexBattleCatalog[index];if(!row)return;
  try{const built=root.HexReplay.build(row);session={...built,row,number:index+1,index:0,progress:0,playing:false};$('replayControls').hidden=false;$('archivePanel').open=false;adapter.enter();show();adapter.fit();}
  catch(error){$('archiveCount').textContent=error.message;}
 }
 function init(value){adapter=value;filters();$('replayVersion').onchange=$('replayScenario').onchange=$('replayResult').onchange=filters;$('loadReplay').onclick=load;
  $('replayPlay').onclick=()=>{if(!session)return;if(session.playing){pause();return;}if(session.index===session.frames.length-1)seek(0);session.playing=true;last=0;controls();raf=requestAnimationFrame(tick);};
  $('replayBack').onclick=()=>seek(session.index-1);$('replayNext').onclick=()=>seek(session.index+1);$('replaySeek').oninput=()=>seek(Number($('replaySeek').value));
  $('exitReplay').onclick=()=>{stop();session=null;$('replayControls').hidden=true;adapter.exit();};
  document.addEventListener('keydown',event=>{if(!session||event.target.matches('input,select,button'))return;if(event.code==='Space'){event.preventDefault();$('replayPlay').click();}if(event.key==='ArrowRight'){event.preventDefault();seek(session.index+1);}if(event.key==='ArrowLeft'){event.preventDefault();seek(session.index-1);}});
 }
 function clear(){stop();session=null;$('replayControls').hidden=true;}
 root.HexReplayControls={init,stop:clear};
})(window);
