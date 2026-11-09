# 🚀 Guia de Deploy 100% Gratuito na Nuvem

Este guia explica passo a passo como colocar o **Controle de Gastos & Fatura Nubank** na nuvem de forma **permanente, segura e 100% gratuita** (sem necessidade de cadastrar cartão de crédito).

---

## 🏗️ Arquitetura da Nuvem

```mermaid
flowchart TD
    User["📱 Celular (PWA) / PC Browser"] -->|HTTPS| Vercel["⚡ Vercel (Frontend Next.js PWA)"]
    Telegram["🤖 App Telegram"] -->|Webhook| Render["🐳 Render.com (Backend .NET 8 Docker)"]
    Vercel -->|REST API com X-App-Pin| Render
    Render -->|SSL Connection| Neon["🐘 Neon.tech (PostgreSQL Serverless)"]
    Render -->|Chat AI| Groq["🧠 Groq API (Llama 3 70B)"]
```

| Componente | Provedor | Plano Gratuito | Função |
| :--- | :--- | :--- | :--- |
| **Banco de Dados** | **Neon.tech** | Gratuito Permanente (0.5 GB) | PostgreSQL serverless que nunca expira |
| **Backend API + Bot** | **Render.com** | Gratuito (Web Service Docker) | .NET 8 API + Webhook do Telegram |
| **Frontend PWA** | **Vercel** | Hobby Gratuito | Next.js 14 + PWA instalável no celular |
| **Inteligência Artificial** | **Groq Cloud** | Gratuito (30 req/min) | Processamento de linguagem natural |

---

