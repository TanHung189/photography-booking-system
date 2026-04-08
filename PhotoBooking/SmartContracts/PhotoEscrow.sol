// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract PhotoEscrow {
    address public customer;
    address public photographer;
    uint256 public amount;
    bool public isCompleted;
    bool public isRefunded;

    // Khớp với ABI: constructor nhận _photographer và có payable
   constructor(address _photographer, address _customer) payable {
    customer = _customer; // Gán đúng ví của người đang ngồi trước màn hình Web
    photographer = _photographer;
    amount = msg.value; 
}

    // Hàm giải ngân
    function confirmCompletion() public {
        //require(msg.sender == customer, "Only customer can confirm");
        require(!isCompleted, "Already completed");
        
        isCompleted = true;
        payable(photographer).transfer(amount);
    }

    // Hàm hủy bởi thợ ảnh
    function cancelByPhotographer() public {
        require(msg.sender == photographer, "Only photographer can cancel");
        require(!isCompleted, "Work already completed");

        isRefunded = true;
        payable(customer).transfer(amount);
    }
}