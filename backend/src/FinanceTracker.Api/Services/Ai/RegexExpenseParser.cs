using System.Globalization;
using System.Text.RegularExpressions;

namespace FinanceTracker.Api.Services.Ai;

public static class RegexExpenseParser
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    public static ParsedExpenseIntent Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ParsedExpenseIntent { Intent = "UNKNOWN" };
        }

        var text = input.Trim().ToLowerInvariant();

        // 1. Status command
        if (text == "status" || text == "/status" || text.Contains("como ta minha fatura") || text.Contains("como está minha fatura") || text == "resumo")
        {
            return new ParsedExpenseIntent { Intent = "STATUS" };
        }

        // 2. Undo command
        if (text == "desfazer" || text == "/desfazer" || text == "apagar ultimo" || text == "desfazer ultimo" || text == "apagar último" || text == "desfaz")
        {
            return new ParsedExpenseIntent { Intent = "UNDO" };
        }

        // 2.1 Pay Invoice command
        if (text == "paguei a fatura" || text == "fatura paga" || text == "/pagarfatura" || text == "pagar fatura" || text == "quitei a fatura" || text == "quitei o cartao")
        {
            return new ParsedExpenseIntent { Intent = "PAY_INVOICE" };
        }

        // 2.2 Invest Surplus command
        if (text == "investir sobra" || text == "reinvestir sobra" || text == "guardar sobra" || text == "/investirsobra" || text == "aplicar sobra" || text.Contains("investir a sobra"))
        {
            return new ParsedExpenseIntent { Intent = "INVEST_SURPLUS" };
        }

        // 3. Sync Invoice command: "fatura atual 350", "ajustar fatura 350", "fatura 400"
        var syncMatch = Regex.Match(text, @"^(?:ajustar\s+fatura|sincronizar\s+fatura|fatura\s+atual|fatura)\s+(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)$", RegexOptions.IgnoreCase);
        if (syncMatch.Success && decimal.TryParse(syncMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var syncAmount) && syncAmount >= 0)
        {
            return new ParsedExpenseIntent
            {
                Intent = "SYNC_INVOICE",
                Amount = syncAmount,
                Description = "Ajuste de Fatura"
            };
        }

        // 4. Update Caixinha balance: "saldo caixinha 4500", "atualizar caixinha 5000", "saldo da caixinha 4500"
        var caixinhaMatch = Regex.Match(text, @"^(?:saldo\s+caixinha|atualizar\s+caixinha|saldo\s+da\s+caixinha|caixinha\s+saldo)\s+(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)$", RegexOptions.IgnoreCase);
        if (caixinhaMatch.Success && decimal.TryParse(caixinhaMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var caixinhaBalance) && caixinhaBalance >= 0)
        {
            return new ParsedExpenseIntent
            {
                Intent = "UPDATE_CAIXINHA",
                Amount = caixinhaBalance,
                Description = "Saldo Caixinha"
            };
        }

        // 5. Simulate command: "simular 400 4x", "/simular 500 em 3x", "simular 150"
        var simMatch = Regex.Match(text, @"^(?:/|)?simular\s+(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)(?:\s+(?:em\s+)?([0-9]{1,2})x?)?", RegexOptions.IgnoreCase);
        if (simMatch.Success)
        {
            var amountStr = simMatch.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var simAmount))
            {
                var installments = 1;
                if (simMatch.Groups[2].Success && int.TryParse(simMatch.Groups[2].Value, out var inst))
                {
                    installments = Math.Max(1, inst);
                }

                return new ParsedExpenseIntent
                {
                    Intent = "SIMULATE",
                    Amount = simAmount,
                    Installments = installments,
                    Description = "Simulação"
                };
            }
        }

        // 6. Relative dates (e.g., "ontem")
        DateTime? targetDate = null;
        if (text.Contains("ontem"))
        {
            targetDate = DateTime.Now.AddDays(-1);
            text = text.Replace("ontem", "").Trim();
        }

        // 7. Pix / Caixinha check
        var isPix = Regex.IsMatch(text, @"\b(pix|caixinha)\b", RegexOptions.IgnoreCase);
        if (isPix)
        {
            var amountMatch = Regex.Match(text, @"(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)", RegexOptions.IgnoreCase);
            if (amountMatch.Success && decimal.TryParse(amountMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var pixAmount) && pixAmount > 0)
            {
                var remaining = text.Remove(amountMatch.Index, amountMatch.Length).Trim();
                var cleanDesc = Regex.Replace(remaining, @"\b(tirei\s+da\s+caixinha\s+pro\s+pix|tirei\s+da\s+caixinha|retirar\s+da\s+caixinha|da\s+caixinha|na\s+caixinha|caixinha|no\s+pix|pro\s+pix|pelo\s+pix|pix|comprei(?:\s+um|\s+uma)?|paguei(?:\s+o|\s+a)?|gastei\s+com|gastei|com|em|no|na|de|do|da|o|a|pro|para)\b", " ", RegexOptions.IgnoreCase);
                cleanDesc = Regex.Replace(cleanDesc, @"\s+", " ").Trim();

                if (string.IsNullOrWhiteSpace(cleanDesc))
                {
                    cleanDesc = "Pix Caixinha";
                }
                else
                {
                    cleanDesc = char.ToUpperInvariant(cleanDesc[0]) + cleanDesc.Substring(1);
                }

                var category = InferCategory(cleanDesc);

                return new ParsedExpenseIntent
                {
                    Intent = "ADD_EXPENSE",
                    Description = cleanDesc,
                    Amount = pixAmount,
                    Installments = 1,
                    Category = category,
                    PaymentMethod = "PIX_CAIXINHA",
                    TransactionDate = targetDate
                };
            }
        }

        // 6. Regular Card Expense parsing
        // Matches patterns like:
        // "uber 25", "uber 24,90", "comprei jogo 150 em 3x", "teclado 300 3x", "almoço 45.00"
        var expenseRegex = new Regex(
            @"(?<desc>[\p{L}\p{N}\s\-]+?)\s+(?:r\$\s*)?(?<amount>[0-9]+(?:[\.,][0-9]{1,2})?)(?:\s+(?:em\s+)?(?<inst>[0-9]{1,2})x)?$",
            RegexOptions.IgnoreCase
        );

        var match = expenseRegex.Match(text);
        if (match.Success)
        {
            var rawDesc = match.Groups["desc"].Value.Trim();
            var rawAmount = match.Groups["amount"].Value.Replace(',', '.');
            var rawInst = match.Groups["inst"].Value;

            if (decimal.TryParse(rawAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) && amount > 0)
            {
                var installments = 1;
                if (!string.IsNullOrEmpty(rawInst) && int.TryParse(rawInst, out var instVal))
                {
                    installments = Math.Max(1, instVal);
                }

                // Clean description prefixes like "comprei um", "paguei", etc.
                var cleanDesc = Regex.Replace(rawDesc, @"^(comprei(?:\s+um|\s+uma)?|paguei(?:\s+o|\s+a)?|gastei\s+com|gastei)\s+", "", RegexOptions.IgnoreCase).Trim();
                if (string.IsNullOrWhiteSpace(cleanDesc))
                {
                    cleanDesc = "Despesa";
                }

                cleanDesc = char.ToUpperInvariant(cleanDesc[0]) + cleanDesc.Substring(1);

                var category = InferCategory(cleanDesc);

                return new ParsedExpenseIntent
                {
                    Intent = "ADD_EXPENSE",
                    Description = cleanDesc,
                    Amount = amount,
                    Installments = installments,
                    Category = category,
                    PaymentMethod = "CARD",
                    TransactionDate = targetDate
                };
            }
        }

        return new ParsedExpenseIntent { Intent = "UNKNOWN" };
    }

    public static string InferCategory(string description)
    {
        var d = description.ToLowerInvariant();

        if (Regex.IsMatch(d, @"\b(uber|99|taxi|onibus|ônibus|metro|metrô|gasolina|combustivel|combustível|estacionamento|pedagio|pedágio)\b"))
            return "Transporte";

        if (Regex.IsMatch(d, @"\b(almoço|almoco|jantar|lanche|ifood|rappi|mercado|supermercado|pizza|hamburguer|padaria|restaurante|cafe|café|comida|açougue)\b"))
            return "Alimentação";

        if (Regex.IsMatch(d, @"\b(cinema|jogo|jogos|steam|playstation|xbox|bar|cerveja|balada|show|teatro|festa|lazer|viagem)\b"))
            return "Lazer";

        if (Regex.IsMatch(d, @"\b(netflix|spotify|amazon\s*prime|youtube|gympass|smartfit|apple|disney|hbo|max|assinatura)\b"))
            return "Assinaturas";

        if (Regex.IsMatch(d, @"\b(farmacia|farmácia|remedio|remédio|medico|médico|consulta|exame|dentista)\b"))
            return "Saúde";

        if (Regex.IsMatch(d, @"\b(roupa|tenis|tênis|livro|eletronico|teclado|mouse|monitor|shopee|aliexpress|mercado\s*livre|amazon)\b"))
            return "Compras";

        return "Outros";
    }
}
