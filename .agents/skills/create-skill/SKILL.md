---
name: skill-creator
description: Create, refine, and optimize reusable Agent Skills for AI agents. Use when designing new skills, improving existing skills, defining evaluation criteria, increasing trigger accuracy, or structuring reusable workflows for agent-based systems.
---

# Skill Creator

A framework for creating, refining, and maintaining high-quality Agent Skills.

This skill helps design reusable capabilities for AI agents in a portable and implementation-agnostic format.

The focus is on:
- skill architecture
- instruction quality
- trigger discoverability
- workflow clarity
- evaluation criteria
- maintainability
- interoperability across agent platforms

This skill should work independently of any specific provider, runtime, SDK, or model vendor.

---

# What Is an Agent Skill?

An Agent Skill is a structured capability package that gives an AI agent specialized knowledge, workflows, constraints, and operational guidance.

A skill usually contains:
- a `SKILL.md` file
- instructions
- references
- examples
- optional supporting resources

Skills should be:
- modular
- reusable
- composable
- portable across agent ecosystems

---

# Skill Creation Workflow

The recommended workflow for creating a skill is:

1. Define the problem the skill solves
2. Identify the target workflows
3. Define expected agent behavior
4. Draft the skill instructions
5. Add examples and constraints
6. Define evaluation criteria
7. Test with realistic prompts
8. Review outputs qualitatively
9. Refine instructions iteratively

---

# Responsibilities

When using this skill, determine the user's current stage in the workflow and help them progress.

Examples:
- If the user only has an idea, help define scope and architecture
- If the user already has a draft, help improve clarity and structure
- If the user has test prompts, help evaluate quality and edge cases
- If the user wants better trigger accuracy, optimize the description and metadata

---

# Skill Design Principles

## 1. Clear Triggering

Descriptions should clearly communicate:
- when the skill should activate
- what problems it solves
- what tasks it supports

Good descriptions improve discoverability across agent systems.

---

## 2. Explicit Instructions

Avoid ambiguous guidance.

Prefer:
- deterministic workflows
- explicit constraints
- concrete expectations
- operational boundaries

Agents perform better with:
- step-by-step procedures
- examples
- edge-case handling
- defined success criteria

---

## 3. Portability

Do not assume:
- a specific model provider
- proprietary APIs
- local scripting environments
- shell access
- Python availability
- external SDKs

Skills should remain portable across:
- coding agents
- autonomous agents
- orchestration systems
- IDE agents
- workflow assistants

---

## 4. Progressive Disclosure

Keep the main `SKILL.md` concise.

Move large references or specialized knowledge into:
- `REFERENCE.md`
- `EXAMPLES.md`
- `CHECKLISTS.md`
- domain-specific resources

Load complexity only when needed.

---

## 5. Evaluation-Oriented Design

A good skill defines:
- expected outcomes
- quality criteria
- failure cases
- edge cases
- behavioral constraints

Evaluation can be:
- qualitative
- human-reviewed
- benchmark-based
- scenario-based

---

# Evaluation Guidance

When evaluating a skill, consider:

## Accuracy
Does the agent produce correct results?

## Reliability
Does behavior remain consistent across prompts?

## Trigger Precision
Does the skill activate in the correct scenarios?

## Instruction Following
Does the agent respect constraints and workflow rules?

## Failure Handling
Does the skill define what to do when uncertain or blocked?

## Maintainability
Can the skill evolve without becoming fragile or overly complex?

---

# Anti-Patterns

Avoid:
- provider-specific assumptions
- excessive prompt verbosity
- hidden operational dependencies
- vague instructions
- implicit workflows
- hardcoded implementation details
- unnecessary chain-of-thought instructions

---

# Recommended Skill Structure

```text
skill-name/
├── SKILL.md
├── REFERENCE.md
├── EXAMPLES.md
├── CHECKLISTS.md
└── resources/