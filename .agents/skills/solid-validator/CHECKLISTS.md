# CHECKLISTS: Quick SOLID Evaluation

Fast reference checklists for evaluating each SOLID principle during code review.

---

## Single Responsibility Principle (SRP) Checklist

### Quick Assessment

- [ ] Class has a single, clear responsibility
- [ ] Can you describe the class purpose in one sentence?
- [ ] Would changes in different requirements modify this class?
- [ ] Does the class have more than one reason to change?
- [ ] Are there methods doing unrelated tasks?

### Issues Detected?

If you checked multiple items, the class likely needs:
- [ ] Split into multiple classes
- [ ] Extract unrelated methods to other classes
- [ ] Create focused service classes

### Severity Assessment

**Critical** (Current Score: 0/10):
- [ ] Class has 5+ unrelated responsibilities
- [ ] Cannot be understood without studying 10+ methods
- [ ] Thread-local state causing side effects
- [ ] Multiple external dependencies tightly coupled

**High** (Current Score: 3/10):
- [ ] Class has 3-4 responsibilities
- [ ] Difficult to test without complex setup
- [ ] Future changes likely affect multiple concerns

**Medium** (Current Score: 6/10):
- [ ] Class has 2 slightly-related responsibilities
- [ ] Clear primary responsibility, minor secondary task
- [ ] Testability impact is limited

**Low** (Current Score: 8/10):
- [ ] Single primary responsibility, very minor secondary task
- [ ] Good design overall, nice-to-have refactoring

---

## Open/Closed Principle (OCP) Checklist

### Quick Assessment

- [ ] Can you add new features without modifying this class?
- [ ] Are new features added through extension or modification?
- [ ] Does the class use sealed types unnecessarily?
- [ ] Are dependencies replaceable/swappable?
- [ ] Would new functionality require editing code?

### Issues Detected?

If you checked multiple items, the class likely needs:
- [ ] Extract to abstractions (interfaces)
- [ ] Use inheritance/polymorphism strategically
- [ ] Avoid if-else chains for type handling
- [ ] Implement factory patterns

### Severity Assessment

**Critical** (Current Score: 0/10):
- [ ] if-else chains checking concrete types
- [ ] New feature = mandatory code modification
- [ ] All classes sealed or final
- [ ] No polymorphism used

**High** (Current Score: 3/10):
- [ ] Some if-else type handling
- [ ] Most new features need modification
- [ ] Inheritance chains but not well abstracted

**Medium** (Current Score: 5/10):
- [ ] Some extension points exist
- [ ] Mixed - some features extend, some modify
- [ ] Partial abstraction

**Low** (Current Score: 8/10):
- [ ] Mostly extensible
- [ ] Minor type coupling in edge cases
- [ ] Generally well abstracted

---

## Liskov Substitution Principle (LSP) Checklist

### Quick Assessment

- [ ] Can derived classes replace the base class without breaking code?
- [ ] Do subclasses honor the base class contract?
- [ ] Are preconditions/postconditions preserved in subclasses?
- [ ] Do derived classes throw unexpected exceptions?
- [ ] Is the inheritance relationship semantically correct?

### Issues Detected?

If you checked multiple items, the class hierarchy likely needs:
- [ ] Review inheritance design
- [ ] Consider composition instead of inheritance
- [ ] Verify contract adherence in subclasses
- [ ] Extract behavioral differences to strategies

### Severity Assessment

**Critical** (Current Score: 0/10):
- [ ] Subclass throws NotImplementedException
- [ ] Downcasting required for correct behavior
- [ ] Subclass violates base class preconditions
- [ ] Base class contract violated

**High** (Current Score: 3/10):
- [ ] Inconsistent behavior between subclasses
- [ ] Some contract violations in edge cases
- [ ] Inheritance questionable but not broken

**Medium** (Current Score: 6/10):
- [ ] Minor behavioral differences
- [ ] Contract mostly preserved
- [ ] Some defensive coding needed

**Low** (Current Score: 8/10):
- [ ] Inheritance semantically correct
- [ ] All subclasses properly implement contract
- [ ] No defensive coding needed

---

## Interface Segregation Principle (ISP) Checklist

### Quick Assessment

- [ ] Does each implementation use ALL interface members?
- [ ] Are there NotImplementedExceptions in implementations?
- [ ] Would the interface benefit from being split?
- [ ] Do clients depend on methods they never call?
- [ ] Is the interface focused or bloated?

