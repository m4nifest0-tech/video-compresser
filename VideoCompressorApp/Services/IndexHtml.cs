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
  :root { color-scheme: light dark; }
  body { font-family: Segoe UI, Arial, sans-serif; margin: 0; padding: 16px; background: #f3f3f5; color: #1a1a1a; }
  .topbar { display: flex; justify-content: space-between; align-items: baseline; }
  h1 { font-size: 18px; margin: 0 0 4px; }
  .sub { color: #666; font-size: 12px; margin-bottom: 16px; }
  .card { background: #fff; border-radius: 8px; padding: 14px 16px; margin-bottom: 14px; box-shadow: 0 1px 3px rgba(0,0,0,.1); }
  .row { display: flex; flex-wrap: wrap; gap: 12px; align-items: center; margin-bottom: 10px; }
  .row label { font-size: 12px; color: #555; display: block; margin-bottom: 3px; }
  input[type=text], select { padding: 6px 8px; border: 1px solid #ccc; border-radius: 4px; font-size: 13px; }
  button { padding: 8px 14px; border: 1px solid #888; border-radius: 4px; background: #eee; cursor: pointer; font-size: 13px; }
  button.primary { background: #2563eb; color: #fff; border-color: #2563eb; }
  button.link { border: none; background: none; color: #2563eb; padding: 0; font-size: 12px; }
  button:disabled { opacity: .5; cursor: default; }
  #dropzone { border: 2px dashed #aaa; border-radius: 8px; padding: 22px; text-align: center; color: #666; font-size: 13px; cursor: pointer; }
  #dropzone.drag { border-color: #2563eb; color: #2563eb; background: #eef4ff; }
  table { width: 100%; border-collapse: collapse; font-size: 12px; }
  th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid #eee; }
  progress { width: 100%; height: 14px; }
  .status-Errore { color: #c0392b; font-weight: bold; }
  .status-Completato { color: #1e8449; font-weight: bold; }
  .status-In-corso { color: #2563eb; font-weight: bold; }
  #overallProgress { width: 100%; height: 18px; margin-bottom: 6px; }
  #statusLine { display: flex; justify-content: space-between; font-size: 12px; color: #555; }
  .actions button { font-size: 11px; padding: 4px 8px; margin-right: 4px; }
  a.download { font-size: 12px; }
  .gpu-title { font-size: 13px; font-weight: bold; margin-bottom: 8px; }
  .gpu-card { border: 1px solid #eee; border-radius: 6px; padding: 10px 12px; margin-bottom: 8px; }
  .gpu-card:last-child { margin-bottom: 0; }
  .gpu-name { font-size: 12px; font-weight: bold; margin-bottom: 8px; }
  .gpu-metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 10px; }
  .gpu-metric label { font-size: 11px; color: #777; display: block; margin-bottom: 3px; }
  .gpu-metric .value { font-size: 13px; font-weight: 600; }
  .meter { height: 8px; border-radius: 4px; background: #eee; overflow: hidden; margin-top: 4px; }
  .meter > div { height: 100%; background: #2563eb; }
  .meter.warn > div { background: #e67e22; }
  .meter.hot > div { background: #c0392b; }
  .gpu-empty { font-size: 12px; color: #777; }
  @media (prefers-color-scheme: dark) {
    body { background: #1c1c1e; color: #eee; }
    .card { background: #2a2a2d; box-shadow: none; }
    th, td { border-color: #3a3a3d; }
    input[type=text], select { background: #1c1c1e; color: #eee; border-color: #555; }
    button { background: #3a3a3d; color: #eee; border-color: #666; }
    .gpu-card { border-color: #3a3a3d; }
    .gpu-metric label { color: #999; }
    .meter { background: #3a3a3d; }
  }
</style>
</head>
<body>
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

async function uploadFiles(files) {
  if (!files || files.length === 0) return;
  const form = new FormData();
  for (const f of files) form.append('files', f);
  try { await api('/api/upload', { method: 'POST', body: form }); } catch (e) { alert(e.message); }
  refresh();
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
