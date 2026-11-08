# 💳 BurnTracker — Personal Finance & Credit Card Burn-Rate Tracker

Um rastreador financeiro full-stack desenhado para controle rigoroso de fluxo de caixa mensal e proteção do teto da fatura do cartão de crédito.

---

## 🎯 Estratégia de Fluxo Mensal
- **Renda:** **R$ 2.200,00/mês** (R$ 1.200 no 5º dia útil + R$ 1.000 no dia 14).
- **Meta Blindada de Investimento:** **R$ 1.000,00/mês** intocáveis no CDI.
- **Compromissos Fixos Externos (Pix):**
  - **R$ 50,00/mês** recorrente fixo (*Internet Móvel Mãe*).
  - **R$ 120,00/mês por 5 meses** (*Monitor Mãe* de R$ 600,00 iniciado no ciclo `2026-12`).
- **Cartão de Crédito Único:** Fecha todo **dia 8** e vence todo **dia 16**.
- **Fórmula do Teto Rígido (Hard Cap):**
  $$\text{Renda } (R\$\ 2.200) - \text{Meta } (R\$\ 1.000) - \text{Pix } (R\$\ 170) = \mathbf{R\$\ 1.030,00}$$
  *(salta automaticamente para **R$ 1.150,00** quando as 5 parcelas do monitor terminarem no ciclo `2027-05`)*.

---

## 🛠️ Stack Tecnológica
- **Backend:** .NET 8 Web API (C#), Dapper, `Microsoft.Data.SqlClient`, xUnit.
- **Banco de Dados:** Microsoft SQL Server 2025 (SQLEXPRESS / LocalDB).
- **IA & Mensageria:** Groq API (`llama-3.3-70b-versatile`) + Telegram Bot Webhook + Fallback Regex Local em português brasileiro.
- **Frontend:** Next.js 14 (App Router, TypeScript), Tailwind CSS, `framer-motion`, `lucide-react`.

---

## 🚀 Como Executar

### 1. Banco de Dados (SQL Server)
O script de inicialização e seed idempotente está em:
```bash
database/01_init_schema_and_seed.sql
```
Ele cria o banco `FinanceTrackerDb` e faz o seed do perfil padrão e dos compromissos fixos.

### 2. Backend (.NET 8 Web API)
```bash
cd backend/src/FinanceTracker.Api
dotnet run --launch-profile http
```
- A API inicia em: `http://localhost:5154`
- Swagger UI disponível em: `http://localhost:5154/swagger`
- Testes unitários: `dotnet test backend/FinanceTracker.sln`

### 3. Frontend (Next.js)
```bash
cd frontend
npm install
npm run dev
```
- Acesse no navegador: `http://localhost:3000`

---

## ⚡ Comandos Rápidos da Barra de IA / Telegram
Pressione `/` no teclado na página inicial para focar a barra de comando ou envie mensagens pelo Telegram:

| Comando | O que faz | Exemplo |
| :--- | :--- | :--- |
| **Gasto à vista** | Registra o gasto e recalcula o limite diário | `"uber 24,90"`, `"almoço 35"` |
| **Gasto parcelado** | Registra parcelas exatas nos ciclos futuros | `"teclado 300 3x"`, `"monitor 600 em 5x"` |
| **Simular compra** | Mostra o impacto no limite diário e se invade a meta | `"simular 400 4x"`, `"simular 150"` |
| **Ver status** | Exibe resumo da fatura e teto | `"/status"` ou `"status"` |
| **Desfazer** | Apaga o último lançamento feito | `"/desfazer"` ou `"desfazer"` |