### Issues Detected?

If you checked multiple items, interfaces likely need:
- [ ] Split into multiple focused interfaces
- [ ] Reduce interface scope
- [ ] Create role-based interfaces
- [ ] Segregate concerns

### Severity Assessment

**Critical** (Current Score: 0/10):
- [ ] Implementations throw NotImplementedException
- [ ] 50%+ of interface unused by implementers
- [ ] Forced to implement 10+ unrelated methods

**High** (Current Score: 3/10):
- [ ] 30-50% of interface unused
- [ ] Clear sub-groups of functionality
- [ ] Multiple reasons to split

**Medium** (Current Score: 6/10):
- [ ] 10-30% unused methods
- [ ] Minor segregation benefit
- [ ] Some implementers don't need all members

**Low** (Current Score: 8/10):
- [ ] <10% unused methods
- [ ] Mostly focused
- [ ] Could be split but not critical

---

## Dependency Inversion Principle (DIP) Checklist

### Quick Assessment

- [ ] Are dependencies injected or hard-coded?
- [ ] Does the class use `new` to create dependencies?
- [ ] Are dependencies abstractions (interfaces) or concrete types?
- [ ] Would you need to modify code to swap implementations?
- [ ] How testable is this class (can you mock dependencies)?

### Issues Detected?

If you checked multiple items, dependencies likely need:
- [ ] Extract to interfaces
- [ ] Inject via constructor
- [ ] Use dependency container
- [ ] Remove hard-coded instantiation

### Severity Assessment

**Critical** (Current Score: 0/10):
- [ ] All dependencies hard-coded with `new`
- [ ] Cannot mock for testing
- [ ] Tightly coupled to concrete classes
- [ ] Circular dependencies

**High** (Current Score: 3/10):
- [ ] 50%+ of dependencies hard-coded
- [ ] Mix of injected and hard-coded
- [ ] Testing requires real objects

**Medium** (Current Score: 5/10):
- [ ] Some dependencies injected
- [ ] Configuration classes hard-coded
- [ ] Testable but requires setup

**Low** (Current Score: 8/10):
- [ ] All dependencies injected
- [ ] Clear abstraction layer
- [ ] Fully mockable
- [ ] Easy to test

---

## Overall Assessment Flow

### Step 1: Quick Scan
1. Read class name and understand purpose
2. Count distinct responsibilities
3. Identify dependencies
4. Review inheritance hierarchy
5. Check interface completeness

### Step 2: Principle Evaluation
- [ ] SRP: Run SRP checklist → get score
- [ ] OCP: Run OCP checklist → get score
- [ ] LSP: Run LSP checklist → get score (if inherited)
- [ ] ISP: Run ISP checklist → get score (if interfaces)
- [ ] DIP: Run DIP checklist → get score

### Step 3: Calculate Overall Score
`Overall = (SRP + OCP + LSP/N/A + ISP/N/A + DIP) / number_of_applicable_principles`

Use N/A principles if not applicable to class type.

### Step 4: Identify Violations
- [ ] Collect all issues from each checklist
- [ ] Assign severity level
- [ ] Prioritize by severity
- [ ] Group related violations

### Step 5: Recommend Fixes
For each violation:
1. Describe what needs to change
2. Explain why it matters
3. Provide concrete action items
4. Estimate effort if possible

---

## Decision Trees

### Should this responsibility be extracted?

```
Does this class have multiple responsibilities?
├─ NO → Keep as-is (SRP: 9/10)
└─ YES → Would separating improve testability?
   ├─ NO → Keep together (SRP: 7/10)
   └─ YES → Extract to separate class (SRP: 9/10)
```

### Should we use inheritance or composition?

```
Is this a true "is-a" relationship?
├─ NO → Use composition (better DIP, LSP)
└─ YES → Will subclasses truly substitute the base?
   ├─ NO → Reconsider relationship
   └─ YES → Use inheritance (with care for LSP)
```

### Should we split this interface?

```
Do all implementers use all methods?
├─ YES → Keep single interface (ISP: 9/10)
└─ NO → Can methods be grouped by role?
   ├─ NO → Questionable interface design
   └─ YES → Split into role-based interfaces (ISP: 9/10)
```
