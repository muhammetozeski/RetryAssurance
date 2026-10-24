import {handleRequest} from '../../server/handler.mjs';

/** The independent origin keeps its existing root-level API and customer URLs. */
export async function onRequest(context){return handleRequest(context);}
