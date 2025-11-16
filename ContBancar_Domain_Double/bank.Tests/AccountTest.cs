using NUnit.Framework;
using Moq;

namespace bank
{
    // TEST STUB pentru Currency Converter
    // Implementare simpla care returneaza un curs fix pentru testare
    // Permite testarea functionalitatii fara dependenta de API-ul BNR
    public class CurrencyConverterStub : ICurrencyConverter
    {
        private float fixedRate;

        // Constructor care permite setarea unui curs fix pentru teste
        public CurrencyConverterStub(float eurToRonRate)
        {
            fixedRate = eurToRonRate;
        }

        // Returneaza cursul fix (nu face HTTP request la BNR)
        public float GetEurToRonRate()
        {
            return fixedRate;
        }
    }

    // Clasa care contine toate testele pentru contul bancar
    [TestFixture]
    [Description("Teste pentru operatiile contului bancar")]
    public class AccountTest
    {
        // Variabile pentru conturile sursa si destinatie folosite in teste
        private Account? sourceAccount;
        private Account? destinationAccount;

        // Aceasta functie ruleaza INAINTE de fiecare test
        [SetUp]
        public void FunctieInitializare()
        {
            // Resetam conturile la null pentru fiecare test
            sourceAccount = null;
            destinationAccount = null;
        }

        // Aceasta functie ruleaza DUPA fiecare test
        [TearDown]
        public void FunctieTerminare()
        {
            // Curatam memoria dupa test
            sourceAccount = null;
            destinationAccount = null;
        }

        // Test 1: Verifica ca soldul se initializeaza corect
        [Test, Category("pass")]
        [Description("Testeaza initializarea soldului contului")]
        public void Ctor_ShouldInitializeBalance()
        {
            // Cream un cont cu 500.000
            var acc = new Account(500000);
            // Verificam ca soldul este exact 500.000
            Assert.That(acc.Balance, Is.EqualTo(500000), "Soldul initial trebuie sa fie 500000");
            Assert.Pass("Testul de initializare a trecut cu succes");
        }

        // Test 2: Verifica ca transferul simplu functioneaza corect
        [Test, Category("pass")]
        [Description("Testeaza transferul simplu de fonduri intre conturi")]
        public void TransferFunds_ShouldUpdateBothAccounts()
        {
            // Cream contul sursa cu 1.000.000 si destinatie cu 500.000
            sourceAccount = new Account(1000000);
            destinationAccount = new Account(500000);

            // Transferam 250.000 de la sursa la destinatie
            sourceAccount.TransferFunds(destinationAccount, 250000);

            // Verificam: sursa 1.000.000 - 250.000 = 750.000
            Assert.That(sourceAccount.Balance, Is.EqualTo(750000), "Soldul sursei trebuie sa fie 750000");
            // Verificam: destinatie 500.000 + 250.000 = 750.000
            Assert.That(destinationAccount.Balance, Is.EqualTo(750000), "Soldul destinatiei trebuie sa fie 750000");
            Assert.Pass("Transferul de fonduri a reusit");
        }

        // ---------- TESTE DE DOMENIU SI LIMITE pentru TransferMinFunds ----------
        
        // Functie helper: creeaza o pereche de conturi (sursa + destinatie)
        private static (Account src, Account dst) NewPair(int srcInit = 500000, int dstInit = 0)
        {
            var s = new Account(); s.Deposit(srcInit);
            var d = new Account(); d.Deposit(dstInit);
            return (s, d);
        }

        // Test 3: Transfer valid undeva IN MIJLOCUL domeniului valid
        [Test, Category("pass")]
        [Description("Testeaza transfer valid in interiorul domeniului")]
        public void TransferMinFunds_IN_ArbitraryInside_ShouldPass()
        {
            // Cream conturi: sursa cu 500.000, destinatie cu 0
            var (s, d) = NewPair(srcInit: 500000, dstInit: 0);
            // Transferam 250.000 (sursa ramane cu 250.000 care e > 1)
            s.TransferMinFunds(d, 250000);
            // Verificam ca destinatia a primit banii
            Assert.That(d.Balance, Is.EqualTo(250000), "Destinatia trebuie sa primeasca 250000");
            // Verificam ca sursa a ramas cu 250.000
            Assert.That(s.Balance, Is.EqualTo(250000), "Sursa trebuie sa aiba 250000 ramas");
            Assert.Pass("Transferul in domeniu valid a reusit");
        }

