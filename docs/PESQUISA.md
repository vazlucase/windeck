# Pesquisa e decisões

## Fontes primárias

- [The Taskbar — Microsoft](https://learn.microsoft.com/windows/win32/shell/taskbar): tarefas, bandeja e protocolos de integração.
- [Application Desktop Toolbars — Microsoft](https://learn.microsoft.com/windows/win32/shell/application-desktop-toolbars): registro de AppBars e área reservada.
- [SHAppBarMessage — Microsoft](https://learn.microsoft.com/windows/win32/api/shellapi/nf-shellapi-shappbarmessage): mensagens de posicionamento e estado.
- [Notification Area — Microsoft](https://learn.microsoft.com/windows/win32/shell/notification-area): contrato Shell_NotifyIcon e participação dos aplicativos.
- [GSMTC Session Manager — Microsoft](https://learn.microsoft.com/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager): controle e consulta das sessões publicadas ao Windows.
- [IApplicationDocumentLists — Microsoft](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nn-shobjidl_core-iapplicationdocumentlists): recentes/frequentes; não expõe categorias personalizadas ou tarefas da Jump List.
- [DWM Thumbnail Overview — Microsoft](https://learn.microsoft.com/windows/win32/dwm/thumbnail-ovw): miniaturas compositoras de janelas.
- [Windows Notifications — Microsoft](https://learn.microsoft.com/windows/apps/develop/notifications/): APIs modernas, identidade e separação entre notificações do app e do sistema.
- [ManagedShell](https://github.com/cairoshell/ManagedShell): biblioteca de substituição de shell usada por Cairo e RetroBar; tarefas, bandeja e AppBars.
- [ManagedShell 0.0.374 no NuGet](https://www.nuget.org/packages/ManagedShell/0.0.374): versão fixada no projeto.
- [Open-Meteo](https://open-meteo.com/): previsão e geocodificação. [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

## Decisões

1. **Aplicativo Windows, interface WPF.** A prévia HTML é um instrumento de
   design; o executável não depende de navegador/WebView.
2. **Explorer permanece executando.** Sua barra é ocultada, preservando área de
   trabalho e serviços oficiais. Não há troca permanente do shell de login.
3. **Biblioteca de shell para a bandeja.** Ícones são parte de um protocolo com
   estado e callbacks. Uma fileira de imagens não equivale a uma bandeja funcional.
4. **Separar desenho próprio e acesso a painéis nativos.** Um botão que abre a
   central do Windows preserva acesso à função, mas não constitui uma central
   redesenhada. Essa distinção aparece na matriz de paridade.
5. **Recuperação fora do processo principal.** Travamento da UI não deve deixar
   o usuário sem a barra original ou sem caminho de saída.
6. **Sem prometer paridade por compilação.** APIs do shell, DPI, jogos, apps
   elevados, acessibilidade e notificações exigem execução no Windows.
7. **Gestos testáveis sem GUI.** O algoritmo é puro e usa unidades em DIPs e
   tempo monotônico; casos de falso positivo entram nos testes.
8. **Sem controles falsos no executável.** Dados ausentes aparecem como
   indisponíveis; amostras ficam explicitamente na prévia de design.
