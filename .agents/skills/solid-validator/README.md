# SOLID Validator Skill

A specialized skill for analyzing .NET classes against SOLID principles with structured scoring and actionable recommendations.

## Skill Contents

This skill package includes:

### SKILL.md
The main skill definition. Contains:
- Overview of SOLID validation capabilities
- How to use the skill (single class vs. folder analysis)
- Detailed SOLID principle explanations
- Analysis workflow steps
- Output format specification
- Severity levels and scoring logic
- Example scenarios

### REFERENCE.md
Deep-dive reference guide with:
- Detailed explanation of each SOLID principle
- Anti-patterns (what NOT to do)
- Best practices (what TO do)
- Concrete .NET code examples for each principle
- How principles interact with each other

### EXAMPLES.md
Real-world analysis examples showing:
- **Example 1**: God class violating all SOLID principles (Score: 1.5/10)
- **Example 2**: Well-designed service with high compliance (Score: 8.5/10)
- **Example 3**: Module-level analysis approach
- Complete analysis reports with violations, severity, and remediation
- Scoring reference guide

### CHECKLISTS.md
Quick evaluation checklists for:
- SRP (Single Responsibility Principle)
- OCP (Open/Closed Principle)
- LSP (Liskov Substitution Principle)
- ISP (Interface Segregation Principle)
- DIP (Dependency Inversion Principle)

Each checklist includes:
- Quick assessment questions
- Issues detected guidance
- Severity assessment criteria
- Decision trees for common scenarios

## How to Use This Skill

### Analyze a Single Class

1. Provide the class code or file path
2. Share context about its purpose
3. The skill will:
   - Evaluate against each SOLID principle
   - Generate a 0-10 score
   - Identify specific violations
   - Assess severity levels
   - Provide remediation steps

### Analyze a Folder/Module

1. Provide the folder path with multiple related classes
2. Share context about module purpose
3. The skill will:
   - Analyze each class
   - Calculate module-level scores
   - Identify cross-class violations
   - Prioritize remediation efforts

### Quick Reference

Use the checklists for fast evaluation during code review:
- 2-3 minutes per principle
- Suitable for quick assessments
- Better for experienced reviewers

### Detailed Analysis

Use the full skill for comprehensive evaluations:
- 15-30 minutes per class
- Generates detailed reports
- Includes remediation plans
- Suitable for significant refactoring efforts

## Output You'll Get

Each analysis produces:

```markdown
# SOLID Analysis Report: [ClassName]

## Overall Score: X/10

## Principle Scores
- Single Responsibility: X/10
- Open/Closed: X/10
- Liskov Substitution: X/10
- Interface Segregation: X/10
- Dependency Inversion: X/10

## Violations
[Listed with severity and details]

## Summary & Priorities
[Prioritized remediation recommendations]

## Next Steps
[Ranked list of actions]
```

## Severity Levels

- **Critical** (8+ points loss): Major refactoring required
- **High** (5-7 points loss): Substantial improvements needed
- **Medium** (2-4 points loss): Focused refactoring recommended
- **Low** (0-1 points loss): Minor improvements suggested

## Scoring Guide

- **9-10**: Excellent SOLID compliance
- **7-8**: Good design, minor issues
- **5-6**: Acceptable, should refactor
- **3-4**: Poor design, likely untestable
- **0-2**: Severe violations, major rework needed

## Context: WideEvents Project

This skill is designed to help maintain code quality in the WideEvents observability framework, supporting:
- Code reviews
- Refactoring efforts
- Architectural decisions
- Team knowledge sharing
- Mentoring and training

WideEvents emphasizes:
- High performance
- Clean architecture
- Extensibility
- Testability
- Data governance

This skill ensures codebase evolution maintains these principles.
