using System;
using System.Collections.Generic;
using System.Text;

namespace bank
{
    // Replace incorrect exception definition with proper constructors
    public class NotEnoughFundsException : ApplicationException
    {
        public NotEnoughFundsException() : base("Not enough funds in account!") { }
    }

    // Interfata pentru serviciul de conversie valutara
    // Permite injectarea de implementari diferite (reale sau stub pentru testing)
    public interface ICurrencyConverter
    {
        // Obtine cursul de schimb EUR -> RON (cati lei pentru 1 EUR)
        float GetEurToRonRate();
    }

    // Implementare reala - ar trebui sa fetch-eze cursul de la BNR API
    // In productie, aceasta ar face un HTTP request la API-ul BNR
    public class BnrCurrencyConverter : ICurrencyConverter
    {
        public float GetEurToRonRate()
        {
            // TODO: In productie, aici ar trebui sa faci un HTTP request la BNR
            // Pentru moment, returnez un curs aproximativ (4.97 RON = 1 EUR)
            // BNR API: https://www.bnr.ro/nbrfxrates.xml
            return 4.97f;
        }
    }

    public class Account
    {
        private float balance;        // sold curent (float -> atentie la precizie)
        private float minBalance = 1; // prag minim permis in cont
        private ICurrencyConverter currencyConverter; // serviciu pentru conversie valutara

        public Account()
        {
            balance = 0;              // init sold 0
            currencyConverter = new BnrCurrencyConverter(); // foloseste implementarea reala by default
        }

        public Account(int value)
        {
            balance = value;          // init sold cu o valoare
            currencyConverter = new BnrCurrencyConverter(); // foloseste implementarea reala by default
        }

        // Constructor pentru Dependency Injection - permite injectarea unui converter custom (ex: stub pentru teste)
        public Account(int value, ICurrencyConverter converter)
        {
            balance = value;
            currencyConverter = converter;
        }

        public void Deposit(float amount)
        {
            balance += amount;        // adauga suma fara validare
        }

        public void Withdraw(float amount)
        {
            balance -= amount;        // scade suma fara validare
        }

        public void TransferFunds(Account destination, float amount)
        {
            destination.Deposit(amount); // transfer simplu: +la destinatie
            Withdraw(amount);            // si -la sursa
        }

        public Account TransferMinFunds(Account destination, float amount)
        {
            // blocheaza sume nepozitive
            if (amount <= 0)
                throw new NotEnoughFundsException();

            // permite transfer doar daca soldul ramas > prag
            if (Balance - amount > MinBalance)
            {
                destination.Deposit(amount);
                Withdraw(amount);
            }
            else
            {
                throw new NotEnoughFundsException();
            }

            return destination;        // intoarce referinta destinatiei
        }

        // Converteste RON in EUR bazat pe cursul BNR
        // amount = suma in RON de convertit
        // returneaza: suma echivalenta in EUR
        public float ConvertRonToEur(float amountRon)
        {
            if (amountRon <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            float eurToRonRate = currencyConverter.GetEurToRonRate();
            return amountRon / eurToRonRate; // RON / (RON per EUR) = EUR
        }

        // Converteste EUR in RON bazat pe cursul BNR
        // amount = suma in EUR de convertit
        // returneaza: suma echivalenta in RON
        public float ConvertEurToRon(float amountEur)
        {
            if (amountEur <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            float eurToRonRate = currencyConverter.GetEurToRonRate();
            return amountEur * eurToRonRate; // EUR * (RON per EUR) = RON
        }

        // Transfer international: retrage RON din contul sursa si depune EUR in contul destinatie
        // amountRon = suma in RON de transferat din contul sursa
        public void TransferRonToEur(Account destination, float amountRon)
        {
            if (amountRon <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            // Verifica daca contul sursa are suficienti bani
            if (Balance - amountRon <= MinBalance)
                throw new NotEnoughFundsException();

            // Converteste RON -> EUR
            float amountEur = ConvertRonToEur(amountRon);

            // Retrage RON din sursa
            Withdraw(amountRon);

            // Depune EUR in destinatie
            destination.Deposit(amountEur);
        }

        // Transfer international: retrage EUR din contul sursa si depune RON in contul destinatie
        // amountEur = suma in EUR de transferat din contul sursa
        public void TransferEurToRon(Account destination, float amountEur)
        {
            if (amountEur <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            // Verifica daca contul sursa are suficienti bani
            if (Balance - amountEur <= MinBalance)
                throw new NotEnoughFundsException();

            // Converteste EUR -> RON
            float amountRon = ConvertEurToRon(amountEur);

            // Retrage EUR din sursa
            Withdraw(amountEur);

            // Depune RON in destinatie
            destination.Deposit(amountRon);
        }

        public float Balance
        {
            get { return balance; }    // doar citire
        }

        public float MinBalance
        {
            get { return minBalance; } // prag minim configurat in clasa
        }
    }
}
