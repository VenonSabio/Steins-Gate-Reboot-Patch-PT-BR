# STEINS;GATE RE:BOOT — Tradução PT-BR

Tradução não oficial, feita por um fã, de **STEINS;GATE RE:BOOT** (versão Steam) para português brasileiro.

O patch **não contém o jogo**. Ele guarda só as diferenças em relação aos arquivos originais e precisa de uma cópia legítima do jogo para funcionar. O resultado é conferido arquivo por arquivo por SHA-256.

## O que está traduzido

- **Roteiro:** todo o texto narrativo, 24.524 segmentos em 175 cenários. O japonês foi usado como referência durante a tradução; a revisão humana completa do roteiro ainda está em andamento.
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

> Para remover a tradução, veja [Desinstalação](#desinstalação).
> Se o instalador disser que o jogo é incompatível, verifique a integridade pela Steam e tente de novo.

Requisitos: Windows com .NET Framework 4.x (já incluso no Windows 10/11) e conexão com a internet e cerca de 7 GB livres durante a instalação.

## Desinstalação

Abra o instalador, escolha a pasta do jogo e clique em **Desinstalar**. Ele confere tudo antes de mexer e devolve o jogo aos arquivos originais, conferindo o SHA-256 de cada um.

- A partir da v1.3.1, o instalador guarda os originais dos 10 arquivos que substitui por inteiro (cerca de 460 MB) em `%LOCALAPPDATA%\SteinsGateRebootPTBR`, fora da pasta do jogo. Os arquivos grandes não precisam de cópia: a tradução é acrescentada no fim deles, e o original continua intacto no começo.
- Se a tradução foi instalada com uma versão anterior do instalador, não há essa cópia. Nesse caso, use a Steam: botão direito no jogo → *Propriedades* → *Arquivos instalados* → *Verificar integridade dos arquivos*.

## Compatibilidade

| Item | Situação |
|---|---|
| Versão Steam, atualização de 30/10/2026 | Suportada (patch v1.3.1) |
| Versões anteriores da Steam | Use a release correspondente (v1.1) |
| Outras lojas, cópias modificadas ou com outros patches | Não suportadas: o instalador recusa arquivos que não sejam os originais |
| Windows 11 | Testado (Windows 10 deve funcionar, mas não foi testado) |

Se uma atualização do jogo mudar os arquivos, o instalador avisa que a versão é incompatível em vez de instalar por cima.

## O que já foi testado

| Parte | Situação |
|---|---|
| Instalação, verificação de hashes e reinstalação por cima | Testado |
| Início do jogo e diálogos dos primeiros capítulos | Testado parcialmente |
| Menu SYSTEM e tela de ajustes | Testado |
| Salvar/carregar e histórico | Testado parcialmente |
| TIPS, e-mails, telefone e fluxograma | Testados parcialmente |
| Finais e galeria | Verificados nos arquivos, não vistos em execução |

A lista detalhada fica em [`TESTES.md`](TESTES.md).

## Perguntas frequentes

**O antivírus acusou o instalador.**
É um falso positivo comum em executáveis sem assinatura digital que baixam arquivos. O código-fonte está neste repositório (`InstaladorPatch.cs`) e pode ser compilado por você mesmo.

**Perco meus saves?**
Não. O patch não toca nos saves. Mesmo assim, fazer uma cópia antes nunca é demais.

**Posso trocar de idioma depois?**
Sim. Japonês, inglês e chinês tradicional continuam disponíveis em *AJUSTES → IDIOMA*.

**O jogo atualizou e a tradução sumiu ou o instalador recusou.**
Aguarde uma nova versão do patch para a atualização do jogo e acompanhe as Releases.

**Por que o português aparece no lugar do chinês simplificado?**
O jogo só tem quatro espaços de idioma. O português ocupa o último para não remover nenhum idioma oficial do menu.

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
  csc /target:winexe /out:Instalar_Traducao_PTBR.exe -win32icon:sgre.ico /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll InstaladorPatch.cs
  ```
- `patch_ptbr.json`: índice do patch, com SHA-256 dos originais exigidos e dos resultados.
- `validar_patch.py`: confere o pacote antes de publicar (veja abaixo).

### Formato SGPT1

O patch é um índice (`patch_ptbr.json`) mais um blob dividido em partes (`patch_ptbr.pak.000`, `.001`...).

- `partes`: lista de partes, cada uma com `nome`, `bytes` e `sha256`.
- `arquivos`: um item por arquivo do jogo, com:
  - `destino`: caminho relativo à pasta do jogo;
  - `tipo`: `append` (mantém o começo do original e acrescenta dados), `full` (substitui o arquivo inteiro) ou `novo` (arquivo que não existe no jogo);
  - `bytes_original` e `sha256_original` (só em `append`): tamanho e hash do trecho original que precisa estar no começo do arquivo;
  - `blocos`: lista `[parte, offset, tamanho]`, copiados em ordem para o fim do arquivo;
  - `bytes_resultado` e `sha256_resultado`: tamanho e hash do arquivo final.

O instalador confere todos os originais antes de alterar qualquer coisa, gera cada arquivo num temporário, confere o hash do resultado e só então substitui o original de forma atômica.

### Validação antes de publicar

```
python validar_patch.py <pasta com patch_ptbr.json e as partes> [pasta do jogo original]
```

Confere hashes e tamanhos das partes, se todos os blocos estão dentro das partes, caminhos de destino, destinos duplicados e, se a pasta do jogo for passada, os hashes dos originais e a reconstrução completa de cada arquivo.

## Aviso legal

STEINS;GATE RE:BOOT © MAGES. / Chiyo St. Inc. © MAGES. / NITRO PLUS. Este é um projeto de fãs sem fins lucrativos, sem afiliação com os detentores dos direitos. Compre o jogo original para usar a tradução.
