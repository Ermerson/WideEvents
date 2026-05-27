---
description: Validate SOLID principles in .NET classes and provide structured assessments. Use when analyzing individual classes or entire folders for SOLID compliance, generating scores (0-10), identifying violations, assessing severity levels, and providing actionable remediation guidance.
---

# SOLID Validator

A specialized capability for analyzing .NET classes against SOLID principles with structured scoring and actionable recommendations.

This skill helps evaluate code quality by:
- analyzing class structure and dependencies
- scoring SOLID compliance (0-10 scale)
- identifying principle violations
- assessing severity levels (Critical, High, Medium, Low)
- providing specific remediation steps
- generating summary reports

---

# What This Skill Does

The SOLID Validator analyzes one or more .NET classes and produces:

1. **Overall Score**: 0-10 rating based on SOLID compliance
2. **Principle Breakdown**: Individual scores for each SOLID principle
3. **Violations**: Identified issues with severity levels
4. **Remediation Plan**: Specific, actionable steps to resolve violations
5. **Summary Report**: Executive overview with priorities

---

# SOLID Principles

## Single Responsibility Principle (SRP)
A class should have only one reason to change. Each class should have one job.

## Open/Closed Principle (OCP)
Classes should be open for extension but closed for modification.

## Liskov Substitution Principle (LSP)
Derived classes must be substitutable for their base classes.

## Interface Segregation Principle (ISP)
Clients should not depend on interfaces they don't use.

## Dependency Inversion Principle (DIP)
Depend on abstractions, not concrete implementations.

---

# How to Use This Skill

## Single Class Analysis

Provide:
- The class file path or code snippet
- Context about its purpose and dependencies

The skill will:
- Analyze class structure
- Identify SOLID violations
- Score compliance
- Suggest improvements

## Folder Analysis

Provide:
- The folder path containing multiple classes
- Context about the module's purpose

The skill will:
- Analyze all classes
- Identify cross-class violations
- Compute module-level scores
- Prioritize remediation efforts

---

# Severity Levels

### Critical (8+ points loss)
- Severe violations that compromise code quality
- Major refactoring required
- Blocks maintainability and testing

### High (5-7 points loss)
- Significant violations affecting multiple principles
- Substantial refactoring needed
- Impacts code reusability or testing

### Medium (2-4 points loss)
- Moderate violations affecting maintainability
- Focused refactoring needed
- Impacts specific use cases

### Low (0-1 points loss)
- Minor violations or code smells
- Small improvements recommended
- Optional refactoring

---

# Analysis Workflow

When analyzing classes, follow this workflow:

1. **Read the code** - Understand class purpose, structure, dependencies
2. **Evaluate SRP** - Is there a single, clear responsibility?
3. **Evaluate OCP** - How would extension work? Is modification needed?
4. **Evaluate LSP** - Are inheritance relationships correct?
5. **Evaluate ISP** - Are interfaces bloated or focused?
6. **Evaluate DIP** - Are dependencies injected and abstracted?
7. **Score each principle** - 0-10 per principle
8. **Calculate overall score** - Average of principle scores
9. **Identify violations** - List specific issues with severity
10. **Recommend fixes** - Provide concrete remediation steps

---

# Output Format

Generate a structured report:

```markdown
# SOLID Analysis Report

## Overall Score: X/10

## Principle Scores
- Single Responsibility: X/10
- Open/Closed: X/10
- Liskov Substitution: X/10
- Interface Segregation: X/10
- Dependency Inversion: X/10

## Violations

### [Principle Name] - [Severity]
**Issue**: Description of the violation
**Impact**: Why this matters
**Remediation**: 
- Specific step 1
- Specific step 2
- Specific step 3

## Summary & Priorities

### Critical Issues (Must Fix)
- Issue 1: ...

### High Priority (Should Fix)
- Issue 2: ...

### Medium Priority (Nice to Have)
- Issue 3: ...

## Next Steps
- Ranked list of recommended actions
```

---

# Key Guidelines

## Analysis Depth

- **Examine class responsibilities**: methods, fields, reason for change
- **Check dependencies**: what does the class depend on?
- **Review inheritance**: is there a proper class hierarchy?
- **Assess interfaces**: are they focused and segregated?
- **Evaluate abstraction**: are dependencies injected or concrete?

## Scoring Logic

- **9-10**: Excellent SOLID compliance, minimal violations
- **7-8**: Good compliance, minor violations
- **5-6**: Moderate compliance, consider refactoring
- **3-4**: Poor compliance, significant violations
- **0-2**: Severe violations, major refactoring needed

## Realistic Assessment

- Account for project context (new code, legacy code, MVP vs. stable)
- Don't penalize pragmatic trade-offs
- Focus on violations that impact maintainability or testing
- Be constructive in recommendations

---

# Constraints

- This skill analyzes **code structure**, not runtime behavior
- Severity is relative to code context
- Perfect SOLID compliance isn't always practical
- Legacy code expectations differ from new code
- Refactoring recommendations assume standard .NET practices

---

# REFERENCE: SOLID Principles Deep Dive

## Single Responsibility Principle (SRP)

**Definition**: A class should have only one reason to change.

**Evaluation Checklist**:
- Does the class have one primary responsibility?
- Would changes in different requirements require modifying this class?
- Does the class handle multiple concerns (logging, validation, persistence)?
- Are there methods doing unrelated things?

**Anti-Pattern: God Class**