        // Test 4: Limita INFERIOARA - suma minima posibila (1 leu)
        [Test, Category("pass")]
        [Description("Testeaza limita inferioara a sumei de transfer (amount = 1)")]
        public void TransferMinFunds_LB_On_Amount1_ShouldPass()
        {
            var (s, d) = NewPair(srcInit: 500000, dstInit: 0);
            // Transferam doar 1 leu (cea mai mica suma permisa)
            s.TransferMinFunds(d, 1);
            Assert.That(d.Balance, Is.EqualTo(1), "Destinatia trebuie sa primeasca 1");
            Assert.That(s.Balance, Is.EqualTo(499999), "Sursa trebuie sa aiba 499999 ramas");
            Assert.Pass("Transferul sumei minime a reusit");
        }

        // Test 5: SUB limita inferioara - sume INVALIDE (0 sau negative)
        [TestCase(0)]
        [TestCase(-50000)]
        [Category("pass")]
        [Description("Testeaza rejectia sumelor zero sau negative")]
        public void TransferMinFunds_LB_Off_AmountZeroOrNegative_ShouldThrowAndPass(int amount)
        {
            var (s, d) = NewPair(srcInit: 500000, dstInit: 0);
            // Asteptam exceptie pentru sume <= 0
            Assert.Throws<NotEnoughFundsException>(() => s.TransferMinFunds(d, amount), 
                "Trebuie sa arunce NotEnoughFundsException pentru sume nepozitive");
            Assert.Pass("Exceptia pentru suma invalida a fost aruncata corect");
        }

        // Test 6: Limita SUPERIOARA - cea mai mare suma permisa
        [Test, Category("pass")]
        [Description("Testeaza transfer la limita superioara valida")]
        public void TransferMinFunds_UB_In_AmountJustInside_ShouldPass()
        {
            var (s, d) = NewPair(srcInit: 500000, dstInit: 0);
            // Transferam 499.998 (sursa ramane cu 2, care e > 1)
            s.TransferMinFunds(d, 499998);
            Assert.That(d.Balance, Is.EqualTo(499998), "Destinatia trebuie sa primeasca 499998");
            Assert.That(s.Balance, Is.EqualTo(2), "Sursa trebuie sa aiba 2 ramas");
            Assert.Pass("Transferul maxim valid a reusit");
        }

        // Test 7: PE limita superioara - transferul ar lasa sold exact = minim (INVALID)
        [Test, Category("pass")]
        [Description("Testeaza rejectia transferului care lasa sold sub minim")]
        public void TransferMinFunds_UB_On_AmountAtLimit_ShouldThrowAndPass()
        {
            var (s, d) = NewPair(srcInit: 500000, dstInit: 0);
            // Transferam 499.999 (sursa ar ramane cu 1, dar trebuie > 1, deci EXCEPTIE)
            Assert.Throws<NotEnoughFundsException>(() => s.TransferMinFunds(d, 499999), 
                "Trebuie sa arunce NotEnoughFundsException cand soldul ramas = minBalance");
            Assert.Pass("Exceptia la limita a fost aruncata corect");
        }

        // Test 8: PESTE limita superioara - transfer prea mare
        [Test, Category("pass")]
        [Description("Testeaza rejectia transferului peste limita superioara")]
        public void TransferMinFunds_UB_Off_AmountOverLimit_ShouldThrowAndPass()
        {
            var (s, d) = NewPair(srcInit: 500000, dstInit: 0);
            // Transferam 500.000 (sursa ar ramane cu 0, INVALID)
            Assert.Throws<NotEnoughFundsException>(() => s.TransferMinFunds(d, 500000), 
                "Trebuie sa arunce NotEnoughFundsException cand soldul ramas < minBalance");
            Assert.Pass("Exceptia pentru transfer prea mare a fost aruncata corect");
        }

