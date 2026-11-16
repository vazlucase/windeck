# Inventário de paridade com a barra do Windows

O objetivo do produto é substituir a barra inteira. **Inventário não significa
homologação.** “Implementado” abaixo significa que há código e integração;
a execução desses itens ainda não foi verificada em um Windows real nesta entrega.

| Recurso | Implementação nesta alpha | Situação / limite |
| --- | --- | --- |
| Executável independente | .NET 10/WPF, distribuição autocontida x64 | Build executado; GUI Windows pendente. |
| Substituição visual da barra | Oculta a barra do Explorer após armar recuperação | Não modifica o shell de login. |
| Área de trabalho e Explorador | Explorer continua executando | Fornecidos pelo Windows. |
| Aproximação, olhos e clique | Máquina de estados, cursor global, molas analíticas | Núcleo testado; entrada nativa pendente. |
| Gesto rápido para cima | Direção, velocidade, deslocamento e cooldown | Núcleo testado com casos negativos. |
| Dois temas | Fluent e Dock | Preview visual verificada. |
| Ampliação do dock | Transformações por distância do cursor | Código Windows pendente de execução. |
| Iniciar | Catálogo próprio e botão para Iniciar oficial | O Iniciar oficial mantém sua aparência. |
| Aplicativos Win32 e de Store | AppsFolder e atalhos dos menus Iniciar | Enumeração COM real; validação Windows pendente. |
| Pesquisa por aplicativos | Filtro do catálogo instalado | Implementado. |
| Pesquisa por arquivos | Índice Windows Search via OLE DB | Somente locais indexados; fallback de acesso à busca oficial. |
| Pesquisa completa/web | Pesquisa oficial do Windows | Integração nativa, não redesenhada. |
| Aplicativos fixados | Importação de atalhos .lnk e fixação própria | A ordem é do Windeck; importação de todos os pins modernos ainda incompleta. |
| Fixados + aplicativos abertos | Combinação por caminho ou AppUserModelID | Atalhos que não resolvem podem aparecer separados. |
| Reordenação de fixados | Alt+arrastar atalho | Reordenação de pin combinado com grupo aberto ainda pendente. |
| Aplicativos abertos | ManagedShell Tasks | Usa janelas reais, sem lista simulada. |
| Agrupamento de janelas | AppUserModelID/caminho | Menu de escolha do grupo. |
| Ativar/restaurar/minimizar | Ações ManagedShell | Aplicativos elevados/protegidos precisam de ensaio. |
| Mover/redimensionar/maximizar/fechar | Menu de ações real | Disponibilidade depende da janela. |
| Nova instância | Shift+clique ou botão do meio | Depende de o aplicativo permitir múltiplas instâncias. |
| Prévia de janela | Thumbnail DWM | Precisa de validação em DPI misto e conteúdo protegido. |
| Progresso na tarefa | ProgressValue fornecido pelo shell | Valor presente; estados visuais completos requerem ensaio. |
| Ícones de sobreposição/badges | OverlayIcon fornecido pelo shell | Não gera contadores fictícios. |
| Atenção de aplicativos | Estado Flashing com destaque | Sem pulso animado nesta alpha. |
| Jump Lists recentes | IApplicationDocumentLists por AppUserModelID | Apenas documentos recentes disponibilizados pelo shell. |
| Jump Lists personalizadas/tarefas do app | API pública usada não expõe essas categorias | Pendente; não apresentado como recurso concluído. |
| Arraste de arquivos | Abrir no app fixado/aberto; hover ativa janela | Entrega OLE arbitrária ao processo alvo ainda não equivale a todos os cenários nativos. |
| Bandeja de aplicativos | ManagedShell NotificationArea | Listener/seed/callbacks reais. |
| Ícones ocultos | Página própria de outros ícones | Código integrado. |
| Menus e duplo clique da bandeja | Encaminhamento ao NotifyIcon | Compatibilidade depende do aplicativo e versão do shell. |
| Fixar/desafixar ícones da bandeja | Shift+botão direito, com persistência | Código integrado. |
| Rede e Wi-Fi | Estado real da conexão + controles do Windows | Lista de redes e conexão usam a UI oficial. |
| Bluetooth | Configurações oficiais | Não há enumerador Bluetooth próprio nesta alpha. |
| Volume e mudo | Core Audio COM | Saída/mixer avançado abre o Windows. |
| Troca automática de endpoint de áudio | Endpoint do serviço | Reconexão/troca de dispositivo ainda exige validação e aprimoramento. |
| Bateria/energia | GetSystemPowerStatus + configurações | Dados desconhecidos são indicados; não inventa porcentagem. |
| Brilho | Configurações de tela | Slider próprio pendente. |
| Projetar/transmitir | Win+P / Win+K | Painéis oficiais. |
| Idioma/teclado/IME | Win+Espaço | Seletor oficial, não redesenhado. |
| Teclado de toque/caneta | Acessíveis pelos componentes do Windows | Botões dedicados e equivalência de comportamento pendentes. |
| Relógio e data | Hora local do sistema | Implementado. |
| Calendário | Grade própria + acesso à central oficial | Calendário próprio usa datas locais. |
| Agenda | Lembretes locais | Sincronização com Outlook/Google não implementada. |
| Notificações modernas | Abre a central oficial com Win+N | Inbox própria, badges e sincronização de leitura pendentes. |
| Notificações clássicas | Captura do serviço de bandeja, histórico e banners | Respeita estado de apresentação/ocupado; callbacks precisam de teste Windows. |
| Não perturbe/foco | Banner consulta SHQueryUserNotificationState | UI avançada permanece no Windows. |
| Widgets e clima | Previsão Open-Meteo + acesso Win+W | Dados reais no app, amostras só na prévia. |
| Música | GSMTC: capa, título, artista, controle e posição | Depende da sessão publicada pelo aplicativo. |
| Múltiplas sessões de mídia | Escolhe sessão em reprodução ou sessão atual | Seletor manual ainda pendente. |
| Mostrar área de trabalho | Win+D | Preview por hover/Peek nativo ainda pendente. |
| Visão de tarefas | Win+Tab | UI oficial. |
| Áreas de trabalho virtuais | Criar e navegar com atalhos oficiais | Filtragem exata por desktop e arraste entre desktops pendentes. |
| Clipboard, emojis e captura | Atalhos oficiais | UI oficial. |
| Menu avançado e Executar | Win+X e RunDialog | Integração nativa. |
| Gerenciador de tarefas | Executável do sistema | Acesso implementado. |
| Bloquear/suspender/reiniciar/desligar/logoff | Funções do sistema com confirmação | Não são executadas durante os testes em Linux. |
| Atalhos próprios | Ctrl+Alt+Espaço / Ctrl+Alt+Backspace | Registro com detecção de conflito. |
| Win+T, Win+B, Win+número e atalhos de shell personalizados | Explorer ainda recebe parte desses atalhos | Reendereçamento com paridade total pendente. |
| Múltiplos monitores | Uma cápsula por monitor, reconciliação após mudança | Hotplug, fullscreen e DPI misto precisam de ensaio real. |
| Área reservada de tela | AppBar no topo | Cache evita loop de reposicionamento entre AppBars. |
| Fullscreen | Ocultação por monitor | Precisa de teste com jogos exclusivos e borda invisível. |
| Redução de movimento | Preferência própria + configuração do sistema | Molas testadas; integração de acessibilidade pendente. |
| Alto contraste | Paleta de SystemColors | Alterações durante a sessão e leitores de tela precisam de ensaio. |
| Narrator/teclado | AutomationProperties, foco, Escape e labels | Não certificado. |
| Início com Windows | Opção explícita em HKCU Run | Desativado até escolha do usuário. |
| Restauração na saída | Dispose e ABM_REMOVE | Código implementado; testes Windows obrigatórios. |
| Recuperação após travamento | Processo independente e heartbeat | Só pode encerrar o próprio pai Windeck. Falha ainda precisa ser ensaiada em Windows. |
| Assinatura/instalador/atualização | Portátil sem assinatura | Assinatura e atualização automática pendentes. |

## Critério para chamar a substituição de completa

Fechar os itens pendentes relevantes, implementar a experiência unificada que
hoje abre painéis oficiais e executar o plano de validação nas versões-alvo do
Windows, com múltiplos monitores, escalas diferentes, aplicações elevadas,
notificações, acessibilidade e cenários de recuperação. A compilação e a prévia
não comprovam esse critério.
