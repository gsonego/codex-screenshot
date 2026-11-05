# Desktop Print

Um botão flutuante para recortar a tela e anexar a imagem à mensagem do **ChatGPT ou Codex no Windows**.

<img src="assets/camera.png" alt="Ícone de câmera do Desktop Print" width="96">

Clique em **Print**, selecione uma área e o recorte é colado automaticamente na caixa de mensagem que estava em foco. Você revisa e envia a mensagem.

## Recursos

- Botão visível somente enquanto o aplicativo desktop ChatGPT ou Codex está em foco.
- Seleção retangular em um ou vários monitores.
- Colagem automática na mensagem, sem enviar por conta própria.
- Botão reposicionável pelos três pontos à direita.
- Atalho opcional **Ctrl+Alt+S**, disponível quando não estiver ocupado por outro aplicativo.
- **Esc** ou botão direito para cancelar o recorte.
- Ícone na bandeja para ocultar, reativar ou fechar o utilitário.

## Compilar e abrir

Requisitos do botão: Windows com sessão de desktop ativa, Windows PowerShell 5.1 e compilador C# do .NET Framework em `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

Na pasta do projeto, execute:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-Button.ps1
.\DesktopPrint.exe
```

O script gera o ícone, compila o programa e executa verificações de coordenadas e identificação dos aplicativos. Os executáveis são gerados localmente e não fazem parte do repositório.

Se o botão já estiver aberto, use **Sair** no menu da bandeja e execute a compilação novamente para atualizar `DesktopPrint.exe`. A nova versão também fica em `build\DesktopPrint.exe`.

## Usar

1. Clique na caixa de mensagem do ChatGPT/Codex, deixando o cursor onde digitaria.
2. Clique no botão **Print**.
3. Arraste sobre a área desejada e solte o mouse.
4. Confira a imagem anexada e envie a mensagem quando quiser.

Arraste pelos três pontos à direita para posicionar o botão perto do chat. Ao trocar para outro programa, o botão se oculta em aproximadamente 150 ms; ao voltar, reaparece na mesma posição. O ícone da bandeja permanece disponível.

O botão é um utilitário local separado: ele não modifica o menu **+** do aplicativo. O reconhecimento por foco se aplica aos processos desktop `ChatGPT` e `Codex`, não ao ChatGPT aberto em uma aba do navegador.

## Comportamento e limites

- O recorte substitui o conteúdo da área de transferência.
- O botão não salva capturas no disco, não inicia com o Windows e não envia mensagens.
- A colagem usa o campo que estava em foco antes da seleção. Clique na caixa de mensagem antes de capturar.
- Se o foco mudar ou o Windows impedir a restauração da janela, o recorte fica copiado para você colar com **Ctrl+V**.
- Aplicativos executados como administrador podem bloquear a colagem.
- Conteúdo protegido, tela bloqueada ou sessão sem desktop interativo podem produzir uma captura preta ou erro.
- A posição do botão é preservada ao alternar entre aplicativos, mas é reiniciada ao fechar e abrir o utilitário.

## Plugin MCP opcional

O projeto também inclui uma ferramenta MCP para pedir ao assistente uma captura do monitor principal ou de todos os monitores. Essa ferramenta devolve uma imagem como resultado da chamada; ela oferece um fluxo diferente do botão de recorte.

Requisitos adicionais: Node.js 18 ou superior no `PATH` e Codex CLI para instalar o plugin. Não requer dependências npm, chave de API ou servidor remoto.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
```

O instalador registra o catálogo local `guilherme-local` e instala `desktop-print@guilherme-local`. Ele altera a configuração de plugins do usuário. Depois, abra uma nova conversa local; pode ser necessário reiniciar o aplicativo.

Para remover o plugin:

```powershell
codex plugin remove desktop-print@guilherme-local
codex plugin marketplace remove guilherme-local
```

O arquivo `Copiar print.cmd` oferece ainda uma captura do monitor principal com atraso de três segundos e cópia para colagem manual.

## Estrutura

| Arquivo | Função |
| --- | --- |
| `PrintButton.cs` | Botão, recorte, controle de foco e colagem |
| `Build-Button.ps1` | Geração do ícone, compilação e verificações |
| `assets/` | Ícone da câmera em PNG e ICO |
| `plugins/desktop-print/` | Manifesto, servidor MCP e captura PowerShell |
| `tests/server.test.mjs` | Testes do protocolo MCP com imagens sintéticas |
| `Install.ps1` | Instalação opcional do plugin MCP |

## Verificações

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-Button.ps1
node --test tests/server.test.mjs
```

Os testes automatizados não capturam o desktop real. A seleção e a colagem foram verificadas manualmente no aplicativo; alterações nessa integração devem ser conferidas na interface do cliente.

Este projeto é independente e não é um produto oficial da OpenAI.
