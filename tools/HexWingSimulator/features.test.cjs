const {test}=require('node:test'),assert=require('node:assert/strict'),R=require('./logic.js');
const drone=(id,q,type='light')=>({...R.initialUnits().find(u=>u.type===type),id,name:id,q,r:0,heading:90,side:'player'});
const enemy=(hp=9)=>({...drone('target',3,'heavy'),side:'ai',hp});
test('Selectable speed changes required plan length and preserves turn cooldown',()=>{
 for(const [type,budgets] of [['light',[6,9]],['heavy',[4,6]]]){
  const u={...drone('a',0,type),straightSinceTurn:0};assert.deepEqual(R.speedChoices(u),budgets);
  for(const budget of budgets){const slow=R.withSpeed(u,budget);assert.equal(slow.straightSinceTurn,0);assert.ok(R.validatePlan(slow,'F'.repeat(budget)));assert.ok(!R.validatePlan(slow,'F'.repeat(budget-1)));}
 }
 assert.throws(()=>R.withSpeed(drone('a',0),4));
});
test('Shortened heavy plan waits while light keeps moving without refreshing readiness',()=>{
 const a=R.withSpeed(drone('a',0,'heavy'),4),b={...R.withSpeed(drone('b',20),9),r:5};
 const result=R.simulate([a,b],{a:'FFFL',b:'FFFFFFFFF'});
 assert.equal(result.frames.length,9);for(const f of result.frames.slice(3)){assert.equal(f.units[0].q,3);assert.equal(f.units[0].heading,30);assert.equal(f.units[0].straightSinceTurn,0);}
});
test('New modes use exact hit thresholds, damage intervals and unchanged sectors',()=>{
 for(const [mode,hit,min,max] of [['precise',.9,1,2],['power',.6,2,4]]){
  const a=R.withFireMode(drone('a',0),mode),b=enemy();assert.equal(a.gun.range,12);assert.equal(a.gun.halfAngle,30);
  let rolls=[hit-1e-6,0];assert.equal(R.resolveFire([a,b],{a:'target'},()=>rolls.shift()).shots[0].damage,min);
  rolls=[0,.999999];assert.equal(R.resolveFire([a,b],{a:'target'},()=>rolls.shift()).shots[0].damage,max);
  assert.equal(R.resolveFire([a,b],{a:'target'},()=>hit).shots[0].damage,0);
 }
});
test('Salvo distribution includes misses and computes exact mixed-mode kill probability',()=>{
 const a=R.withFireMode(drone('a',0),'precise'),b=R.withFireMode(drone('b',1),'power');
 const [g]=R.salvoForecast([a,b,enemy(5)],{a:'target',b:'target'},'player');
 assert.equal(g.count,2);assert.equal(g.max,6);assert.ok(Math.abs(g.expected-3.15)<1e-12);assert.ok(Math.abs(g.killChance-.27)<1e-12);
 assert.equal(R.salvoForecast([a,b,enemy()],{a:null,b:'missing'}).length,0);
});
test('Salvo excludes dead/disabled shooters and can report certain impossibility',()=>{
 const a=R.withFireMode(drone('a',0),'precise'),b={...drone('b',1),disabled:true};
 const [g]=R.salvoForecast([a,b,enemy(9)],{a:'target',b:'target'});assert.equal(g.count,1);assert.equal(g.killChance,0);
 const [lethal]=R.salvoForecast([a,enemy(1)],{a:'target'});assert.ok(Math.abs(lethal.killChance-.9)<1e-12);
});
