# Unity project rules

- Use normal filesystem editing for C# and text-based project files.
- Do not directly edit .unity, .prefab, .asset YAML unless Unity CLI/Pipeline is unavailable.
- Prefer Unity CLI to manipulate scenes, GameObjects, components and assets.
- Use `unity status` and `unity command` to inspect the running Editor.
- Use `unity command eval` for Editor API operations not covered by built-in commands.
- After C# changes, check compilation (`recompile_status` / `console_status`) before continuing.
- Use `--project-path <project-path>` (or current working directory `.`) explicitly for Unity CLI commands.
- If Git is initialized, inspect `git diff` after substantial changes; if not a Git repository, keep before/after file copies for diff review.
- If there is an Editor already running, do NOT launch a new Editor, and ask the user if intervention is needed.
- **Language Convention**: All in-project code, comments, identifiers, UI texts, asset names, and documentation must strictly use English (EN), regardless of what language the developer uses to communicate with agents, tough native language is allowed when communicate with developers in chats.

## Project & Environment status

- **Project Path**: `<project-path>` (root of the Unity project)
- **Pipeline Package**: `com.unity.pipeline` resolved and ready (default port: `7800`)
- **Input System**: New Input System (`com.unity.inputsystem`, `activeInputHandler: 1`)
- **Authoring Root**: `Assets/` (bare relative paths in asset commands resolve under `Assets/`)
- **Execution Environment**: Direct host shell access (local processes and sockets are directly accessible, no sandbox escalation needed)

## Cold-start routine

1. **Read project configurations**:
   Read this file, `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and `Packages/packages-lock.json`. Confirm editor versions with `unity editors`. Check whether the directory is a Git repository (`git status --short`); if Git metadata is unavailable, keep before/after file copies for diff review.

2. **Check Editor connection and state**:
   Run `unity status --project-path <project-path>` to check whether the Editor is running and ready.
   Run `unity command --project-path <project-path> editor_status` to inspect `status`, `compiling`, and `playMode`.
   Check `unity command --project-path <project-path> console_status` to confirm there are no existing compilation failures or unhandled errors.
   Do NOT automatically open another Editor or restart the current one if an Editor process is already running.

3. **Distinguish Pipeline states**:
   - `manifest.json` entry declares the dependency;
   - `packages-lock.json` and `Library/PackageCache/com.unity.pipeline*` show package resolution;
   - `unity status` reporting `ready` and successful `unity command editor_status` confirm a live socket connection.
   - If Pipeline is missing or unresolved: report the state and recommend `unity pipeline install --project-path <project-path>`. Do not open another Editor.

4. **Scene and Asset Inspection**:
   - List open scenes: `unity command --project-path <project-path> list_open_scenes`
   - Inspect hierarchy: `unity command --project-path <project-path> get_scene_hierarchy`
   - Use `unity command --project-path <project-path> editor_stop` before making persistent scene changes. Preserve unsaved user work; do not blindly reopen the on-disk scene without saving.
   - Save modifications explicitly: `unity command --project-path <project-path> save_all` or `save_scene`.

5. **Eval usage (when built-in commands are insufficient)**:
   When using `unity command eval`, provide valid C# statements returning a value, e.g.:
   `unity command --project-path <project-path> eval --code "return UnityEditor.EditorApplication.isPlaying.ToString();"`

6. **C# Script changes & Compilation**:
   After writing or editing C# files, trigger or wait for compilation and verify:
   - `unity command --project-path <project-path> recompile`
   - `unity command --project-path <project-path> recompile_status`
   - `unity command --project-path <project-path> console --tail 10`
   Wait for compilation to complete and ensure 0 compilation errors before proceeding.

7. **Functional verification & Play Mode**:
   - If Play Mode verification is needed: `unity command --project-path <project-path> editor_play`
   - Simulate inputs using `simulate_key` or `simulate_pointer` (compatible with the new Input System).
   - Release every simulated key, then stop Play Mode (`editor_stop`) before making persistent scene changes or building.

8. **Build verification**:
   - Validate build with dry-run first:
     `unity command --project-path <project-path> build --target StandaloneLinux64 --dry_run true`
   - Build using:
     `unity command --project-path <project-path> build --target StandaloneLinux64 --outputPath Builds/Linux/build.x86_64`
   - Poll `unity command --project-path <project-path> build_status` until status is `completed`, then inspect errors/warnings.
