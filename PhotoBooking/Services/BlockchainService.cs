using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using System.IO;
using System.Threading.Tasks;

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
            var account = new Account(_privateKey);
            var web3 = new Web3(account, _rpcUrl);

            // 3. Đọc nội dung 2 file ABI và BIN
            // LƯU Ý: Sửa lại đường dẫn này cho khớp với vị trí thư mục "bin" mà bạn vừa tìm thấy
            // Ví dụ nếu nó nằm ở ngoài cùng: "bin/PhotoEscrow.abi"
            // Nếu nó nằm trong SmartContracts: "SmartContracts/bin/PhotoEscrow.abi"
            string abi = await File.ReadAllTextAsync("bin/PhotoEscrow.abi");
            string bytecode = await File.ReadAllTextAsync("bin/PhotoEscrow.bin");

            // 4. Gửi lệnh tạo Hợp đồng lên Ganache
            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(
                abi, 
                bytecode, 
                account.Address, // Người trả phí Gas (chủ ví)
                new HexBigInteger(4000000), // Giới hạn Gas
                account.Address  // Tham số truyền vào Constructor (địa chỉ ví thợ ảnh)
            );

            // 5. Chờ Ganache xác nhận và lấy Địa chỉ Hợp đồng mới (0x...)
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            
            return receipt.ContractAddress;
        }
    }
}