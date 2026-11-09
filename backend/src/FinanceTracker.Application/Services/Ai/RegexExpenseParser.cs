using System.Globalization;
using System.Text.RegularExpressions;
using FinanceTracker.Domain.Http.Groq.Models;

namespace FinanceTracker.Application.Services.Ai;

public static class RegexExpenseParser
{
    public static ParsedExpenseIntentModel Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ParsedExpenseIntentModel { Intent = "UNKNOWN" };
        }

        var text = input.Trim().ToLowerInvariant();

        if (text == "status" || text == "/status" || text.Contains("como ta minha fatura") || text.Contains("como está minha fatura") || text == "resumo")
        {
            return new ParsedExpenseIntentModel { Intent = "STATUS" };
        }

        if (text == "desfazer" || text == "/desfazer" || text == "apagar ultimo" || text == "desfazer ultimo" || text == "apagar último" || text == "desfaz")
        {
            return new ParsedExpenseIntentModel { Intent = "UNDO" };
        }

        if (text == "paguei a fatura" || text == "fatura paga" || text == "/pagarfatura" || text == "pagar fatura" || text == "quitei a fatura" || text == "quitei o cartao")
        {
            return new ParsedExpenseIntentModel { Intent = "PAY_INVOICE" };
        }

        if (text == "investir sobra" || text == "reinvestir sobra" || text == "guardar sobra" || text == "/investirsobra" || text == "aplicar sobra" || text.Contains("investir a sobra"))
        {
            return new ParsedExpenseIntentModel { Intent = "INVEST_SURPLUS" };
        }

        var syncMatch = Regex.Match(text, @"^(?:ajustar\s+fatura|sincronizar\s+fatura|fatura\s+atual|fatura)\s+(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)$", RegexOptions.IgnoreCase);
        if (syncMatch.Success && decimal.TryParse(syncMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var syncAmount) && syncAmount >= 0)
        {
            return new ParsedExpenseIntentModel
            {
                Intent = "SYNC_INVOICE",
                Amount = syncAmount,
                Description = "Ajuste de Fatura"
            };
        }

        var caixinhaMatch = Regex.Match(text, @"^(?:saldo\s+caixinha|atualizar\s+caixinha|saldo\s+da\s+caixinha|caixinha\s+saldo)\s+(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)$", RegexOptions.IgnoreCase);
        if (caixinhaMatch.Success && decimal.TryParse(caixinhaMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var caixinhaBalance) && caixinhaBalance >= 0)
        {
            return new ParsedExpenseIntentModel
            {
                Intent = "UPDATE_CAIXINHA",
                Amount = caixinhaBalance,
                Description = "Saldo Caixinha"
            };
        }

        var depositMatch = Regex.Match(
            text,
            @"^(?:(?:\+|\/)?\s*(?:adicionar|guardar|colocar|depositar|botar|\+)\s*(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)\s*(?:reais\s*)?(?:na\s+caixinha|pra\s+caixinha|a\s+caixinha|caixinha)?|caixinha\s*\+\s*(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?))$",
            RegexOptions.IgnoreCase
        );
        if (depositMatch.Success)
        {
            var rawVal = depositMatch.Groups[1].Success ? depositMatch.Groups[1].Value : depositMatch.Groups[2].Value;
            if (decimal.TryParse(rawVal.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var depositAmount) && depositAmount > 0)
            {
                return new ParsedExpenseIntentModel
                {
                    Intent = "DEPOSIT_CAIXINHA",
                    Amount = depositAmount,
                    Description = "Depósito Caixinha"
                };
            }
        }

        var receiveMatch = Regex.Match(
            text,
            @"^(?:recebi|pix\s+recebido|reembolso|recebido|recebimento)\s+(?:(?:de\s+)?(?:reembolso|pix)\s+)?(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)(?:\s+(.*))?$",
            RegexOptions.IgnoreCase
        );
        if (receiveMatch.Success)
        {
            var rawVal = receiveMatch.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(rawVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var recAmount) && recAmount > 0)
            {
                var cleanDesc = receiveMatch.Groups[2].Success ? receiveMatch.Groups[2].Value.Trim() : "";
                while (Regex.IsMatch(cleanDesc, @"^(?:no\s+pix|pelo\s+pix|do\s+pix|pix|de\s+reembolso|reembolso|de|do|da|com|em|para|pro)\s+", RegexOptions.IgnoreCase))
                {
                    cleanDesc = Regex.Replace(cleanDesc, @"^(?:no\s+pix|pelo\s+pix|do\s+pix|pix|de\s+reembolso|reembolso|de|do|da|com|em|para|pro)\s+", "", RegexOptions.IgnoreCase).Trim();
                }

                if (string.IsNullOrWhiteSpace(cleanDesc))
                {
                    cleanDesc = "Pix Recebido";
                }
                else
                {
                    cleanDesc = "Pix " + (char.ToUpperInvariant(cleanDesc[0]) + cleanDesc.Substring(1));
                }

                return new ParsedExpenseIntentModel
                {
                    Intent = "RECEIVE_PIX",
                    Amount = recAmount,
                    Description = cleanDesc,
                    Category = "Entrada",
                    PaymentMethod = "PIX_INFLOW"
                };
            }
        }

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

                return new ParsedExpenseIntentModel
                {
                    Intent = "SIMULATE",
                    Amount = simAmount,
                    Installments = installments,
                    Description = "Simulação"
                };
            }
        }

        DateTime? targetDate = null;
        if (text.Contains("ontem"))
        {
            targetDate = DateTime.Now.AddDays(-1);
            text = text.Replace("ontem", "").Trim();
        }

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

                return new ParsedExpenseIntentModel
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

                var cleanDesc = Regex.Replace(rawDesc, @"^(comprei(?:\s+um|\s+uma)?|paguei(?:\s+o|\s+a)?|gastei\s+com|gastei)\s+", "", RegexOptions.IgnoreCase).Trim();
                if (string.IsNullOrWhiteSpace(cleanDesc))
                {
                    cleanDesc = "Despesa";
                }

                cleanDesc = char.ToUpperInvariant(cleanDesc[0]) + cleanDesc.Substring(1);
                var category = InferCategory(cleanDesc);

                return new ParsedExpenseIntentModel
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

        return new ParsedExpenseIntentModel { Intent = "UNKNOWN" };
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
