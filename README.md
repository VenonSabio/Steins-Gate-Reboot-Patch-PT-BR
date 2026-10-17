# STEINS;GATE RE:BOOT — Tradução PT-BR

Tradução não oficial, feita por fãs, de **STEINS;GATE RE:BOOT** (versão Steam) para português brasileiro.

O patch **não contém o jogo**. Ele guarda só as diferenças em relação aos arquivos originais e precisa de uma cópia legítima do jogo para funcionar. O resultado é conferido arquivo por arquivo por SHA-256.

## O que está traduzido

- **Roteiro:** todo o texto narrativo, 24.524 segmentos em 175 cenários, revisado com o japonês como fonte principal.
- **Interface:** menus, ajustes, salvar/carregar, histórico, diálogos de sistema, ajuda e guias de controle.
- **Telefone, e-mails, TIPS e fluxograma:** 290 verbetes de TIPS, e-mails, contatos, sinopses e nós do fluxograma.
- **Gráficos:** tela inicial, títulos de capítulo, nomes de personagens, fóruns, telas de celular e textos decorativos da interface.
- **Vídeos:** legendas do prólogo e do poema, cartelas de capítulo, créditos e mudanças de Linha de Mundo.

Japonês, inglês e chinês tradicional ficam intactos. O português ocupa o espaço do chinês simplificado.

## Instalação

1. Baixe **só** o `Instalar_Traducao_PTBR.exe` da [Release mais recente](../../releases/latest).
2. Feche o jogo e execute o instalador.
3. Escolha a pasta do jogo, a que contém `sgre_steam.exe`.
4. O instalador baixa sozinho o restante do patch (cerca de 3,3 GB) desta mesma Release, confere e instala.
5. No jogo: **AJUSTES → IDIOMA → Português** (4ª opção).

Instalação offline: se `patch_ptbr.json` e as partes `patch_ptbr.pak.*` estiverem na mesma pasta do `.exe`, ele usa esses arquivos e não baixa nada.

O instalador:
- confere se os arquivos do jogo são os originais da Steam;
- aplica a tradução e confere o SHA-256 de cada arquivo gerado;
- avisa se a tradução já estiver instalada.

> ⚠️ O instalador **não faz backup**. Para voltar ao original, use *Propriedades → Arquivos instalados → Verificar integridade dos arquivos* na Steam.
> Se o instalador disser que o jogo é incompatível, verifique a integridade pela Steam e tente de novo.

Requisitos: Windows com .NET Framework 4.x (já incluso no Windows 10/11) e conexão com a internet e cerca de 7 GB livres durante a instalação.

## Glossário

As principais escolhas de termos estão em [`Glossario_PTBR.csv`](Glossario_PTBR.csv). Exemplos: Linha de Mundo, Salto Temporal, D-Mail, Reading Steiner, Laboratório de Gadgets Futurísticos, SERN, El Psy Kongroo. A seção TIPS mantém esse nome.

## Problemas conhecidos

- Alguns microtextos ilegíveis em vídeos e textos diegéticos em japonês dentro de cenários (TVs, jornais, lousas) ficam como no original. É o mesmo comportamento da versão em inglês.
- Telas que dependem de progresso avançado, como Finais, Galeria e o fim dos TIPS mais longos, foram verificadas na estrutura dos arquivos, mas nem todas foram vistas em execução.

Encontrou um erro? Abra uma *Issue* com uma captura de tela e o ponto da história.

## Para desenvolvedores

- `gerar_patch.py`: gera o patch (formato SGPT1) a partir do pacote traduzido e da instalação original.
- `InstaladorPatch.cs`: código do instalador. Compile com:
  ```
  csc /target:exe /out:Instalar_Traducao_PTBR.exe /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll InstaladorPatch.cs
  ```
- `patch_ptbr.json`: índice do patch, com SHA-256 dos originais exigidos e dos resultados.

## Aviso legal

STEINS;GATE RE:BOOT © MAGES. / Chiyo St. Inc. © MAGES. / NITRO PLUS. Este é um projeto de fãs sem fins lucrativos, sem afiliação com os detentores dos direitos. Compre o jogo original para usar a tradução.
