# Windeck 0.1.0 — alpha

Primeira entrega nativa em C#/WPF com execução independente, dois temas e
cápsula dinâmica no topo dos monitores. Inclui integração ManagedShell para
janelas e bandeja, serviços de mídia e áudio, previsão, agenda local e acesso
às ferramentas e painéis oficiais do Windows.

A interface tem repouso, aproximação e expansão, olhos que seguem o cursor,
gesto ascendente, molas analíticas, navegação, miniaturas DWM e dock com
ampliação no tema escuro. Há recuperação por processo separado, lease com
identificador de sessão e atalho de emergência.

## Estado da verificação

Compilado e publicado para Windows x64 pelo GitHub Actions. As 17 verificações
do núcleo passaram em Windows e Linux. A inicialização e a renderização das
oito páginas WPF nos dois temas são verificadas em Windows, sem ativar a
substituição. A prévia visual foi verificada com Playwright em Linux.
Os cenários de uso interativo das integrações ainda precisam de homologação.

## Limites que impedem declarar paridade completa

- Interface unificada própria para notificações modernas, redes e idioma ainda não implementada; esses itens usam acesso aos painéis oficiais.
- Jump Lists personalizadas/tarefas de apps, reendereçamento de todos os atalhos Win e detalhes de áreas de trabalho virtuais ainda incompletos.
- Importação de pins modernos, drag-and-drop OLE arbitrário e reordenação de todos os casos de grupos precisam de mais implementação.
- Recuperação, bandeja, conteúdo protegido, DPI misto e acessibilidade precisam de validação no Windows.
- Sem assinatura de código, instalador ou atualizador automático.

A lista detalhada e verificável está em docs/PARIDADE.md.
