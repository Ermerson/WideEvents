# EXAMPLES: SOLID Analysis Samples

Real-world analysis examples showing how to evaluate classes and generate assessment reports.

---

## Example 1: God Class - Low Score

### Code Under Analysis

```csharp
public class OrderProcessor
{
    private string _connectionString = "Server=localhost;Database=Orders";
    
    public void ProcessOrder(Order order)
    {
        ValidateOrder(order);
        CalculateTax(order);
        ApplyDiscount(order);
        SaveToDatabase(order);
        SendConfirmationEmail(order);
        LogActivity($"Order {order.Id} processed");
        UpdateInventory(order);
    }
    
    private bool ValidateOrder(Order order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));
        if (order.Items.Count == 0) return false;
        if (order.Total <= 0) return false;
        // ... 20 more validations
        return true;
    }
    
    private void CalculateTax(Order order)
    {
        order.Tax = order.Total * 0.1m;
    }
    
    private void ApplyDiscount(Order order)
    {
        if (order.Customer.IsVip) order.Total *= 0.9m;
    }
    
    private void SaveToDatabase(Order order)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            // Direct SQL execution
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"INSERT INTO Orders VALUES ({order.Id}, '{order.Customer.Name}', {order.Total})";
            cmd.ExecuteNonQuery();
        }
    }
    
    private void SendConfirmationEmail(Order order)
    {
        var smtpClient = new SmtpClient("smtp.gmail.com", 587);
        var message = new MailMessage("noreply@company.com", order.Customer.Email);
        message.Subject = "Order Confirmation";
        message.Body = $"Your order {order.Id} has been processed.";
        smtpClient.Send(message);
    }
    
    private void LogActivity(string message)
    {
        File.AppendAllText("log.txt", $"{DateTime.Now}: {message}\n");
    }
    
    private void UpdateInventory(Order order)
    {
        foreach (var item in order.Items)
        {
            // Update inventory logic
        }
    }
}
```

### Analysis Report

```markdown
# SOLID Analysis Report: OrderProcessor

## Overall Score: 1.5/10

## Principle Scores
- Single Responsibility: 0/10
- Open/Closed: 1/10
- Liskov Substitution: N/A
- Interface Segregation: 0/10
- Dependency Inversion: 0/10

## Violations

### Single Responsibility - CRITICAL
**Issue**: Class has 7+ distinct responsibilities (validation, calculation, persistence, notification, logging, inventory)
**Impact**: 
- Difficult to test (requires mocking SMTP, database, file system)
- Hard to maintain (changes in any concern require modification here)
- Cannot be reused without carrying all dependencies
**Remediation**: 
1. Extract validation to `IValidator`/`OrderValidator`
2. Extract tax calculation to `ITaxCalculator`/`TaxCalculationService`
3. Extract discount logic to `IDiscountService`/`DiscountEngine`
4. Extract persistence to `IOrderRepository`/`SqlOrderRepository`
5. Extract email to `IEmailService`/`EmailNotificationService`
6. Extract logging to `ILogger` (use Serilog or similar)
7. Extract inventory to `IInventoryService`/`InventoryManager`

### Open/Closed - CRITICAL
**Issue**: Must modify this class to support new payment methods, different persistence strategies, or email providers
**Impact**: 
- Adding features requires editing existing code
- Each change risks breaking existing functionality
- No way to extend without modification
**Remediation**:
1. Extract concerns into interfaces
2. Use dependency injection for each concern
3. Clients interact via abstractions, not this class
4. New implementations simply implement the interface

### Liskov Substitution - N/A
Not applicable (no inheritance involved)

### Interface Segregation - CRITICAL
**Issue**: This class depends on incompatible abstractions (Email, Database, File, Inventory APIs simultaneously)
**Impact**: 
- Cannot implement a subset of functionality
- Tight coupling to multiple external systems
- Difficult to test individual concerns
**Remediation**:
1. Segregate into single-purpose interfaces
2. Accept each dependency as injection parameter
3. Each interface represents one concern

### Dependency Inversion - CRITICAL
**Issue**: Hard-coded dependencies on SqlConnection, SmtpClient, File.AppendAllText, direct instantiation
**Impact**: 
- Impossible to test without actual database, email server, file system
- Cannot swap implementations
- Tightly coupled to specific technologies
**Remediation**:
1. Create interfaces for each external dependency
2. Inject abstractions through constructor
3. Use setter injection for optional dependencies
4. Remove all `new` keyword usages
5. Example: `IOrderRepository repo`, `IEmailService email`, `ILogger logger`

## Summary & Priorities

### Critical Issues (Must Fix)
1. **Extract responsibilities into separate services** - This is a blocker for testability and maintenance
2. **Introduce dependency injection** - Cannot test without real external systems
3. **Define service interfaces** - Each concern needs its own abstraction

### High Priority (Should Fix)
1. Remove hard-coded connection strings
2. Eliminate direct SQL string building (use ORM or parameterized queries)
3. Separate business logic from infrastructure

### Medium Priority (Nice to Have)
1. Consider repository pattern for persistence
2. Implement factory pattern for service creation

## Recommended Refactoring Path

### Phase 1: Extract Services (Priority 1)
```csharp
public class OrderProcessor
{
    private readonly IOrderValidator _validator;
    private readonly ITaxCalculator _taxCalculator;
    private readonly IDiscountService _discountService;
    private readonly IOrderRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    private readonly IInventoryService _inventory;
    
