---
agent: "Project-Generator"
tools:
  [
    "codebase",
    "mcp_baseline-mcp_GetContentTypes",
    "mcp_baseline-mcp_GetContentTypeDetails",
    "mcp_baseline-mcp_GetReusableFieldSchemas",
    "mcp_baseline-mcp_GetTaxonomies",
    "mcp_baseline-mcp_GetTaxonomyTags",
    "mcp_baseline-mcp_GetAllContentTypeIcons",
    "mcp_baseline-mcp_GenerateFeatureModulePlan",
    "mcp_baseline-mcp_GenerateContentTypeIntegrationPlan",
    "mcp_baseline-mcp_GenerateViewComponentPlan",
    "mcp_baseline-mcp_GenerateApiControllerPlan",
    "mcp_baseline-mcp_GenerateProjectPlan",
    "mcp_baseline-mcp_GenerateAdminClientPlan",
    "mcp_baseline-mcp_GenerateRCLProjectPlan",
    "mcp_baseline-mcp_GenerateModuleRegistrationPlan",
    "mcp_kentico-cm-mc_content_modeling_validate_requirements",
    "mcp_kentico-cm-mc_content_modeling_validate_content_types",
    "mcp_kentico-cm-mc_content_modeling_validate_relationships",
    "mcp_kentico-cm-mc_content_modeling_validate_pagebuilder",
    "mcp_kentico-cm-mc_content_modeling_final_validation",
    "mcp_kentico-docs_kentico_docs_search",
    "mcp_kentico-docs_kentico_docs_fetch",
    "mcp_xperience-man_list_content_types",
    "mcp_xperience-man_get_content_type_by_name",
    "mcp_xperience-man_create_content_types",
    "mcp_xperience-man_update_content_type_by_name",
    "mcp_xperience-man_list_reusable_schemas",
    "mcp_xperience-man_create_reusable_schemas",
    "mcp_xperience-man_list_data_types",
    "mcp_xperience-man_list_form_components",
    "mcp_xperience-man_list_taxonomies",
    "mcp_xperience-man_create_taxonomies",
    "mcp_xperience-man_create_tags",
  ]
description: "Generate a complete Kentico Xperience project with content model, admin UI, and frontend components"
---

# Generate Kentico Project

Generate a complete, validated Kentico Xperience by Kentico project using an
interactive questionnaire, content model validation, and scaffolding tools.

## ⚠️ CRITICAL WARNINGS

1. **DO NOT** use PowerShell, Python, or any programmatic tool to read/write
   `project_plan.json`. Use file read/edit operations only.
2. **DO NOT** skip content model validation (Phase 4). All content types MUST
   pass kentico-cm-mcp 5-phase validation before creation.
3. **DO NOT** create content types before validation passes.
4. **DO NOT** ask all questions at once. Ask in iterations of 3 questions max.
5. **DO NOT** proceed to the next phase without updating `project_plan.json`.
6. **DO NOT** mark the workflow complete until both `project_plan.json` and
   `PROJECT_DOCUMENTATION.md` exist.

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding.

## Workflow Phases

| Phase | Name                     | Action                                       |
| ----- | ------------------------ | -------------------------------------------- |
| 1     | Requirements Gathering   | Ask 3 questions per iteration (5 iterations) |
| 2     | Discovery                | baseline-mcp + xperience-management-api      |
| 3     | Documentation Check      | kentico-docs search + fetch                  |
| 4     | Content Model Validation | kentico-cm-mcp 5 sub-phases                  |
| 5     | Content Model Creation   | xperience-management-api CRUD                |
| 6     | Project Scaffolding      | baseline-mcp scaffolding tools               |
| 7     | Verification & Output    | dotnet build + PROJECT_DOCUMENTATION.md      |

## Phase-by-Phase Execution

### Phase 1: Requirements Gathering

Ask questions in **5 iterations of 3 questions**. Use alphabetical
multiple-choice (`a)`, `b)`, `c)`…) for every question. Number questions
starting from 1 in each iteration.

**Iteration 1** — Project Identity: name, namespace, description
**Iteration 2** — Project Structure: type, sub-projects, channels
**Iteration 3** — Content Model: strategy, content types, reusable schemas
**Iteration 4** — Technical Requirements: admin UI, frontend, features
**Iteration 5** — Dependencies: Baseline deps, taxonomies, constraints

After all iterations:

