using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace PhotoBooking.Services
{
    public class BlockchainService
    {
        private readonly string _rpcUrl;
        private readonly string _privateKey;
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Khởi tạo BlockchainService với cấu hình từ appsettings.Development.json
        /// </summary>
        public BlockchainService(IConfiguration configuration, IWebHostEnvironment env)
        {
            // Đọc cấu hình mạng Ganache
            _rpcUrl = configuration["BlockchainConfig:RpcUrl"] 
                      ?? "http://127.0.0.1:7545"; 

            // Đọc Private Key của Ví Hệ Thống (Admin) dùng để trả phí Gas
            _privateKey = configuration["BlockchainConfig:PhotographerPrivateKey"] 
                          ?? throw new InvalidOperationException("Chưa cấu hình Private Key hệ thống trong appsettings."); 

            _env = env;

            Console.WriteLine($"=== [BlockchainService] Khởi tạo thành công. Kết nối: {_rpcUrl} ===");
        }

        /// <summary>
        /// Thực hiện đúc (Deploy) Smart Contract mới cho mỗi đơn hàng.
        /// photographerWallet: Ví của thợ ảnh lấy từ database.
        /// customerWallet: Ví của khách hàng đang đăng nhập thực hiện đặt lịch.
        /// </summary>
        public async Task<string> DeployContractAsync(string photographerWallet, string customerWallet)
        {
            try
            {
                // --- BƯỚC 1: XÁC THỰC ĐỊA CHỈ VÍ (CYBERSECURITY CHECK) --- [cite: 71]
                var addressUtil = new AddressUtil();
                if (!addressUtil.IsValidEthereumAddressHexFormat(photographerWallet) || 
                    !addressUtil.IsValidEthereumAddressHexFormat(customerWallet))
                {
                    throw new Exception("Địa chỉ ví thợ ảnh hoặc khách hàng không đúng định dạng Ethereum.");
                }

                Console.WriteLine($">>> [Blockchain] Đang chuẩn bị đúc hợp đồng:");
                Console.WriteLine($"    - Ví Thợ ảnh (Thụ hưởng): {photographerWallet}");
                Console.WriteLine($"    - Ví Khách hàng (Chủ đơn): {customerWallet}");

                // --- BƯỚC 2: KHỞI TẠO TÀI KHOẢN ĐIỀU HÀNH --- [cite: 63]
                var account = new Account(_privateKey);
                var web3 = new Web3(account, _rpcUrl);

            // --- BƯỚC 3: ĐỌC TÀI LIỆU BIÊN DỊCH (ABI & BIN) --- [cite: 70, 75]
                string abiPath = Path.Combine(_env.ContentRootPath, "SmartContracts", "PhotoEscrow.abi");
                string binPath = Path.Combine(_env.ContentRootPath, "SmartContracts", "PhotoEscrow.bin");

                // Phương án dự phòng nếu chạy trong môi trường Debug/Build
                if (!File.Exists(abiPath))
                {
                    abiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmartContracts", "PhotoEscrow.abi");
                    binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmartContracts", "PhotoEscrow.bin");
                }

                if (!File.Exists(abiPath) || !File.Exists(binPath))
                {
                    throw new Exception($"Không tìm thấy file ABI/BIN tại: {abiPath}");
                }

                string abi = await File.ReadAllTextAsync(abiPath);
                string bytecode = await File.ReadAllTextAsync(binPath);

               // --- BƯỚC 4: GỬI GIAO DỊCH LÊN GANACHE --- [cite: 62, 66]
                Console.WriteLine(">>> [Blockchain] Đang gửi giao dịch Deploy tới Ganache...");

                // Quan trọng: Nạp đúng 2 tham số vào Constructor theo thứ tự trong file .sol
                var constructorArgs = new object[] { photographerWallet, customerWallet };

                var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                    abi,
                    bytecode,
                    account.Address,
                    new HexBigInteger(4000000), // Gas Limit
                    null,
                    constructorArgs
                );

           // --- BƯỚC 5: KIỂM TRA VÀ TRẢ VỀ ĐỊA CHỈ HỢP ĐỒNG --- [cite: 72]
                if (receipt == null || string.IsNullOrEmpty(receipt.ContractAddress))
                {
                    throw new Exception("Giao dịch thành công nhưng không nhận được địa chỉ hợp đồng.");
                }

                Console.WriteLine($"✅ [Blockchain] Đúc thành công! Address: {receipt.ContractAddress}");
                return receipt.ContractAddress;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Blockchain Error]: {ex.Message}");
                throw; // Ném lỗi để Controller xử lý cập nhật trạng thái FAILED_ON_CHAIN
            }
        }
    }
}