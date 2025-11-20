
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

public class Transaction : Entity
{
    public Transaction()
    {
        Type = "Transaction";
    }

    public string TransactionType
    {
        get => Properties.TryGetValue("transaction_type", out var value) ? value?.ToString() ?? "payment" : "payment";
        set => Properties["transaction_type"] = value;
    }

    public decimal Amount
    {
        get => Properties.TryGetValue("amount", out var value) ? Convert.ToDecimal(value) : 0m;
        set => Properties["amount"] = value;
    }

    public string Currency
    {
        get => Properties.TryGetValue("currency", out var value) ? value?.ToString() ?? "USD" : "USD";
        set => Properties["currency"] = value;
    }

    public DateTime TransactionDate
    {
        get => Properties.TryGetValue("transaction_date", out var value) 
            ? DateTime.Parse(value?.ToString() ?? DateTime.UtcNow.ToString("O")) 
            : DateTime.UtcNow;
        set => Properties["transaction_date"] = value.ToString("O");
    }

    public string? Description
    {
        get => Properties.TryGetValue("description", out var value) ? value?.ToString() : null;
        set => Properties["description"] = value;
    }
}