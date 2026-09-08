namespace VideoCompressor.Services;

public static class LoginHtml
{
    public const string Content = """
<!doctype html>
<html lang="it">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Compressore Video - Accesso</title>
<script>
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
    --bg: #f5f5f7; --fg: #1d1d1f; --sub: #6e6e73;
    --card-bg: rgba(255,255,255,.72); --card-border: rgba(0,0,0,.06);
    --card-shadow: 0 20px 50px -20px rgba(0,0,0,.18), 0 2px 6px rgba(0,0,0,.04);
    --input-bg: rgba(0,0,0,.04); --input-border: rgba(0,0,0,.12);
    --accent: #2563eb; --accent2: #7c3aed;
    --accent-glow: rgba(37,99,235,.35);
    --error-bg: rgba(192,57,43,.12); --error-border: rgba(192,57,43,.35); --error-fg: #c0392b;
    --aurora-opacity: .16;
  }
  * { box-sizing: border-box; }
  html, body { height: 100%; }
  body {
    margin: 0; display: flex; align-items: center; justify-content: center;
    font-family: -apple-system, "Segoe UI", Arial, sans-serif; color: var(--fg);
    background: var(--bg);
    position: relative; overflow: hidden;
    transition: background-color .4s ease, color .4s ease;
  }
  /* Sfondo "aurora": macchie di colore sfocate e in lento movimento, nello spirito delle
     pagine Apple (WWDC, visionOS) - al posto della vecchia foto statica. */
  .aurora { position: fixed; inset: 0; z-index: 0; overflow: hidden; pointer-events: none;
    opacity: var(--aurora-opacity); transition: opacity .4s ease; }
  .aurora span { position: absolute; border-radius: 50%; filter: blur(80px); will-change: transform; }
  .aurora span:nth-child(1) { width: 60vmax; height: 60vmax; top: -25vmax; left: -20vmax;
    background: var(--accent); animation: auroraDrift1 26s ease-in-out infinite; }
  .aurora span:nth-child(2) { width: 50vmax; height: 50vmax; bottom: -22vmax; right: -15vmax;
    background: var(--accent2); animation: auroraDrift2 32s ease-in-out infinite; }
  .aurora span:nth-child(3) { width: 38vmax; height: 38vmax; top: 20%; left: 55%;
    background: var(--accent); opacity: .6; animation: auroraDrift3 22s ease-in-out infinite; }
  @keyframes auroraDrift1 { 0%, 100% { transform: translate(0,0) scale(1); } 50% { transform: translate(8vmax,6vmax) scale(1.15); } }
  @keyframes auroraDrift2 { 0%, 100% { transform: translate(0,0) scale(1); } 50% { transform: translate(-6vmax,-8vmax) scale(1.1); } }
  @keyframes auroraDrift3 { 0%, 100% { transform: translate(0,0) scale(1); } 50% { transform: translate(-9vmax,5vmax) scale(.92); } }
  .card {
    position: relative; z-index: 1;
    width: 320px;
    background: var(--card-bg);
    -webkit-backdrop-filter: blur(24px) saturate(180%);
    backdrop-filter: blur(24px) saturate(180%);
    border: 1px solid var(--card-border);
    border-radius: 20px; padding: 32px 28px;
    box-shadow: var(--card-shadow);
    animation: riseIn .6s cubic-bezier(.16,1,.3,1) both;
    transition: background-color .4s ease, border-color .4s ease, box-shadow .4s ease;
  }
  @keyframes riseIn { from { opacity: 0; transform: translateY(18px) scale(.97); } to { opacity: 1; transform: translateY(0) scale(1); } }
  /* Se backdrop-filter non e' supportato (o disattivato per risparmio energetico), la card
     resterebbe quasi trasparente sull'aurora sottostante invece che su un vetro sfocato: senza il
     blur a coprirla, serve un'opacita' molto piu' alta per garantire comunque il contrasto del testo. */
  @supports not (backdrop-filter: blur(1px)) {
    .card { background: rgba(255,255,255,.94); }
    html:not([data-theme="light"]) .card { background: rgba(22,23,28,.94); }
    html[data-theme="dark"] .card { background: rgba(22,23,28,.94); }
  }
  /* Icona "Face ID": un anello SVG che respira in idle, gira come indicatore di avanzamento
     durante la verifica delle credenziali e si completa in un segno di spunta verde in caso di
     successo (o si tinge di rosso con uno scatto orizzontale in caso di errore), invece di limitarsi
     a un redirect istantaneo dopo l'invio del form. */
  .faceid { position: relative; width: 60px; height: 60px; margin: 0 auto 16px;
    animation: popIn .5s cubic-bezier(.34,1.56,.64,1) .15s both; }
  @keyframes popIn { from { opacity: 0; transform: scale(.5); } to { opacity: 1; transform: scale(1); } }
  .faceid-ring { position: absolute; inset: 0; transform: rotate(-90deg);
    animation: faceidBreathe 2.6s ease-in-out .65s infinite; }
  .faceid-ring-bg { fill: none; stroke: var(--input-border); stroke-width: 3; }
  .faceid-ring-scan {
    fill: none; stroke: var(--accent); stroke-width: 3; stroke-linecap: round;
    stroke-dasharray: 170; stroke-dashoffset: 170; opacity: 0;
    transition: stroke-dashoffset .4s ease, stroke .3s ease, opacity .2s ease;
  }
  @keyframes faceidBreathe {
    0%, 100% { opacity: .5; transform: rotate(-90deg) scale(1); }
    50% { opacity: .9; transform: rotate(-90deg) scale(1.05); }
  }
  .faceid-check {
    position: absolute; inset: 0; margin: auto; width: 24px; height: 24px;
    fill: none; stroke: var(--accent); stroke-width: 3; stroke-linecap: round; stroke-linejoin: round;
    stroke-dasharray: 20; stroke-dashoffset: 20; opacity: 0;
  }
  .faceid.scanning .faceid-ring { animation: faceidSpin 1s linear infinite; }
  .faceid.scanning .faceid-ring-scan { opacity: 1; stroke-dashoffset: 120; }
  @keyframes faceidSpin { to { transform: rotate(270deg); } }
  .faceid.success .faceid-ring { animation: none; }
  .faceid.success .faceid-ring-scan { opacity: 1; stroke-dashoffset: 0; stroke: #34c759; }
  .faceid.success .faceid-check {
    opacity: 1; stroke-dashoffset: 0; stroke: #34c759;
    transition: stroke-dashoffset .35s ease .25s, opacity .15s ease .25s;
  }
  .faceid.faceid-error .faceid-ring { animation: none; }
  .faceid.faceid-error .faceid-ring-scan { opacity: 1; stroke-dashoffset: 0; stroke: var(--error-fg); }
  .faceid.faceid-error { animation: faceidShake .4s ease; }
  @keyframes faceidShake {
    20%, 60% { transform: translateX(-6px); }
    40%, 80% { transform: translateX(6px); }
  }
  /* Uscita della card verso la dashboard dopo un accesso riuscito, invece di un redirect a scatto. */
  .card.leave { animation: cardLeave .38s cubic-bezier(.4,0,1,1) both; }
  @keyframes cardLeave { to { opacity: 0; transform: scale(.96) translateY(-6px); } }
  h1 {
    font-size: 20px; font-weight: 700; text-align: center; margin: 0 0 26px; letter-spacing: -.02em;
    color: var(--accent); transition: color .3s ease;
  }
  label { font-size: 13px; color: var(--sub); display: block; margin: 0 0 4px; }
  input[type=text], input[type=password] {
    width: 100%; padding: 11px 13px; margin-bottom: 16px;
    border: 1px solid var(--input-border); border-radius: 10px; font-size: 14px;
    background: var(--input-bg); color: var(--fg);
    transition: border-color .15s ease, box-shadow .15s ease, background-color .3s ease;
  }
  input[type=text]:hover, input[type=password]:hover { border-color: var(--accent); }
  button {
    width: 100%; padding: 12px; border: none; border-radius: 980px; font-size: 14px; font-weight: 600;
    background: var(--accent); color: #fff; cursor: pointer;
    box-shadow: 0 8px 20px -6px var(--accent-glow);
    transition: transform .15s cubic-bezier(.34,1.56,.64,1), filter .15s ease, box-shadow .15s ease, background-color .3s ease;
  }
  button:hover { filter: brightness(1.08); transform: translateY(-1px); box-shadow: 0 10px 24px -6px var(--accent-glow); }
  button:active { transform: scale(.97); }
  /* Indicatore di focus da tastiera come "alone" morbido, in stile macOS, al posto del semplice
     outline: resta ben leggibile sopra il vetro smerigliato senza tagli dovuti al blur. */
  input:focus-visible, button:focus-visible {
    outline: none; box-shadow: 0 0 0 4px var(--accent-glow);
  }
  .error {
    background: var(--error-bg); color: var(--error-fg); border: 1px solid var(--error-border);
    border-radius: 10px; padding: 10px 12px; font-size: 13px; margin-bottom: 16px; display: none;
  }
  .error.show { display: block; animation: riseIn .3s cubic-bezier(.16,1,.3,1) both; }
  @media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) {
      --bg: #050506; --fg: #f5f5f7; --sub: #98989d;
      --card-bg: rgba(28,29,33,.62); --card-border: rgba(255,255,255,.09);
      --card-shadow: 0 24px 60px -20px rgba(0,0,0,.65);
      --input-bg: rgba(255,255,255,.06); --input-border: rgba(255,255,255,.14);
      --error-bg: rgba(192,57,43,.2); --error-border: rgba(192,57,43,.42); --error-fg: #f28b82;
      --aurora-opacity: .55;
    }
  }
  :root[data-theme="dark"] {
    color-scheme: dark;
    --bg: #050506; --fg: #f5f5f7; --sub: #98989d;
    --card-bg: rgba(28,29,33,.62); --card-border: rgba(255,255,255,.09);
    --card-shadow: 0 24px 60px -20px rgba(0,0,0,.65);
    --input-bg: rgba(255,255,255,.06); --input-border: rgba(255,255,255,.14);
    --error-bg: rgba(192,57,43,.2); --error-border: rgba(192,57,43,.42); --error-fg: #f28b82;
    --aurora-opacity: .55;
  }
  /* Senza questo, i controlli nativi continuano a seguire il tema scuro/chiaro del sistema
     operativo anche quando qui si sceglie esplicitamente "Chiaro" o "Scuro" in app. */
  :root[data-theme="light"] { color-scheme: light; }

  [data-accent="green"] { --accent: #16a34a; --accent2: #0d9488; --accent-glow: rgba(22,163,74,.35); }
  [data-accent="purple"] { --accent: #7c3aed; --accent2: #db2777; --accent-glow: rgba(124,58,237,.35); }
  [data-accent="orange"] { --accent: #ea580c; --accent2: #d97706; --accent-glow: rgba(234,88,12,.35); }

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
  <form class="card" id="loginForm" method="post" action="/login">
    <div class="faceid" id="faceid" aria-hidden="true">
      <svg class="faceid-ring" viewBox="0 0 60 60">
        <circle class="faceid-ring-bg" cx="30" cy="30" r="27"></circle>
        <circle class="faceid-ring-scan" cx="30" cy="30" r="27"></circle>
      </svg>
      <svg class="faceid-check" viewBox="0 0 24 24">
        <path d="M5 13l4 4L19 7"></path>
      </svg>
    </div>
    <h1>Compressore Video</h1>
    <div class="error" id="errorBox" role="alert" aria-live="assertive"></div>
    <label for="username">Utente</label>
    <input type="text" id="username" name="username" autocomplete="username" autofocus required>
    <label for="password">Password</label>
    <input type="password" id="password" name="password" autocomplete="current-password" required>
    <button type="submit">Accedi</button>
  </form>
<script>
  const form = document.getElementById('loginForm');
  const faceid = document.getElementById('faceid');
  const errorBox = document.getElementById('errorBox');
  const submitBtn = form.querySelector('button');

  function showError(text) {
    errorBox.textContent = text;
    errorBox.classList.add('show');
  }
  function errorMessage(code) {
    return code === 'locked'
      ? 'Troppi tentativi falliti. Riprova tra qualche minuto.'
      : 'Utente o password non validi.';
  }

  const initialErr = new URLSearchParams(location.search).get('error');
  if (initialErr) showError(errorMessage(initialErr));

  // Invio via fetch invece del normale postback: permette di mostrare l'esito (successo/errore)
  // con l'animazione dell'anello prima di lasciare la pagina, cosa impossibile con un form
  // tradizionale che ricarica subito la pagina di destinazione.
  let busy = false;
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (busy) return;
    busy = true;
    errorBox.classList.remove('show');
    faceid.classList.remove('success', 'faceid-error');
    faceid.classList.add('scanning');
    submitBtn.disabled = true;
    try {
      const res = await fetch('/login', { method: 'POST', body: new FormData(form) });
      const resUrl = new URL(res.url);
      const success = resUrl.pathname !== '/login';
      faceid.classList.remove('scanning');
      if (success) {
        faceid.classList.add('success');
        setTimeout(() => {
          form.classList.add('leave');
          setTimeout(() => { location.href = '/'; }, 380);
        }, 550);
      } else {
        faceid.classList.add('faceid-error');
        showError(errorMessage(resUrl.searchParams.get('error')));
        submitBtn.disabled = false;
        busy = false;
        setTimeout(() => faceid.classList.remove('faceid-error'), 500);
      }
    } catch {
      faceid.classList.remove('scanning');
      showError('Errore di rete. Riprova.');
      submitBtn.disabled = false;
      busy = false;
    }
  });
</script>
</body>
</html>
""";
}