        // ---------- TESTE MAi Plauzibile ----------
        
        // Test 9: Transfer cand destinatia DEJA ARE bani in cont
        [Test, Category("pass")]
        [Description("Testeaza transfer cand destinatia are sold initial")]
        public void TransferMinFunds_WithDestinationInitialBalance_ShouldAccumulate()
        {
            // Cream conturi: sursa cu 500.000, destinatie cu 300.000
            var (s, d) = NewPair(srcInit: 500000, dstInit: 300000);
            // Transferam 200.000 (destinatia va avea 300.000 + 200.000 = 500.000)
            s.TransferMinFunds(d, 200000);
            Assert.That(d.Balance, Is.EqualTo(500000), "Destinatia trebuie sa aiba 500000 (300000 + 200000)");
            Assert.That(s.Balance, Is.EqualTo(300000), "Sursa trebuie sa aiba 300000 ramas");
            Assert.Pass("Transferul cu sold initial la destinatie a reusit");
        }

        // Test 10: Teste cu MULTIPLE COMBINATII de valori (parametrizat)
        [TestCase(100000, 50000, 50000)]    // Sursa: 100k, Transfer: 50k, Asteptat: 50k
        [TestCase(250000, 100000, 100000)]  // Sursa: 250k, Transfer: 100k, Asteptat: 100k
        [TestCase(500000, 250000, 250000)]  // Sursa: 500k, Transfer: 250k, Asteptat: 250k
        [Category("pass")]
        [Description("Testeaza multiple combinatii de transferuri valide")]
        public void TransferMinFunds_MultipleCombinations_ShouldPass(int srcInit, int transferAmount, int expectedDst)
        {
            // Cream conturi cu valorile din parametri
            var (s, d) = NewPair(srcInit: srcInit, dstInit: 0);
            
            // Verificam daca transferul ar fi valid (sold ramas > 1)
            if (srcInit - transferAmount > 1)
            {
                // Daca e valid, facem transferul
                s.TransferMinFunds(d, transferAmount);
                Assert.That(d.Balance, Is.EqualTo(expectedDst), $"Destinatia trebuie sa aiba {expectedDst}");
                Assert.That(s.Balance, Is.GreaterThan(1), "Soldul sursei trebuie sa fie mai mare decat minBalance");
                Assert.Pass($"Transferul de {transferAmount} din {srcInit} a reusit");
            }
            else
            {
                // Daca e invalid, asteptam exceptie
                Assert.Throws<NotEnoughFundsException>(() => s.TransferMinFunds(d, transferAmount),
                    "Trebuie sa arunce exceptie pentru transfer invalid");
                Assert.Pass("Exceptia pentru transfer invalid a fost aruncata corect");
            }
        }

        // ---------- TESTE CURRENCY CONVERTER cu STUB ----------

        // Test 11: Verifica conversia RON -> EUR cu curs fix (STUB)
        [Test, Category("pass")]
        [Description("Testeaza conversie RON la EUR folosind stub cu curs fix 5.0")]
        public void ConvertRonToEur_WithStub_ShouldCalculateCorrectly()
        {
            // Cream un stub cu curs fix: 1 EUR = 5.0 RON
            var stubConverter = new CurrencyConverterStub(5.0f);
            var account = new Account(1000, stubConverter);

            // Convertim 100 RON la EUR
            // 100 RON / 5.0 = 20 EUR
            float result = account.ConvertRonToEur(100);

            Assert.That(result, Is.EqualTo(20.0f).Within(0.01f), "100 RON trebuie sa fie 20 EUR la curs 5.0");
            Assert.Pass("Conversia RON->EUR cu stub a reusit");
        }

        // Test 12: Verifica conversia EUR -> RON cu curs fix (STUB)
        [Test, Category("pass")]
        [Description("Testeaza conversie EUR la RON folosind stub cu curs fix 5.0")]
        public void ConvertEurToRon_WithStub_ShouldCalculateCorrectly()
        {
            // Cream un stub cu curs fix: 1 EUR = 5.0 RON
            var stubConverter = new CurrencyConverterStub(5.0f);
            var account = new Account(1000, stubConverter);

            // Convertim 20 EUR la RON
            // 20 EUR * 5.0 = 100 RON
            float result = account.ConvertEurToRon(20);

            Assert.That(result, Is.EqualTo(100.0f).Within(0.01f), "20 EUR trebuie sa fie 100 RON la curs 5.0");
            Assert.Pass("Conversia EUR->RON cu stub a reusit");
        }

