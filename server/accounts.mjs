import {day,fail,hash,id,json,limit,object,readBody,requireAccount,secret,sessionCookie,sessionHeader,text} from './security.mjs';

/** Actual secret-backed accounts; an alias is not a verified identity or an email address. */
export async function accounts(context){const {request,db,account,parts,ip,basePath=''}=context,now=Date.now();
  if(request.method==='GET'&&parts.join('/')==='account')return json({account});
  if(request.method!=='POST')return null;
  if(parts.join('/')==='accounts'){
    await limit(db,ip,'account-create',5,3600);const body=await readBody(request);object(body,['alias']);const alias=text(body.alias,'Account alias',80),accountId=crypto.randomUUID(),recoveryKey=secret(),session=secret();
    await db.batch([db.prepare('INSERT INTO ra_accounts(id,alias,recovery_hash,created_at) VALUES(?,?,?,?)').bind(accountId,alias,await hash(recoveryKey),now),db.prepare('INSERT INTO ra_sessions(token_hash,account_id,expires_at) VALUES(?,?,?)').bind(await hash(session),accountId,now+30*day)]);
    return json({account:{id:accountId,alias,createdAt:now},recoveryKey,warning:'This recovery key is shown once. Store it privately. Anyone holding it can access this account.'},201,sessionHeader(session,undefined,basePath));
  }
  if(parts.join('/')==='sessions'){
    await limit(db,ip,'account-recover',15,3600);const body=await readBody(request);object(body,['recoveryKey']);const key=text(body.recoveryKey,'Recovery key',43),found=await db.prepare('SELECT id,alias,created_at AS createdAt FROM ra_accounts WHERE recovery_hash=?').bind(await hash(key)).first();if(!found)fail(401,'The recovery key was not accepted.','authentication');const session=secret();await db.prepare('INSERT INTO ra_sessions(token_hash,account_id,expires_at) VALUES(?,?,?)').bind(await hash(session),found.id,now+30*day).run();return json({account:found},200,sessionHeader(session,undefined,basePath));
  }
  if(parts.join('/')==='sessions/logout'){
    const item=(request.headers.get('cookie')||'').split(';').map(value=>value.trim()).find(value=>value.startsWith(sessionCookie+'='));if(item)await db.prepare('DELETE FROM ra_sessions WHERE token_hash=?').bind(await hash(item.slice(sessionCookie.length+1))).run();return json({ok:true},200,sessionHeader('',0,basePath));
  }
  if(parts.join('/')==='invites/join'){
    requireAccount(account);const body=await readBody(request);object(body,['token']);const digest=await hash(text(body.token,'Invitation token',43)),invite=await db.prepare('SELECT * FROM ra_invites WHERE token_hash=? AND used_by IS NULL AND expires_at>?').bind(digest,now).first();if(!invite)fail(404,'The invitation is unavailable or expired.','not_found');
    const result=await db.batch([db.prepare('UPDATE ra_invites SET used_by=? WHERE token_hash=? AND used_by IS NULL AND expires_at>? AND (SELECT COUNT(*) FROM ra_members WHERE project_id=ra_invites.project_id)<10').bind(account.id,digest,now),db.prepare('INSERT INTO ra_members(project_id,account_id,role) SELECT project_id,?,role FROM ra_invites WHERE token_hash=? AND used_by=? ON CONFLICT(project_id,account_id) DO NOTHING').bind(account.id,digest,account.id)]);if(!result[0].meta.changes)fail(409,'This invitation was used or the project reached its ten-member allowance.','invite_used');return json({projectId:invite.project_id,role:invite.role});
  }
  if(parts.join('/')==='account/delete'){
    requireAccount(account);const body=await readBody(request);object(body,['confirmation']);if(body.confirmation!=='DELETE MY ACCOUNT')fail(400,'Type DELETE MY ACCOUNT to confirm account deletion.');const active=await db.prepare("SELECT subscription_id FROM ra_subscriptions WHERE account_id=? AND status IN('active','trialing') AND is_live=1 AND expires_at>?").bind(account.id,now).first();if(active)fail(409,'Cancel the active subscription and let its paid period end before deleting this account.','active_subscription');await db.batch([db.prepare('UPDATE ra_capabilities SET revoked_at=COALESCE(revoked_at,?) WHERE created_by=?').bind(now,account.id),db.prepare('DELETE FROM ra_accounts WHERE id=?').bind(account.id)]);return json({ok:true},200,sessionHeader('',0,basePath));
  }
  return null;
}
