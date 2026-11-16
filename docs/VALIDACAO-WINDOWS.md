# Plano de validação Windows

## Evidência já obtida

- Build Release Windows x64 e publicação autocontida executados em Linux.
- Núcleo: 17 verificações executadas.
- Prévia visual: oito telas, dois temas, navegação, hover, clique, Escape,
  play/pause ilustrativo, quatro larguras e ausência de erros JavaScript.

**Não executado:** GUI WPF, shell, COM de áudio, WinRT de mídia, callback de
bandeja, fullscreen e recuperação dentro de uma instalação Windows.

## Preparação

O workflow `.github/workflows/windows.yml` chama
`Windeck.exe --verify-layouts <diretório>` em uma sessão Windows. Esse comando de
desenvolvimento constrói e renderiza as páginas reais nos dois temas, sem ativar
a substituição. O relatório JSON e as 16 imagens são artefatos da execução CI.
Ele não executa as ações dos botões nem substitui o roteiro manual abaixo.

Usar uma instalação de teste com restauração disponível e executar primeiro
`Previa.cmd`. Registrar edição/build do Windows, GPU, número de monitores,
resolução e escala de cada monitor. Não executar Windeck e outro substituto de
barra simultaneamente durante a homologação.

## Cenários obrigatórios

| Grupo | Ação | Resultado esperado |
| --- | --- | --- |
| Inicialização | Abrir Windeck.exe | Cápsulas aparecem, UI responde e barra original só é ocultada com guardião pronto. |
| Prévia | Abrir com --preview | Barra original visível; acesso às páginas do Windeck. |
| Recuperação | Encerrar Windeck na interface | Barra original, estado de auto-hide e área de trabalho restaurados. |
| Crash | Encerrar somente o processo principal no Gerenciador de Tarefas | Guardião restaura o Explorer. |
| UI travada | Pausar o thread da UI em depurador por mais de 15 s | Explorer volta; guardião pode encerrar apenas o próprio pai Windeck. |
| Guardião indisponível | Impedir lançamento do processo auxiliar | A barra original permanece visível. |
| Múltiplas sessões | Ativar/desativar substituição repetidamente | Guardas antigos não interferem com uma nova lease. |
| Escape de emergência | Ctrl+Alt+Backspace e --restore | Barra original reaparece e substituição é desativada. |
| DPI | Escalas 100%, 125%, 150%, 200% e monitores mistos | Cápsula centralizada, textos legíveis, ícones e menus na posição correta. |
| Hotplug | Conectar/desconectar monitor e trocar principal | Sem saída da aplicação, monitores e AppBars reconciliados. |
| Work area | Maximizar janelas e mudar reserva do topo | Área reservada consistente; sem loop de reposicionamento. |
| Fullscreen | Jogo, vídeo, janela borderless e retorno | Ocultação apenas no monitor afetado; reabertura explícita funciona. |
| Entrada | Hover curto, hover longo, afastamento e movimento lateral rápido | Sem abertura involuntária. |
| Gesto | Movimento dirigido para cima e sensibilidades extremas | Expansão previsível; sem depender do FPS. |
| Janelas | Win32, Store/UWP, elevadas, minimizadas e vários grupos | Ativar/restaurar/minimizar/fechar coerentes; ações bloqueadas pelo SO tratadas. |
| Grupos | Várias janelas do mesmo app | Grupo abre escolha de janela e não executa ação dupla. |
| Fixados | Importar atalhos, adicionar, remover e reordenar | Persistência após reinício, sem perda de alvos ou argumentos. |
| Arquivos | Arrastar arquivo para atalho e janela aberta | Arquivo entregue quando o aplicativo suporta; nenhum comando concatenado por shell. |
| Prévia | Hover prolongado e passagem para a miniatura | Thumbnail DWM visível, clicável e descartado ao sair. |
| Progresso | Aplicativos que usam ITaskbarList3 e badges | Progresso/overlay correspondem à aplicação. |
| Recentes | App com AppUserModelID e documentos recentes | Itens reais; aplicativo sem lista permanece utilizável. |
| Bandeja | Win32, Steam, nuvem, VPN, driver e apps de terceiros | Ícones adicionados, alterados e removidos; menus e duplo clique funcionam. |
| Bandeja fixada | Shift+direito para fixar/desafixar | Sem abrir simultaneamente o menu do aplicativo; escolha persistida. |
| Notificações | Balão clássico, lembrete e central moderna | Histórico/banners próprios coerentes e acesso à central oficial. |
| Foco | Não perturbe, apresentação e fullscreen | Banners próprios respeitam estado do sistema. |
| Áudio | Fones, HDMI, Bluetooth, saída removida e mudo | Volume muda o endpoint correto; serviço reconhece troca. |
| Mídia | Spotify, navegador e outro player; metadados ausentes | Sessão real, ações suportadas e ausência de capa tratados. |
| Rede/energia | Offline, retomada, sem bateria e leitura desconhecida | Nenhum valor fictício ou estado “sem bateria” falso. |
| Clima | Belém/BR, cidade ambígua, ausência de internet e retorno | Local resolvido correto; dados antigos identificados e atualização retomada. |
| Busca | Nomes com acentos, aspas e caracteres especiais | Sem SQL inválido; resultado cancelado não substitui consulta nova. |
| Acessibilidade | Narrator, tabulação, alto contraste e animações desativadas | Foco e nomes coerentes; sem depender exclusivamente do mouse. |
| Persistência | JSON inválido, diretório sem escrita e desligamento | Falha tratada e barra original recuperável. |
| Inicialização | Ativar/desativar início automático e mover pasta | Entrada HKCU correta, sem elevação. |
| Energia | Bloquear, logoff, suspender, reiniciar e desligar | Executar apenas em ambiente de teste; confirmação funciona. |

## Saída da homologação

Não promover a release a estável enquanto os casos de recuperação, bandeja e monitor
estiverem pendentes. Coletar o log local `windeck.log` com consentimento do usuário,
removendo dados pessoais antes de compartilhamento. Registrar falhas com passos
mínimos, build do Windows e configurações de tela.