```csharp
// Violates SRP - handles logging, validation, data access, business logic
public class UserManager
{
    public bool ValidateEmail(string email) { }
    public void LogActivity(string message) { }
    public void SaveToDatabase(User user) { }
    public void SendConfirmationEmail(User user) { }
    public decimal CalculateSubscriptionFee(User user) { }
}
```

**Best Practice: Segregated Responsibilities**

```csharp
public class UserValidator { public bool ValidateEmail(string email) { } }
public class UserRepository { public void Save(User user) { } }
public class NotificationService { public void SendConfirmationEmail(User user) { } }
public class SubscriptionCalculator { public decimal CalculateFee(User user) { } }
```

---

## Open/Closed Principle (OCP)

**Definition**: Software entities should be open for extension but closed for modification.

**Anti-Pattern: Modification Required for Extension**

```csharp
// Violates OCP - must modify to add payment types
public class PaymentProcessor
{
    public void Process(string paymentType, decimal amount)
    {
        if (paymentType == "CreditCard") ProcessCreditCard(amount);
        else if (paymentType == "PayPal") ProcessPayPal(amount);
        // Must add else if here for every new type
    }
}
```

**Best Practice: Extension Through Abstraction**

```csharp
public interface IPaymentMethod { void Process(decimal amount); }

public class PaymentProcessor
{
    private readonly IPaymentMethod _method;
    public PaymentProcessor(IPaymentMethod method) => _method = method;
    public void Process(decimal amount) => _method.Process(amount);
}

public class ApplePayMethod : IPaymentMethod
{
    public void Process(decimal amount) { }
}
```

---

## Liskov Substitution Principle (LSP)

**Definition**: Objects of a superclass should be replaceable with objects of its subclasses without breaking the application.

**Anti-Pattern: Violated Substitutability**

```csharp
// Violates LSP
public class Bird { public virtual void Fly() { } }
public class Penguin : Bird
{
    public override void Fly() => throw new NotImplementedException("Penguins can't fly!");
}
```

**Best Practice: Correct Abstraction**

```csharp
public abstract class Bird { }
public interface IFlyingBird { void Fly(); }
public interface ISwimmingBird { void Swim(); }

public class Eagle : Bird, IFlyingBird { public void Fly() { } }
public class Penguin : Bird, ISwimmingBird { public void Swim() { } }
```

---

## Interface Segregation Principle (ISP)

**Definition**: Clients should not be forced to depend on interfaces they don't use.

**Anti-Pattern: Bloated Interface**

```csharp
// Violates ISP
public interface IWorker { void Work(); void Eat(); void Sleep(); void Drive(); }
public class Robot : IWorker
{
    public void Work() { }
    public void Eat() => throw new NotImplementedException();
    public void Sleep() => throw new NotImplementedException();
    public void Drive() => throw new NotImplementedException();
}
```

**Best Practice: Focused Interfaces**

```csharp
public interface IWorker { void Work(); }
public interface ILivingBeing { void Eat(); void Sleep(); }
public interface IDriver { void Drive(); }

public class Robot : IWorker { public void Work() { } }
public class Employee : IWorker, ILivingBeing, IDriver
{
    public void Work() { } public void Eat() { } public void Sleep() { } public void Drive() { }
}
```

---

## Dependency Inversion Principle (DIP)

**Definition**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

**Anti-Pattern: Direct Dependencies**

```csharp
// Violates DIP - tightly coupled, hard to test
public class OrderService
{
    private readonly SqlDatabase _database = new SqlDatabase();
    private readonly EmailNotifier _emailer = new EmailNotifier();
}
```

**Best Practice: Dependency Injection**

```csharp
public interface IDatabase { void Save(Order order); }
public interface INotifier { void SendConfirmation(Order order); }

public class OrderService
{
    private readonly IDatabase _database;
    private readonly INotifier _notifier;

    public OrderService(IDatabase database, INotifier notifier)
    {
        _database = database;
        _notifier = notifier;
    }
}
```

---

# CHECKLISTS: Quick SOLID Evaluation

## SRP Checklist
- [ ] Class has a single, clear responsibility
- [ ] Can you describe the class purpose in one sentence?
- [ ] Would changes in different requirements modify this class?
- [ ] Are there methods doing unrelated tasks?

## OCP Checklist
- [ ] Can you add new features without modifying this class?
- [ ] Are new features added through extension or modification?
- [ ] Would new functionality require editing code?

## LSP Checklist
- [ ] Can derived classes replace the base class without breaking code?
- [ ] Do subclasses honor the base class contract?
- [ ] Do derived classes throw unexpected exceptions?

## ISP Checklist
- [ ] Does each implementation use ALL interface members?
- [ ] Are there NotImplementedExceptions in implementations?
- [ ] Do clients depend on methods they never call?

## DIP Checklist
- [ ] Are dependencies injected or hard-coded?
- [ ] Does the class use `new` to create dependencies?
- [ ] Are dependencies abstractions (interfaces) or concrete types?

## Overall Assessment Flow

1. Read class name and understand purpose
2. Count distinct responsibilities
3. Run each principle checklist → assign score
4. `Overall = sum of applicable scores / number of applicable principles`
5. Identify violations with severity
6. Recommend concrete remediation steps

### Scoring Reference

| Score | Characteristics |
|-------|-----------------|
| 9-10  | Excellent design, minimal issues, fully testable |
| 7-8   | Good design, minor violations, mostly testable |
| 5-6   | Acceptable design, moderate violations, testability limited |
| 3-4   | Poor design, significant violations, difficult to test |
| 0-2   | Severely flawed, multiple critical violations, untestable |
