const {test}=require('node:test'),assert=require('node:assert/strict'),R=require('./replay-rules-v2.js'),Live=require('./logic.js'),V2=require('./replay-v2.js');
test('Frozen second-series units and modes match current live rules',()=>{
 assert.deepEqual(R.initialUnits(),Live.initialUnits());assert.deepEqual(R.initialCarriers(),Live.initialCarriers());assert.deepEqual(R.FIRE_MODES,Live.FIRE_MODES);
});
test('Gun selection chooses precise for 1 hull and power for healthy targets without mutating input',()=>{
 for(const [hp,mode] of [[1,'precise'],[2,'power'],[9,'power']]){
  const state=[{...R.initialUnits()[0],q:0,r:0},{...R.initialUnits()[4],q:3,r:0,hp}],before=JSON.stringify(state),result=V2.aiming(state,{player:'current',ai:'current'});
  assert.equal(result.state[0].fireMode,mode);assert.equal(result.orders.a1,'b1');assert.equal(JSON.stringify(state),before);
 }
});
test('New movement orders are valid for selected speed and do not mutate their snapshot',()=>{
 const state=[...R.initialUnits(),...R.initialCarriers()],before=JSON.stringify(state),move=V2.movement(state,1,{player:'guard',ai:'raid'});
 assert.equal(JSON.stringify(state),before);for(const u of move.state.filter(R.isOperational)){assert.ok(R.speedChoices(u).includes(u.budget));assert.ok(R.validatePlan(u,move.orders[u.id]));}
});
test('Versioned replay rejects a changed recorded outcome or speed/mode statistics',()=>{
 const row=require('./battle-catalog.js').find(r=>r.rulesVersion===2);assert.throws(()=>V2.build({...row,result:'tampered'}),/не совпал/);assert.throws(()=>V2.build({...row,speeds:{}}),/не совпал/);
});
