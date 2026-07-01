namespace OpsFlow.Core.Models;

public enum VendorCategory { Agency, Technology, Media, Production, Consulting, Other }

public class Vendor
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public VendorCategory Category { get; set; }
    public DateTime ContractStart { get; set; }
    public DateTime ContractEnd { get; set; }
    public string PaymentTerms { get; set; } = "Net30";
    public decimal TotalContractValue { get; set; }
    public List<Invoice> Invoices { get; set; } = new();
}

public enum InvoiceStatus { Pending, Approved, Paid, Overdue, Cancelled }

public class Invoice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string VendorId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public List<string> LineItems { get; set; } = new();
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
