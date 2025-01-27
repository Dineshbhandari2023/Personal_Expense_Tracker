﻿using System.Text.Json;
using System.Text;
using ExpenseTracker.Models;

public class TransactionService
{
    private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private static readonly string FolderPath = Path.Combine(DesktopPath, "LocalDB");
    private static readonly string TransactionFilePath = Path.Combine(FolderPath, "transactions.json");

    public List<Transaction> LoadTransactions()
    {
        if (!File.Exists(TransactionFilePath))
            return new List<Transaction>();

        var json = File.ReadAllText(TransactionFilePath);
        return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
    }

    public List<Transaction> LoadDebtTransactions()
    {
        if (!File.Exists(TransactionFilePath))
            return new List<Transaction>();

        var json = File.ReadAllText(TransactionFilePath);
        return JsonSerializer.Deserialize<List<Transaction>>(json).Where(transaction => transaction.Type == TransactionType.Debt).ToList() ?? new List<Transaction>();

    }


    public List<Transaction> GetTopRecentTransactions()
    {
        var allTransactions = LoadTransactions();
        var topFiveTransactions = allTransactions.OrderByDescending(transaction => transaction.Amount).Take(5).ToList();
        return topFiveTransactions;
    }

    public List<Transaction> GetTopFiveTransactions()
    {
        var allTransactions = LoadTransactions();
        var topFiveTransactions = allTransactions.OrderByDescending(transaction => transaction.Amount).Take(5).ToList();
        return topFiveTransactions;
    }
    public List<Transaction> GetPendingDebtTransactions()
    {
        var allTransactions = LoadTransactions();
        var unpaidDebtTransactions = allTransactions.Where(transaction => transaction.Type == TransactionType.Debt && transaction.Status == DebtStatus.Unpaid).ToList();
        return unpaidDebtTransactions;
    }

    public void AddCashTransaction(Transaction transaction)
    {
        var allTransactions = LoadTransactions();


        var trans = new Transaction
        {
            Id = allTransactions.Count > 0 ? allTransactions.Max(transaction => transaction.Id) + 1 : 1, // Increment ID
            Source = transaction.Source,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Tags = transaction.Tags,
            Type = transaction.Type, // CashIn or CashOut
            Notes = transaction.Notes
        };

        if (transaction.Type == TransactionType.CashOut)
        {
            if (transaction.Amount > GetTotalCash())
            {
                throw new InvalidOperationException("Insufficient funds");
            }
        }

        allTransactions.Add(trans);


        EnsureDirectoryExists();
        var json = JsonSerializer.Serialize(allTransactions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(TransactionFilePath, json);
    }

    public void AddDebtTransaction(Transaction transaction)
    {
        var allTransactions = LoadTransactions();
        var trans = new Transaction
        {
            Id = allTransactions.Count > 0 ? allTransactions.Max(transaction => transaction.Id) + 1 : 1, // Increment ID
            Source = transaction.Source,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Tags = transaction.Tags,
            Type = TransactionType.Debt,
            Notes = transaction.Notes,
            DueDate = transaction.DueDate,
            Status = DebtStatus.Unpaid
        };
        allTransactions.Add(trans);
        EnsureDirectoryExists();
        var json = JsonSerializer.Serialize(allTransactions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(TransactionFilePath, json);
    }

    public void RemoveDebtTransaction(Transaction transaction)
    {
        var allTransactions = LoadTransactions();
        var debtTrans = allTransactions.FirstOrDefault(debtTrans => debtTrans.Source == transaction.Source && debtTrans.DueDate == transaction.DueDate && debtTrans.Amount == transaction.Amount && debtTrans.Notes == transaction.Notes);
        if (debtTrans == null)
        {
            throw new InvalidOperationException("Debt transaction not found");
        }

        var totalCashInflow = new TransactionService().GetTotalCashInflow();
        if (totalCashInflow < debtTrans.Amount)
        {
            throw new InvalidOperationException("You do not have enough funds to pay off this debt.");
        }

        // allDebtTransactions.Remove(debtTrans);
        debtTrans.Status = DebtStatus.Paid;
        allTransactions[allTransactions.FindIndex((t) => t.Id == transaction.Id)] = debtTrans;

        EnsureDirectoryExists();
        var json = JsonSerializer.Serialize(allTransactions, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(TransactionFilePath, json);
    }

    public decimal GetTotalCashInflow()
    {
        var allTransactions = LoadTransactions();
        var totalCashInAmt = allTransactions.Where(transaction => transaction.Type == TransactionType.CashIn).Sum(transaction => transaction.Amount);
        var totalDebtAmt = GetClearedDebt();

        return totalCashInAmt - totalDebtAmt;
    }

    public decimal GetTotalCashOutflow()
    {
        var allTransactions = LoadTransactions();
        return allTransactions.Where(transaction => transaction.Type == TransactionType.CashOut).Sum(transaction => transaction.Amount);
    }

    public decimal GetTotalCash()
    {
        return GetTotalCashInflow() - GetTotalCashOutflow();
    }

    public decimal GetTotalDebt()
    {
        var allTransactions = LoadTransactions();
        return allTransactions.Where(transaction => transaction.Type == TransactionType.Debt).Sum(transaction => transaction.Amount);
    }

    public decimal GetClearedDebt()
    {
        var allTransactions = LoadTransactions();
        return allTransactions.Where(transaction => transaction.Type == TransactionType.Debt && transaction.Status == DebtStatus.Paid).Sum(transaction => transaction.Amount);
    }

    public decimal GetRemainingDebt()
    {
        return GetTotalDebt() - GetClearedDebt();
    }

    public int GetTotalTransactions()
    {
        var allTransactions = LoadTransactions();
        return allTransactions.Count;
    }

    // Ensure the directory for storage exists
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

    }
    public void AddTransactions(List<Transaction> transactions)
    {
        var allTransactions = LoadTransactions();
        allTransactions.AddRange(transactions);

        SaveTransactions(allTransactions);
    }

    public List<Transaction> GetAllTransactions()
    {
        return LoadTransactions();
    }

    public string ExportTransactionsToCsv()
    {
        var allTransactions = GetAllTransactions();
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Date,Amount,Type,Tags,Notes");

        foreach (var transaction in allTransactions)
        {
            var tags = string.Join(";", transaction.Tags ?? new List<string>());
            csvBuilder.AppendLine($"{transaction.Date:yyyy-MM-dd},{transaction.Amount},{transaction.Type},{tags},{transaction.Notes}");
        }

        return csvBuilder.ToString();
    }

    public List<Transaction> ImportTransactionsFromCsv(string csvContent)
    {
        var transactions = new List<Transaction>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1)) // Skip header row
        {
            var fields = line.Split(',');

            if (fields.Length >= 5)
            {
                transactions.Add(new Transaction
                {
                    Date = DateTime.Parse(fields[0]),
                    Amount = decimal.Parse(fields[1]),
                    Type = Enum.Parse<TransactionType>(fields[2]),
                    Tags = fields[3].Split(';').ToList(),
                    Notes = fields[4]
                });
            }
        }

        return transactions;
    }

    private void SaveTransactions(List<Transaction> transactions)
    {
        EnsureDirectoryExists();
        var json = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(TransactionFilePath, json);
    }
}