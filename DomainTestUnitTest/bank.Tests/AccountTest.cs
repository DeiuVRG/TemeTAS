using NUnit.Framework;

namespace bank
{
    // Clasa care contine toate testele pentru contul bancar
    [TestFixture]
    [Description("Teste pentru operatiile contului bancar corporativ")]
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

        // ---------- TESTE DE ROBUSTETE ----------
        
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
    }
}
