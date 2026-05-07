# REFERENCE: SOLID Principles Deep Dive

This reference provides detailed explanations of each SOLID principle with concrete .NET examples and anti-patterns.

---

## Single Responsibility Principle (SRP)

**Definition**: A class should have only one reason to change.

**What This Means**:
- Each class should have a single, well-defined responsibility
- If you must describe the class purpose in more than one sentence, it likely violates SRP
- The "reason to change" refers to business logic that would require modification

### Evaluation Checklist

- Does the class have one primary responsibility?
- Would changes in different requirements require modifying this class?
- Does the class handle multiple concerns (logging, validation, persistence)?
- Are there methods doing unrelated things?

### Anti-Pattern: God Class

```csharp
// 🔴 Violates SRP - handles logging, validation, data access, business logic
public class UserManager
{
    public bool ValidateEmail(string email) { }
    public void LogActivity(string message) { }
    public void SaveToDatabase(User user) { }
    public void SendConfirmationEmail(User user) { }
    public decimal CalculateSubscriptionFee(User user) { }
    // ... 15 more methods
}
```

### Best Practice: Segregated Responsibilities

```csharp
// ✅ Follows SRP - each class has one responsibility
public class UserValidator
{
    public bool ValidateEmail(string email) { }
}

public class UserRepository
{
    public void Save(User user) { }
}

public class NotificationService
{
    public void SendConfirmationEmail(User user) { }
}

public class SubscriptionCalculator
{
    public decimal CalculateFee(User user) { }
}
```

---

## Open/Closed Principle (OCP)

**Definition**: Software entities should be open for extension but closed for modification.

**What This Means**:
- Add new functionality through extension (inheritance, composition, interfaces)
- Avoid modifying existing code to add features
- Use polymorphism and inheritance strategically

### Evaluation Checklist

- Can you add new features without modifying existing code?
- Does the class use sealed types or final methods unnecessarily?
- Are dependencies easily replaceable?
- Would each new feature require code changes or just new implementations?

### Anti-Pattern: Modification Required for Extension

```csharp
// 🔴 Violates OCP - must modify to add payment types
public class PaymentProcessor
{
    public void Process(string paymentType, decimal amount)
    {
        if (paymentType == "CreditCard")
        {
            ProcessCreditCard(amount);
        }
        else if (paymentType == "PayPal")
        {
            ProcessPayPal(amount);
        }
        else if (paymentType == "ApplePay")  // Must modify here
        {
            ProcessApplePay(amount);
        }
    }
}
```

### Best Practice: Extension Through Abstraction

```csharp
// ✅ Follows OCP - extend by adding new implementations
public interface IPaymentMethod
{
    void Process(decimal amount);
}

public class PaymentProcessor
{
    private readonly IPaymentMethod _method;
    
    public PaymentProcessor(IPaymentMethod method)
    {
        _method = method;
    }
    
    public void Process(decimal amount)
    {
        _method.Process(amount);  // Closed for modification
    }
}

// New payment types are extensions, not modifications
public class ApplePayMethod : IPaymentMethod
{
    public void Process(decimal amount) { }
}
```

---

## Liskov Substitution Principle (LSP)

**Definition**: Objects of a superclass should be replaceable with objects of its subclasses without breaking the application.

**What This Means**:
- Derived classes must honor the contract of their base class
- Subclasses shouldn't throw unexpected exceptions
- Subclasses shouldn't restrict the behavior of the parent
- Inheritance hierarchies must be logically consistent

### Evaluation Checklist

- Can a child class replace a parent without breaking code?
- Do derived classes respect parent class preconditions/postconditions?
- Are inheritance relationships semantically correct?
- Would downcasting ever be necessary to use a subclass correctly?

### Anti-Pattern: Violated Substitutability

```csharp
// 🔴 Violates LSP
public class Bird
{
    public virtual void Fly() { }
}

public class Penguin : Bird
{
    public override void Fly()
    {
        throw new NotImplementedException("Penguins can't fly!");
    }
}

// Client code expects all birds to fly
public void MakeBirdFly(Bird bird)
{
    bird.Fly();  // Fails for Penguin!
}
```

