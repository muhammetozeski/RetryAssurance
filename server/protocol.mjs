import {hash,fail,text,object} from './security.mjs';

/** Immutable operator-maintained packs. No customer code or arbitrary response script is evaluated. */
export const packs=Object.freeze([
  Object.freeze({id:'ra-core',version:'1.0.0',title:'Durable job, delayed acknowledgement',scenario:'commit-then-delay',delayMs:3000,maxAttempts:6,description:'Commit the first resource, then withhold its response headers for three seconds. Subsequent attempts respond immediately. One logical operation may create at most one resource.',operation:{method:'POST',path:'jobs'},evidenceVersion:1})
]);
export async function packOf(version){const pack=packs.find(item=>item.version===version);if(!pack)fail(400,'Choose an available immutable scenario-pack version.','pack_version');return {...pack,hash:await hash(JSON.stringify(pack))};}
/** Canonical provider semantics; private payloads, endpoints and production keys are not accepted here. */
export function contractOf(value){object(value,['name','keyHeader','keyOwner','allowedTerminals'],'Behavior contract');const allowed=value.allowedTerminals;if(!Array.isArray(allowed)||allowed.length<1||allowed.length>3||new Set(allowed).size!==allowed.length||allowed.some(item=>!['success','indeterminate','failure'].includes(item)))fail(400,'Choose distinct allowed terminal observations: success, indeterminate or failure.');if(value.keyHeader!=='Idempotency-Key'||value.keyOwner!=='client')fail(400,'This pack requires the client to own a stable Idempotency-Key for one logical operation.');return {name:text(value.name,'Contract name',100),keyHeader:'Idempotency-Key',keyOwner:'client',allowedTerminals:allowed.slice().sort(),effectLimit:1,operationCount:1};}
/** Server-derived result; a runner terminal report cannot erase observed duplicate effects. */
export function deriveResult(run,requests,resources,terminal,returnedResourceId){
  const contract=JSON.parse(run.contract_document),issues=[];let verdict='pass';
  if(resources.length>contract.effectLimit){verdict='fail';issues.push('More than one durable remote resource was created for this one logical operation.');}
  if(requests.some(item=>item.response_policy==='payload-conflict')){verdict='fail';issues.push('The same idempotency key was reused with a different payload.');}
  if(!requests.length||!resources.length){if(verdict!=='fail')verdict='incomplete';issues.push('No durable resource effect was observed; the cardinality scenario was not exercised.');}
  if(requests.length<2){if(verdict!=='fail')verdict='incomplete';issues.push('No second HTTP attempt was observed. The retry behavior has not been exercised.');}
  if(!contract.allowedTerminals.includes(terminal)){if(verdict!=='fail')verdict='incomplete';issues.push('The runner-reported terminal observation is outside this pinned contract.');}
  const returnedResourceObserved=terminal==='success'&&resources.some(item=>item.id===returnedResourceId);
  if(terminal==='success'&&!returnedResourceObserved){if(verdict!=='fail')verdict='incomplete';issues.push('The declared successful client result does not reference a resource created in this run.');}
  return {verdict,resourceCount:resources.length,requestCount:requests.length,keyCount:new Set(requests.map(item=>item.key_hash)).size,issues,serverProven:['Durable resource count','Accepted request sequence','Key and payload digest relationships','Commit recorded before delayed-response policy'],runnerReported:{terminal,returnedResourceId:returnedResourceId||null,returnedResourceObserved,independentlyAttested:false},coverage:'One declared logical operation against the selected synthetic scenario. This is not proof of all client behavior or production correctness.'};
}
