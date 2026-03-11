// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract PhotoEscrow {
    address payable public photographer;
    address public customer;
    uint256 public depositAmount;
    bool public isCompleted;

    constructor(address payable _photographer) {
        photographer = _photographer;
        isCompleted = false;
    }

    function deposit() public payable {
        require(msg.value > 0, "So tien coc phai lon hon 0");
        require(depositAmount == 0, "Don hang nay da duoc dat coc");
        
        customer = msg.sender; 
        depositAmount = msg.value; 
    }

    function confirmCompletion() public {
        require(msg.sender == customer, "Chi khach hang moi duoc xac nhan!");
        require(!isCompleted, "Don hang da hoan thanh roi");
        require(depositAmount > 0, "Chua co tien coc");

        isCompleted = true;
        
        // Đã sửa dòng cảnh báo transfer thành lệnh call chuẩn mới
        (bool success, ) = photographer.call{value: depositAmount}("");
        require(success, "Chuyen tien cho tho anh that bai!");
    }
}