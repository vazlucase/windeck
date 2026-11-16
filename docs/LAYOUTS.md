# Layouts e comportamento

A prévia interativa permite alternar entre todos os painéis e os dois temas.
O arquivo `design/board.html` mostra os oito layouts em conjunto. O aplicativo
usa as mesmas divisões funcionais, com conteúdo real do Windows.

## Anatomia expandida

1. Cabeçalho: marca, relógio local, configurações e recolher.
2. Navegação: visão geral, aplicativos, janelas, bandeja, música, agenda e controle.
3. Área de conteúdo: painel com rolagem quando não couber verticalmente.
4. Dock: Iniciar/catálogo, pesquisa, fixados, tarefas abertas e acessos de sistema.
5. Rodapé: quantidade de janelas, estado da substituição e data.

| Painel | Componentes e estados |
| --- | --- |
| Visão geral | Capa/controles reais, previsão, lembretes do dia, acessos rápidos e atividade. Estado sem reprodução; estado sem conexão. |
| Aplicativos | Campo de busca, catálogo do sistema, resultados indexados, fixação pelo menu, acesso ao Iniciar oficial. Carregando/vazio/indisponível. |
| Janelas | Tiles de janelas, seleção de grupo, prévia DWM, menu de operações, desktops virtuais. Janela ativa, minimizada e pedindo atenção. |
| Bandeja | Fixados e outros ícones, tooltip, clique/duplo clique/menu do próprio app. Gerenciamento por Shift+direito. |
| Música | Capa ampliada, título/artista, anterior/play/próxima, seek e tempo. Comandos desabilitados quando a sessão não os oferece. |
| Agenda | Mês selecionável, dia, lista e criação de lembretes. Integração com a central oficial para o calendário do Windows. |
| Controle | Volume/mudo, rede, energia, ferramentas de desktop e operações de sessão. Leitura indisponível e ausência de bateria tratadas. |
| Configurações | Temas, substituição, movimentos, fullscreen, área reservada, cidade, startup, fixados e restauração. |

## Temas

- **Fluent**: superfície azul-ardósia, acento azul, cartões arredondados e foco
  visível; inspirado nos materiais do Windows 11.
- **Dock**: cápsula grafite/quase preta, acento violeta e ampliação dos ícones
  conforme a distância do cursor.
- **Alto contraste**: usa cores do sistema. **Redução de movimento**: elimina
  interpolação e ampliação dinâmica.

A composição WPF desta alpha usa translucidez e sombras. Não é uma promessa de
Acrylic/Mica idêntico a todos os componentes proprietários do Windows.

## Movimento

A abertura expande largura e altura com molas criticamente amortecidas. Os
olhos recebem o vetor do cursor global, com deslocamento limitado e suavização.
O gesto considera direção ascendente, corredor central, velocidade, distância
mínima e cooldown. Movimentos laterais e para baixo não devem abrir o painel.

## Limites da referência

As imagens orientam a cápsula, os cartões, o player e o movimento. O Windeck
usa ícones e dados reais quando executado no Windows. As capas, nomes, números
e agenda da prévia são amostras explícitas. As telas oficiais que o aplicativo
abre conservam o desenho e a posição que o Windows lhes atribuir.
