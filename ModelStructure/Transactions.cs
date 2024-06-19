using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelStructure
{
    /**
    * Transactions makes sure that the receive new transaction method stores new transactions in some static space.
    * It contains a static fields of transaction queue.
    */
    public static class Transactions
    {
        //public static fields
        public static Queue<TransactionModel> transactionList = new Queue<TransactionModel>();
    }
}
