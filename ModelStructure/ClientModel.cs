using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelStructure
{
    /**
      * ClientModel is a class that defines the client.
      * The clients has an id, IP address, and a port to add client to the server.
      */
    public class ClientModel
    {
        //public fields
        public string id;
        public string ip = "localhost";
        public string port;
    }
}
