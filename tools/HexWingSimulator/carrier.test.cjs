const {test}=require('node:test');
const assert=require('node:assert/strict');
const R=require('./logic.js');
const gun=(id,side,q,hp=6)=>({id,name:id,side,q,r:0,heading:side==='player'?90:270,type:'light',hp,maxHp:6,budget:3,gun:{...R.COURSE_GUN}});
const carrier=(side,q,hp=15)=>({id:'carrier-'+side,name:side,side,type:'carrier',q,r:0,heading:90,hp,maxHp:15});
test('Carriers have 15 hull, can be shot but cannot shoot',()=>{
  assert.ok(R.initialCarriers().every(c=>c.hp===15&&R.maxHp(c)===15));
  const a=gun('a','player',0),c=carrier('ai',2);
  assert.ok(R.canFireAt(a,c));assert.equal(R.canFireAt(c,a),false);
  assert.equal(R.resolveFire([a,c],{a:c.id},()=>0).units[1].hp,14);
});
test('Carrier death disables surviving drones after simultaneous shots',()=>{
  const a=gun('a','player',0),b=gun('b','ai',2),c=carrier('ai',4,1);
  const result=R.resolveFire([a,b,c],{a:c.id,b:'a'},()=>0);
  assert.equal(result.shots.length,2);assert.equal(result.units[0].hp,5);
  assert.equal(result.units[1].hp,6);assert.ok(result.units[1].disabled);
  assert.equal(R.canFireAt(result.units[1],result.units[0]),false);
  assert.equal(R.battleResult(result.units),'player');
  assert.deepEqual(R.simulate(result.units,{a:'FFF'}).units[1],result.units[1]);
});
test('Disabled drones and living carriers remain stationary and block entry',()=>{
  for(const blocker of [{...gun('b','ai',1),disabled:true},carrier('ai',1)]){
    const result=R.step([gun('a','player',0),blocker],{a:'FFF'},0);
    assert.equal(result.units[0].q,0);assert.equal(result.units[1].q,1);
  }
});
test('Both carriers lost simultaneously yields draw with surviving disabled drones',()=>{
  const a=gun('a','player',0),b=gun('b','ai',2),ca=carrier('player',-1,1),cb=carrier('ai',3,1);
  const result=R.resolveFire([a,b,ca,cb],{a:cb.id,b:ca.id},()=>0);
  assert.equal(R.battleResult(result.units),'draw');assert.ok(result.units.slice(0,2).every(u=>u.disabled&&u.hp===6));
});
test('Existing dead carrier suppresses movement and fire even without disabled flag',()=>{
  const a=gun('a','player',0),b=gun('b','ai',2),c=carrier('ai',4,0);
  const result=R.resolveFire([a,b,c],{b:'a'},()=>0);
  assert.equal(result.shots.length,0);assert.ok(result.units[1].disabled);
  const moved=R.step([a,b,c],{a:'FFF'},0);assert.equal(moved.units[1].q,2);
});
