using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Newtonsoft.Json;
using RestSharp;
using ModelStructure;
using System.ServiceModel;
using ServerLib;
using System.Security.Cryptography;

namespace ClientWpfApp
{
    /**
    * MainWindow is the interaction logic for MainWindow.xaml.
    * It is the client gui application and a transaction generator of a current client.
    * It display how many blocks exist and what the current balances of an accounts is. 
    * It also create transactions to be submitted to the Miner web application.
    */
    public partial class MainWindow : Window
    {
        //private fields 
        List<ClientModel> clientList;
        static Mutex mutex = new Mutex();
        bool isWindowDone;
        //create a client variable to track the current client on a specific port
        ClientModel currClient;
        //private fields of LogHelper object to help log message to file
        LogHelper logHelper = new LogHelper();

        //The main window
        public MainWindow()
        {
            InitializeComponent();

            //set variables initial value
            currClient = new ClientModel();
            isWindowDone = false;
            currClient.port = Clients.startingPort.ToString();
            currClient.id = Clients.id.ToString();

            //get the current block state
            getCurrBlockState();

            //start the server(blockhain) and networking(mining) thread on initialisation, as the GUI is basically the main for this program
            Thread server = new Thread(new ThreadStart(startServerThread));
            Thread networking = new Thread(new ThreadStart(startNetworkingThread));
            server.Start();
            networking.Start();
        }

        /**
      * removeDeadClient method represent a method to remove the client from the web application using Rest.
      * In the rest client post method, it will remove a specific client that has exited.
      */
        public void removeDeadClient()
        {
            //if the window is closed or if client close the gui, remove the dead client
            if (isWindowDone == true)
            {
                //set the base url
                string URL = "https://localhost:44302/";
                //use RestClient and set the URL
                RestClient client = new RestClient(URL);
                //set up and call the API method
                RestRequest request = new RestRequest("api/Host/RemoveAClient/" + currClient.port);
                //use IRestResponse and set the request in the client post method
                IRestResponse resp = client.Post(request);

                //check if response is succesful
                if (resp.IsSuccessful)
                {
                    //write description to console
                    Console.WriteLine("A user has exited and have been removed.");
                    //log message to file
                    logHelper.log("[INFO] removeDeadClient() - A user has exited and have been removed.");
                }
                //if response is not succesful, log the error message to file
                else
                {
                    //log error message to file
                    logHelper.log(resp.Content);
                }
            }
        }

        /**
         * startServerThread method is like the client’s job board.
         * It host a .NET Remoting service ServerService and register a client.
         */
        private void startServerThread()
        {
            //This is the actual host service system
            ServiceHost host;
            //create a false bool to check if the server if done being registered in the server thread
            bool isServerDone = false;

            //while the server is not done being created or is false, do try to regsiter a client
            while (isServerDone == false)
            {
                try
                {
                    //Bind server to the implementation of Server
                    host = new ServiceHost(typeof(Server));
                    //This represents a tcp/ip binding in the Windows network stack
                    NetTcpBinding tcp = new NetTcpBinding();
                    //set the url
                    string hostUrl = "net.tcp://" + currClient.ip + ":" + currClient.port + "/ServerService";
                    //present the publicly accessible interface to the client. It tells .net to use current client ip, port and service name of ServerService
                    host.AddServiceEndpoint(typeof(ServerInterface), tcp, hostUrl);
                    //write description to console
                    Console.WriteLine("Opening the host.");
                    //log message to file
                    logHelper.log("[INFO] startServerThread() - opening the host.");
                    //open the host
                    host.Open();

                    //register a client and get the bool result
                    isServerDone = registerAClient();

                    //Executes the delegate synchronously
                    Dispatcher.Invoke(() =>
                    {
                        //set the port text in the gui
                        thisPort.Text = currClient.port;
                        thisId.Text = currClient.id;
                    });

                    //while the window is still open, always loops
                    while (isWindowDone == false) { }
                    //close the host if only window is closed
                    host.Close();
                }
                //catch exception of already in use port
                catch (AddressAlreadyInUseException)
                {
                    //write description to console
                    Console.WriteLine("Error - the port is already in use. AddressAlreadyInUseException occured. changing the clients port.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - the port is already in use. AddressAlreadyInUseException occured. changing the clients port.");

                    //add the port number by 1 
                    Clients.startingPort++;
                    //set the current client port to new port number
                    currClient.port = Clients.startingPort.ToString();
                    //add the client id by 1
                    Clients.id++;
                    //set the current client id to new id
                    currClient.id = Clients.id.ToString();

                    //Bind server to the implementation of Server
                    host = new ServiceHost(typeof(Server));
                }
                //catch other exception 
                catch (Exception)
                {
                    //write description to console
                    Console.WriteLine("Error - an exception occured. changing the clients port.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - an exception occured. changing the clients port.");

                    //add the port number by 1 
                    Clients.startingPort++;
                    //set the current client port to new port number
                    currClient.port = Clients.startingPort.ToString();
                    //add the client id by 1
                    Clients.id++;
                    //set the current client id to new id
                    currClient.id = Clients.id.ToString();

                    //Bind server to the implementation of Server
                    host = new ServiceHost(typeof(Server));
                }
            }
        }

