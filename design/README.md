# Prévia de design

Abra `preview.html` para explorar os estados e painéis. As informações e ações
são ilustrativas. O aplicativo nativo está em `src/Windeck.App`.

Para verificar a prévia automaticamente:

```sh
npm ci
npx playwright install chromium
python -m http.server 8765 --bind 127.0.0.1
```

Em outro terminal na pasta `design`:

```sh
npm test
```

Pode-se definir `WINDECK_BROWSER` com o caminho de Chromium/Chrome instalado e
`WINDECK_PREVIEW_URL` com a URL do servidor de desenvolvimento. O relatório
`verification.json` se refere somente à prévia, não à execução Windows.
