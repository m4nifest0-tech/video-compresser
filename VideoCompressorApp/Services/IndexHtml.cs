namespace VideoCompressor.Services;

public static class IndexHtml
{
    public const string Content = """
<!doctype html>
<html lang="it">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Compressore Video - Accesso remoto</title>
<style>
  :root {
    color-scheme: light dark;
    --bg: #eef0f5;
    --fg: #1a1a1a;
    --sub: #666;
    --card-bg: rgba(255,255,255,.55);
    --card-border: rgba(255,255,255,.6);
    --card-shadow: 0 8px 32px rgba(31,38,135,.12);
    --input-bg: rgba(255,255,255,.7);
    --input-border: rgba(0,0,0,.12);
    --row-border: rgba(0,0,0,.06);
    --track: rgba(0,0,0,.08);
    --accent: #2563eb;
    --accent2: #7c3aed;
  }
  * { box-sizing: border-box; }
  html, body { min-height: 100%; }
  body {
    font-family: -apple-system, "Segoe UI", Arial, sans-serif; margin: 0; padding: 20px; color: var(--fg);
    background: var(--bg);
    background-image:
      radial-gradient(600px circle at 8% 8%, rgba(124,58,237,.25), transparent 60%),
      radial-gradient(600px circle at 92% 18%, rgba(37,99,235,.22), transparent 60%),
      radial-gradient(700px circle at 50% 100%, rgba(16,185,129,.16), transparent 60%);
    background-attachment: fixed;
  }
  .wrap { max-width: 980px; margin: 0 auto; }
  .topbar { display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 16px; }
  h1 { font-size: 20px; margin: 0 0 4px; letter-spacing: -.02em; }
  .sub { color: var(--sub); font-size: 12px; }
  .card {
    background: var(--card-bg);
    -webkit-backdrop-filter: blur(18px) saturate(160%);
    backdrop-filter: blur(18px) saturate(160%);
    border: 1px solid var(--card-border);
    border-radius: 16px; padding: 16px 18px; margin-bottom: 16px;
    box-shadow: var(--card-shadow);
  }
  .row { display: flex; flex-wrap: wrap; gap: 12px; align-items: center; margin-bottom: 10px; }
  .row label { font-size: 12px; color: var(--sub); display: block; margin-bottom: 3px; }
  input[type=text], select {
    padding: 7px 10px; border: 1px solid var(--input-border); border-radius: 8px; font-size: 13px;
    background: var(--input-bg); color: var(--fg);
  }
  button {
    padding: 8px 16px; border: 1px solid rgba(0,0,0,.12); border-radius: 8px;
    background: rgba(255,255,255,.6); color: var(--fg); cursor: pointer; font-size: 13px;
    transition: transform .08s ease, background .15s ease;
  }
  button:hover:not(:disabled) { transform: translateY(-1px); }
  button.primary { background: linear-gradient(135deg, var(--accent), var(--accent2)); color: #fff; border: none; }
  button.link { border: none; background: none; color: var(--accent); padding: 0; font-size: 12px; }
  button:disabled { opacity: .5; cursor: default; transform: none; }
  #dropzone {
    border: 2px dashed rgba(0,0,0,.2); border-radius: 12px; padding: 26px; text-align: center;
    color: var(--sub); font-size: 13px; cursor: pointer; transition: all .15s ease;
  }
  #dropzone.drag { border-color: var(--accent); color: var(--accent); background: rgba(37,99,235,.08); }
  #uploadProgressWrap { margin-top: 12px; }
  #uploadProgressWrap[hidden] { display: none; }
  #uploadProgressLabel { font-size: 12px; color: var(--sub); display: block; margin-top: 4px; }
  table { width: 100%; border-collapse: collapse; font-size: 12px; }
  th, td { text-align: left; padding: 7px 8px; border-bottom: 1px solid var(--row-border); }
  progress { width: 100%; height: 14px; }
  progress::-webkit-progress-bar { background: var(--track); border-radius: 7px; }
  progress::-webkit-progress-value { background: linear-gradient(90deg, var(--accent), var(--accent2)); border-radius: 7px; }
  progress::-moz-progress-bar { background: linear-gradient(90deg, var(--accent), var(--accent2)); border-radius: 7px; }
  .status-Errore { color: #c0392b; font-weight: bold; }
  .status-Completato { color: #1e8449; font-weight: bold; }
  .status-In-corso { color: var(--accent); font-weight: bold; }
  #overallProgress { width: 100%; height: 18px; margin-bottom: 6px; }
  #statusLine { display: flex; justify-content: space-between; font-size: 12px; color: var(--sub); }
  .actions button { font-size: 11px; padding: 4px 10px; margin-right: 4px; }
  a.download { font-size: 12px; color: var(--accent); }
  .gpu-title { font-size: 13px; font-weight: 600; margin-bottom: 10px; }
  .gpu-card { border: 1px solid var(--row-border); border-radius: 12px; padding: 12px 14px; margin-bottom: 8px; background: rgba(255,255,255,.25); }
  .gpu-card:last-child { margin-bottom: 0; }
  .gpu-name { font-size: 12px; font-weight: 600; margin-bottom: 8px; }
  .gpu-metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 12px; }
  .gpu-metric label { font-size: 11px; color: var(--sub); display: block; margin-bottom: 3px; }
  .gpu-metric .value { font-size: 13px; font-weight: 600; }
  .meter { height: 8px; border-radius: 4px; background: var(--track); overflow: hidden; margin-top: 4px; }
  .meter > div { height: 100%; background: linear-gradient(90deg, var(--accent), var(--accent2)); }
  .meter.warn > div { background: #e67e22; }
  .meter.hot > div { background: #c0392b; }
  .gpu-empty { font-size: 12px; color: var(--sub); }
  @media (prefers-color-scheme: dark) {
    :root {
      --bg: #131417; --fg: #eee; --sub: #999;
      --card-bg: rgba(35,36,42,.55); --card-border: rgba(255,255,255,.08);
      --card-shadow: 0 8px 32px rgba(0,0,0,.35);
      --input-bg: rgba(255,255,255,.06); --input-border: rgba(255,255,255,.14);
      --row-border: rgba(255,255,255,.08); --track: rgba(255,255,255,.1);
    }
    body {
      background-image:
        radial-gradient(600px circle at 8% 8%, rgba(124,58,237,.30), transparent 60%),
        radial-gradient(600px circle at 92% 18%, rgba(37,99,235,.28), transparent 60%),
        radial-gradient(700px circle at 50% 100%, rgba(16,185,129,.18), transparent 60%);
    }
    button { background: rgba(255,255,255,.08); }
    .gpu-card { background: rgba(255,255,255,.03); }
  }
</style>
</head>
<body>
<div class="wrap">
  <div class="topbar">
    <div>
      <h1>Compressore Video</h1>
      <div class="sub">Accesso remoto - carica video, avvia la compressione e scarica il risultato.</div>
    </div>
    <button class="link" id="logoutBtn">Esci</button>
  </div>

  <div class="card">
    <div class="gpu-title">GPU</div>
    <div id="gpuBody"><div class="gpu-empty">Lettura in corso...</div></div>
  </div>

  <div class="card">
    <div class="row">
      <div>
        <label>Cartella destinazione (sul server)</label>
        <input type="text" id="destDir" style="width:320px">
      </div>
      <div>
        <label>Codec</label>
        <select id="codec"></select>
      </div>
      <div>
        <label>Livello compressione</label>
        <select id="level"></select>
      </div>
      <div>
        <button id="applySettings">Applica impostazioni</button>
      </div>
    </div>
    <div class="row">
      <label><input type="checkbox" id="preserveStructure"> Mantieni struttura cartelle</label>
      <label><input type="checkbox" id="skipExisting"> Salta se gia' esistente</label>
      <label><input type="checkbox" id="deleteSource"> Elimina originale dopo compressione</label>
    </div>
  </div>

  <div class="card">
    <div id="dropzone">Trascina qui i video oppure clicca per selezionarli</div>
    <input type="file" id="fileInput" multiple accept="video/*" style="display:none">
    <div id="uploadProgressWrap" hidden>
      <progress id="uploadProgress" max="100" value="0"></progress>
      <span id="uploadProgressLabel"></span>
    </div>
  </div>

  <div class="card">
    <progress id="overallProgress" max="1" value="0"></progress>
    <div id="statusLine">
      <span id="statusText">Pronto.</span>
      <span id="etaText"></span>
    </div>
    <div id="estimateSummary" class="sub"></div>
    <div class="row" style="margin-top:10px">
      <button id="estimateBtn">Stima compressione</button>
      <button id="startBtn" class="primary">Avvia compressione</button>
      <button id="cancelBtn" disabled>Annulla</button>
    </div>
  </div>

  <div class="card">
    <table>
      <thead>
        <tr>
          <th>File</th><th>Originale</th><th>Stato</th><th>Avanzamento</th>
          <th>Stima</th><th>Risultato</th><th></th>
        </tr>
      </thead>
      <tbody id="itemsBody"></tbody>
    </table>
  </div>
</div>

<script>
let settingsLoaded = false;

function escapeHtml(s) {
  return (s || "").replace(/[&<>"']/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
}

async function api(path, opts) {
  const res = await fetch(path, opts);
  if (res.status === 401) {
    location.href = '/login';
    throw new Error('Sessione scaduta.');
  }
  if (!res.ok) {
    let msg = res.statusText;
    try { const j = await res.json(); if (j && j.error) msg = j.error; } catch {}
    throw new Error(msg);
  }
  return res.status === 204 ? null : res.json().catch(() => null);
}

function renderItems(items) {
  const body = document.getElementById('itemsBody');
  body.innerHTML = items.map(it => `
    <tr>
      <td>${escapeHtml(it.fileName)}</td>
      <td>${escapeHtml(it.originalSizeText)}</td>
      <td class="status-${it.status.replace(/\s+/g,'-')}">${escapeHtml(it.status)}${it.hasErrorLog ? ' &#9888;' : ''}</td>
      <td><progress max="100" value="${it.progressPercent}"></progress></td>
      <td>${escapeHtml(it.estimatedSizeText)}</td>
      <td>${escapeHtml(it.resultSizeText)}</td>
      <td class="actions">
        ${it.hasResult ? `<a class="download" href="/api/download/${it.id}">Scarica</a>` : ''}
        <button onclick="removeItem('${it.id}')">Rimuovi</button>
      </td>
    </tr>`).join('');
}

function fillSelect(sel, options, valueKey, labelKey, selectedValue) {
  sel.innerHTML = options.map(o => `<option value="${o[valueKey]}">${escapeHtml(o[labelKey])}</option>`).join('');
  sel.value = selectedValue;
}

function meterClass(percent) {
  if (percent >= 90) return 'meter hot';
  if (percent >= 75) return 'meter warn';
  return 'meter';
}

function metric(label, valueText, percent) {
  const bar = percent == null ? '' : `<div class="${meterClass(percent)}"><div style="width:${Math.min(100, Math.max(0, percent))}%"></div></div>`;
  return `<div class="gpu-metric"><label>${label}</label><div class="value">${valueText}</div>${bar}</div>`;
}

function renderGpu(gpus) {
  const body = document.getElementById('gpuBody');
  if (!gpus || gpus.length === 0) {
    body.innerHTML = '<div class="gpu-empty">GPU NVIDIA non rilevata (nvidia-smi non disponibile).</div>';
    return;
  }

  body.innerHTML = gpus.map(g => {
    const memPercent = (g.memoryUsedMb != null && g.memoryTotalMb) ? (100 * g.memoryUsedMb / g.memoryTotalMb) : null;
    const memText = (g.memoryUsedMb != null && g.memoryTotalMb != null)
      ? `${Math.round(g.memoryUsedMb)} / ${Math.round(g.memoryTotalMb)} MB` : '-';
    const powerText = (g.powerDrawW != null)
      ? `${g.powerDrawW.toFixed(0)} W${g.powerLimitW ? ' / ' + g.powerLimitW.toFixed(0) + ' W' : ''}` : '-';
    return `
      <div class="gpu-card">
        <div class="gpu-name">${escapeHtml(g.name)}</div>
        <div class="gpu-metrics">
          ${metric('Temperatura', g.temperatureC != null ? g.temperatureC.toFixed(0) + ' &deg;C' : '-', g.temperatureC != null ? (g.temperatureC / 90 * 100) : null)}
          ${metric('Utilizzo GPU', g.utilizationGpuPercent != null ? g.utilizationGpuPercent.toFixed(0) + ' %' : '-', g.utilizationGpuPercent)}
          ${metric('Utilizzo memoria', g.utilizationMemPercent != null ? g.utilizationMemPercent.toFixed(0) + ' %' : '-', g.utilizationMemPercent)}
          ${metric('Memoria', memText, memPercent)}
          ${metric('Potenza', powerText, null)}
        </div>
      </div>`;
  }).join('');
}

async function refresh() {
  let state;
  try { state = await api('/api/state'); } catch { return; }

  if (!settingsLoaded) {
    fillSelect(document.getElementById('codec'), state.codecs, 'value', 'label', state.codecValue);
    fillSelect(document.getElementById('level'), state.levels, 'cq', 'label', state.levelCq);
    document.getElementById('destDir').value = state.destDir;
    document.getElementById('preserveStructure').checked = state.preserveStructure;
    document.getElementById('skipExisting').checked = state.skipExisting;
    document.getElementById('deleteSource').checked = state.deleteSource;
    settingsLoaded = true;
  }

  document.getElementById('overallProgress').max = state.overallProgressMax || 1;
  document.getElementById('overallProgress').value = state.overallProgressValue;
  document.getElementById('statusText').textContent = state.statusText;
  document.getElementById('etaText').textContent = state.etaText;
  document.getElementById('estimateSummary').textContent = state.estimateSummaryText;
  document.getElementById('estimateBtn').disabled = state.busy;
  document.getElementById('startBtn').disabled = state.busy;
  document.getElementById('cancelBtn').disabled = !state.busy;
  renderItems(state.items);
}

async function refreshGpu() {
  try { renderGpu(await api('/api/gpu')); } catch { /* la prossima chiamata riprovera' */ }
}

async function removeItem(id) {
  try { await api('/api/items/' + id, { method: 'DELETE' }); } catch (e) { alert(e.message); }
  refresh();
}

document.getElementById('logoutBtn').addEventListener('click', async () => {
  try { await api('/logout', { method: 'POST' }); } catch {}
  location.href = '/login';
});

document.getElementById('applySettings').addEventListener('click', async () => {
  const body = {
    destDir: document.getElementById('destDir').value,
    codecValue: document.getElementById('codec').value,
    levelCq: parseInt(document.getElementById('level').value, 10),
    preserveStructure: document.getElementById('preserveStructure').checked,
    skipExisting: document.getElementById('skipExisting').checked,
    deleteSource: document.getElementById('deleteSource').checked,
  };
  try {
    await api('/api/settings', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  } catch (e) { alert(e.message); }
  refresh();
});

document.getElementById('estimateBtn').addEventListener('click', async () => {
  try { await api('/api/estimate', { method: 'POST' }); } catch (e) { alert(e.message); }
  refresh();
});
document.getElementById('startBtn').addEventListener('click', async () => {
  try { await api('/api/start', { method: 'POST' }); } catch (e) { alert(e.message); }
  refresh();
});
document.getElementById('cancelBtn').addEventListener('click', async () => {
  try { await api('/api/cancel', { method: 'POST' }); } catch (e) { alert(e.message); }
  refresh();
});

function uploadFiles(files) {
  if (!files || files.length === 0) return Promise.resolve();

  const form = new FormData();
  for (const f of files) form.append('files', f);

  const wrap = document.getElementById('uploadProgressWrap');
  const bar = document.getElementById('uploadProgress');
  const label = document.getElementById('uploadProgressLabel');
  wrap.hidden = false;
  bar.value = 0;
  label.textContent = `Caricamento di ${files.length} file: 0%`;

  return new Promise(resolve => {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/upload');
    xhr.upload.addEventListener('progress', e => {
      if (!e.lengthComputable) return;
      const pct = Math.round(100 * e.loaded / e.total);
      bar.value = pct;
      label.textContent = `Caricamento di ${files.length} file: ${pct}%`;
    });
    xhr.addEventListener('load', () => {
      wrap.hidden = true;
      if (xhr.status === 401) { location.href = '/login'; resolve(); return; }
      if (xhr.status < 200 || xhr.status >= 300) {
        let msg = xhr.statusText;
        try { const j = JSON.parse(xhr.responseText); if (j && j.error) msg = j.error; } catch {}
        alert(msg);
      }
      refresh();
      resolve();
    });
    xhr.addEventListener('error', () => {
      wrap.hidden = true;
      alert('Errore di rete durante il caricamento.');
      resolve();
    });
    xhr.send(form);
  });
}

const dropzone = document.getElementById('dropzone');
const fileInput = document.getElementById('fileInput');
dropzone.addEventListener('click', () => fileInput.click());
fileInput.addEventListener('change', () => uploadFiles(fileInput.files));
dropzone.addEventListener('dragover', e => { e.preventDefault(); dropzone.classList.add('drag'); });
dropzone.addEventListener('dragleave', () => dropzone.classList.remove('drag'));
dropzone.addEventListener('drop', e => {
  e.preventDefault();
  dropzone.classList.remove('drag');
  uploadFiles(e.dataTransfer.files);
});

refresh();
refreshGpu();
setInterval(refresh, 1500);
setInterval(refreshGpu, 2000);
</script>
</body>
</html>
""";
}