        /**
         * registerAClient method returns a bool indicating that it has finish registring a client.
         * It will call the api method and set the request in the client post method.
         */
        public bool registerAClient()
        {
            //set the base url
            string URL = "https://localhost:44302/";
            //use RestClient and set the URL
            RestClient client = new RestClient(URL);
            //set up and call the API method
            RestRequest request2 = new RestRequest("api/Host/RegisterAClient/");
            //add json body to the request
            request2.AddJsonBody(currClient);
            //use IRestResponse and set the request in the client post method
            IRestResponse resp = client.Post(request2);

            //initialize bool
            bool isSuccess = false;
            //check if response is succesful
            if (resp.IsSuccessful)
            {
                //set bool to true as it has succesfully register a client
                isSuccess = true;
                //write description to console
                Console.WriteLine("Client on port " + currClient.port + " is registered, running server thread.");
                //log message to file
                logHelper.log("[INFO] registerAClient() - Client on port " + currClient.port + " is registered, running server thread.");
            }
            //if response is not succesful, log the error message to file
            else
            {
                //log error message to file
                logHelper.log(resp.Content);
            }

            //return bool
            return isSuccess;
        }

        /**
         * startNetworkingThread method do two things in a loop.
         * It look for new clients and
         * Check each client for the transaction, and do them if it can.
         * Also, it uses a Dictionary that contains a string as the key and client list as the value.
         * This helps to check the most popular hash and if it is out of sync.
         */
        private void startNetworkingThread()
        {
            //initialize the Server
            ServerInterface server;
            //initialize the channel factory of Server
            ChannelFactory<ServerInterface> serverFactory;
            //This represents a tcp/ip binding in the Windows network stack
            NetTcpBinding tcp = new NetTcpBinding();

            //inifinitly runs the loop for the networking thread
            while (true)
            {
                try
                {
                    //check if the queue is greater than 0 and contains a new transaction
                    if (Transactions.transactionList.Count > 0)
                    {
                        //dequeue the new transaction
                        TransactionModel transactionModel = Transactions.transactionList.Dequeue();
                        //log message to console and file
                        Console.WriteLine($"dequeue a transaction from {transactionModel.sender} to {transactionModel.receiver} with amount of {transactionModel.amount}.");
                        logHelper.log($"[INFO] startNetworkingThread() - dequeue a transaction from {transactionModel.sender} to {transactionModel.receiver} with amount of {transactionModel.amount}.");

                        //validate the transaction details. check if the amount is greater than 0, sender id and receiver id is greater or equal than 0
                        if (transactionModel.amount > 0 && transactionModel.sender >= 0 && transactionModel.receiver >= 0)
                        {
                            //get the current coin balance of the transaction
                            float coinBal = Blocks.GetTransactedCurrCoinBalance(transactionModel.sender);
                            //log message to console and file
                            Console.WriteLine($"[INFO] startNetworkingThread() - all transaction details are validated. the current coin balance is {coinBal}.");
                            logHelper.log($"[INFO] startNetworkingThread() - all transaction details are validated. the current coin balance is {coinBal}.");

                            //check if its coin balance is enough for transaction
                            if (coinBal >= transactionModel.amount)
                            {
                                //log message to console and file
                                Console.WriteLine($"[INFO] startNetworkingThread() - the current coin balance is enough for transaction to occurs.");
                                logHelper.log($"[INFO] startNetworkingThread() - the current coin balance is enough for transaction to occurs.");

                                //get the blockchain and pull down the last block from the current blockchain
                                BlockModel prevBlock = Blocks.blockList.Last();

                                //create new block and insert the transaction details
                                BlockModel blockModel = new BlockModel();
                                blockModel.blockId = prevBlock.blockId + 1;
                                blockModel.prevBlockHashStr = prevBlock.currBlockHashStr;
                                blockModel.walletIdFrom = transactionModel.sender;
                                blockModel.walletIdTo = transactionModel.receiver;
                                blockModel.amount = transactionModel.amount;

                                //brute force a valid hash
                                blockModel = newHashGenerator(blockModel);
                                //validate and submit new block to the blockchain if it is a correct block
                                Blocks.SubmitNewBlock(blockModel);
                            }
                        }
                    }

                    //call getOtherClientList method to get the client list 
                    clientList = getOtherClientList();
                    //create a new dictionary object that contains a hash string as the key and list of client model as the value
                    Dictionary<string, List<ClientModel>> dict = new Dictionary<string, List<ClientModel>>();

                    //check if client is not null
                    if (clientList != null)
                    {
                        //loop through the client list
                        for (int i = 0; i < clientList.Count; i++)
                        {
                            //present the publicly accessible interface to the client. It tells .net to use current client ip, port and service name of ServerService
                            serverFactory = new ChannelFactory<ServerInterface>(tcp, "net.tcp://" + clientList[i].ip + ":" + clientList[i].port + "/ServerService");
                            //create the channel
                            server = serverFactory.CreateChannel();

                            //get the current block hash
                            BlockModel blockModel = server.GetCurrentOrLastBlock();
                            string currBlockHashStr = blockModel.currBlockHashStr;
                            List<ClientModel> clients = new List<ClientModel>();
                            //if the dict contains the current block hash as the key, add that client as the value
                            if (dict.ContainsKey(currBlockHashStr))
                            {
                                //add a client as the value of the dict current hash string key
                                dict[currBlockHashStr].Add(clientList[i]);
                            }
                            //if the dict does not contain the current block hash as the key, add its key and value
                            else
                            {
                                //add a client to the dict with its current hash string key
                                clients.Add(clientList[i]);
                                dict.Add(currBlockHashStr, clients);
                            }
                        }

                        //set the initial value
                        int numOfBlock = 0;
                        ClientModel mostPopClient = new ClientModel();
                        string mostPopHash = "";
                        //loop through the dict
                        foreach (KeyValuePair<string, List<ClientModel>> entry in dict)
                        {
                            //if the total number of value is larger that 0, set the most popular block values
                            if (entry.Value.Count > numOfBlock)
                            {
                                //set the number of block
                                numOfBlock = entry.Value.Count;
                                //set the most popular client
                                mostPopClient = entry.Value[0];
                                //set the most popular hash
                                mostPopHash = entry.Key;
                            }
                        }

                        //get the current block hash
                        BlockModel currBlock = Blocks.blockList.Last();
                        string currBlockHash = currBlock.currBlockHashStr;
                        //check if the most popular hash is not the current block hash 
                        if (mostPopHash.Equals(currBlockHash) == false)
                        {
                            //present the publicly accessible interface to the client. It tells .net to use current client ip, port and service name of ServerService
                            serverFactory = new ChannelFactory<ServerInterface>(tcp, "net.tcp://" + mostPopClient.ip + ":" + mostPopClient.port + "/ServerService");
                            //create the channel
                            server = serverFactory.CreateChannel();
                            //get the blockchain
                            Blocks.blockList = server.GetBlockList();
                            //set the total block number gui values to the updated blockchain length
                            Dispatcher.Invoke(() =>
                            {
                                //set the total block number gui values
                                TotalBlockNum.Text = Blocks.blockList.Count.ToString();
                            });
                        }
                    }
                }
                //if the endpoint is not found, catch the exception of endpoint not found and show error message to user
                catch (EndpointNotFoundException)
                {
                    //write description to console
                    Console.WriteLine("Error - an endpoint not found exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startNetworkingThread() - an endpoint not found exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if there is a fault, catch the exception of fault exception and show error message to user
                catch (FaultException)
                {
                    //write description to console
                    Console.WriteLine("Error - a fault exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startNetworkingThread() - a fault exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if the task is cancelled, catch the exception of task cancelled and show error message to user
                catch (TaskCanceledException)
                {
                    //write description to console
                    Console.WriteLine("Error - a task cancelled exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startNetworkingThread() - a task cancelled exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if there is a communication error, catch the exception communication error and show error message to user
                catch (CommunicationException)
                {
                    //write description to console
                    Console.WriteLine("Error - a communication exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startNetworkingThread() - a communication exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if there is an error, catch the exception and show error message to user
                catch (Exception)
                {
                    //write description to console
                    Console.WriteLine("Error - an exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startNetworkingThread() - an exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
            }
        }

        /**
      * getOtherClientList method represent a method to get the client list from the web application using Rest.
      * It will retrives the rest client get method and return it as the client list.
      */
        public List<ClientModel> getOtherClientList()
        {
            //set the base url
            string URL = "https://localhost:44302/";
            //use RestClient and set the URL
            RestClient client = new RestClient(URL);
            //set up and call the API method
            RestRequest request = new RestRequest("api/Host/GetOtherClientList");
            //use IRestResponse and set the request in the client get method
            IRestResponse resp = client.Get(request);

            //check if response is succesful
            if (resp.IsSuccessful)
            {
                //deserialize object using JsonConvert
                clientList = JsonConvert.DeserializeObject<List<ClientModel>>(resp.Content);
            }
            //if response is not succesful, log the error message to file
            else
            {
                //log error message to file
                logHelper.log(resp.Content);
            }

            //return the client list
            return clientList;
        }


        /**
         * This method is called when user click on create transaction button.
         * When user enter values for sender, receiver and amount, and clicks on create transation button,
         * A transaction will happens if no error occured.
         * It uses a static mutex to ensure that, at once, only one holder of the mutex executes.
         * This method represents a resource that must be synchronized so that only one thread at a time can enter.
         */
        private void CreateTransactionBtn_Click(object sender, RoutedEventArgs e)
        {
            //use mutex wait one to block the current thread until it receives a signal
            mutex.WaitOne();

            //initialize the Server
            ServerInterface server;
            //initialize the channel factory of Server
            ChannelFactory<ServerInterface> serverFactory;
            //This represents a tcp/ip binding in the Windows network stack
            NetTcpBinding tcp = new NetTcpBinding();

            try
            {
                //check if sender, receiver and amount text box is not empty
                if (String.IsNullOrEmpty(SenderTxt.Text) == false && String.IsNullOrEmpty(ReceiverTxt.Text) == false && String.IsNullOrEmpty(AmountTxt.Text) == false)
                {
                    //check if the inputted values is of the correct type
                    bool isSenderAnUint = uint.TryParse(SenderTxt.Text, out uint numericValue1);
                    bool isReceiverAnUint = uint.TryParse(ReceiverTxt.Text, out uint numericValue2);
                    bool isAmountAFloat = float.TryParse(AmountTxt.Text, out float numericValue3);

                    //check for any format exception or if the inputted values is of the correct type
                    if (isSenderAnUint && isReceiverAnUint && isAmountAFloat)
                    {
                        //check if the sender is the bank or the current sender with the same id, log error message to user, console and file
                        if (SenderTxt.Text.Equals("0") || SenderTxt.Text.Equals(currClient.id))
                        {
                            //check if the inputted sender and receiver is not the same user id
                            if (SenderTxt.Text != ReceiverTxt.Text)
                            {
                                //get the client
                                clientList = getOtherClientList();
                                //check if the client list is not null
                                if (clientList != null)
                                {
                                    //loop through the client list
                                    for (int i = 0; i < clientList.Count; i++)
                                    {
                                        //present the publicly accessible interface to the client. It tells .net to use current client ip, port and service name of ServerService
                                        serverFactory = new ChannelFactory<ServerInterface>(tcp, "net.tcp://" + clientList[i].ip + ":" + clientList[i].port + "/ServerService");
                                        //create the channel
                                        server = serverFactory.CreateChannel();
                                        //add the new transaction
                                        server.GetNewTransaction(Convert.ToUInt32(SenderTxt.Text), Convert.ToUInt32(ReceiverTxt.Text), float.Parse(AmountTxt.Text));
                                    }
                                }
                            }
                            //if the inputted sender and receiver is the same user id, log error message to user, console and file
                            else
                            {
                                MessageBox.Show("Error - sender and receiver id is the same. it must be a diffrent user id.");
                                logHelper.log("[ERROR] CreateTransactionBtn_Click() - sender and receiver id is the same. it must be a diffrent user id.");
                                Console.WriteLine("Error - sender and receiver id is the same. it must be a diffrent user id.");
                            }
                        }
                        //if the sender is not bank or the current sender with the same id, log error message to user, console and file
                        else
                        {
                            MessageBox.Show("Error - sender is not the current client id. users can only make transactions that withdraw from bank and their own account.");
                            logHelper.log("[ERROR] CreateTransactionBtn_Click() - sender is not the current client id. users can only make transactions that withdraw from bank and their own account.");
                            Console.WriteLine("Error - sender is not the current client id. users can only make transactions that withdraw from bank and their own account.");
                        }
                    }
                    //if the inputted values is not of the correct type, log error message to user, console and file
                    else
                    {
                        MessageBox.Show("Error - the inputted values is of the correct type.");
                        logHelper.log("[ERROR] CreateTransactionBtn_Click() - the inputted values is of the correct type.");
                        Console.WriteLine("Error - the inputted values is of the correct type.");
                    }
                }
                //if sender, receiver and amount text box is empty, log error message to user, console and file
                else
                {
                    MessageBox.Show("Error - the inputted values is empty.");
                    logHelper.log("[ERROR] CreateTransactionBtn_Click() - the inputted values is empty.");
                    Console.WriteLine("Error - the inputted values is empty.");
                }
            }
            //catch a json reader exception and log error to user, console and file
            catch (JsonReaderException)
            {
                MessageBox.Show("Error - json reader exception occured. the inputted values is not valid.");
                logHelper.log("[ERROR] CreateTransactionBtn_Click() - json reader exception occured. the inputted values is not valid.");
                Console.WriteLine("Error - json reader exception occured. the inputted values is not valid.");
            }
            //catch other exception and log error to user, console and file
            catch (Exception)
            {
                MessageBox.Show("Error - an exception occured.");
                logHelper.log("[ERROR] CreateTransactionBtn_Click() - an exception occured.");
                Console.WriteLine("Error - an exception occured.");
            }

            //get the current block state
            getCurrBlockState();
            //to allow another clients to post a transaction, release the mutex
            mutex.ReleaseMutex();

        }

        /**
         * This method is called when user click on get balance button.
         * When user enter values of user id and clicks on get balance button,
         * Balance of a user id is displayed.
         * Any valid currently unused number is an account and its amount will be displayed to 0.
         */
        private void GetBlockStateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //check if user id text box is not empty
                if (string.IsNullOrEmpty(UserTxt.Text) == false)
                {
                    //check if the inputted values is of the correct type
                    bool isUserAnUint = uint.TryParse(UserTxt.Text, out uint numericValue1);
                    //check for any format exception or if the inputted values is of the correct type
                    if (isUserAnUint)
                    {
                        //get the current coin balance
                        float currCoinBal = Blocks.GetTransactedCurrCoinBalance(Convert.ToUInt32(UserTxt.Text));
                        //set the balance value in the gui
                        BalanceTxt.Text = currCoinBal.ToString();
                        //get the current block state
                        getCurrBlockState();
                    }
                    //if the inputted values is not of the correct type, log error message to user, console and file
                    else
                    {
                        MessageBox.Show("Error - the inputted values is of the correct type.");
                        logHelper.log("[ERROR] GetBlockStateBtn_Click() - the inputted values is of the correct type.");
                        Console.WriteLine("Error - the inputted values is of the correct type.");
                    }
                }
                //if user id text box is empty, log error message to user, console and file
                else
                {
                    MessageBox.Show("Error - the inputted values is empty.");
                    logHelper.log("[ERROR] GetBlockStateBtn_Click() - the inputted values is empty.");
                    Console.WriteLine("Error - the inputted values is empty.");
                }
            }
            //catch a json reader exception and log error to user, console and file
            catch (JsonReaderException)
            {
                MessageBox.Show("Error - json reader exception occured. the inputted values is not valid.");
                logHelper.log("[ERROR] GetBlockStateBtn_Click() - json reader exception occured. the inputted values is not valid.");
                Console.WriteLine("Error - json reader exception occured. the inputted values is not valid.");
            }
            //catch other exception and log error to user, console and file
            catch (Exception)
            {
                MessageBox.Show("Error - an exception occured.");
                logHelper.log("[ERROR] GetBlockStateBtn_Click() - an exception occured.");
                Console.WriteLine("Error - an exception occured.");
            }
        }

        /**
        * getCurrBlockState method retreives and set the current block state.
        * It gets the blockchain and set the total block number text.
        * Also, if the blockchain contains nothing, a reserved bank account is created at the start of the chain.
        */
        private void getCurrBlockState()
        {
            //get the client list
            clientList = getOtherClientList();
            //get the blockchain
            List<BlockModel> blockList = Blocks.blockList;
            //if the blockchain contains nothing, creates a reserved bank account at the start of the chain
            if (blockList == null || blockList.Count == 0)
            {
                //creates a reserved bank account at the start of the chain
                Blocks.ReservedBankAcct();
            }
            //get the blockchain
            blockList = Blocks.GetBlockList();
            //set the gui text boxes value
            TotalBlockNum.Text = blockList.Count.ToString();
            thisPort.Text = currClient.port;
            thisId.Text = currClient.id;
        }

        /**
         * newHashGenerator method brute force a valid hash that starts with 12345 and ends with 54321.
         * It increments the hash offset to the next multiple of 5,
         * Concatenate all elements of the block (minus the hash you’re trying to create) into a string,
         * Create a SHA256 hash of that string, and 
         * Check to see if the hash is valid.
         */
        public BlockModel newHashGenerator(BlockModel blockModel)
        {
            //initialize the variables
            uint validBlockOffset = 0;
            string validHashStr = "";
            //using SHA256 for hashes for verification
            SHA256 sha256 = SHA256.Create();
            //loop until a valid offset that is a multiple of 5 and a hash that starts with 12345 and ends with 54321 is generated
            while (validHashStr.StartsWith("12345") == false)
            {
                //create block offset that is a multiple of 5
                validBlockOffset = validBlockOffset + 5;

                //create the new block fields string
                string newBlockStr = blockModel.blockId.ToString() + blockModel.walletIdFrom.ToString() + blockModel.walletIdTo.ToString() + blockModel.amount.ToString() + validBlockOffset + blockModel.prevBlockHashStr;
                //compute the sha256Hash
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(newBlockStr));
                //get the unsigned integer using bit converted from eight bytes in a byte array and make it as a string
                validHashStr = BitConverter.ToUInt64(hash, 0).ToString();
            }

            //set the valid block off set and its hash
            blockModel.blockOffset = validBlockOffset;
            blockModel.currBlockHashStr = validHashStr;

            //log message to file
            logHelper.log($"[INFO] newHashGenerator() - trying to generate a new block with block id:{blockModel.blockId}, from:{blockModel.walletIdFrom}, to:{blockModel.walletIdTo}, " +
                $"amount:{blockModel.amount}, offset:{blockModel.blockOffset}, curr hash:{blockModel.currBlockHashStr}, prev hash:{blockModel.prevBlockHashStr}.");
            //show message to console
            Console.WriteLine($"trying to generate a new block with block id:{blockModel.blockId}, from:{blockModel.walletIdFrom}, to:{blockModel.walletIdTo}, " +
                $"amount:{blockModel.amount}, offset:{blockModel.blockOffset}, curr hash:{blockModel.currBlockHashStr}, prev hash:{blockModel.prevBlockHashStr}.");

            //return the new block 
            return blockModel;
        }

    }
}