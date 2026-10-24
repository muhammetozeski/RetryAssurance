import {readdir,readFile} from 'node:fs/promises';
import {execFileSync} from 'node:child_process';
import {join} from 'node:path';

/** Checks authored service/browser scripts without installing a package or changing production source. */
async function files(folder){const result=[];for(const entry of await readdir(folder,{withFileTypes:true})){const path=join(folder,entry.name);if(entry.isDirectory())result.push(...await files(path));else result.push(path);}return result;}
let count=0;for(const folder of ['server','functions','public'])for(const path of await files(folder)){if(/\.(mjs|js)$/.test(path)){execFileSync(process.execPath,['--check',path],{stdio:'inherit'});count++;}if(/\.(mjs|js|html|json|css)$/.test(path)){const source=await readFile(path,'utf8');if(/(?:cfut_|sk_live_|sk_test_|whsec_)[A-Za-z0-9]{16,}/.test(source))throw new Error('A provider-secret-shaped literal was found in '+path);}}
console.log(`${count} authored script files parsed; no provider-secret-shaped literal found.`);
