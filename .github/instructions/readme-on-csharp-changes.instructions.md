---
name: "README Updates for C# Changes"
description: "Use when changing C# application code. Keep README.md accurate by documenting changes to user-visible behavior, setup, configuration, dependencies, or operation in the same task."
applyTo: "**/*.cs"
---

# README Updates for C# Changes

- When changing application C# code, review `README.md` and determine whether the change affects documented behavior, user workflows, prerequisites, setup, configuration, dependencies, data storage, or deployment and operation.
- Update the relevant README section in the same task whenever one of those details changes. Do not wait for a separate documentation request.
- Describe the implemented behavior accurately; do not document planned or unsupported functionality.
- Preserve the README's existing structure and style. Make focused edits rather than duplicating existing explanations or rewriting unrelated sections.
- For test-only changes or internal refactors that do not affect documented behavior or instructions, leave `README.md` unchanged.
- Before finishing, verify that the README remains consistent with the resulting application code and mention whether it was updated or why no README change was needed.