    public OrderProcessor(
        IOrderValidator validator,
        ITaxCalculator taxCalculator,
        IDiscountService discountService,
        IOrderRepository repository,
        IEmailService emailService,
        ILogger logger,
        IInventoryService inventory)
    {
        _validator = validator;
        _taxCalculator = taxCalculator;
        _discountService = discountService;
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
        _inventory = inventory;
    }
    
    public void ProcessOrder(Order order)
    {
        _validator.Validate(order);
        _taxCalculator.CalculateTax(order);
        _discountService.ApplyDiscount(order);
        _repository.Save(order);
        _emailService.SendConfirmation(order);
        _logger.Information("Order processed: {OrderId}", order.Id);
        _inventory.Update(order);
    }
}
```

This addresses all critical violations immediately.

---

## Example 2: Well-Designed Service - High Score

### Code Under Analysis

```csharp
public interface ILogger { void Log(string message); }
public interface IEmailService { Task SendAsync(string to, string subject, string body); }
public interface IOrderRepository { Task SaveAsync(Order order); }

public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    
    public OrderService(
        IOrderRepository repository,
        IEmailService emailService,
        ILogger logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));
        
        await _repository.SaveAsync(order);
        _logger.Log($"Order saved: {order.Id}");
        
        await _emailService.SendAsync(
            order.Customer.Email,
            "Order Confirmation",
            $"Your order {order.Id} has been received.");
    }
}
```

### Analysis Report

```markdown
# SOLID Analysis Report: OrderService

## Overall Score: 8.5/10

## Principle Scores
- Single Responsibility: 9/10
- Open/Closed: 9/10
- Liskov Substitution: N/A
- Interface Segregation: 8/10
- Dependency Inversion: 9/10

## Violations

### Single Responsibility - MINOR
**Issue**: Slight mixing of orchestration logic with email notification responsibility
**Impact**: Low - the class correctly delegates concerns but coordinates order saving + notification
**Remediation**: 
1. (Optional) Extract to a separate orchestration layer
2. (Optional) Use events/messaging pattern for notifications
3. Current design is acceptable for most scenarios

### Interface Segregation - MINOR
**Issue**: Could segregate order saving and notification into separate workflows
**Impact**: Low - not forced, well-designed
**Remediation**: 
1. (Optional) Create `IOrderWorkflow` with separate steps
2. Current design provides good balance between simplicity and segregation

## Summary & Priorities

### No Critical Issues
Design is fundamentally sound.

### No High Priority Issues
Dependencies properly injected, responsibilities well-separated.

### Low Priority Improvements
1. Consider async/await pattern throughout (already using)
2. Add custom exceptions for failure scenarios
3. Implement retry logic in caller (not here - SRP)

## Strengths

✅ Single responsibility clearly defined (process order coordination)
✅ All dependencies injected through constructor
✅ Interfaces are focused and segregated
✅ Async patterns enable scalability
✅ Easy to test (mock all dependencies)
✅ Easy to extend (swap implementations)

## Example Unit Tests

```csharp
[Fact]
public async Task ProcessOrderAsync_WithValidOrder_SavesAndNotifies()
{
    // Arrange
    var mockRepo = new Mock<IOrderRepository>();
    var mockEmail = new Mock<IEmailService>();
    var mockLogger = new Mock<ILogger>();
    
    var service = new OrderService(mockRepo.Object, mockEmail.Object, mockLogger.Object);
    var order = new Order { Id = "12345", Customer = new Customer { Email = "user@example.com" } };
    
    // Act
    await service.ProcessOrderAsync(order);
    
    // Assert
    mockRepo.Verify(x => x.SaveAsync(order), Times.Once);
    mockEmail.Verify(x => x.SendAsync("user@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}
```

This demonstrates how proper SOLID design enables easy, focused testing.
```

---

## Example 3: Module-Level Analysis

When analyzing a folder with multiple related classes, consider:

1. **Cohesion**: Do classes in this module work together toward one goal?
2. **Coupling**: Are classes tightly coupled to each other or external systems?
3. **Boundaries**: Is the module's responsibility clear?
4. **Dependencies**: Do classes form a logical dependency tree?

### High-Cohesion Module: Payment Processing

```markdown
# Module Score: 8.2/10

## Classes
- PaymentValidator: 7/10
- PaymentGateway (interface): 9/10
- StripePaymentGateway: 8/10
- PaymentService: 9/10
- PaymentResult: 10/10

## Module Characteristics
✅ All classes serve payment processing
✅ Clear dependency directions (service → gateway → validator)
✅ All depend on abstractions
✅ Easy to add new payment providers
✅ Testability high (all interfaces available)

## Module Recommendations
1. (Optional) Consolidate PaymentValidator into PaymentService
2. (Optional) Use factory for gateway creation
```

---

## Scoring Guide

Use these as reference points when scoring:

| Score | Characteristics |
|-------|-----------------|
| 9-10  | Excellent design, minimal issues, fully testable |
| 7-8   | Good design, minor violations, mostly testable |
| 5-6   | Acceptable design, moderate violations, testability limited |
| 3-4   | Poor design, significant violations, difficult to test |
| 0-2   | Severely flawed, multiple critical violations, untestable |
