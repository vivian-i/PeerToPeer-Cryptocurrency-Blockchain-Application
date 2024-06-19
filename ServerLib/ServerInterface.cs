using ModelStructure;
using System.Collections.Generic;
using System.ServiceModel;

namespace ServerLib
{
    //makes this a service contract as it is a service interface
    [ServiceContract]
    /**
     * ServerInterface is the public interface for the .NET server
     * It is the .NET Remoting network interface.
     */
    public interface ServerInterface
    {
        //OperationContracts is tagged as it is a service function contracts
        [OperationContract]
        void GetNewTransaction(uint sender, uint receiver, float amount);

        //OperationContracts is tagged as it is a service function contracts
        [OperationContract]
        BlockModel GetCurrentOrLastBlock();

        //OperationContracts is tagged as it is a service function contracts
        [OperationContract]
        List<BlockModel> GetBlockList();
    }
}
