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

    public class Account
    {
        private float balance;        // sold curent (float -> atentie la precizie)
        private float minBalance = 1; // prag minim permis in cont

        public Account()
        {
            balance = 0;              // init sold 0
        }

        public Account(int value)
        {
            balance = value;          // init sold cu o valoare
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
