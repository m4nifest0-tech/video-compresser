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
  .logo { text-align: center; font-size: 34px; margin-bottom: 8px;
    animation: popIn .5s cubic-bezier(.34,1.56,.64,1) .15s both; }
  @keyframes popIn { from { opacity: 0; transform: scale(.5); } to { opacity: 1; transform: scale(1); } }
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
  <form class="card" method="post" action="/login">
    <div class="logo" aria-hidden="true">&#127909;</div>
    <h1>Compressore Video</h1>
    <div class="error" id="errorBox" role="alert" aria-live="assertive"></div>
    <label for="username">Utente</label>
    <input type="text" id="username" name="username" autocomplete="username" autofocus required>
    <label for="password">Password</label>
    <input type="password" id="password" name="password" autocomplete="current-password" required>
    <button type="submit">Accedi</button>
  </form>
<script>
  const params = new URLSearchParams(location.search);
  const err = params.get('error');
  if (err) {
    const box = document.getElementById('errorBox');
    box.textContent = err === 'locked'
      ? 'Troppi tentativi falliti. Riprova tra qualche minuto.'
      : 'Utente o password non validi.';
    box.classList.add('show');
  }
</script>
</body>
</html>
""";
}
