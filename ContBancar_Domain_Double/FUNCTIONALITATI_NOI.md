# Funcționalități Noi Adăugate la Clasa Account

## 📋 Rezumat

Am extins clasa `Account.cs` cu multiple funcționalități noi și am adăugat **5 teste Mock** în `AccountTest.cs` pentru a demonstra conceptul de **Mock Object**.

---

## 🆕 Funcționalități Adăugate în Account.cs

### 1. **Interfață INotificationService** (pentru teste Mock)
```csharp
public interface INotificationService
{
    void SendEmail(string recipient, string subject, string message);
    void SendSms(string phoneNumber, string message);
    void LogActivity(string accountId, string activity);
}
```

### 2. **Model Transaction** (istoric tranzacții)
Fiecare tranzacție conține:
- Data și ora
- Tipul (Deposit, Withdraw, Transfer, Interest)
- Suma
- Soldul după tranzacție
- Descriere

### 3. **Noi Proprietăți**
- `accountId` - ID unic generat automat (GUID)
- `transactionHistory` - istoric complet al tuturor tranzacțiilor
- `dailyWithdrawLimit` - limită de retragere zilnică (10,000 RON default)
- `totalWithdrawnToday` - contor pentru retragerile zilnice
- `interestRate` - rata dobânzii anuale (2% default)

### 4. **Funcții Noi**

#### a) Gestionarea Istoricului
- `GetTransactionsByDateRange(startDate, endDate)` - filtrează tranzacții după perioadă
- `GetTransactionsByType(type)` - filtrează după tip
- `GetTotalDeposits()` - suma totală depusă
- `GetTotalWithdrawals()` - suma totală retrasă

#### b) Calcul Dobândă
- `CalculateInterest(daysCount)` - calculează dobânda simplă
- `ApplyInterest(daysCount)` - aplică dobânda la sold

#### c) Utilități
- `HasSufficientBalance(amount)` - verifică dacă există sold suficient
- `GenerateAccountReport()` - generează raport detaliat al contului
- Proprietate `DailyWithdrawLimit` - configurabilă

#### d) Notificări Automate (pentru Mock testing)
- Email pentru depuneri > 50,000 RON
- SMS pentru retrageri > 5,000 RON
- Logging pentru toate activitățile

### 5. **Limită de Retragere Zilnică**
- Verificare automată la fiecare retragere directă
- Resetare automată la schimbarea zilei
- **NU se aplică** la transferuri între conturi

---

## 🎭 Ce Este un MOCK Object?

### Diferența dintre STUB și MOCK:

| **TEST STUB** | **MOCK OBJECT** |
|---------------|-----------------|
| ✅ Returnează valori pre-configurate | ✅ Verifică că metodele sunt apelate |
| ✅ Înlocuiește dependențe externe | ✅ Verifică ordinea apelurilor |
| ❌ NU verifică interacțiunile | ✅ Verifică parametrii apelurilor |
| Exemplu: `CurrencyConverterStub` | Exemplu: `Mock<INotificationService>` |

### Tipuri de Mock:
1. **Lenient** (implicit) - tolerează apeluri neașteptate
2. **Strict** - aruncă excepție pentru apeluri neașteptate

---

## 🧪 Teste Mock Adăugate (5 teste)

### Test 1: `Deposit_LargeAmount_ShouldCallSendEmail()`
**Scop:** Verifică că se trimite email pentru depuneri mari (> 50,000 RON)

**Verificări Mock:**
- `SendEmail` apelat EXACT o dată
- Parametrii corecți (recipient, subject, message conține suma)
- `LogActivity` apelat pentru logging

### Test 2: `Withdraw_LargeAmount_ShouldCallSendSms()`
**Scop:** Verifică că se trimite SMS pentru retrageri mari (> 5,000 RON)

**Verificări Mock:**
- `SendSms` apelat EXACT o dată
- Număr de telefon corect
- Message conține suma retrasă

### Test 3: `GenerateAccountReport_ShouldCallLogActivity()`
**Scop:** Verifică că se loghează generarea raportului

**Verificări Mock:**
- `LogActivity` apelat cu mesajul corect
- Raportul conține informații relevante

### Test 4: `Deposit_SmallAmount_ShouldNotCallSendEmail()` ⭐
**Scop:** Verifică că email-ul NU este trimis pentru depuneri mici

**Verificări Mock:**
- `SendEmail` apelat NICIODATĂ (`Times.Never`)
- `LogActivity` apelat totuși (pentru orice depunere)

### Test 5: `Deposit_ShouldCallMethodsInCorrectOrder()` ⭐⭐
**Scop:** Verifică ORDINEA STRICTĂ a apelurilor

**Caracteristici:**
- `MockBehavior.Strict` - aruncă excepție pentru apeluri neașteptate
- `MockSequence` - definește ordinea exactă așteptată
- Verifică că `LogActivity` este apelat ÎNAINTE de `SendEmail`

---

## 🚀 Cum să Rulezi Testele

### Toate testele:
```bash
dotnet test
```

### Doar testele Mock (prin nume):
```bash
dotnet test --filter "Deposit_LargeAmount"
dotnet test --filter "Withdraw_LargeAmount"
dotnet test --filter "GenerateAccountReport"
```

### Rezultat Așteptat:
```
Test summary: total: 26, failed: 0, succeeded: 26, skipped: 0
```

---

## 📊 Exemplu de Utilizare

### Cont cu Notificări (Mock):
```csharp
var mockNotification = new Mock<INotificationService>();
var converter = new CurrencyConverterStub(5.0f);
var account = new Account(10000, converter, mockNotification.Object);

// Depunere mare -> va trimite email
account.Deposit(60000);

// Verifică că email-ul a fost trimis
mockNotification.Verify(
    m => m.SendEmail(
        "owner@example.com",
        "Depunere mare",
        It.Is<string>(msg => msg.Contains("60000"))
    ),
    Times.Once
);
```

### Funcționalități Noi:
```csharp
// Istoric tranzacții
var transactions = account.TransactionHistory;

// Calcul dobândă
float interest = account.CalculateInterest(30); // pentru 30 zile
account.ApplyInterest(30); // aplică dobânda

// Raport cont
string report = account.GenerateAccountReport();
Console.WriteLine(report);

// Verificare sold
bool canWithdraw = account.HasSufficientBalance(5000);

// Filtrare tranzacții
var deposits = account.GetTransactionsByType("Deposit");
var recentTransactions = account.GetTransactionsByDateRange(
    DateTime.Now.AddDays(-7),
    DateTime.Now
);
```

---

## 🎓 Concepte Învățate

1. **Dependency Injection** - injectarea dependențelor prin constructor
2. **Test Doubles** - Stub vs Mock
3. **Moq Framework** - framework popular pentru Mock în C#
4. **Verification** - verificarea interacțiunilor, nu doar rezultatelor
5. **MockBehavior.Strict** - comportament strict pentru teste
6. **Times** - specificarea numărului de apeluri așteptat (Once, Never, AtLeast, etc.)
7. **It.Is<T>** - verificare parametri cu predicate custom
8. **MockSequence** - verificarea ordinii apelurilor

---

## 📚 Resurse

- [Moq Documentation](https://github.com/moq/moq4)
- [Test Doubles - Martin Fowler](https://martinfowler.com/bliki/TestDouble.html)
- [NUnit Documentation](https://docs.nunit.org/)

---

**✨ Autor:** GitHub Copilot
**📅 Data:** Noiembrie 2025
