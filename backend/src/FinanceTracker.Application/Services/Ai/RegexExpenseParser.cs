using System.Globalization;
using System.Text.RegularExpressions;
using FinanceTracker.Domain.Http.Groq.Models;

namespace FinanceTracker.Application.Services.Ai;

public static class RegexExpenseParser
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    private static readonly Regex PixExclusionRegex = new(
        @"\b(tipo\s+de\s+transfer[eê]ncia\s+pix|pix\s+enviado|comprovante\s+(?:de\s+)?(?:transfer[eê]ncia\s+)?pix|transfer[eê]ncia\s+pix|chave\s+pix|pix\s+para)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex ChatCommandPrefixRegex = new(
        @"^(?:/|)?(?:status|resumo|desfazer|apagar|paguei|fatura\s+paga|pagarfatura|pagar\s+fatura|quitei|investir|reinvestir|guardar|aplicar|ajustar\s+fatura|sincronizar\s+fatura|fatura\s+atual|saldo\s+caixinha|atualizar\s+caixinha|caixinha|adicionar|colocar|depositar|botar|recebi|pix\s+recebido|reembolso|recebido|recebimento|simular|simula)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex CardKeywordRegex = new(
        @"\b(compra\s+aprovada|compra\s+no\s+cart[ãa]o|cart[ãa]o|cr[eé]dito|apple\s*pay|ifood|uber|mercado\s*livre|amazon|pedido|fatura)\b|\b(?:(?:em|parcelado\s+em)\s+\d{1,2}\s*x|\d{1,2}\s*x\s+(?:de\s+r\$|sem\s+juros))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex CurrencyOrAmountRegex = new(
        @"r\$\s*\d|\b(?:total|valor)\s*:?\s*\d+(?:[.,]\d{1,2})?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex NotificationAmountPrimaryRegex = new(
        @"(?:valor(?:\s+de)?|total(?:\s+do\s+pedido)?(?:\s*:)?|compra\s+de|de)\s*r\$\s*(\d{1,3}(?:\.\d{3})*(?:,\d{1,2})?|\d+(?:[.,]\d{1,2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex NotificationAmountCurrencyRegex = new(
        @"r\$\s*(\d{1,3}(?:\.\d{3})*(?:,\d{1,2})?|\d+(?:[.,]\d{1,2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex NotificationAmountLabelRegex = new(
        @"\b(?:total(?:\s+do\s+pedido)?|valor)\s*:?\s*(\d{1,3}(?:\.\d{3})*(?:,\d{1,2})?|\d+(?:[.,]\d{1,2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex InstallmentPatternRegex = new(
        @"\b(?:(?:em|parcelado\s+em)\s+(\d{1,2})\s*x|(\d{1,2})\s*x\s+(?:de\s+r\$|sem\s+juros|no\s+cart[ãa]o))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex NotificationMerchantEmRegex = new(
        @"\b(?:aprovad[ao]\s+(?:no\s+(?:seu\s+)?cart[ãa]o(?:\s+de\s+cr[eé]dito)?(?:\s+final\s+\d+)?\s+)?em|compra\s+(?:de\s+r\$\s*[\d.,]+\s+)?(?:em\s+\d{1,2}\s*x\s+)?(?:aprovad[ao]\s+)?em)\s+(?!\d+\s*x\b)(?<merchant>[A-Za-zÀ-ÖØ-öø-ÿ0-9\s\.\*\-&']{2,60})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex MerchantCutoffRegex = new(
        @"^(.*?)(?:\s+(?:no\s+valor(?:\s+de)?|no\s+(?:seu\s+)?cart[ãa]o(?:\s+de\s+cr[eé]dito)?|com\s+(?:o\s+)?(?:seu\s+)?cart[ãa]o|pago\s+com|final\s+\d+|de\s+r\$|r\$|[àa]s\s+\d{2}:\d{2}|\d{2}:\d{2}|\.|\r|\n|$))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex InvalidMerchantCandidateRegex = new(
        @"^(?:\d+|\d+\s*(?:min|minutos|seg|segundos|h|horas|dias)|andamento|preparo|prepara[çc][ãa]o|rota|breve|cart[ãa]o)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly (Regex Pattern, string CanonicalName)[] KnownAppPatterns =
    {
        (new Regex(@"\bifood\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "iFood"),
        (new Regex(@"\buber(?:\s*\*trip|\s*eats)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Uber"),
        (new Regex(@"\bmercado\s*livre\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Mercado Livre"),
        (new Regex(@"\bamazon(?:\s*prime)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Amazon"),
        (new Regex(@"\brappi\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Rappi"),
        (new Regex(@"\b99(?:\s*app|\s*pop|\s*taxi)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "99"),
        (new Regex(@"\bmcdonald'?s\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "McDonald's"),
        (new Regex(@"\bburger\s*king\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Burger King"),
        (new Regex(@"\bspotify\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Spotify"),
        (new Regex(@"\bnetflix\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Netflix")
    };

    private static readonly (string Prefix, string CanonicalName)[] CanonicalMerchantPrefixes =
    {
        ("ifood", "iFood"),
        ("uber", "Uber"),
        ("mercado livre", "Mercado Livre"),
        ("amazon", "Amazon"),
        ("rappi", "Rappi"),
        ("mcdonald", "McDonald's"),
        ("burger king", "Burger King"),
        ("spotify", "Spotify"),
        ("netflix", "Netflix")
    };

    public static bool IsReceiptText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        return Regex.IsMatch(input, @"\b(comprovante|transfer[eê]ncia|destino\s+nome|tipo\s+de\s+transfer[eê]ncia)\b", RegexOptions.IgnoreCase)
            || (Regex.IsMatch(input, @"r\$\s*\d", RegexOptions.IgnoreCase) && Regex.IsMatch(input, @"\b(nome|destino|favorecido|recebedor|pix)\b", RegexOptions.IgnoreCase));
    }

    public static bool IsCardOrCheckoutText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (PixExclusionRegex.IsMatch(text)) return false;
        if (ChatCommandPrefixRegex.IsMatch(text.Trim())) return false;

        // Also do not classify as card if it explicitly mentions Pix receipt/transfer keywords
        if (Regex.IsMatch(text, @"\bpix\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(text, @"\b(cart[ãa]o|cr[eé]dito|apple\s*pay)\b", RegexOptions.IgnoreCase))
        {
            return false;
        }

        return CardKeywordRegex.IsMatch(text);
    }

    public static bool IsNotificationOrCheckoutText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        return IsCardOrCheckoutText(input) && CurrencyOrAmountRegex.IsMatch(input);
    }

    public static ParsedExpenseIntentModel? ParseNotificationOrCheckout(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // 1. Amount Extraction (supports both decimal "45,90" and integer "150")
        var amountMatch = NotificationAmountPrimaryRegex.Match(input);
        if (!amountMatch.Success)
        {
            amountMatch = NotificationAmountCurrencyRegex.Match(input);
        }
        if (!amountMatch.Success)
        {
            amountMatch = NotificationAmountLabelRegex.Match(input);
        }

        if (!amountMatch.Success)
        {
            return null;
        }

        var rawAmount = amountMatch.Groups[1].Value.Trim();
        if (rawAmount.Contains('.') && rawAmount.Contains(','))
        {
            rawAmount = rawAmount.Replace(".", "").Replace(',', '.');
        }
        else if (rawAmount.Contains(','))
        {
            rawAmount = rawAmount.Replace(',', '.');
        }

        if (!decimal.TryParse(rawAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            return null;
        }

        // 2. Installments extraction (only explicit installment patterns, avoiding "2x Hambúrguer")
        int installments = 1;
        var instMatch = InstallmentPatternRegex.Match(input);
        if (instMatch.Success)
        {
            var instStr = instMatch.Groups[1].Success ? instMatch.Groups[1].Value : instMatch.Groups[2].Value;
            if (int.TryParse(instStr, out var inst) && inst > 0)
            {
                installments = inst;
            }
        }

        // 3. Merchant / Description extraction
        string? merchant = null;

        // Pattern A: Look for explicit purchase notification "aprovada em <LOJA>" or "compra ... em <LOJA>"
        var emMatch = NotificationMerchantEmRegex.Match(input);
        if (emMatch.Success)
        {
            var candidate = emMatch.Groups["merchant"].Value.Trim();
            var cutoff = MerchantCutoffRegex.Match(candidate);

            if (cutoff.Success && !string.IsNullOrWhiteSpace(cutoff.Groups[1].Value))
            {
                candidate = cutoff.Groups[1].Value.Trim();
            }

            candidate = candidate.TrimEnd('.', ',', '-', ' ', ':', '*');
            if (!string.IsNullOrWhiteSpace(candidate)
                && candidate.Length >= 2
                && !InvalidMerchantCandidateRegex.IsMatch(candidate))
            {
                merchant = candidate;
            }
        }

        // Pattern B: Known major app/merchant fallback
        if (string.IsNullOrWhiteSpace(merchant))
        {
            foreach (var (pattern, canonicalName) in KnownAppPatterns)
            {
                if (pattern.IsMatch(input))
                {
                    merchant = canonicalName;
                    break;
                }
            }
        }

        // Pattern C: Explicit store or order label
        if (string.IsNullOrWhiteSpace(merchant))
        {
            var storeMatch = Regex.Match(input, @"\b(?:estabelecimento|loja|restaurante|fornecedor|vendedor)\s*:?\s*([A-Za-zÀ-ÖØ-öø-ÿ0-9\s\.\*\-&']{2,60})", RegexOptions.IgnoreCase);
            if (storeMatch.Success)
            {
                var candidate = storeMatch.Groups[1].Value.Trim().TrimEnd('.', ',', '-', ' ', ':');
                if (!InvalidMerchantCandidateRegex.IsMatch(candidate))
                {
                    merchant = candidate;
                }
            }
        }

        merchant = FormatMerchantName(merchant ?? "Compra Cartão");
        var category = InferCategory(merchant);

        return new ParsedExpenseIntentModel
        {
            Intent = "ADD_EXPENSE",
            Description = merchant,
            Amount = amount,
            Installments = installments,
            Category = category,
            PaymentMethod = "CARD",
            Confidence = 0.95m
        };
    }

    private static string FormatMerchantName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Compra Cartão";
        var clean = Regex.Replace(raw.Trim(), @"\s+", " ").TrimEnd('.', ',', '-', ' ', ':', '*');

        foreach (var (prefix, canonical) in CanonicalMerchantPrefixes)
        {
            if (clean.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase)
                || clean.StartsWith(prefix + "'", StringComparison.OrdinalIgnoreCase))
            {
                return canonical;
            }
        }

        if (clean.Length > 3 && clean == clean.ToUpperInvariant())
        {
            return PtBrCulture.TextInfo.ToTitleCase(clean.ToLowerInvariant());
        }

        if (char.IsLower(clean[0]))
        {
            return char.ToUpperInvariant(clean[0]) + clean.Substring(1);
        }

        return clean;
    }

    public static ParsedExpenseIntentModel? ParseReceipt(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        // 1. Amount Extraction
        // Prioritize explicit currency markers: "Valor R$ XX,XX" or "Valor da transferência R$ XX,XX"
        var amountMatch = Regex.Match(input, @"(?:valor(?:\s+da\s+transfer[eê]ncia)?\s*:?\s*)r\$\s*(\d{1,3}(?:\.\d{3})*,\d{2}|\d+[.,]\d{2})", RegexOptions.IgnoreCase);
        if (!amountMatch.Success)
        {
            // Any "R$ XX,XX"
            amountMatch = Regex.Match(input, @"r\$\s*(\d{1,3}(?:\.\d{3})*,\d{2}|\d+[.,]\d{2})", RegexOptions.IgnoreCase);
        }
        if (!amountMatch.Success)
        {
            // "Valor: XX,XX"
            amountMatch = Regex.Match(input, @"valor\s*:?\s*(\d{1,3}(?:\.\d{3})*,\d{2}|\d+[.,]\d{2})", RegexOptions.IgnoreCase);
        }

        if (!amountMatch.Success)
        {
            return null;
        }

        var rawAmount = amountMatch.Groups[1].Value.Trim();
        if (rawAmount.Contains('.') && rawAmount.Contains(','))
        {
            rawAmount = rawAmount.Replace(".", "").Replace(',', '.');
        }
        else if (rawAmount.Contains(','))
        {
            rawAmount = rawAmount.Replace(',', '.');
        }

        if (!decimal.TryParse(rawAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            return null;
        }

        // 2. Recipient / Description Extraction
        string description = "Despesa OCR";

        var recipientMatch = Regex.Match(
            input,
            @"\b(?:Destino\s+Nome|Nome\s+do\s+destinat[aá]rio|Nome\s+do\s+recebedor|Nome\s+do\s+favorecido|Destinat[aá]rio|Favorecido|Recebedor)\s*:?\s*([A-Za-zÀ-ÖØ-öø-ÿ0-9\s\.\-&]{2,80})",
            RegexOptions.IgnoreCase
        );

        if (!recipientMatch.Success)
        {
            recipientMatch = Regex.Match(
                input,
                @"\bNome\s*:?\s*([A-Za-zÀ-ÖØ-öø-ÿ0-9\s\.\-&]{2,80})",
                RegexOptions.IgnoreCase
            );
        }

        if (!recipientMatch.Success)
        {
            recipientMatch = Regex.Match(
                input,
                @"\b(?:Destino|Para)\s*:?\s*([A-Za-zÀ-ÖØ-öø-ÿ0-9\s\.\-&]{2,80})",
                RegexOptions.IgnoreCase
            );
        }

        if (recipientMatch.Success)
        {
            var rawName = recipientMatch.Groups[1].Value.Trim();

            var cutoff = Regex.Match(
                rawName,
                @"^(.*?)(?:\s+(?:CPF|CNPJ|Chave(?:\s+pix)?|Institui[çc][ãa]o|Banco|Conta(?:\s+corrente)?|Ag[êe]ncia|Tipo|\d{3}\.\d{3}|\r|\n|$))",
                RegexOptions.IgnoreCase
            );

            if (cutoff.Success && !string.IsNullOrWhiteSpace(cutoff.Groups[1].Value))
            {
                rawName = cutoff.Groups[1].Value.Trim();
            }

            rawName = Regex.Replace(rawName, @"^(?:Destino\s+Nome|Nome|Para)\s*:?\s*", "", RegexOptions.IgnoreCase).Trim();
            rawName = rawName.TrimEnd('.', ',', '-', ' ', ':');

            if (!string.IsNullOrWhiteSpace(rawName) && rawName.Length >= 2)
            {
                description = rawName;
            }
        }

        // 3. Date extraction if present (e.g. "09 NOV 2026")
        DateTime? transactionDate = null;
        var dateMatch = Regex.Match(input, @"(\d{1,2})\s+(JAN|FEV|MAR|ABR|MAI|JUN|JUL|AGO|SET|OUT|NOV|DEZ)\w*\s+(\d{4})", RegexOptions.IgnoreCase);
        if (dateMatch.Success)
        {
            var day = int.Parse(dateMatch.Groups[1].Value);
            var monthStr = dateMatch.Groups[2].Value.ToUpperInvariant();
            var year = int.Parse(dateMatch.Groups[3].Value);
            var month = monthStr switch
            {
                "JAN" => 1, "FEV" => 2, "MAR" => 3, "ABR" => 4,
                "MAI" => 5, "JUN" => 6, "JUL" => 7, "AGO" => 8,
                "SET" => 9, "OUT" => 10, "NOV" => 11, "DEZ" => 12,
                _ => DateTime.Now.Month
            };
            try
            {
                transactionDate = new DateTime(year, month, day);
            }
            catch { }
        }

        // 4. Payment method inference from receipt
        var isPix = Regex.IsMatch(input, @"\b(pix|caixinha|transfer[eê]ncia)\b", RegexOptions.IgnoreCase);
        var paymentMethod = isPix ? "PIX_CAIXINHA" : "CARD";

        // 5. Category inference
        var category = InferCategory(description);

        return new ParsedExpenseIntentModel
        {
            Intent = "ADD_EXPENSE",
            Description = description,
            Amount = amount,
            Installments = 1,
            Category = category,
            PaymentMethod = paymentMethod,
            TransactionDate = transactionDate,
            Confidence = 0.95m
        };
    }

    public static ParsedExpenseIntentModel Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ParsedExpenseIntentModel { Intent = "UNKNOWN" };
        }

        // Check if input is a structured receipt / transfer dump first
        if (IsReceiptText(input))
        {
            var receipt = ParseReceipt(input);
            if (receipt != null && receipt.Amount > 0)
            {
                return receipt;
            }
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

        if (Regex.IsMatch(text, @"^(?:paguei\s+a\s+fatura|fatura\s+paga|/pagarfatura|pagar\s+fatura|quitei\s+a\s+fatura|quitei\s+o\s+cart[ãa]o)(?:\s+(?:de\s+)?(?:r\$\s*)?[0-9]+(?:[\.,][0-9]{1,2})?)?$", RegexOptions.IgnoreCase))
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

        var simMatch = Regex.Match(text, @"^(?:/|)?simula(?:r)?\s+(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)(?:\s+(?:em\s+)?([0-9]{1,2})x?)?", RegexOptions.IgnoreCase);
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

        // Check if input is a purchase notification or order checkout screen (after chat commands)
        if (IsNotificationOrCheckoutText(input))
        {
            var notif = ParseNotificationOrCheckout(input);
            if (notif != null && notif.Amount > 0)
            {
                return notif;
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
            var amountMatch = text.Contains("r$")
                ? Regex.Match(text, @"r\$\s*([0-9]+(?:[\.,][0-9]{1,2})?)", RegexOptions.IgnoreCase)
                : Regex.Match(text, @"(?:r\$\s*)?([0-9]+(?:[\.,][0-9]{1,2})?)", RegexOptions.IgnoreCase);

            if (amountMatch.Success && decimal.TryParse(amountMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var pixAmount) && pixAmount > 0)
            {
                var remaining = text.Remove(amountMatch.Index, amountMatch.Length).Trim();
                var cleanDesc = Regex.Replace(remaining, @"\b(comprovante\s+de\s+transfer[eê]ncia\s+pix|comprovante\s+de\s+transfer[eê]ncia|comprovante\s+pix|comprovante|transfer[eê]ncia\s+pix\s+realizada|transfer[eê]ncia\s+realizada|transfer[eê]ncia\s+pix|transfer[eê]ncia|realizada|enviada|enviado|pago\s+com\s+pix|pago|tirei\s+da\s+caixinha\s+pro\s+pix|tirei\s+da\s+caixinha|retirar\s+da\s+caixinha|da\s+caixinha|na\s+caixinha|caixinha|no\s+pix|pro\s+pix|pelo\s+pix|pix|comprei(?:\s+um|\s+uma)?|paguei(?:\s+o|\s+a)?|gastei\s+com|gastei|destinat[aá]rio|recebedor|com|em|no|na|de|do|da|o|a|pro|para)\b", " ", RegexOptions.IgnoreCase);
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

        if (Regex.IsMatch(d, @"\b(uber|99|taxi|onibus|ônibus|metro|metrô|gasolina|combustivel|combustível|estacionamento|pedagio|pedágio|posto|ipiranga|shell|br\s+distribuidora|abastecimento|etanol|diesel|sem\s*parar|veloe)\b"))
            return "Transporte";

        if (Regex.IsMatch(d, @"\b(almoço|almoco|jantar|lanche|ifood|rappi|mercado|supermercado|pizza|hamburguer|padaria|restaurante|cafe|café|comida|açougue|mcdonald|mcdonald's|mcdonalds|burger\s*king|bk|subway|habib|outback|coco\s*bambu|carrefour|extra|pao\s*de\s*acucar|pão\s*de\s*açúcar|assai|assaí|atacadão|atacadao)\b"))
            return "Alimentação";

        if (Regex.IsMatch(d, @"\b(cinema|jogo|jogos|steam|playstation|xbox|bar|cerveja|balada|show|teatro|festa|lazer|viagem|hotel|airbnb|ingresso)\b"))
            return "Lazer";

        if (Regex.IsMatch(d, @"\b(netflix|spotify|amazon\s*prime|youtube|gympass|smartfit|apple|disney|hbo|max|assinatura)\b"))
            return "Assinaturas";

        if (Regex.IsMatch(d, @"\b(farmacia|farmácia|remedio|remédio|medico|médico|consulta|exame|dentista|droga\s*raia|drogasil|pague\s*menos|panvel)\b"))
            return "Saúde";

        if (Regex.IsMatch(d, @"\b(roupa|tenis|tênis|livro|eletronico|teclado|mouse|monitor|shopee|aliexpress|mercado\s*livre|amazon|barbearia|cabelo|salao|salão|corte|magazine\s*luiza|magalu|zara|renner|riachuelo|casas\s*bahia)\b"))
            return "Compras";

        return "Outros";
    }
}
