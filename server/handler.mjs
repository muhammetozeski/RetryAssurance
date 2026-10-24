import {accountFrom,fail,hash,json,limit,sameOrigin,ServiceError} from './security.mjs';
import {accounts} from './accounts.mjs';
import {projects} from './projects.mjs';
import {executions} from './executions.mjs';
import {billing} from './billing.mjs';
import {packs,packOf} from './protocol.mjs';

/** Serves the original origin or the explicitly configured Retry Assurance mount without rewriting Origin. */
export async function handleRequest({request,env},{basePath=''}={}){
  try{
    if(!['','/retry-assurance'].includes(basePath))throw new Error('Unsupported service mount.');
    if(!env.DB||!env.RATE_SALT)fail(503,'The service is not configured. Your local source and runner remain available.','configuration');
    if(!['GET','POST'].includes(request.method))fail(405,'This method is not supported.','method');
    const url=new URL(request.url),apiBase=basePath+'/api';
    if(url.pathname!==apiBase&&!url.pathname.startsWith(apiBase+'/'))fail(404,'This service action was not found.','not_found');
    const parts=url.pathname.slice(apiBase.length).replace(/^\/+|\/+$/g,'').split('/').filter(Boolean),db=env.DB,ip=await hash(env.RATE_SALT+'|'+(request.headers.get('CF-Connecting-IP')||'unknown'));
    if(request.method==='POST'&&parts.join('/')!=='billing/webhook'){
      if(['runner','simulate'].includes(parts[0])){const origin=request.headers.get('origin');if(origin&&origin!==url.origin)fail(403,'The origin is not permitted.','origin');}
      else sameOrigin(request);
    }
    await limit(db,ip,'http-'+request.method,request.method==='GET'?300:180);
    if(request.method==='GET'&&parts.join('/')==='health')return json({ok:true,service:'Retry Assurance',evidenceVersion:1});
    if(request.method==='GET'&&parts.join('/')==='packs')return json({packs:await Promise.all(packs.map(item=>packOf(item.version)))});
    const account=await accountFrom(request,db),context={request,env,db,account,url,parts,ip,basePath,apiBase};
    for(const handler of [accounts,projects,executions,billing]){const response=await handler(context);if(response)return response;}
    fail(404,'This service action was not found.','not_found');
  }catch(error){
    if(error instanceof ServiceError)return json({error:error.message,code:error.code},error.status);
    console.error('Retry Assurance request failed',{type:error?.name||'Error'});
    return json({error:'The action could not be completed. Keep your local work. Retry idempotent runner actions with the same request identity; refresh account state before repeating a write.',code:'service_failure'},500);
  }
}
