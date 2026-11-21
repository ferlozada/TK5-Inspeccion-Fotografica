/* r52: only receives a finished Blob. Never reads or modifies inspection state. */
window.TK5PowerPoint = (() => {
 'use strict';
 const key='tk5-powerpoint-helper-token';
 if(location.origin==='https://ferlozada.github.io' && location.pathname==='/TK5-Inspeccion-Fotografica/') {
  const match=/^#tk5-helper=([a-f0-9]{64})$/.exec(location.hash);
  if(match) {
   history.replaceState(null,'',location.pathname+location.search);
   try { localStorage.setItem(key,match[1]); } catch (_) { /* fallback download remains available */ }
  }
 }
 async function deliver(blob) {
  let token;try {token=localStorage.getItem(key);}catch (_) {return {launched:false};}
  if(!/^[a-f0-9]{64}$/.test(token||'') || blob.size>256*1024*1024) return {launched:false};
  const controller=new AbortController(),timer=setTimeout(()=>controller.abort(),120000);
  try {
   const response=await fetch('http://127.0.0.1:18765/open',{
    method:'POST',mode:'cors',credentials:'omit',cache:'no-store',redirect:'error',
    targetAddressSpace:'loopback',signal:controller.signal,
    headers:{'Content-Type':'application/vnd.openxmlformats-officedocument.presentationml.presentation','X-TK5-Token':token},body:blob
   });
   if(!response.ok) throw Error('Helper HTTP '+response.status);
   const result=await response.json();
   return {launched:result.launched===true,saved:result.saved===true};
  }catch (_) {return {launched:false};}finally {clearTimeout(timer);}
 }
 return {deliver};
})();