## 📋 Pré-requisitos
Antes de começar, certifique-se de ter:
1. Uma conta no [GitHub](https://github.com) com este repositório enviado (`git push`).
2. Conta gratuita no [Neon.tech](https://neon.tech).
3. Conta gratuita no [Render.com](https://render.com).
4. Conta gratuita no [Vercel.com](https://vercel.com).
5. Chave de API do [Groq Console](https://console.groq.com/keys) (gratuita).
6. Token do seu bot do Telegram criado via [@BotFather](https://t.me/BotFather).

---

## Passo 1: Criar o Banco de Dados no Neon.tech

1. Acesse o [Neon Console](https://console.neon.tech) e faça login.
2. Clique em **Create Project**:
   - **Name**: `finance-tracker` (ou o nome que preferir)
   - **Region**: Escolha a mais próxima (ex: `US East (Ohio)` ou `South America (São Paulo)` se disponível).
3. Após criar, você verá o painel com a **Connection details**.
4. Selecione a opção **Connection string** e marque a caixa de seleção com a senha revelada. O formato será algo como:
   ```text
   postgresql://finance_owner:AbC123XyZ@ep-cool-cloud-123456.us-east-2.aws.neon.tech/finance-tracker?sslmode=require
   ```
5. **Guarde essa URL**. 
> [!NOTE]
> Você **não precisa** executar nenhum script SQL! O backend possui **auto-migração e seed automático**. Ao iniciar pela primeira vez conectado ao Neon, ele cria todas as tabelas (`FinancialProfile`, `CardExpenses`, `FixedCommitments`, `InvoicePayments`) e insere os dados iniciais padrão automaticamente.

---

## Passo 2: Hospedar o Backend no Render.com

Você pode subir via Blueprint automático ou criando o Web Service manualmente.

### Opção Recomendada: Criar Web Service Manual

1. Acesse o [Render Dashboard](https://dashboard.render.com).
2. Clique em **New +** > **Web Service**.
3. Conecte sua conta do GitHub e selecione o repositório `controle-gastos`.
4. Configure os campos:
   - **Name**: `finance-tracker-api` (ou similar)
   - **Region**: `Ohio (US East)` (idealmente próxima à região do Neon)
   - **Language / Runtime**: **Docker**
   - **Dockerfile Path**: `./backend/Dockerfile`
   - **Instance Type**: **Free**
5. Role até a seção **Environment Variables** e adicione as seguintes variáveis:

| Key | Exemplo de Valor | Descrição |
| :--- | :--- | :--- |
| `DATABASE_URL` | `postgresql://finance_owner:senha@ep-...neon.tech/finance-tracker?sslmode=require` | A connection string copiada do Neon |
| `Groq__ApiKey` | `gsk_...` | Sua chave do Groq |
| `Telegram__BotToken` | `123456789:AA...` | Token do BotFather |
| `APP_PIN` | `1234` | *(Recomendado)* PIN numérico de 4 a 6 dígitos para proteger alterações no app |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Modo de produção |

6. Clique em **Create Web Service**.
7. O Render começará a compilar a imagem Docker do .NET 8 e iniciará o container.
8. Ao finalizar, copie a URL gerada pelo Render (ex: `https://finance-tracker-api.onrender.com`).

> [!TIP]
> **Como funciona o Telegram Webhook no Render Free?**
> A camada gratuita do Render suspende containers após 15 minutos de inatividade. O nosso backend detecta a URL do Render e registra um **Webhook no Telegram** automaticamente. Toda vez que você mandar mensagem no Telegram, o Telegram chama a URL do Render, **acordando o container em segundos** para responder sua mensagem!

---

## Passo 3: Hospedar o Frontend no Vercel

1. Acesse o [Vercel Dashboard](https://vercel.com/dashboard).
2. Clique em **Add New...** > **Project**.
3. Importe o repositório `controle-gastos` do GitHub.
4. Na tela de configuração:
   - **Framework Preset**: `Next.js`
   - **Root Directory**: Clique em `Edit` e selecione a pasta **`frontend`**.
5. Abra a seção **Environment Variables** e adicione:

| Name | Value |
| :--- | :--- |
| `NEXT_PUBLIC_API_URL` | `https://finance-tracker-api.onrender.com` *(URL do seu backend no Render gerada no Passo 2)* |

6. Clique em **Deploy**.
7. Em cerca de 1 minuto seu site estará no ar com HTTPS gratuito em uma URL como `https://controle-gastos.vercel.app`!

---

## Passo 4: Instalar como Aplicativo no Celular (PWA)

O frontend foi totalmente configurado com Progressive Web App (PWA), manifest, temas escuros e ícones nativos:

### No iPhone (iOS Safari):
1. Abra a URL da Vercel no Safari.
2. Toque no botão de **Compartilhar** (ícone quadrado com seta para cima).
3. Role e toque em **"Adicionar à Tela de Início"**.
4. O app abrirá em tela cheia, sem barra de navegação do navegador, funcionando como um app nativo!

### No Android (Google Chrome):
1. Abra a URL da Vercel no Chrome.
2. Toque nos 3 pontinhos no canto superior direito (ou aguarde a barra "Instalar aplicativo").
3. Toque em **"Instalar aplicativo"** ou **"Adicionar à tela inicial"**.

---

## Passo 5: Configurar o PIN de Segurança

Se você configurou a variável `APP_PIN` no Render (ex: `1234`):
1. Ao abrir o painel pela primeira vez, clique no ícone de **Cadeado (PIN)** no cabeçalho.
2. Digite o seu código PIN e clique em **Salvar PIN**.
3. O código ficará salvo com segurança no seu dispositivo (`localStorage`) e será enviado automaticamente no cabeçalho `X-App-Pin` em todas as ações de adicionar gastos, atualizar caixinha ou pagar fatura.
4. Qualquer pessoa que acessar o link sem saber o PIN poderá apenas visualizar os dados, sem conseguir alterar nada.

---

## 💡 Manter o Servidor Rápido (Cold Starts no Render)

- O plano gratuito do Render desliga após 15 minutos sem requisições. A primeira requisição após o sono pode levar de 30 a 50 segundos.
- O frontend possui um **Banner Inteligente** que avisa automaticamente quando o Render está acordando.
- **Dica Opcional (Zero Cold Start)**: Você pode cadastrar a URL `https://sua-api.onrender.com/api/ping` em um serviço gratuito de monitoramento como [UptimeRobot](https://uptimerobot.com) ou [Cron-Job.org](https://cron-job.org) para fazer um ping a cada 10 minutos, mantendo o backend sempre acordado 24/7!

---

## 🛠️ Desenvolvimento Local

Se você quiser continuar desenvolvendo no seu computador:
- O backend mantém suporte a **SQL Server Local** (`localhost\SQLEXPRESS`).
- Se a variável de ambiente `DATABASE_URL` não for fornecida, o sistema usa automaticamente a connection string local do SQL Server sem quebrar nada.
- Se você quiser testar com o Neon Postgres localmente, basta definir `DATABASE_URL` no terminal antes de rodar `dotnet run`:
  ```powershell
  $env:DATABASE_URL="postgresql://finance_owner:senha@ep-...neon.tech/finance-tracker?sslmode=require"
  dotnet run --project backend/src/FinanceTracker.Api
  ```