        // Test 13: Transfer international RON -> EUR cu STUB
        [Test, Category("pass")]
        [Description("Testeaza transfer international RON->EUR folosind stub")]
        public void TransferRonToEur_WithStub_ShouldTransferCorrectAmount()
        {
            // Cream stub cu curs fix: 1 EUR = 5.0 RON
            var stubConverter = new CurrencyConverterStub(5.0f);
            
            // Cont sursa: 1000 RON
            var sourceAccount = new Account(1000, stubConverter);
            // Cont destinatie: 0 EUR
            var destinationAccount = new Account(0, stubConverter);

            // Transferam 500 RON -> EUR
            // 500 RON / 5.0 = 100 EUR
            sourceAccount.TransferRonToEur(destinationAccount, 500);

            // Verificam: sursa a ramas cu 500 RON
            Assert.That(sourceAccount.Balance, Is.EqualTo(500).Within(0.01f), 
                "Sursa trebuie sa aiba 500 RON ramas");
            // Verificam: destinatia a primit 100 EUR
            Assert.That(destinationAccount.Balance, Is.EqualTo(100).Within(0.01f), 
                "Destinatia trebuie sa aiba 100 EUR");
            Assert.Pass("Transferul RON->EUR cu stub a reusit");
        }

        // Test 14: Transfer international EUR -> RON cu STUB
        [Test, Category("pass")]
        [Description("Testeaza transfer international EUR->RON folosind stub")]
        public void TransferEurToRon_WithStub_ShouldTransferCorrectAmount()
        {
            // Cream stub cu curs fix: 1 EUR = 5.0 RON
            var stubConverter = new CurrencyConverterStub(5.0f);
            
            // Cont sursa: 100 EUR
            var sourceAccount = new Account(100, stubConverter);
            // Cont destinatie: 0 RON
            var destinationAccount = new Account(0, stubConverter);

            // Transferam 50 EUR -> RON
            // 50 EUR * 5.0 = 250 RON
            sourceAccount.TransferEurToRon(destinationAccount, 50);

            // Verificam: sursa a ramas cu 50 EUR
            Assert.That(sourceAccount.Balance, Is.EqualTo(50).Within(0.01f), 
                "Sursa trebuie sa aiba 50 EUR ramas");
            // Verificam: destinatia a primit 250 RON
            Assert.That(destinationAccount.Balance, Is.EqualTo(250).Within(0.01f), 
                "Destinatia trebuie sa aiba 250 RON");
            Assert.Pass("Transferul EUR->RON cu stub a reusit");
        }

        // Test 15: Teste cu CURSURI DIFERITE - demonstreaza flexibilitatea STUB-ului
        [TestCase(4.5f, 450, 100)]   // Curs 4.5: 450 RON = 100 EUR
        [TestCase(5.0f, 500, 100)]   // Curs 5.0: 500 RON = 100 EUR
        [TestCase(4.97f, 497, 100)]  // Curs 4.97 (BNR real): 497 RON ≈ 100 EUR
        [Category("pass")]
        [Description("Testeaza conversii cu cursuri diferite folosind stub parametrizat")]
        public void ConvertRonToEur_WithDifferentRates_ShouldCalculateCorrectly(float rate, float ron, float expectedEur)
        {
            // Cream stub cu cursul specificat
            var stubConverter = new CurrencyConverterStub(rate);
            var account = new Account(1000, stubConverter);

            // Convertim RON la EUR
            float result = account.ConvertRonToEur(ron);

            // Verificam rezultatul (cu toleranta de 0.1 pentru erori de rotunjire)
            Assert.That(result, Is.EqualTo(expectedEur).Within(0.1f), 
                $"{ron} RON trebuie sa fie aproximativ {expectedEur} EUR la curs {rate}");
            Assert.Pass($"Conversia cu curs {rate} a reusit");
        }

