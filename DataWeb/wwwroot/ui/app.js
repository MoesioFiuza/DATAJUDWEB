const $ = (s) => document.querySelector(s);

function setProgress(el, pct){
  if(!el) return;
  el.classList.remove('hidden');
  const bar = el.querySelector('.bar');
  if(bar) bar.style.width = `${pct}%`;
  if(pct >= 100) setTimeout(()=> el.classList.add('hidden'), 500);
}
function toast(msg){
  const t = $('#toast'); if(!t) return;
  t.textContent = msg; t.classList.remove('hidden');
  setTimeout(()=> t.classList.add('hidden'), 2500);
}

async function postExcel(file, onProgress){
  const form = new FormData(); form.append('file', file);
  const xhr = new XMLHttpRequest();
  return await new Promise((resolve, reject)=>{
    xhr.open('POST', '../upload-xlsx-excel', true);
    xhr.responseType = 'blob';
    xhr.upload.onprogress = e=>{
      if(e.lengthComputable && onProgress){
        onProgress(Math.round((e.loaded/e.total)*100));
      }
    };
    xhr.onload = ()=>{
      if(xhr.status>=200 && xhr.status<300) resolve(xhr.response);
      else{
        const reader = new FileReader();
        reader.onload = ()=> reject(new Error(`HTTP ${xhr.status}: ${reader.result}`));
        reader.readAsText(xhr.response || new Blob(['Erro']));
      }
    };
    xhr.onerror = ()=> reject(new Error('Erro de rede'));
    xhr.send(form);
  });
}

async function generate(){
  const file = $('#file-xlsx')?.files?.[0];
  const progress = $('#progress');
  const dl = $('#btn-download');
  const st = $('#status');

  if(!file){ toast('Selecione um arquivo .xlsx.'); return; }
  if(dl) dl.classList.add('hidden');
  if(st) st.textContent = 'Processando...';

  try{
    setProgress(progress, 5);
    const blob = await postExcel(file, p=>setProgress(progress, p));
    setProgress(progress, 100);

    const url = URL.createObjectURL(blob);
    if(dl){ dl.href = url; dl.classList.remove('hidden'); }
    if(st) st.textContent = 'Pronto! Se algum CNJ não retornou dados, veja a aba "Não encontrados" no Excel.';
  }catch(err){
    setProgress(progress, 100);
    if(st) st.textContent = '';
    toast(err.message || 'Falha ao gerar Excel');
  }
}

// Botões
$('#btn-generate')?.addEventListener('click', (e)=>{ e.preventDefault(); generate(); });

// Drag & Drop
(function(){
  const zone = document.getElementById('drop-xlsx');
  const input = document.getElementById('file-xlsx');
  if(!zone || !input) return;
  const highlight = on => zone.style.borderColor = on ? 'rgba(255,255,255,.22)' : 'var(--border)';
  ['dragenter','dragover'].forEach(evt => zone.addEventListener(evt, e=>{e.preventDefault(); e.stopPropagation(); highlight(true);}));
  ['dragleave','drop'].forEach(evt => zone.addEventListener(evt, e=>{e.preventDefault(); e.stopPropagation(); highlight(false);}));
  zone.addEventListener('drop', e=>{
    const file = e.dataTransfer?.files?.[0];
    if(file){ input.files = e.dataTransfer.files; toast(`Selecionado: ${file.name}`); }
  });
  zone.addEventListener('click', ()=> input.click());
})();

// Tema
(function(){
  const btn = document.getElementById('theme-toggle');
  if(!btn) return;
  const apply = t => document.documentElement.setAttribute('data-theme', t);
  try{
    const saved = localStorage.getItem('theme'); if(saved) apply(saved);
  }catch{}
  btn.addEventListener('click', ()=>{
    const cur = document.documentElement.getAttribute('data-theme') || 'dark';
    const next = cur === 'dark' ? 'light' : 'dark';
    apply(next);
    try{ localStorage.setItem('theme', next);}catch{}
  });
})();