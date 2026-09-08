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
<script>
  // Applicato subito, prima del CSS/paint: evita un lampo con il tema sbagliato al caricamento.
  (function() {
    try {
      var theme = localStorage.getItem('vc_theme');
      var accent = localStorage.getItem('vc_accent');
      if (theme && theme !== 'system') document.documentElement.setAttribute('data-theme', theme);
      document.documentElement.setAttribute('data-accent', accent || 'blue');
    } catch {}
  })();
</script>
<style>
  :root {
    color-scheme: light dark;
    --bg: #f5f5f7;
    --fg: #1d1d1f;
    --sub: #6e6e73;
    --card-bg: rgba(255,255,255,.72);
    --card-border: rgba(0,0,0,.06);
    --card-shadow: 0 20px 50px -20px rgba(0,0,0,.18), 0 2px 6px rgba(0,0,0,.04);
    --input-bg: rgba(0,0,0,.04);
    --input-border: rgba(0,0,0,.12);
    --control-bg: rgba(0,0,0,.04);
    --control-border: rgba(0,0,0,.1);
    --control-hover: rgba(0,0,0,.08);
    --row-border: rgba(0,0,0,.08);
    --track: rgba(0,0,0,.08);
    --gpu-card-bg: rgba(0,0,0,.03);
    --accent: #2563eb;
    --accent2: #7c3aed;
    --accent-glow: rgba(37,99,235,.35);
    --accent-tint: rgba(37,99,235,.1);
    /* i menu a tendina nativi non renderizzano bene sfondi semi-trasparenti: qui serve un colore pieno */
    --select-bg: #ffffff;
    --select-fg: #1d1d1f;
    --error-bg: rgba(192,57,43,.12);
    --error-border: rgba(192,57,43,.35);
    --error-fg: #c0392b;
    --aurora-opacity: .14;
    /* Superficie del "chip" GPU: gradiente chiaro->scuro per simulare un die in metallo spazzolato,
       come nei render dei chip Apple. */
    --chip-hi: #ffffff;
    --chip-mid: #e4e6eb;
    --chip-lo: #aeb2bc;
  }
  * { box-sizing: border-box; }
  html, body { min-height: 100%; }
  body {
    font-family: -apple-system, "Segoe UI", Arial, sans-serif; margin: 0; padding: 20px; color: var(--fg);
    background: var(--bg);
    position: relative; overflow-x: hidden;
    transition: background-color .4s ease, color .4s ease;
  }
  /* Sfondo "aurora": macchie di colore sfocate e in lento movimento, nello spirito delle pagine
     Apple (WWDC, visionOS) - al posto della vecchia foto statica. */
  .aurora { position: fixed; inset: 0; z-index: 0; overflow: hidden; pointer-events: none;
    opacity: var(--aurora-opacity); transition: opacity .4s ease; }
  .aurora span { position: absolute; border-radius: 50%; filter: blur(80px); will-change: transform; }
  .aurora span:nth-child(1) { width: 60vmax; height: 60vmax; top: -25vmax; left: -20vmax;
    background: var(--accent); animation: auroraDrift1 26s ease-in-out infinite; }
  .aurora span:nth-child(2) { width: 50vmax; height: 50vmax; bottom: -22vmax; right: -15vmax;
    background: var(--accent2); animation: auroraDrift2 32s ease-in-out infinite; }
  .aurora span:nth-child(3) { width: 38vmax; height: 38vmax; top: 30%; left: 60%;
    background: var(--accent); opacity: .6; animation: auroraDrift3 22s ease-in-out infinite; }
  @keyframes auroraDrift1 { 0%, 100% { transform: translate(0,0) scale(1); } 50% { transform: translate(8vmax,6vmax) scale(1.15); } }
  @keyframes auroraDrift2 { 0%, 100% { transform: translate(0,0) scale(1); } 50% { transform: translate(-6vmax,-8vmax) scale(1.1); } }
  @keyframes auroraDrift3 { 0%, 100% { transform: translate(0,0) scale(1); } 50% { transform: translate(-9vmax,5vmax) scale(.92); } }
  .wrap { position: relative; z-index: 1; max-width: 980px; margin: 0 auto; }
  .topbar { display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 16px;
    animation: riseIn .5s cubic-bezier(.16,1,.3,1) both; }
  h1 { font-size: 28px; font-weight: 700; margin: 0 0 4px; letter-spacing: -.03em; }
  .sub { color: var(--sub); font-size: 13px; }
  .card {
    position: relative;
    background: var(--card-bg);
    -webkit-backdrop-filter: blur(30px) saturate(200%);
    backdrop-filter: blur(30px) saturate(200%);
    border: 1px solid var(--card-border);
    border-radius: 24px; padding: 26px 28px; margin-bottom: 20px;
    box-shadow: var(--card-shadow);
    animation: riseIn .6s cubic-bezier(.16,1,.3,1) both;
    transition: background-color .4s ease, border-color .4s ease, box-shadow .4s ease;
  }
  @keyframes riseIn { from { opacity: 0; transform: translateY(16px) scale(.98); } to { opacity: 1; transform: translateY(0) scale(1); } }
  .wrap > .card:nth-of-type(1) { animation-delay: .04s; }
  .wrap > .card:nth-of-type(2) { animation-delay: .09s; }
  .wrap > .card:nth-of-type(3) { animation-delay: .14s; }
  .wrap > .card:nth-of-type(4) { animation-delay: .19s; }
  .wrap > .card:nth-of-type(5) { animation-delay: .24s; }
  /* Se backdrop-filter non e' supportato (o disattivato per risparmio energetico), la card
     resterebbe quasi trasparente sull'aurora sottostante invece che su un vetro sfocato: senza il
     blur a coprirla, serve un'opacita' molto piu' alta per garantire comunque il contrasto del testo. */
  @supports not (backdrop-filter: blur(1px)) {
    .card { background: rgba(255,255,255,.94); }
    .gpu-card { background: rgba(255,255,255,.85); }
    html:not([data-theme="light"]) .card { background: rgba(22,23,28,.94); }
    html:not([data-theme="light"]) .gpu-card { background: rgba(10,10,12,.8); }
    html[data-theme="dark"] .card { background: rgba(22,23,28,.94); }
    html[data-theme="dark"] .gpu-card { background: rgba(10,10,12,.8); }
  }
  .row { display: flex; flex-wrap: wrap; gap: 12px; align-items: center; margin-bottom: 10px; }
  .row label { font-size: 13px; color: var(--sub); display: block; margin-bottom: 3px; }
  input[type=checkbox] {
    width: 16px; height: 16px; margin: 0 6px 0 0; vertical-align: -3px;
    accent-color: var(--accent); cursor: pointer;
  }
  input[type=text] {
    padding: 9px 12px; border: 1px solid var(--input-border); border-radius: 10px; font-size: 13px;
    background: var(--input-bg); color: var(--fg);
    transition: border-color .15s ease, box-shadow .15s ease;
  }
  input[type=text]:hover { border-color: var(--accent); }
  select {
    appearance: none; -webkit-appearance: none;
    padding: 9px 30px 9px 12px; border: 1px solid var(--input-border); border-radius: 10px; font-size: 13px;
    background: var(--select-bg) url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='6'%3E%3Cpath d='M1 1l4 4 4-4' stroke='%238e8e93' stroke-width='1.5' fill='none' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E") no-repeat right 12px center;
    color: var(--select-fg); cursor: pointer;
    transition: border-color .15s ease, box-shadow .15s ease;
  }
  select:hover { border-color: var(--accent); }
  select option { background: var(--select-bg); color: var(--select-fg); }
  button, a.btn {
    padding: 10px 18px; border: 1px solid var(--control-border); border-radius: 12px;
    background: var(--control-bg); color: var(--fg); cursor: pointer; font-size: 13px; font-weight: 500;
    transition: transform .15s cubic-bezier(.34,1.56,.64,1), background-color .15s ease, box-shadow .15s ease, border-color .15s ease;
    display: inline-block; text-decoration: none; box-sizing: border-box;
  }
  button:hover:not(:disabled), a.btn:hover { background: var(--control-hover); transform: translateY(-1px); }
  button:active:not(:disabled), a.btn:active { transform: scale(.96); }
  button.primary, a.btn.primary {
    background: var(--accent); color: #fff; border-color: transparent; font-weight: 600;
    border-radius: 980px; padding: 10px 22px;
    box-shadow: 0 8px 20px -6px var(--accent-glow);
  }
  button.primary:hover:not(:disabled), a.btn.primary:hover {
    background: var(--accent); filter: brightness(1.08); transform: translateY(-1px);
    box-shadow: 0 10px 24px -6px var(--accent-glow);
  }
  button:disabled { opacity: .45; cursor: default; transform: none; box-shadow: none; }
  /* Indicatore di focus da tastiera come "alone" morbido, in stile macOS, al posto del semplice
     outline: resta ben leggibile sopra il vetro smerigliato senza tagli dovuti al blur. */
  button:focus-visible, a.btn:focus-visible, input:focus-visible, select:focus-visible,
  #dropzone:focus-visible {
    outline: none; box-shadow: 0 0 0 4px var(--accent-glow);
  }
  #dropzone {
    border: 1.5px dashed var(--input-border); border-radius: 16px; padding: 32px 20px; text-align: center;
    color: var(--sub); font-size: 13px; cursor: pointer;
    transition: border-color .2s ease, background-color .2s ease, transform .2s ease, box-shadow .2s ease;
  }
  #dropzone:hover { border-color: var(--accent); background: var(--control-bg); }
  #dropzone.drag {
    border-color: var(--accent); color: var(--accent); background: var(--accent-tint);
    transform: scale(1.01); animation: dropPulse 1.4s ease-in-out infinite;
  }
  @keyframes dropPulse {
    0%, 100% { box-shadow: 0 0 0 0 var(--accent-glow); }
    50% { box-shadow: 0 0 0 8px transparent; }
  }
  #uploadProgressWrap { margin-top: 12px; }
  #uploadProgressWrap[hidden] { display: none; }
  #uploadProgressLabel { font-size: 12px; color: var(--sub); display: block; margin-top: 4px; }
  table { width: 100%; border-collapse: collapse; font-size: 12px; }
  th, td { text-align: left; padding: 7px 8px; border-bottom: 1px solid var(--row-border); }
  /* non sono elementi interattivi: pointer-events:none impedisce qualunque stato hover nativo
     del browser (alcuni motori applicano un lieve highlight/cursore ai controlli <progress>) */
  progress { width: 100%; height: 14px; pointer-events: none; }
  progress::-webkit-progress-bar { background: var(--track); border-radius: 7px; }
  progress::-webkit-progress-value { background: var(--accent); border-radius: 7px; transition: width .25s cubic-bezier(.4,0,.2,1); }
  progress::-moz-progress-bar { background: var(--accent); border-radius: 7px; transition: width .25s cubic-bezier(.4,0,.2,1); }
  .status-Errore { color: #c0392b; font-weight: bold; }
  .status-Completato { color: #1e8449; font-weight: bold; }
  .status-In-corso { color: var(--accent); font-weight: bold; }
  #overallProgress { width: 100%; height: 18px; margin-bottom: 6px; }
  #statusLine { display: flex; justify-content: space-between; font-size: 12px; color: var(--sub); }
  .actions button, .actions a.btn { font-size: 12px; padding: 4px 10px; margin-right: 4px; }
  .gpu-card { border: 1px solid var(--row-border); border-radius: 16px; padding: 18px 20px; margin-bottom: 10px;
    background: var(--gpu-card-bg); transition: background-color .4s ease, border-color .4s ease; }
  .gpu-card:last-child { margin-bottom: 0; }
  .gpu-metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 10px; }
  .gpu-metric {
    background: var(--control-bg); border-radius: 14px; padding: 12px 14px;
    transition: background-color .4s ease, transform .18s cubic-bezier(.34,1.56,.64,1), box-shadow .18s ease;
  }
  .gpu-metric label { font-size: 12px; color: var(--sub); display: block; margin-bottom: 4px; }
  .gpu-metric .value { font-size: 14px; font-weight: 600; }
  .meter { height: 8px; border-radius: 4px; background: var(--track); overflow: hidden; margin-top: 4px; }
  .meter > div { height: 100%; background: var(--accent); transition: width .3s cubic-bezier(.4,0,.2,1); }
  .meter.warn > div { background: #e67e22; }
  .meter.hot > div { background: #c0392b; }
  .gpu-empty { font-size: 13px; color: var(--sub); }
  /* Sezione GPU: un modellino della scheda video vera e propria (shroud, ventola, bordo PCIe,
     staffa I/O), assemblato con facce CSS 3D reali (transform-style: preserve-3d) invece di un
     trucco 2D - ruota davvero nello spazio. La ventola gira alla velocita' reale della GPU (si
     ferma se il sensore riporta 0%, come una scheda vera a riposo) e la barra luminosa sul bordo
     si accende in base all'utilizzo, virando all'arancio/rosso alle temperature piu' alte: il
     risultato e' quindi sempre diverso, e vivo, per ogni utente/macchina. */
  .gpu-hero { display: flex; align-items: center; gap: 36px; margin-bottom: 20px; flex-wrap: wrap; }
  .chip-stage { flex: none; width: 260px; height: 180px; perspective: 950px; }
  .chip-tilt {
    width: 100%; height: 100%; transform-style: preserve-3d;
    transform: rotateX(var(--ry, 0deg)) rotateY(var(--rx, 0deg));
    transition: transform .4s cubic-bezier(.22,1,.36,1);
  }
  .chip-float {
    width: 100%; height: 100%; transform-style: preserve-3d;
    display: flex; align-items: center; justify-content: center;
    animation: chipFloat 9s ease-in-out infinite;
  }
  @keyframes chipFloat {
    0%, 100% { transform: rotateX(14deg) rotateY(-30deg) translateY(0); }
    50% { transform: rotateX(11deg) rotateY(-24deg) translateY(-8px); }
  }
  /* Costruzione del box 3D: ogni faccia e' centrata nel genitore (position:absolute, top/left 50%,
     margine negativo pari a meta' delle proprie dimensioni) e poi ruotata e allontanata lungo Z di
     meta' della terza dimensione del box (W=192, H=58, D=74) - la formula standard per assemblare
     un cuboide in CSS 3D. Servono solo le 3 facce visibili dall'angolazione della card (fronte con
     doppia ventola, dissipatore visto dall'alto, staffa I/O): l'oggetto non ruota mai abbastanza da
     scoprire le facce mancanti. */
  .gpu-model {
    position: relative; width: 192px; height: 58px; flex: none;
    transform-style: preserve-3d;
    filter: drop-shadow(0 22px 26px rgba(0,0,0,.45)) drop-shadow(0 0 26px var(--chip-tile-color, var(--accent-glow)));
    transition: filter .6s ease;
  }
  .gpu-face { position: absolute; top: 50%; left: 50%; backface-visibility: hidden; }
  .gpu-face-front {
    width: 192px; height: 58px; margin: -29px 0 0 -96px;
    border-radius: 9px;
    background: linear-gradient(155deg, #38393f 0%, #18191c 55%, #0a0a0c 100%);
    box-shadow: inset 0 0 0 1px rgba(255,255,255,.07), inset 0 1px 0 rgba(255,255,255,.09);
    transform: translateZ(37px);
    display: flex; align-items: center; justify-content: space-evenly; padding: 0 12px;
  }
  .gpu-face-top {
    width: 192px; height: 74px; margin: -37px 0 0 -96px;
    background: repeating-linear-gradient(90deg, #dcdee3 0 1.5px, #9a9ca3 1.5px 2.5px, #4c4d52 2.5px 6px);
    transform: rotateX(90deg) translateZ(29px);
    border-radius: 9px 9px 0 0;
    box-shadow: inset 0 1px 0 rgba(255,255,255,.3);
  }
  .gpu-face-end {
    width: 74px; height: 58px; margin: -29px 0 0 -37px;
    background: linear-gradient(90deg, #404146, #1c1d20);
    transform: rotateY(90deg) translateZ(96px);
    border-radius: 0 9px 9px 0;
    display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px;
    box-shadow: inset 0 0 14px rgba(0,0,0,.55);
  }
  .gpu-port { height: 5px; border-radius: 1px; background: #060607; box-shadow: inset 0 0 1px rgba(255,255,255,.18); }
  .gpu-port-wide { width: 58%; }
  .gpu-port:not(.gpu-port-wide) { width: 40%; }
  /* Feritoie di sfiato tra le due ventole, come su una scheda a doppia/tripla ventola vera. */
  .gpu-vents {
    align-self: stretch; width: 14px; margin: 10px 0; border-radius: 2px;
    background-image: radial-gradient(circle, rgba(255,255,255,.12) 1px, transparent 1.4px);
    background-size: 6px 6px;
    opacity: .8;
  }
  .gpu-fan { position: relative; width: 42px; height: 42px; border-radius: 50%; flex: none; }
  .gpu-fan-ring {
    position: absolute; inset: 0; border-radius: 50%;
    background: radial-gradient(circle at 35% 30%, #45464c, #08090a 72%);
    box-shadow: inset 0 0 5px rgba(0,0,0,.85), 0 0 0 1px rgba(255,255,255,.06);
  }
  .gpu-fan-blades {
    position: absolute; inset: 3px; border-radius: 50%;
    background: conic-gradient(#212227 0deg 12deg, transparent 12deg 45deg,
      #212227 45deg 57deg, transparent 57deg 90deg, #212227 90deg 102deg, transparent 102deg 135deg,
      #212227 135deg 147deg, transparent 147deg 180deg, #212227 180deg 192deg, transparent 192deg 225deg,
      #212227 225deg 237deg, transparent 237deg 270deg, #212227 270deg 282deg, transparent 282deg 315deg,
      #212227 315deg 327deg, transparent 327deg 360deg);
    animation: fanSpin linear infinite;
    animation-duration: var(--fan-duration, 3s);
    animation-play-state: var(--fan-state, paused);
  }
  @keyframes fanSpin { to { transform: rotate(360deg); } }
  .gpu-fan-hub { position: absolute; inset: 14px; border-radius: 50%; background: radial-gradient(circle at 35% 30%, #5b5c63, #0d0e10); box-shadow: 0 0 0 1px rgba(255,255,255,.08); }
  .gpu-glow-strip {
    position: absolute; left: 8%; right: 8%; bottom: 4px; height: 3px; border-radius: 2px;
    background: var(--chip-tile-color, var(--accent));
    box-shadow: 0 0 10px 2px var(--chip-tile-color, var(--accent-glow));
    opacity: var(--chip-intensity, .35);
    transition: opacity .6s ease, background-color .6s ease;
  }
  .gpu-hero-info { flex: 1 1 240px; min-width: 220px; }
  .gpu-hero-name { font-size: 17px; font-weight: 700; margin-bottom: 18px; letter-spacing: -.01em; }
  .gpu-hero-stats { display: flex; gap: 8px; flex-wrap: wrap; margin: -6px -10px; }
  .gpu-stat {
    padding: 6px 10px; border-radius: 12px; cursor: default;
    transition: background-color .18s ease, transform .18s cubic-bezier(.34,1.56,.64,1);
  }
  .gpu-stat:hover { background: var(--control-bg); transform: translateY(-2px); }
  .gpu-stat-value {
    display: inline-block; font-size: 36px; font-weight: 700; letter-spacing: -.02em; line-height: 1; color: var(--fg);
    font-variant-numeric: tabular-nums; transition: color .4s ease, transform .18s cubic-bezier(.34,1.56,.64,1);
  }
  .gpu-stat:hover .gpu-stat-value { transform: scale(1.08); }
  .gpu-stat-label {
    display: flex; align-items: center; gap: 6px; font-size: 12px; color: var(--sub);
    margin-top: 9px; text-transform: uppercase; letter-spacing: .05em;
  }
  .legend-dot { width: 9px; height: 9px; border-radius: 50%; flex: none; }
  .legend-dot.util { background: var(--accent); }
  .legend-dot.enc { background: var(--accent2); }
  .legend-dot.mem { background: #8e8e93; }
  /* Piccole animazioni al passaggio del mouse sulle metriche dettagliate (temperatura, memoria...):
     la tile si solleva leggermente e l'icona ha un piccolo "rimbalzo", per dare un riscontro
     immediato e piacevole senza essere invasivo. */
  .gpu-metric:hover {
    background: var(--control-hover); transform: translateY(-3px);
    box-shadow: 0 12px 22px -12px rgba(0,0,0,.4);
  }
  .gpu-metric:hover .meter > div { filter: brightness(1.15); }
  .gpu-metric label span { display: inline-block; transition: transform .25s cubic-bezier(.34,1.56,.64,1); }
  .gpu-metric:hover label span { transform: scale(1.25) rotate(-6deg); }
  /* Sezione informativa sotto l'interfaccia: appare quando si scorre fino a lei (Intersection
     Observer), in stile pagine prodotto Apple, invece di comparire tutta insieme al caricamento. */
  .reveal { opacity: 0; transform: translateY(28px); transition: opacity .7s cubic-bezier(.16,1,.3,1), transform .7s cubic-bezier(.16,1,.3,1); }
  .reveal.in-view { opacity: 1; transform: none; }
  .card.reveal { animation: none; }
  .section-heading { margin-bottom: 22px; }
  .section-heading h2 { font-size: 22px; font-weight: 700; margin: 0 0 4px; letter-spacing: -.02em; }
  .codec-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 14px; margin: 16px 0 22px; }
  .codec-card {
    border: 1px solid var(--row-border); border-radius: 14px; padding: 16px 18px;
    background: var(--gpu-card-bg); transition: background-color .4s ease, border-color .4s ease, transform .2s ease, opacity .6s cubic-bezier(.16,1,.3,1);
    opacity: 0; transform: translateY(16px);
  }
  .reveal.in-view .codec-card { opacity: 1; transform: none; }
  .reveal.in-view .codec-card:nth-child(1) { transition-delay: .05s; }
  .reveal.in-view .codec-card:nth-child(2) { transition-delay: .12s; }
  .reveal.in-view .codec-card:nth-child(3) { transition-delay: .19s; }
  .codec-card:hover { transform: translateY(-2px); }
  .codec-badge {
    display: inline-block; font-size: 11px; font-weight: 700; letter-spacing: .02em;
    padding: 3px 9px; border-radius: 980px; background: var(--control-bg); color: var(--sub); margin-bottom: 10px;
  }
  .codec-card h3 { font-size: 14px; font-weight: 600; margin: 0 0 6px; }
  .codec-card p { font-size: 13px; color: var(--sub); margin: 0; line-height: 1.5; }
  .level-explainer-row { display: flex; justify-content: space-between; font-size: 12px; color: var(--sub); margin-bottom: 9px; }
  .level-track { position: relative; height: 6px; border-radius: 3px; background: var(--track); }
  .level-track-fill { position: absolute; inset: 0; border-radius: 3px; background: linear-gradient(90deg, var(--accent), var(--accent2)); }
  .level-dots { display: flex; justify-content: space-between; margin-top: -9px; padding: 0 1px; }
  .level-dot { width: 12px; height: 12px; border-radius: 50%; background: var(--accent); border: 2px solid var(--card-bg); }
  .level-explainer p { font-size: 13px; color: var(--sub); margin: 12px 0 0; line-height: 1.5; }
  /* Notifica non bloccante per errori, al posto di alert(): l'alert nativo del browser
     rompe la coerenza visiva con le card in vetro smerigliato dell'interfaccia. */
  #toast {
    position: fixed; left: 50%; bottom: 24px; z-index: 10;
    max-width: min(420px, calc(100vw - 32px));
    background: var(--error-bg); color: var(--error-fg); border: 1px solid var(--error-border);
    border-radius: 12px; padding: 11px 16px; font-size: 13px;
    box-shadow: 0 12px 30px -8px rgba(0,0,0,.3);
    opacity: 0; pointer-events: none;
    transform: translate(-50%, 12px) scale(.96);
    transition: opacity .25s cubic-bezier(.16,1,.3,1), transform .25s cubic-bezier(.16,1,.3,1);
  }
  #toast.show { opacity: 1; transform: translate(-50%, 0) scale(1); }
  @media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) {
      --bg: #050506; --fg: #f5f5f7; --sub: #98989d;
      --card-bg: rgba(28,29,33,.6); --card-border: rgba(255,255,255,.09);
      --card-shadow: 0 24px 60px -20px rgba(0,0,0,.65);
      --input-bg: rgba(255,255,255,.06); --input-border: rgba(255,255,255,.14);
      --control-bg: rgba(255,255,255,.06); --control-border: rgba(255,255,255,.12); --control-hover: rgba(255,255,255,.12);
      --row-border: rgba(255,255,255,.08); --track: rgba(255,255,255,.1);
      --gpu-card-bg: rgba(0,0,0,.28);
      --select-bg: #26272b; --select-fg: #f5f5f7;
      --error-bg: rgba(192,57,43,.2); --error-border: rgba(192,57,43,.42); --error-fg: #f28b82;
      --aurora-opacity: .5;
    }
  }
  :root[data-theme="dark"] {
    color-scheme: dark;
    --bg: #050506; --fg: #f5f5f7; --sub: #98989d;
    --card-bg: rgba(28,29,33,.6); --card-border: rgba(255,255,255,.09);
    --card-shadow: 0 24px 60px -20px rgba(0,0,0,.65);
    --input-bg: rgba(255,255,255,.06); --input-border: rgba(255,255,255,.14);
    --control-bg: rgba(255,255,255,.06); --control-border: rgba(255,255,255,.12); --control-hover: rgba(255,255,255,.12);
    --row-border: rgba(255,255,255,.08); --track: rgba(255,255,255,.1);
    --gpu-card-bg: rgba(0,0,0,.28);
    --select-bg: #26272b; --select-fg: #f5f5f7;
    --error-bg: rgba(192,57,43,.2); --error-border: rgba(192,57,43,.42); --error-fg: #f28b82;
    --aurora-opacity: .5;
  }
  /* Senza questo, i controlli nativi (checkbox, scrollbar) continuano a seguire il tema
     scuro/chiaro del sistema operativo anche quando qui si sceglie esplicitamente "Chiaro" o
     "Scuro" in app, indipendentemente dai colori della pagina: risultato, ad esempio, una
     checkbox scura su una card chiara. */
  :root[data-theme="light"] { color-scheme: light; }

  [data-accent="green"] { --accent: #16a34a; --accent2: #0d9488; --accent-glow: rgba(22,163,74,.35); --accent-tint: rgba(22,163,74,.1); }
  [data-accent="purple"] { --accent: #7c3aed; --accent2: #db2777; --accent-glow: rgba(124,58,237,.35); --accent-tint: rgba(124,58,237,.1); }
  [data-accent="orange"] { --accent: #ea580c; --accent2: #d97706; --accent-glow: rgba(234,88,12,.35); --accent-tint: rgba(234,88,12,.1); }

  .theme-controls { display: flex; align-items: center; gap: 8px; }
  .theme-controls select, .theme-controls button { font-size: 12px; padding: 6px 12px; }

  /* Rispetta la preferenza di sistema "Riduci movimento": disattiva ogni animazione/transizione
     invece di limitarsi ad abbreviarle, cosi' non c'e' alcun movimento automatico da percepire. */
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      animation-duration: .001ms !important; animation-iteration-count: 1 !important;
      transition-duration: .001ms !important;
    }
    .aurora span { animation: none !important; }
  }
</style>
</head>
<body>
<div class="aurora" aria-hidden="true"><span></span><span></span><span></span></div>
<div class="wrap">
  <div class="topbar">
    <div>
      <h1>Compressore Video</h1>
      <div class="sub">Accesso remoto - carica video, avvia la compressione e scarica il risultato.</div>
    </div>
    <div class="theme-controls">
      <select id="themeSelect" title="Tema" aria-label="Tema">
        <option value="system">Sistema</option>
        <option value="light">Chiaro</option>
        <option value="dark">Scuro</option>
      </select>
      <select id="accentSelect" title="Colore" aria-label="Colore">
        <option value="blue">Blu</option>
        <option value="green">Verde</option>
        <option value="purple">Viola</option>
        <option value="orange">Arancione</option>
      </select>
      <button id="logoutBtn">Esci</button>
    </div>
  </div>

  <div class="card">
    <div class="section-heading">
      <h2>La tua GPU</h2>
      <div class="sub">Monitoraggio in tempo reale, mentre la GPU comprime i tuoi video.</div>
    </div>
    <div id="gpuBody"><div class="gpu-empty">Lettura in corso...</div></div>
  </div>

  <div class="card">
    <div class="row">
      <div>
        <label for="destDir">Cartella destinazione (sul server)</label>
        <input type="text" id="destDir" style="width:320px">
      </div>
      <div>
        <label for="codec">Codec</label>
        <select id="codec"></select>
      </div>
      <div>
        <label for="level">Livello compressione</label>
        <select id="level"></select>
      </div>
      <div>
        <button id="applySettings">Applica impostazioni</button>
        <button id="optimizeBtn" title="Analizza risoluzione, fps e durata dei file in coda e propone codec/livello adatti">Calcola valori ottimali</button>
      </div>
    </div>
    <div class="row">
      <label><input type="checkbox" id="preserveStructure"> Mantieni struttura cartelle</label>
      <label><input type="checkbox" id="skipExisting"> Salta se gia' esistente</label>
      <label><input type="checkbox" id="deleteSource"> Elimina originale dopo compressione (irreversibile)</label>
    </div>
  </div>

  <div class="card">
    <div id="dropzone" tabindex="0" role="button" aria-label="Trascina qui i video oppure premi Invio per selezionarli">Trascina qui i video oppure clicca per selezionarli</div>
    <input type="file" id="fileInput" multiple accept="video/*" style="display:none">
    <div id="uploadProgressWrap" hidden>
      <progress id="uploadProgress" max="100" value="0" aria-label="Avanzamento caricamento"></progress>
      <span id="uploadProgressLabel"></span>
    </div>
  </div>

  <div class="card">
    <progress id="overallProgress" max="1" value="0" aria-label="Avanzamento complessivo della compressione"></progress>
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
          <th scope="col">File</th><th scope="col">Originale</th><th scope="col">Stato</th><th scope="col">Avanzamento</th>
          <th scope="col">Stima</th><th scope="col">Risultato</th><th scope="col"></th>
        </tr>
      </thead>
      <tbody id="itemsBody"></tbody>
    </table>
  </div>

  <div class="card guide-card reveal">
    <div class="section-heading">
      <h2>Guida rapida</h2>
      <div class="sub">Codec ed encoder spiegati in due minuti, per scegliere senza pensieri.</div>
    </div>
    <p class="sub" style="margin:0 0 4px">Ogni video viene codificato dalla GPU NVIDIA (il blocco dedicato NVENC), molto piu' veloce della CPU e che lascia il processore libero per il resto. Il codec scelto determina invece quanto si comprime il file e con chi e' compatibile:</p>
    <div class="codec-grid">
      <div class="codec-card">
        <div class="codec-badge">H.264</div>
        <h3>Compatibilita' universale</h3>
        <p>Il codec piu' diffuso: si apre su qualsiasi telefono, TV, social o editor, anche datato. La scelta piu' sicura se non sai quale usare o devi condividere il file.</p>
      </div>
      <div class="codec-card">
        <div class="codec-badge">H.265 / HEVC</div>
        <h3>File piu' leggeri</h3>
        <p>A parita' di qualita' pesa circa il 40-50% in meno di H.264. Per riprodurlo serve pero' un dispositivo o player relativamente recente.</p>
      </div>
      <div class="codec-card">
        <div class="codec-badge">AV1</div>
        <h3>Il piu' efficiente</h3>
        <p>Comprime ancora meglio di HEVC a parita' di qualita', ma la codifica hardware richiede una GPU NVIDIA RTX serie 40 o successiva.</p>
      </div>
    </div>
    <div class="level-explainer">
      <div class="level-explainer-row"><span>Qualita' massima</span><span>Compressione massima</span></div>
      <div class="level-track"><div class="level-track-fill"></div></div>
      <div class="level-dots" aria-hidden="true">
        <span class="level-dot"></span><span class="level-dot"></span><span class="level-dot"></span><span class="level-dot"></span><span class="level-dot"></span>
      </div>
      <p>Il livello di compressione regola il compromesso tra qualita' e peso del file: valori piu' bassi mantengono piu' dettaglio (file piu' grandi), valori piu' alti riducono il peso a scapito di un po' di qualita'. "Bilanciato" va bene per la maggior parte dei video.</p>
    </div>
  </div>
</div>

<div id="toast" role="alert" aria-live="assertive"></div>

<script>
let settingsLoaded = false;

function escapeHtml(s) {
  return (s || "").replace(/[&<>"']/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
}

let toastTimer = null;
function showToast(message) {
  const el = document.getElementById('toast');
  el.textContent = message;
  el.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove('show'), 4000);
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
        ${it.hasResult ? `<a class="btn" href="/api/download/${it.id}">Scarica</a>` : ''}
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

// Le card GPU vengono create una sola volta per scheda e poi solo aggiornate (mai ricostruite da
// zero): cosi' le transizioni CSS interpolano in modo fluido da un valore al successivo ad ogni
// polling, invece di "scattare" perche' gli elementi vengono ricreati ex novo.
const gpuCards = new Map();

function createMetricEl(icon, label) {
  const el = document.createElement('div');
  el.className = 'gpu-metric';
  el.innerHTML = `<label><span aria-hidden="true">${icon}</span> ${label}</label><div class="value"></div><div class="meter" hidden><div></div></div>`;
  return { el, valueEl: el.querySelector('.value'), meterEl: el.querySelector('.meter'), barEl: el.querySelector('.meter > div') };
}

function updateMetricEl(refs, valueHtml, percent) {
  refs.valueEl.innerHTML = valueHtml;
  if (percent == null) { refs.meterEl.hidden = true; return; }
  refs.meterEl.hidden = false;
  refs.meterEl.className = meterClass(percent);
  refs.barEl.style.width = `${Math.min(100, Math.max(0, percent))}%`;
}

// Sotto gli 65 gradi il bagliore della chip tile resta sul colore dell'accento scelto in app;
// oltre, vira verso l'arancio/rosso per segnalare a colpo d'occhio una temperatura alta - un
// rinforzo puramente visivo: temperatura, utilizzo ecc. restano comunque leggibili come testo.
function chipColorFor(tempC) {
  if (tempC == null) return null;
  if (tempC >= 80) return '#c0392b';
  if (tempC >= 65) return '#e67e22';
  return null;
}

function createGpuHero() {
  const root = document.createElement('div');
  root.className = 'gpu-hero';
  root.innerHTML = `
    <div class="chip-stage">
      <div class="chip-tilt"><div class="chip-float">
        <div class="gpu-model" aria-hidden="true">
          <div class="gpu-face gpu-face-top"></div>
          <div class="gpu-face gpu-face-end">
            <div class="gpu-port gpu-port-wide"></div><div class="gpu-port"></div><div class="gpu-port"></div>
          </div>
          <div class="gpu-face gpu-face-front">
            <div class="gpu-fan">
              <div class="gpu-fan-ring"></div>
              <div class="gpu-fan-blades"></div>
              <div class="gpu-fan-hub"></div>
            </div>
            <div class="gpu-vents"></div>
            <div class="gpu-fan">
              <div class="gpu-fan-ring"></div>
              <div class="gpu-fan-blades"></div>
              <div class="gpu-fan-hub"></div>
            </div>
            <div class="gpu-glow-strip"></div>
          </div>
        </div>
      </div></div>
    </div>
    <div class="gpu-hero-info">
      <div class="gpu-hero-name"></div>
      <div class="gpu-hero-stats">
        <div class="gpu-stat">
          <div class="gpu-stat-value util"></div>
          <div class="gpu-stat-label"><span class="legend-dot util"></span>Utilizzo</div>
        </div>
        <div class="gpu-stat">
          <div class="gpu-stat-value enc"></div>
          <div class="gpu-stat-label"><span class="legend-dot enc"></span>Encoder</div>
        </div>
        <div class="gpu-stat">
          <div class="gpu-stat-value mem"></div>
          <div class="gpu-stat-label"><span class="legend-dot mem"></span>Memoria</div>
        </div>
      </div>
    </div>`;
  return {
    root,
    nameEl: root.querySelector('.gpu-hero-name'),
    modelEl: root.querySelector('.gpu-model'),
    statUtil: root.querySelector('.gpu-stat-value.util'),
    statEnc: root.querySelector('.gpu-stat-value.enc'),
    statMem: root.querySelector('.gpu-stat-value.mem'),
  };
}

function createGpuCard() {
  const root = document.createElement('div');
  root.className = 'gpu-card';
  const hero = createGpuHero();
  root.appendChild(hero.root);
  const metricsWrap = document.createElement('div');
  metricsWrap.className = 'gpu-metrics';
  root.appendChild(metricsWrap);
  const specs = [
    ['temperature', '&#127777;&#65039;', 'Temperatura'],
    ['util', '&#9881;&#65039;', 'Utilizzo GPU'],
    ['encoder', '&#127909;', 'Encoder (NVENC)'],
    ['encoderFps', '&#127916;', 'FPS encoder'],
    ['decoder', '&#128260;', 'Decoder (NVDEC)'],
    ['utilMem', '&#128202;', 'Utilizzo memoria'],
    ['mem', '&#128190;', 'Memoria'],
    ['fan', '&#127744;', 'Ventola'],
    ['power', '&#9889;', 'Potenza'],
  ];
  const metrics = {};
  for (const [key, icon, label] of specs) {
    const m = createMetricEl(icon, label);
    metricsWrap.appendChild(m.el);
    metrics[key] = m;
  }
  return { root, hero, metrics };
}

function updateGpuCard(entry, g) {
  entry.hero.nameEl.textContent = g.name;
  const util = g.utilizationGpuPercent, enc = g.encoderUtilizationPercent;
  const memPercent = (g.memoryUsedMb != null && g.memoryTotalMb) ? (100 * g.memoryUsedMb / g.memoryTotalMb) : null;

  entry.hero.statUtil.textContent = util != null ? util.toFixed(0) + '%' : '-';
  entry.hero.statEnc.textContent = enc != null ? enc.toFixed(0) + '%' : '-';
  entry.hero.statMem.textContent = memPercent != null ? memPercent.toFixed(0) + '%' : '-';
  // Il bagliore sotto il modellino e' vivo: piu' intenso quanto piu' la GPU e' utilizzata, cosi'
  // comunica a colpo d'occhio "sta lavorando" senza dover leggere i numeri.
  entry.hero.modelEl.style.setProperty('--chip-intensity', (0.22 + 0.5 * Math.min(1, Math.max(0, (util ?? 0) / 100))).toFixed(2));
  const hotColor = chipColorFor(g.temperatureC);
  if (hotColor) entry.hero.modelEl.style.setProperty('--chip-tile-color', hotColor);
  else entry.hero.modelEl.style.removeProperty('--chip-tile-color');
  // La ventola gira alla velocita' reale della scheda: si ferma se il sensore riporta 0% (o non
  // la espone), come farebbe una scheda vera a riposo, invece di girare a vuoto senza motivo.
  const fanPercent = g.fanSpeedPercent;
  if (fanPercent > 0) {
    entry.hero.modelEl.style.setProperty('--fan-state', 'running');
    entry.hero.modelEl.style.setProperty('--fan-duration', `${(4 - 3.2 * Math.min(1, fanPercent / 100)).toFixed(2)}s`);
  } else {
    entry.hero.modelEl.style.setProperty('--fan-state', 'paused');
  }

  const memText = (g.memoryUsedMb != null && g.memoryTotalMb != null)
    ? `${Math.round(g.memoryUsedMb)} / ${Math.round(g.memoryTotalMb)} MB` : '-';
  // Molte GPU (anche professionali) non espongono il consumo istantaneo (power.draw) via
  // nvidia-smi ma riportano comunque il limite di potenza (power.limit): scartarlo insieme al
  // consumo mostrava solo "-" anche quando l'unica informazione mancante era il consumo live.
  const powerText = (g.powerDrawW == null && g.powerLimitW == null) ? '-'
    : `${g.powerDrawW != null ? g.powerDrawW.toFixed(0) + ' W' : '-'}${g.powerLimitW != null ? ' / ' + g.powerLimitW.toFixed(0) + ' W' : ''}`;
  // utilization.gpu riflette il motore 3D/compute generale, non il blocco NVENC/NVDEC dedicato
  // che questa app usa per la codifica: utilizzo/fps dell'encoder sono l'indicatore giusto per
  // capire se la GPU sta davvero lavorando su una compressione.
  const encoderFpsText = (g.encoderSessionCount && g.encoderSessionCount > 0)
    ? `${(g.encoderAvgFps ?? 0).toFixed(0)} fps &middot; ${g.encoderSessionCount.toFixed(0)} sessione/i` : '-';

  updateMetricEl(entry.metrics.temperature, g.temperatureC != null ? g.temperatureC.toFixed(0) + ' &deg;C' : '-', g.temperatureC != null ? (g.temperatureC / 90 * 100) : null);
  updateMetricEl(entry.metrics.util, util != null ? util.toFixed(0) + ' %' : '-', util);
  updateMetricEl(entry.metrics.encoder, enc != null ? enc.toFixed(0) + ' %' : '-', enc);
  updateMetricEl(entry.metrics.encoderFps, encoderFpsText, null);
  updateMetricEl(entry.metrics.decoder, g.decoderUtilizationPercent != null ? g.decoderUtilizationPercent.toFixed(0) + ' %' : '-', g.decoderUtilizationPercent);
  updateMetricEl(entry.metrics.utilMem, g.utilizationMemPercent != null ? g.utilizationMemPercent.toFixed(0) + ' %' : '-', g.utilizationMemPercent);
  updateMetricEl(entry.metrics.mem, memText, memPercent);
  updateMetricEl(entry.metrics.fan, g.fanSpeedPercent != null ? g.fanSpeedPercent.toFixed(0) + ' %' : '-', g.fanSpeedPercent);
  updateMetricEl(entry.metrics.power, powerText, null);
}

function renderGpu(gpus) {
  const body = document.getElementById('gpuBody');
  if (!gpus || gpus.length === 0) {
    gpuCards.clear();
    body.innerHTML = '<div class="gpu-empty">GPU NVIDIA non rilevata (nvidia-smi non disponibile).</div>';
    return;
  }
  if (body.querySelector('.gpu-empty')) body.innerHTML = '';

  const seen = new Set();
  gpus.forEach((g, i) => {
    const key = `${g.name}#${i}`;
    seen.add(key);
    let entry = gpuCards.get(key);
    if (!entry) {
      entry = createGpuCard();
      gpuCards.set(key, entry);
      body.appendChild(entry.root);
      // "Fissa" lo stato iniziale (anelli a 0) prima di animare verso il valore reale nella stessa
      // chiamata: senza questo la transizione non avrebbe un valore di partenza da cui animare.
      entry.root.getBoundingClientRect();
    }
    updateGpuCard(entry, g);
  });
  for (const [key, entry] of gpuCards) {
    if (!seen.has(key)) { entry.root.remove(); gpuCards.delete(key); }
  }
}

// Inclinazione 3D del chip al passaggio del mouse (parallasse in stile pagine prodotto Apple).
// Delegato su #gpuBody invece che sui singoli chip perche' le card vengono create dinamicamente.
document.getElementById('gpuBody').addEventListener('mousemove', e => {
  const stage = e.target.closest('.chip-stage');
  if (!stage) return;
  const tilt = stage.querySelector('.chip-tilt');
  const r = stage.getBoundingClientRect();
  const px = (e.clientX - r.left) / r.width - 0.5;
  const py = (e.clientY - r.top) / r.height - 0.5;
  tilt.style.setProperty('--rx', `${(px * 24).toFixed(1)}deg`);
  tilt.style.setProperty('--ry', `${(-py * 24).toFixed(1)}deg`);
});
document.getElementById('gpuBody').addEventListener('mouseleave', () => {
  document.querySelectorAll('#gpuBody .chip-tilt').forEach(t => {
    t.style.removeProperty('--rx');
    t.style.removeProperty('--ry');
  });
});

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
  document.getElementById('optimizeBtn').disabled = state.busy || state.optimizing;
  renderItems(state.items);
}

async function refreshGpu() {
  try { renderGpu(await api('/api/gpu')); } catch { /* la prossima chiamata riprovera' */ }
}

async function removeItem(id) {
  try { await api('/api/items/' + id, { method: 'DELETE' }); } catch (e) { showToast(e.message); }
  refresh();
}

document.getElementById('logoutBtn').addEventListener('click', async () => {
  try { await api('/logout', { method: 'POST' }); } catch {}
  location.href = '/login';
});

document.getElementById('applySettings').addEventListener('click', async () => {
  const deleteSourceCheckbox = document.getElementById('deleteSource');
  if (deleteSourceCheckbox.checked && !confirm("Confermi l'eliminazione dei file originali dopo ogni compressione riuscita?\n\nQuesta azione e' irreversibile: i file di origine verranno cancellati definitivamente, non spostati nel Cestino.")) {
    deleteSourceCheckbox.checked = false;
    return;
  }
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
  } catch (e) { showToast(e.message); }
  refresh();
});

document.getElementById('optimizeBtn').addEventListener('click', async () => {
  try {
    await api('/api/optimize', { method: 'POST' });
  } catch (e) { showToast(e.message); return; }

  // L'analisi (una chiamata ffprobe per file in coda) gira lato server in background: si
  // aspetta che "optimizing" torni false prima di ricaricare i menu codec/livello, che di
  // norma non vengono ripopolati ad ogni refresh per non sovrascrivere una modifica manuale
  // dell'utente ancora in corso - qui pero' il valore e' appena cambiato in risposta a
  // questa stessa azione, quindi va recepito esplicitamente.
  for (let i = 0; i < 40; i++) {
    await new Promise(r => setTimeout(r, 300));
    const state = await api('/api/state').catch(() => null);
    if (state && !state.optimizing) {
      fillSelect(document.getElementById('codec'), state.codecs, 'value', 'label', state.codecValue);
      fillSelect(document.getElementById('level'), state.levels, 'cq', 'label', state.levelCq);
      break;
    }
  }
  refresh();
});

document.getElementById('estimateBtn').addEventListener('click', async () => {
  try { await api('/api/estimate', { method: 'POST' }); } catch (e) { showToast(e.message); }
  refresh();
});
document.getElementById('startBtn').addEventListener('click', async () => {
  try { await api('/api/start', { method: 'POST' }); } catch (e) { showToast(e.message); }
  refresh();
});
document.getElementById('cancelBtn').addEventListener('click', async () => {
  try { await api('/api/cancel', { method: 'POST' }); } catch (e) { showToast(e.message); }
  refresh();
});

function formatBytes(bytes) {
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  const units = ['MB', 'GB', 'TB'];
  let size = bytes / (1024 * 1024);
  let i = 0;
  while (size >= 1024 && i < units.length - 1) { size /= 1024; i++; }
  return `${size.toFixed(size >= 10 || i === 0 ? 0 : 2)} ${units[i]}`;
}

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

  let lastLoaded = 0;
  let lastTime = performance.now();
  let speedBytesPerSec = 0;

  return new Promise(resolve => {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/upload');
    xhr.upload.addEventListener('progress', e => {
      if (!e.lengthComputable) return;
      const now = performance.now();
      const deltaTime = (now - lastTime) / 1000;
      if (deltaTime > 0.2) {
        const instantSpeed = (e.loaded - lastLoaded) / deltaTime;
        // media mobile esponenziale: smorza le oscillazioni dei singoli campioni di rete
        speedBytesPerSec = speedBytesPerSec === 0 ? instantSpeed : speedBytesPerSec * 0.7 + instantSpeed * 0.3;
        lastLoaded = e.loaded;
        lastTime = now;
      }

      const pct = Math.round(100 * e.loaded / e.total);
      bar.value = pct;
      const speedText = speedBytesPerSec > 0 ? ` a ${formatBytes(speedBytesPerSec)}/s` : '';
      label.textContent = `${formatBytes(e.loaded)} / ${formatBytes(e.total)} (${pct}%)${speedText}`;
    });
    xhr.addEventListener('load', () => {
      wrap.hidden = true;
      if (xhr.status === 401) { location.href = '/login'; resolve(); return; }
      if (xhr.status < 200 || xhr.status >= 300) {
        let msg = xhr.statusText;
        try { const j = JSON.parse(xhr.responseText); if (j && j.error) msg = j.error; } catch {}
        showToast(msg);
      }
      refresh();
      resolve();
    });
    xhr.addEventListener('error', () => {
      wrap.hidden = true;
      showToast('Errore di rete durante il caricamento.');
      resolve();
    });
    xhr.send(form);
  });
}

const dropzone = document.getElementById('dropzone');
const fileInput = document.getElementById('fileInput');
dropzone.addEventListener('click', () => fileInput.click());
dropzone.addEventListener('keydown', e => {
  if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); fileInput.click(); }
});
fileInput.addEventListener('change', () => uploadFiles(fileInput.files));
dropzone.addEventListener('dragover', e => { e.preventDefault(); dropzone.classList.add('drag'); });
dropzone.addEventListener('dragleave', () => dropzone.classList.remove('drag'));
dropzone.addEventListener('drop', e => {
  e.preventDefault();
  dropzone.classList.remove('drag');
  uploadFiles(e.dataTransfer.files);
});

function applyTheme(theme) {
  if (theme === 'system') document.documentElement.removeAttribute('data-theme');
  else document.documentElement.setAttribute('data-theme', theme);
}
function applyAccent(accent) {
  document.documentElement.setAttribute('data-accent', accent);
}

const themeSelect = document.getElementById('themeSelect');
const accentSelect = document.getElementById('accentSelect');
themeSelect.value = localStorage.getItem('vc_theme') || 'system';
accentSelect.value = localStorage.getItem('vc_accent') || 'blue';
themeSelect.addEventListener('change', () => {
  localStorage.setItem('vc_theme', themeSelect.value);
  applyTheme(themeSelect.value);
});
accentSelect.addEventListener('change', () => {
  localStorage.setItem('vc_accent', accentSelect.value);
  applyAccent(accentSelect.value);
});

// Rivela le sezioni sotto la piega (come la guida rapida) scorrendo fino a loro, invece di farle
// comparire tutte insieme al caricamento della pagina - un pattern tipico delle pagine prodotto
// Apple. Il motion e' comunque azzerato globalmente se il sistema ha "Riduci movimento" attivo.
const revealEls = document.querySelectorAll('.reveal');
if ('IntersectionObserver' in window) {
  const revealObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      entry.target.classList.add('in-view');
      revealObserver.unobserve(entry.target);
    });
  }, { threshold: 0.12 });
  revealEls.forEach(el => revealObserver.observe(el));
} else {
  revealEls.forEach(el => el.classList.add('in-view'));
}

refresh();
refreshGpu();
setInterval(refresh, 1500);
setInterval(refreshGpu, 2000);
</script>
</body>
</html>
""";
}
