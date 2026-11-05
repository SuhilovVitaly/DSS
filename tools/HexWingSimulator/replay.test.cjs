const {test}=require('node:test'),assert=require('node:assert/strict');
const catalog=require('./battle-catalog.js'),archive=catalog.filter(r=>(r.rulesVersion||1)===1),Replay=require('./replay.js');
test('All 2000 archived battles reproduce their original outcomes, rounds, shots, hits, damage and collisions',()=>{
 assert.equal(archive.length,2000);
 for(const row of archive){const replay=Replay.build(row);assert.ok(replay.verified);assert.equal(replay.result,row.result);assert.ok(replay.frames.length>1);}
});
test('All 2000 new battles reproduce and both series keep stable numbering',()=>{
 const rows=catalog.filter(r=>r.rulesVersion===2);assert.equal(catalog.length,4000);assert.equal(rows.length,2000);
 assert.ok(catalog.slice(0,2000).every(r=>(r.rulesVersion||1)===1));assert.ok(catalog.slice(2000).every(r=>r.rulesVersion===2));
 for(const row of rows){const replay=Replay.build(row);assert.ok(replay.verified);assert.equal(replay.result,row.result);assert.equal(row.modes.precise.shots+row.modes.power.shots,row.shots);}
});
test('Replay is repeatable and independent of new live gun modes',()=>{
 const row=archive.find(r=>r.carriersLost>0),a=Replay.build(row),b=Replay.build(row);
 assert.deepEqual(a,b);assert.ok(a.frames[0].state.filter(u=>u.type!=='carrier').every(u=>u.gun.hitChance===.75));
 assert.ok(a.frames.at(-1).state.some(u=>u.type==='carrier'&&u.hp===0));
});