        // Test 16: Verifica ca nu se poate transfera o suma negativa
        [Test, Category("pass")]
        [Description("Testeaza rejectia sumelor negative la conversie")]
        public void ConvertRonToEur_NegativeAmount_ShouldThrow()
        {
            var stubConverter = new CurrencyConverterStub(5.0f);
            var account = new Account(1000, stubConverter);

            Assert.Throws<ArgumentException>(() => account.ConvertRonToEur(-100), 
                "Trebuie sa arunce ArgumentException pentru suma negativa");
            Assert.Pass("Exceptia pentru suma negativa a fost aruncata corect");
        }

        // ========== TESTE MOCK ==========
        // Mock Object = un obiect de test care simuleaza comportamentul unei dependente externe
        // Si permite VERIFICAREA ca metodele au fost apelate corect (spre deosebire de STUB)
        
        // Test MOCK 1: Verifica ca se trimite EMAIL pentru depuneri mari (> 50,000)
        [Test, Category("pass")]
        [Description("Test MOCK: Verifica ca SendEmail este apelat pentru depuneri mari")]
        public void Deposit_LargeAmount_ShouldCallSendEmail()
        {
            // Arrange (Pregatim mock-ul)
            var stubConverter = new CurrencyConverterStub(5.0f);
            var mockNotificationService = new Mock<INotificationService>(); // Cream un MOCK
            var account = new Account(10000, stubConverter, mockNotificationService.Object);

            // Act (Executam actiunea)
            // Depunem 60,000 RON (peste limita de 50,000)
            account.Deposit(60000);

            // Assert (Verificam ca metoda SendEmail A FOST APELATA)
            // Aceasta e diferenta fata de STUB: verificam INTERACTIUNILE, nu doar rezultatele
            mockNotificationService.Verify(
                m => m.SendEmail(
                    "owner@example.com",                    // recipient
                    "Depunere mare",                        // subject
                    It.Is<string>(msg => msg.Contains("60000")) // message contine suma
                ),
                Times.Once,  // Trebuie apelat EXACT o data
                "SendEmail trebuie apelat pentru depuneri > 50,000 RON"
            );

            // Verificam ca LogActivity a fost apelat de asemenea
            mockNotificationService.Verify(
                m => m.LogActivity(It.IsAny<string>(), It.Is<string>(s => s.Contains("Deposit"))),
                Times.Once,
                "LogActivity trebuie apelat pentru orice depunere"
            );

            Assert.Pass("Mock a verificat corect apelurile pentru depunere mare");
        }

        // Test MOCK 2: Verifica ca se trimite SMS pentru retrageri mari (> 5,000)
        [Test, Category("pass")]
        [Description("Test MOCK: Verifica ca SendSms este apelat pentru retrageri mari")]
        public void Withdraw_LargeAmount_ShouldCallSendSms()
        {
            // Arrange
            var stubConverter = new CurrencyConverterStub(5.0f);
            var mockNotificationService = new Mock<INotificationService>();
            var account = new Account(50000, stubConverter, mockNotificationService.Object);

            // Act
            // Retragem 7,000 RON (peste limita de 5,000)
            account.Withdraw(7000);

            // Assert - Verificam ca SendSms a fost apelat
            mockNotificationService.Verify(
                m => m.SendSms(
                    "+40712345678",                         // phoneNumber
                    It.Is<string>(msg => msg.Contains("7000")) // message contine suma
                ),
                Times.Once,  // STRICT: exact o data
                "SendSms trebuie apelat pentru retrageri > 5,000 RON"
            );

            // Verificam ca LogActivity a fost apelat de asemenea
            mockNotificationService.Verify(
                m => m.LogActivity(It.IsAny<string>(), It.Is<string>(s => s.Contains("Withdraw"))),
                Times.Once,
                "LogActivity trebuie apelat pentru orice retragere"
            );

            Assert.Pass("Mock a verificat corect apelurile pentru retragere mare");
        }

