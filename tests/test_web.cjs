const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const source=fs.readFileSync(require('node:path').join(__dirname,'../powerpoint-helper.js'),'utf8');
async function run(token,fetcher){
 const context={window:{},location:{origin:'https://ferlozada.github.io',pathname:'/TK5-Inspeccion-Fotografica/',hash:''},history:{replaceState(){}},localStorage:{getItem:()=>token},fetch:fetcher,AbortController,setTimeout,clearTimeout};
 vm.runInNewContext(source,context);return context.window.TK5PowerPoint.deliver({size:100});
}
(async()=>{
 assert.equal((await run(null,()=>{throw Error('must not fetch')})).launched,false);
 assert.equal((await run('a'.repeat(64),async()=>{throw Error('offline')})).launched,false);
 assert.equal((await run('a'.repeat(64),async(url,opts)=>{
  assert.equal(url,'http://127.0.0.1:18765/open');assert.equal(opts.targetAddressSpace,'loopback');assert.equal(opts.headers['X-TK5-Token'],'a'.repeat(64));
  return {ok:true,json:async()=>({saved:true,launched:true})};
 })).launched,true);
 assert.equal((await run('a'.repeat(64),async()=>({ok:false,status:401}))).launched,false);
 const html=fs.readFileSync(require('node:path').join(__dirname,'../index.html'),'utf8');
 for(const match of html.matchAll(/<script>([\s\S]*?)<\/script>/g))new vm.Script(match[1]);
 console.log('PASS: web syntax, missing helper, network failure, rejected token, authenticated delivery');
})().catch(e=>{console.error(e);process.exit(1)});
