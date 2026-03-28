using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PhotoBooking.Services 
{
    public class BlockchainService
    {
        // 1. Thông tin mạng Ganache
        private readonly string _rpcUrl = "http://127.0.0.1:7545"; 
        private readonly string _privateKey;
        // 2. Private Key ví Thợ ảnh (Copy từ Ganache dán vào đây)
        public BlockchainService(IConfiguration configuration)
        {
            _privateKey = configuration["BlockchainConfig:PhotographerPrivateKey"];
        }

       public async Task<string> DeployContractAsync()
        {
            try 
            {
           
                var account = new Account(_privateKey);
                var web3 = new Web3(account, _rpcUrl);

    
                string abiPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "PhotoEscrow.abi");
                string binPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "PhotoEscrow.bin");

                if (!File.Exists(abiPath) || !File.Exists(binPath))
                {
                    throw new Exception("Không tìm thấy file ABI hoặc BIN tại thư mục bin/. Hãy kiểm tra lại đường dẫn.");
                }

                string abi = await File.ReadAllTextAsync(abiPath);
                string bytecode = await File.ReadAllTextAsync(binPath);

                var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(
                    abi, 
                    bytecode, 
                    account.Address, 
                    new HexBigInteger(4000000), 
                    account.Address  
                );


                TransactionReceipt receipt = null;
                int retryCount = 0;
                while (receipt == null && retryCount < 10) // Thử lại tối đa 10 lần (10 giây)
                {
                    await Task.Delay(1000); // Đợi 1 giây mỗi lần thử
                    receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    retryCount++;
                }

                if (receipt == null || string.IsNullOrEmpty(receipt.ContractAddress))
                {
                    throw new Exception("Giao dịch đã gửi nhưng không nhận được biên lai hoặc địa chỉ hợp đồng.");
                }

                return receipt.ContractAddress;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi để dễ dàng kiểm tra trong cửa sổ Output
                System.Diagnostics.Debug.WriteLine("BLOCKCHAIN ERROR: " + ex.Message);
                throw; 
            }
        }
    }
}