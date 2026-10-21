# Study Tracker: plano da v1

## Objetivo

Substituir o `status.html` da pasta Infnet por um app local em ASP.NET Core + Blazor, sem as limitações do navegador: gravar arquivos, ler as pastas e abrir o Explorer direto, em qualquer navegador e sem permissão a cada abertura.

A v1 tem o mesmo escopo do painel atual, só que bem feito. Nada de funcionalidade nova além do que o painel já faz.

## Decisões fechadas

| Tema | Decisão |
|---|---|
| Stack | .NET 10 (LTS, já instalado), Blazor Web App com renderização Interactive Server |
| Execução | Servidor local em `127.0.0.1`, aberto no navegador por um atalho |
| Idioma | Código, commits e README em inglês. Interface em português |
| Fonte da verdade | Continua sendo o `status-dados.js` e as pastas em `G:\Meu Drive\Infnet`. O app não tem banco de dados |
| Dados no repo | Nenhum. O repo tem só código; o caminho da pasta Infnet fica em configuração local, fora do Git |
| Acesso ao Drive | Pelo `G:` do Google Drive para desktop, como pasta comum, com `System.IO`. Sem API do Google. O app roda de qualquer pasta |

Por que manter o `status-dados.js`: o Claude lê e edita esse arquivo seguindo o guia, e o `status.html` continua funcionando como reserva. Se o estado fosse para um banco, esse fluxo quebrava.

## Estrutura da solution

```
study-tracker/
├── StudyTracker.slnx
├── src/
│   ├── StudyTracker.Core/            modelo e regras, sem acesso a disco
│   ├── StudyTracker.Infrastructure/  leitura e gravação da pasta Infnet
│   └── StudyTracker.Web/             Blazor (telas e componentes)
├── tests/
│   ├── StudyTracker.Core.Tests/              xUnit
│   └── StudyTracker.Infrastructure.Tests/    xUnit com pasta temporária
├── docs/
└── README.md
```

### Core
- Modelo: `Block`, `CourseGroup`, `Course`, `Assignment`, `AssignmentStatus`, `Stages`, `HistoryEntry`.
- Os nomes do JSON continuam em português (`atualizadoEm`, `blocos`, `trabalhos`, `sem-contexto`), mapeados com `JsonPropertyName` e um conversor de enum. Assim o arquivo não muda de formato.
- `JsonExtensionData` em todas as classes, para não apagar campos que o Claude acrescentar e o app ainda não conheça.
- Regras puras e testáveis:
  - nome proposto para um arquivo solto (`Enunciado TP2.docx`, `Rubricas TP2.txt`, nome original);
  - detecção de etapas pelos nomes da pasta (enunciado, rubricas, guia, artefatos);
  - sugestões quando o disco diverge dos dados (etapas, `semPasta`, mudar para `pronto`);
  - alerta de arquivo `guilherme_cassel_...` fora do padrão;
  - texto automático da linha de histórico a partir do que mudou;
  - texto do prompt para o Claude.
- Validação no padrão da seção 6.8 do guia: encapsulamento, argumentos validados, status e datas consistentes.

### Infrastructure
- `StatusDataStore`: lê e grava o `status-dados.js` no mesmo formato de hoje (cabeçalho preservado, JSON compacto depois de `window.DADOS =`). Antes de gravar, confere o hash do arquivo; se mudou por fora, recarrega e recusa a gravação. Grava num arquivo temporário e troca depois, para não deixar o arquivo pela metade.
- `FolderScanner`: lista as pastas dos trabalhos do bloco atual.
- `FileWatcher`: `FileSystemWatcher` na pasta Infnet. Mudança no `status-dados.js` ou nas pastas atualiza a tela na hora, sem recarregar. O `G:` é um disco virtual do Google Drive e o watcher pode perder eventos nele, então também há uma releitura periódica (30 s, configurável) e outra quando a aba volta ao foco.
- Se o `G:` não estiver montado (Drive fechado), o app abre com um aviso claro e tenta de novo, em vez de quebrar.
- `AssignmentFileWriter`: salva arquivos soltos e rubricas coladas e cria `TPn\` se faltar.
- `ExplorerLauncher`: abre a pasta no Explorer com `Process.Start`.
- Toda operação de arquivo passa por uma checagem de caminho: só dentro da pasta Infnet, sem `..`, e nome de arquivo sem caractere inválido.

### Web (Blazor)
- **Painel**: visão do curso (blocos e entregues), contadores por status com filtro, "O que fazer agora", cards por disciplina e trabalho, blocos concluídos recolhidos e histórico. É o mesmo visual do `status.html`, com claro e escuro.
- **Card do trabalho**: etapas, sugestões com botão "aplicar", alerta de nome, lista de arquivos, abrir pasta, copiar prompt.
- **Editar trabalho**: diálogo com status, prazo, entrega, etapas, próximo passo, correção, observação e linha de histórico gerada.
- **Arrastar arquivos**: área de soltar sobre o card (`InputFile` por baixo), diálogo com tipo e nome editável, confirmação para substituir. Limite de tamanho configurado, porque o padrão do `InputFile` é só 500 KB.
- **Colar rubricas**: caixa de texto que salva `Rubricas TPn.txt`.

### Execução
- O Kestrel escuta só em `127.0.0.1`, numa porta fixa configurável.
- `dotnet publish` gera um executável. Um atalho "Study Tracker" sobe o app e abre o navegador. Se o app já estiver rodando, o atalho só abre o navegador.
- A configuração local fica em `appsettings.Local.json`, fora do Git, com `RootPath = G:\Meu Drive\Infnet`. O repo leva um `appsettings.Local.example.json`.

## Fora da v1
Notas e médias, calendário, integração com o Moodle, login, banco de dados, deploy fora do PC, notificações, serviço do Windows.

## Etapas
Cada etapa vira um commit em `main`.

1. **Esqueleto**: solution, projetos, `.gitignore` (inclui `.vs/`), README curto, configuração da pasta.
2. **Dados**: modelo do Core e `StatusDataStore`. Teste de ida e volta: ler o `status-dados.js` real (uma cópia) e gravar de novo tem que dar o mesmo texto.
3. **Painel só leitura**: a tela igual ao `status.html` de hoje.
4. **Pastas**: scanner, sugestões, alerta de nome, abrir no Explorer e atualização ao vivo pelo watcher.
5. **Edição**: diálogo de edição, histórico e proteção contra edição simultânea com o Claude.
6. **Arquivos**: arrastar e soltar e colar rubricas.
7. **Atalho**: publish, atalho na área de trabalho e README completo em inglês.

## Verificação
- Testes xUnit nas regras do Core e no `StatusDataStore`, com pasta temporária: ida e volta, conflito, caminho com `..`, nome inválido e campo desconhecido preservado.
- Rodar o app contra uma cópia da pasta Infnet antes de apontar para a real.
- Comparar o painel do app com o `status.html` lado a lado.
- Fazer no app uma edição e conferir que o `status.html` antigo continua abrindo o arquivo gravado.

## Depois da v1, na pasta Infnet
- Atualizar a seção 0.2 do guia: o app passa a ser o painel principal, com o mesmo arquivo de dados.
- Decidir se o `status.html` fica como reserva ou sai.
- Remover o link `infnet-pasta:` do registro e o `abrir-pasta.ps1`, porque o app abre o Explorer sozinho.