        // Test MOCK 3: Verifica ca LogActivity este apelat la generarea raportului
        [Test, Category("pass")]
        [Description("Test MOCK: Verifica ca LogActivity este apelat la generare raport")]
        public void GenerateAccountReport_ShouldCallLogActivity()
        {
            // Arrange
            var stubConverter = new CurrencyConverterStub(5.0f);
            var mockNotificationService = new Mock<INotificationService>();
            var account = new Account(10000, stubConverter, mockNotificationService.Object);

            // Facem cateva tranzactii pentru a avea ceva in raport
            account.Deposit(5000);
            account.Withdraw(2000);

            // Resetam mock-ul pentru a nu conta tranzactiile anterioare
            mockNotificationService.Reset();

            // Act
            string report = account.GenerateAccountReport();

            // Assert - Verificam ca LogActivity a fost apelat pentru generarea raportului
            mockNotificationService.Verify(
                m => m.LogActivity(
                    It.IsAny<string>(),                     // accountId (orice string)
                    "Account report generated"              // mesaj exact
                ),
                Times.Once,  // Exact o data
                "LogActivity trebuie apelat cand se genereaza raportul"
            );

            // Verificam si ca raportul contine informatii relevante
            Assert.That(report, Does.Contain("Raport Cont"), "Raportul trebuie sa contina titlul");
            Assert.That(report, Does.Contain("Sold curent"), "Raportul trebuie sa contina soldul");

            Assert.Pass("Mock a verificat corect apelul la generarea raportului");
        }

        // Test MOCK 4 (BONUS): Verifica ca SendEmail NU este apelat pentru depuneri mici
        [Test, Category("pass")]
        [Description("Test MOCK: Verifica ca SendEmail NU este apelat pentru depuneri mici")]
        public void Deposit_SmallAmount_ShouldNotCallSendEmail()
        {
            // Arrange
            var stubConverter = new CurrencyConverterStub(5.0f);
            var mockNotificationService = new Mock<INotificationService>();
            var account = new Account(10000, stubConverter, mockNotificationService.Object);

            // Act
            // Depunem doar 1,000 RON (sub limita de 50,000)
            account.Deposit(1000);

            // Assert - Verificam ca SendEmail NU a fost apelat NICIODATA
            mockNotificationService.Verify(
                m => m.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never,  // NICIODATA
                "SendEmail NU trebuie apelat pentru depuneri mici"
            );

            // DAR verificam ca LogActivity a fost totusi apelat (pentru orice depunere)
            mockNotificationService.Verify(
                m => m.LogActivity(It.IsAny<string>(), It.Is<string>(s => s.Contains("Deposit"))),
                Times.Once,
                "LogActivity trebuie apelat pentru orice depunere"
            );

            Assert.Pass("Mock a verificat corect ca SendEmail NU a fost apelat pentru depunere mica");
        }

        // Test MOCK 5 (BONUS): Verifica ordinea apelurilor (STRICT MOCK)
        [Test, Category("pass")]
        [Description("Test MOCK: Verifica ordinea stricta a apelurilor")]
        public void Deposit_ShouldCallMethodsInCorrectOrder()
        {
            // Arrange
            var stubConverter = new CurrencyConverterStub(5.0f);
            var mockNotificationService = new Mock<INotificationService>(MockBehavior.Strict);
            
            // Setup STRICT: definim ORDINEA EXACTA a apelurilor asteptate
            var sequence = new MockSequence();
            
            mockNotificationService.InSequence(sequence)
                .Setup(m => m.LogActivity(It.IsAny<string>(), It.Is<string>(s => s.Contains("Deposit"))));
            
            mockNotificationService.InSequence(sequence)
                .Setup(m => m.SendEmail("owner@example.com", "Depunere mare", It.IsAny<string>()));

            var account = new Account(10000, stubConverter, mockNotificationService.Object);

            // Act
            account.Deposit(60000); // Depunere mare -> va apela LogActivity apoi SendEmail

            // Assert
            // Daca ordinea nu e respectata, MockBehavior.Strict va arunca exceptie
            mockNotificationService.VerifyAll(); // Verifica ca TOATE setup-urile au fost apelate

            Assert.Pass("Mock a verificat ordinea corecta a apelurilor");
        }
    }
} //verificam daca se salveaza
