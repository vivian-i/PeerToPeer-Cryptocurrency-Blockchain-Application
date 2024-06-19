using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelStructure
{
    /**
      * Client is a static class (static model is the best approach) that connects to the Data Tier via .NET remoting.
      * It contains a static fields of client id and list of clients and a starting port of a client.
      */
    public static class Clients
    {
        //public fields
        public static List<ClientModel> clientList = new List<ClientModel>();
        public static int startingPort = 8001;
        public static int id = 1;
    }
}
