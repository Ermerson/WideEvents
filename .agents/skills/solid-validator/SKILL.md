---
name: solid-validator
description: Validate SOLID principles in .NET classes and provide structured assessments. Use when analyzing individual classes or entire folders for SOLID compliance, generating scores (0-10), identifying violations, assessing severity levels, and providing actionable remediation guidance.
---

# SOLID Validator Skill

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

This skill evaluates:

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

Violations are rated:

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

# Example Scenarios

### Scenario 1: God Class
A 500-line class with 20 methods doing unrelated things.
- SRP: 1/10 (multiple reasons to change)
- OCP: 2/10 (tightly coupled, hard to extend)
- LSP: N/A (not inheritance)
- ISP: 3/10 (implements bloated interface)
- DIP: 2/10 (hard dependencies everywhere)
- **Overall: 2/10** - Critical refactoring needed

### Scenario 2: Well-Structured Service
A service class with clear responsibility, injected dependencies, focused interface.
- SRP: 9/10 (one clear job)
- OCP: 8/10 (easily extensible)
- LSP: N/A (not inheritance)
- ISP: 9/10 (focused interface)
- DIP: 9/10 (depends on abstractions)
- **Overall: 8.75/10** - Good design

---

# Constraints

- This skill analyzes **code structure**, not runtime behavior
- Severity is relative to code context
- Perfect SOLID compliance isn't always practical
- Legacy code expectations differ from new code
- Refactoring recommendations assume standard .NET practices

---

# See Also

- `REFERENCE.md`: Detailed SOLID principle explanations
- `EXAMPLES.md`: Annotated code examples and analyses
- `CHECKLISTS.md`: Quick evaluation checklist
