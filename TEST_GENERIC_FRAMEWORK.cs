// TEST FILE - Verify Generic Framework Works with Non-Order Domains
// This file demonstrates that the framework is truly generic and not tied to Order domain

using System;
using Messages;
using Api.SagaFramework;

namespace TestGenericFramework
{
    // ============================================================================
    // EMAIL NOTIFICATION DOMAIN - Completely different from Order Processing
    // ============================================================================
    
    public record SendEmailCommand
    {
        public required Guid CorrelationId { get; init; }
        public required EmailData EmailData { get; init; }  // ← Different property name
        public required int RetryCount { get; init; }
    }
    
    public record EmailData
    {
        public string To { get; init; } = "";
        public string Subject { get; init; } = "";
        public string Body { get; init; } = "";
    }
    
    // ============================================================================
    // PAYMENT PROCESSING DOMAIN - Another different domain
    // ============================================================================
    
    public record ProcessPaymentCommand
    {
        public required Guid CorrelationId { get; init; }
        public required PaymentInfo PaymentInfo { get; init; }  // ← Another different property name
        public required int RetryCount { get; init; }
    }
    
    public record PaymentInfo
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "";
        public string CardToken { get; init; } = "";
    }
    
    // ============================================================================
    // INVENTORY MANAGEMENT DOMAIN - Yet another domain
    // ============================================================================
    
    public record UpdateInventoryCommand
    {
        public required Guid CorrelationId { get; init; }
        public required InventoryUpdate InventoryUpdate { get; init; }  // ← Yet another property name
        public required int RetryCount { get; init; }
    }
    
    public record InventoryUpdate
    {
        public string ProductId { get; init; } = "";
        public int Quantity { get; init; }
        public string Operation { get; init; } = ""; // ADD, REMOVE, SET
    }
    
    // ============================================================================
    // TEST CLASS - Verify GenericCommandFactory works with all domains
    // ============================================================================
    
    public static class GenericFrameworkTest
    {
        public static void TestEmailDomain()
        {
            var correlationId = Guid.NewGuid();
            var emailData = new EmailData 
            { 
                To = "test@example.com", 
                Subject = "Test", 
                Body = "Test email" 
            };
            
            // This should work without any hardcoded "OrderData" references
            var command = GenericCommandFactory.Create<SendEmailCommand>(
                correlationId, 
                emailData, 
                "EmailData",  // ← Framework should handle this dynamically
                retryCount: 2
            );
            
            Console.WriteLine($"✅ Email Command Created: {command.CorrelationId}, Retry: {command.RetryCount}");
        }
        
        public static void TestPaymentDomain()
        {
            var correlationId = Guid.NewGuid();
            var paymentInfo = new PaymentInfo 
            { 
                Amount = 99.99m, 
                Currency = "USD", 
                CardToken = "tok_123" 
            };
            
            // This should work without any hardcoded property names
            var command = GenericCommandFactory.Create<ProcessPaymentCommand>(
                correlationId, 
                paymentInfo, 
                "PaymentInfo",  // ← Framework should handle this dynamically
                retryCount: 1
            );
            
            Console.WriteLine($"✅ Payment Command Created: {command.CorrelationId}, Amount: {command.PaymentInfo.Amount}");
        }
        
        public static void TestInventoryDomain()
        {
            var correlationId = Guid.NewGuid();
            var inventoryUpdate = new InventoryUpdate 
            { 
                ProductId = "PROD-123", 
                Quantity = 5, 
                Operation = "ADD" 
            };
            
            // This should work with any property name
            var command = GenericCommandFactory.Create<UpdateInventoryCommand>(
                correlationId, 
                inventoryUpdate, 
                "InventoryUpdate",  // ← Framework should handle this dynamically
                retryCount: 0
            );
            
            Console.WriteLine($"✅ Inventory Command Created: {command.CorrelationId}, Product: {command.InventoryUpdate.ProductId}");
        }
        
        public static void RunAllTests()
        {
            Console.WriteLine("🧪 Testing Generic Framework with Multiple Domains...\n");
            
            try
            {
                TestEmailDomain();
                TestPaymentDomain();
                TestInventoryDomain();
                
                Console.WriteLine("\n🎉 ALL TESTS PASSED! Framework is truly generic! 🎉");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ TEST FAILED: {ex.Message}");
                Console.WriteLine("Framework still has domain-specific hardcoding!");
            }
        }
    }
}
