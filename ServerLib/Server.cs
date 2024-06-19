using ModelStructure;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace ServerLib
{
    /**
     *  Server is a C# console application.
     *  It is an implementation of the interface (the Server interface).
     *  It contains a method to get new transaction, get the current block and get the blockchain.
     */
    //defining the behaviours of a service by ServiceBehavior, makes the service multi-threaded by ConcurrencyMode and allow management of the thread synchronisation
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class Server : ServerInterface
    {
        //private fields of LogHelper object to help log message to file
        private LogHelper logHelper = new LogHelper();

        //public constructor
        public Server() { }

        /**
         * GetNewTransaction method takes in a sender, receiver and amount
         * It adds the transaction to the static transaction list.
         */
        public void GetNewTransaction(uint sender, uint receiver, float amount)
        {
            //create ans set the transaction object
            TransactionModel transactionModel = new TransactionModel();
            transactionModel.sender = sender;
            transactionModel.receiver = receiver;
            transactionModel.amount = amount;

            //adds the transaction to the static transaction list.
            Transactions.transactionList.Enqueue(transactionModel);

            //log message to file
            logHelper.log($"[INFO] PostingTransaction() - post a transaction.");
        }

        /**
         * GetCurrentOrLastBlock method returns the last BlockModel object in the blockchain.
         * It takes no parameter in.
         */
        public BlockModel GetCurrentOrLastBlock()
        {
            //returns the last BlockModel object in the blockchain
            return Blocks.blockList.Last();
        }

        /**
         * GetBlockList method returns the blockchain.
         * It takes no parameter in.
         */
        public List<BlockModel> GetBlockList()
        {
            //returns the blockchain
            return Blocks.blockList;
        }
    }
}
