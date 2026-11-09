# Claude Code skills (SS3D)

Curated and adapted from [mattpocock/skills](https://github.com/mattpocock/skills) v1.2.3
(MIT, see `LICENSE-mattpocock-skills`). Copied rather than installed as a plugin so they
follow SS3D's doc layers (`Documents/SKILL.md`) and Unity test loops, and so every
contributor gets the same set.

| Skill | Origin | SS3D changes |
|---|---|---|
| `grilling`, `grill-me` | upstream | Gameplay vs implementation altitude; docs-first grounding; write-up to design doc (owner-approved) or effort/plan |
| `diagnosing-bugs` | upstream | System-map Pitfalls first; EditMode/PlayMode/Unity MCP/smoke loops; Pitfalls banking |
| `tdd` | upstream | SS3D seams (pure logic, SubSystems, PlayMode); C# examples |
| `spec-review` | upstream `code-review` | Renamed (built-in `/code-review` exists); AGENTS.md + composition rules as standards; plans/efforts/design §§ as spec |
| `resolving-merge-conflicts`, `handoff`, `wait-what`, `to-questionnaire` | upstream | Paths/terms only |
| `wizard`, `writing-for-agents` | upstream | None |

Deliberately left out: `domain-modeling`, `grill-with-docs`, `setup-matt-pocock-skills`,
`improve-codebase-architecture`, `research` (parallel CONTEXT.md/ADR doc system);
`to-spec`, `to-tickets`, `triage`, `wayfinder`, `implement` (issue-tracker workflow; SS3D
uses `Documents/plans/` and `Documents/milestones/`); `teach` (writes workspace files);
`prototype`, `codebase-design` and the TypeScript tooling skills.

SS3D's own: `mixamo` (find, preview and download Mixamo clips via `Tools/mixamo/mixamo.py`).

Older SS3D skills (Unity perf, smoke harness) live in `.cursor/skills/`. `update-system-docs` lives there too; `.claude/skills/update-system-docs` is a thin pointer to it, so Claude Code can invoke it.
