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
<style>
  :root { color-scheme: light dark; }
  html, body { height: 100%; }
  body {
    margin: 0; display: flex; align-items: center; justify-content: center;
    font-family: Segoe UI, Arial, sans-serif; background: #f3f3f5; color: #1a1a1a;
  }
  .card {
    width: 320px; background: #fff; border-radius: 10px; padding: 28px 26px;
    box-shadow: 0 4px 18px rgba(0,0,0,.12);
  }
  .logo { text-align: center; font-size: 30px; margin-bottom: 6px; }
  h1 { font-size: 17px; text-align: center; margin: 0 0 22px; }
  label { font-size: 12px; color: #666; display: block; margin: 0 0 4px; }
  input[type=text], input[type=password] {
    width: 100%; box-sizing: border-box; padding: 9px 10px; margin-bottom: 14px;
    border: 1px solid #ccc; border-radius: 6px; font-size: 14px;
  }
  button {
    width: 100%; padding: 10px; border: none; border-radius: 6px; font-size: 14px;
    background: #2563eb; color: #fff; cursor: pointer;
  }
  button:hover { background: #1d4ed8; }
  .error {
    background: #fdecea; color: #c0392b; border: 1px solid #f5c2bc; border-radius: 6px;
    padding: 8px 10px; font-size: 12px; margin-bottom: 14px; display: none;
  }
  .error.show { display: block; }
  @media (prefers-color-scheme: dark) {
    body { background: #1c1c1e; color: #eee; }
    .card { background: #2a2a2d; box-shadow: none; }
    input[type=text], input[type=password] { background: #1c1c1e; color: #eee; border-color: #555; }
    .error { background: #3a2222; border-color: #6b3232; color: #f28b82; }
  }
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
