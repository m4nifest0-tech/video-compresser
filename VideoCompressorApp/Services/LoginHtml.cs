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
    --bg: #eef0f5; --fg: #1a1a1a; --sub: #666;
    --card-bg: rgba(255,255,255,.6); --card-border: rgba(255,255,255,.6);
    --card-shadow: 0 8px 32px rgba(31,38,135,.15);
    --input-bg: rgba(255,255,255,.7); --input-border: rgba(0,0,0,.14);
    --accent: #2563eb; --accent2: #7c3aed;
    --error-bg: rgba(192,57,43,.12); --error-border: rgba(192,57,43,.35); --error-fg: #c0392b;
  }
  * { box-sizing: border-box; }
  html, body { height: 100%; }
  body {
    margin: 0; display: flex; align-items: center; justify-content: center;
    font-family: -apple-system, "Segoe UI", Arial, sans-serif; color: var(--fg);
    background: var(--bg);
    background-image:
      radial-gradient(700px circle at 12% 15%, rgba(124,58,237,.30), transparent 60%),
      radial-gradient(650px circle at 88% 20%, rgba(37,99,235,.26), transparent 60%),
      radial-gradient(550px circle at 85% 90%, rgba(219,39,119,.18), transparent 60%),
      radial-gradient(500px circle at 10% 88%, rgba(245,158,11,.15), transparent 60%);
    background-attachment: fixed;
  }
  /* Texture di grana sottile sopra lo sfondo sfumato, visibile attraverso il vetro smerigliato
     della card: da' un effetto piu' "materiale" invece di un piatto colore. */
  body::before {
    content: ''; position: fixed; inset: 0; z-index: 0; pointer-events: none;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='180' height='180'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E");
    opacity: .05;
    mix-blend-mode: overlay;
  }
  .card {
    position: relative; z-index: 1;
    width: 320px;
    background: var(--card-bg);
    -webkit-backdrop-filter: blur(20px) saturate(160%);
    backdrop-filter: blur(20px) saturate(160%);
    border: 1px solid var(--card-border);
    border-radius: 18px; padding: 30px 28px;
    box-shadow: var(--card-shadow);
  }
  .logo { text-align: center; font-size: 32px; margin-bottom: 6px; }
  h1 {
    font-size: 18px; text-align: center; margin: 0 0 24px; letter-spacing: -.02em;
    background: linear-gradient(135deg, var(--accent), var(--accent2));
    -webkit-background-clip: text; background-clip: text; color: transparent;
  }
  label { font-size: 12px; color: var(--sub); display: block; margin: 0 0 4px; }
  input[type=text], input[type=password] {
    width: 100%; padding: 10px 12px; margin-bottom: 16px;
    border: 1px solid var(--input-border); border-radius: 8px; font-size: 14px;
    background: var(--input-bg); color: var(--fg);
  }
  button {
    width: 100%; padding: 11px; border: none; border-radius: 8px; font-size: 14px;
    background: linear-gradient(135deg, var(--accent), var(--accent2)); color: #fff; cursor: pointer;
    transition: transform .08s ease;
  }
  button:hover { transform: translateY(-1px); }
  .error {
    background: var(--error-bg); color: var(--error-fg); border: 1px solid var(--error-border);
    border-radius: 8px; padding: 9px 11px; font-size: 12px; margin-bottom: 16px; display: none;
  }
  .error.show { display: block; }
  @media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) {
      --bg: #131417; --fg: #eee; --sub: #999;
      --card-bg: rgba(35,36,42,.6); --card-border: rgba(255,255,255,.08);
      --card-shadow: 0 8px 32px rgba(0,0,0,.4);
      --input-bg: rgba(255,255,255,.06); --input-border: rgba(255,255,255,.16);
      --error-bg: rgba(192,57,43,.18); --error-border: rgba(192,57,43,.4); --error-fg: #f28b82;
    }
    html:not([data-theme="light"]) body::before { opacity: .07; }
  }
  html[data-theme="dark"] body::before { opacity: .07; }
  :root[data-theme="dark"] {
    --bg: #131417; --fg: #eee; --sub: #999;
    --card-bg: rgba(35,36,42,.6); --card-border: rgba(255,255,255,.08);
    --card-shadow: 0 8px 32px rgba(0,0,0,.4);
    --input-bg: rgba(255,255,255,.06); --input-border: rgba(255,255,255,.16);
    --error-bg: rgba(192,57,43,.18); --error-border: rgba(192,57,43,.4); --error-fg: #f28b82;
  }

  [data-accent="green"] { --accent: #16a34a; --accent2: #0d9488; }
  [data-accent="purple"] { --accent: #7c3aed; --accent2: #db2777; }
  [data-accent="orange"] { --accent: #ea580c; --accent2: #d97706; }
</style>
</head>
<body>
  <form class="card" method="post" action="/login">
    <div class="logo">&#127909;</div>
    <h1>Compressore Video</h1>
    <div class="error" id="errorBox"></div>
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