### Best Practice: Correct Abstraction

```csharp
// ✅ Follows LSP - correct hierarchy
public abstract class Bird { }

public interface IFlyingBird
{
    void Fly();
}

public interface ISwimmingBird
{
    void Swim();
}

public class Eagle : Bird, IFlyingBird
{
    public void Fly() { }
}

public class Penguin : Bird, ISwimmingBird
{
    public void Swim() { }
}

// Each interface has appropriate implementations
```

---

## Interface Segregation Principle (ISP)

**Definition**: Clients should not be forced to depend on interfaces they don't use.

**What This Means**:
- Keep interfaces focused and specific
- Don't force implementations to implement methods they don't need
- Create multiple focused interfaces instead of one bloated interface
- Clients depend only on methods they actually use

### Evaluation Checklist

- Do implementers use all interface methods?
- Are there unused methods in implementations?
- Could the interface be split into more focused interfaces?
- Are there throw NotImplementedException in implementations?

### Anti-Pattern: Bloated Interface

```csharp
// 🔴 Violates ISP - forces implementations to implement unused methods
public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
    void Drive();
}

public class Robot : IWorker
{
    public void Work() { }
    public void Eat() { throw new NotImplementedException(); }
    public void Sleep() { throw new NotImplementedException(); }
    public void Drive() { throw new NotImplementedException(); }
}
```

### Best Practice: Focused Interfaces

```csharp
// ✅ Follows ISP - segregated responsibilities
public interface IWorker
{
    void Work();
}

public interface ILivingBeing
{
    void Eat();
    void Sleep();
}

public interface IDriver
{
    void Drive();
}

public class Robot : IWorker
{
    public void Work() { }
}

public class Employee : IWorker, ILivingBeing, IDriver
{
    public void Work() { }
    public void Eat() { }
    public void Sleep() { }
    public void Drive() { }
}
```

---

## Dependency Inversion Principle (DIP)

**Definition**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

**What This Means**:
- Depend on interfaces/abstractions, not concrete implementations
- Use dependency injection to provide dependencies
- High-level business logic shouldn't know about low-level implementation details
- Abstractions shouldn't leak implementation details

### Evaluation Checklist

- Are dependencies injected or hard-coded?
- Does the class create its own dependencies with `new`?
- Are dependencies abstractions (interfaces) or concrete types?
- Could you easily swap implementations?
- Is there coupling to concrete classes?

### Anti-Pattern: Direct Dependencies

```csharp
// 🔴 Violates DIP - tightly coupled, hard to test
public class OrderService
{
    private readonly SqlDatabase _database = new SqlDatabase();
    private readonly EmailNotifier _emailer = new EmailNotifier();
    private readonly PaymentProcessor _payments = new PaymentProcessor();
    
    public void ProcessOrder(Order order)
    {
        _database.Save(order);
        _emailer.SendConfirmation(order);
        _payments.Charge(order);
    }
}
```

### Best Practice: Dependency Injection

```csharp
// ✅ Follows DIP - depends on abstractions
public interface IDatabase { void Save(Order order); }
public interface INotifier { void SendConfirmation(Order order); }
public interface IPaymentGateway { void Charge(Order order); }

public class OrderService
{
    private readonly IDatabase _database;
    private readonly INotifier _notifier;
    private readonly IPaymentGateway _gateway;
    
    public OrderService(
        IDatabase database,
        INotifier notifier,
        IPaymentGateway gateway)
    {
        _database = database;
        _notifier = notifier;
        _gateway = gateway;
    }
    
    public void ProcessOrder(Order order)
    {
        _database.Save(order);
        _notifier.SendConfirmation(order);
        _gateway.Charge(order);
    }
}
```

---

## Interactions Between Principles

These principles work together:

- **SRP** ensures classes have focused responsibilities
- **OCP** makes extension easy through abstraction
- **LSP** ensures inheritance is correct
- **ISP** prevents forced dependencies
- **DIP** enables loose coupling and testability

Violating one principle often reveals or causes violations in others.