- Create `project_plan.json` with requirements under `requirements` node
- Initialize empty structures for all subsequent phases
- Set `metadata.phases.requirements_gathering.status` to `"completed"`
- Show requirements summary, wait for user approval

### Phase 2: Discovery

Call these tools to understand existing state:

1. `GetContentTypes` — existing content types
2. `GetReusableFieldSchemas` — shared schemas
3. `GetTaxonomies` — classification structures
4. `GetContentTypeDetails` — inspect related types
5. `list_data_types` — available field data types
6. `list_form_components` — available form components

After discovery:

- Read `project_plan.json`, add results to `discovery` object
- Present summary: existing types, conflicts, reuse opportunities
- Set `metadata.phases.discovery.status` to `"completed"`
- Wait for user approval

### Phase 3: Documentation Verification

Call `kentico_docs_search` and `kentico_docs_fetch` to verify patterns:

1. Content type creation best practices
2. Admin customization patterns (if admin UI)
3. RCL/widget development (if RCL)
4. Module registration patterns
5. DI registration best practices

After verification:

- Read `project_plan.json`, add patterns to `documentationNotes` array
- Present any deviations from initial plan
- Set `metadata.phases.documentation_check.status` to `"completed"`
- Wait for user approval

### Phase 4: Content Model Validation (5 sub-phases)

Run kentico-cm-mcp validation sequentially:

**4a:** `content_modeling_validate_requirements` → update JSON
**4b:** `content_modeling_validate_content_types` → update JSON
**4c:** `content_modeling_validate_relationships` → update JSON
**4d:** `content_modeling_validate_pagebuilder` (skip if no Website types) →
update JSON
**4e:** `content_modeling_final_validation` — **🚨 MANDATORY, do NOT skip**

For each sub-phase:

- Read `project_plan.json` before calling tool
- Pass the complete JSON string to the tool
- Update JSON with validation results
- If validation fails → iterate on model and re-validate

After all sub-phases pass:

- Set all `content_model_*` phase statuses to `"completed"`
- Present validated content model summary
- Wait for user approval

### Phase 5: Content Model Creation

After validation passes, create in dependency order:

1. `create_reusable_schemas` — shared schemas first
2. `create_content_types` — reusable before website types
3. `get_content_type_by_name` — verify each creation
4. `create_taxonomies` + `create_tags` — if defined

After creation:

- Read `project_plan.json`, add to `createdArtifacts` object
- Set `metadata.phases.content_model_creation.status` to `"completed"`
- Wait for user approval

### Phase 6: Project Scaffolding

Use baseline-mcp scaffolding tools:

1. **GenerateProjectPlan** — .csproj files, solution structure, DI extensions
2. **GenerateAdminClientPlan** — React/TS admin client (if admin UI)
3. **GenerateRCLProjectPlan** — Razor components, widgets (if RCL)
4. **GenerateModuleRegistrationPlan** — Module class, installer, middleware
5. **GenerateFeatureModulePlan** — per-feature scaffolding
6. **GenerateContentTypeIntegrationPlan** — DTOs, mappers, query services
7. **GenerateViewComponentPlan** — ViewComponent features
8. **GenerateApiControllerPlan** — API endpoints

After scaffolding:

- Read `project_plan.json`, add file plans to `scaffolding` object
- Create ALL generated files in the workspace
- Set `metadata.phases.project_scaffolding.status` to `"completed"`
- Wait for user approval

### Phase 7: Verification & Final Output

1. Run `dotnet build` — fix errors if any
2. Run `npm install` + `npm run build` (if admin client)
3. Create `PROJECT_DOCUMENTATION.md` incrementally (section by section):
   - Project summary + requirements
   - Project structure tree
   - Content types with field details
   - Mermaid architecture diagram
   - DI registration for `Program.cs`
   - Admin setup instructions (if applicable)
   - Next steps (`kxp-codegen`, `kxp-ci-store`, ProjectReferences)
4. Present final summary
5. Confirm both `project_plan.json` and `PROJECT_DOCUMENTATION.md` exist

Set `metadata.phases.verification_and_output.status` to `"completed"`

## Incremental JSON Management

- **ALWAYS** read `project_plan.json` before modifying it
- **NEVER** overwrite data from previous phases
- **ALWAYS** update `metadata.phases` after completing each phase
- Use file edit operations to add/modify JSON — no programmatic tools
