using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BankLedger.Controllers
{
    public class BankController : Controller
    {
        public IActionResult Index()
        {
            if (isLoggedIn()) {
                var account = getLoggedIn();
                return View(account);
            }
            return View();
        }

        public IActionResult VerifyLogin(string user, string password) {
            var numAccounts = getNumAccounts();

            for (int i=0; i<numAccounts; i++) {
                var account = getAccount(i);
                if (user.Equals(account.UserName) && password.Equals(account.Password)) {
                    updateLoggedIndex(i);
                    @ViewBag.Message = account.ToString();
                    return View("Index", account);
                }
            }
            @ViewBag.Message = "Invalid login credentials";
            return View("Index");
        }

        public IActionResult CreateAccount()
        {
            return View();
        }

        public IActionResult VerifyCreate(string name, string username, string password) {
            var numAccounts = getNumAccounts();

            for (int i=0; i<numAccounts; i++) {
                var account = getAccount(i);
                if (username.Equals(account.UserName)) {
                    @ViewBag.Message = "Username already exists. Please try again.";
                    return View("CreateAccount");
                }
            }

            var newAccount = addAccount(name, username, password);
            HttpContext.Session.SetInt32("loggedIn", -1);
            return View("Index", newAccount);
        }

        public IActionResult Logout() {
            HttpContext.Session.SetInt32("loggedIn", -1);
            return View("Index");
        }

        public IActionResult Transaction()
        {
            if (!isLoggedIn()) {
                return View("Index");
            } else {
                var account = getLoggedIn();
                return View(account);
            }
            
        }
        
        public IActionResult VerifyTransaction(string transactionAmount, string transactionType) {
            
            var account = getLoggedIn();

            try {
                decimal amount = Convert.ToDecimal(transactionAmount);
                if (transactionType.Equals("Withdraw")) {
                    if (account.Balance - amount < 0) {
                        @ViewBag.Message = String.Format("Error: Overdrawn account. New balance: ${0}", account.Balance-amount);
                        return View("Transaction", account);
                    }
                    account.Balance -= amount;
                    
                } else if (transactionType.Equals("Deposit")) {
                    account.Balance += amount;
                } else {
                    @ViewBag.Message = "Error: Invalid transaction type. Please try again";
                    return View("Transaction", account);
                }

                var numTransaction = getNumTransaction();
                addTransaction(account.AccountNumber, transactionType, amount);
                updateLoggedInAccount(account);

                @ViewBag.Success = true;
                @ViewBag.Message = String.Format("Transaction successful!\n{0} amount of {1} from account {2}.\nNew balance is {3}", 
                                                transactionType, 
                                                amount, 
                                                account.AccountNumber, 
                                                account.Balance);
                return View("Transaction", account);

            } catch (FormatException) {
                @ViewBag.Message = "Error: Invalid amount. Please try again";
                return View("Transaction", account);
            }  
        }

        public IActionResult CheckBalance()
        {
            if (!isLoggedIn()) {
                return View("Index");
            } else {
                return View("Index", getLoggedIn());
            }
        }

        public IActionResult TransactionHistory()
        {
            if (!isLoggedIn()) {
                return View("Index");
            } else {
                var account = getLoggedIn();
                var accountNumber = account.AccountNumber;
                var numTransaction = getNumTransaction();
                var transactionList = new List<Models.Transaction>();

                for (int i=0; i<numTransaction; i++) {
                    var currentTransaction = getTransaction(i);
                    if (currentTransaction.AccountNumber.Equals(accountNumber)) {
                        transactionList.Add(currentTransaction);
                    }
                }
                Models.TransactionViewModel transactionViewModel = 
                            new Models.TransactionViewModel(account, transactionList);
                return View(transactionViewModel);
            }
        }

        private bool isLoggedIn() {
            var loggedIndex = HttpContext.Session.GetInt32("loggedIn");
            return (loggedIndex != null && loggedIndex >= 0);
        }

        private Models.Account getLoggedIn() {
            var loggedIndex = HttpContext.Session.GetInt32("loggedIn");
            string sessionKey = String.Format("account{0}", loggedIndex);
            return HttpContext.Session.GetObjectFromJson<Models.Account>(sessionKey);
        }

        private void updateLoggedIndex(int? newNumber) {
            HttpContext.Session.SetInt32("loggedIn", (Int32)newNumber);
        }

        private void updateLoggedInAccount(Models.Account account) {
            var loggedIndex = HttpContext.Session.GetInt32("loggedIn");
            string accountKey = String.Format("account{0}", loggedIndex);
            HttpContext.Session.SetObjectAsJson(accountKey, account);
        }

        private Models.Account getAccount(int index) {
            string sessionKey = String.Format("account{0}", index);
            return HttpContext.Session.GetObjectFromJson<Models.Account>(sessionKey);
        }

        private int? getNumAccounts() {
            var numAccounts = HttpContext.Session.GetInt32("numAccounts");
            if (numAccounts == null) {
                numAccounts = 0;
            }
            return numAccounts;
        }

        private Models.Account addAccount(string name, string username, string password) {
            var numAccounts = getNumAccounts();
            var newAccount = new Models.Account(name, username, password, (numAccounts+1).ToString());
            
            string newAccountKey = String.Format("account{0}", numAccounts++);
            HttpContext.Session.SetObjectAsJson(newAccountKey, newAccount);
            HttpContext.Session.SetInt32("numAccounts", (Int32)numAccounts);
            return newAccount;
        }

        private int? getNumTransaction() {
            var numTransaction = HttpContext.Session.GetInt32("numTransaction");
            if (numTransaction == null) {
                numTransaction = 0;
            }
            return numTransaction;
        }

        private Models.Transaction getTransaction(int index) {
            string sessionKey = String.Format("transaction{0}", index);
            return HttpContext.Session.GetObjectFromJson<Models.Transaction>(sessionKey);
        }

        private Models.Transaction addTransaction(string accountNumber, string transactionType, decimal amount) {
            var newTransaction = new Models.Transaction(accountNumber, transactionType, amount);
            var numTransaction = getNumTransaction();
            string newTransactionKey = String.Format("transaction{0}", numTransaction++);
            HttpContext.Session.SetObjectAsJson(newTransactionKey, newTransaction);
            HttpContext.Session.SetInt32("numTransaction", (Int32)numTransaction);
            return newTransaction;
        }
        
    }
}
