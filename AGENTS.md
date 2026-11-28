# AGENTS.md

## Workflow
- Before a large change, give the human a short plan (a few bullets) and wait for approval before editing any code.
- Stay in scope. No drive-by refactors, renames or dependency bumps in code the task doesn't need.

## Code
- Write the least code that works. No placeholder files, wrapper types that only rename a library, unused API, or extra `pub` items.
- Add a dependency only when code uses it, with minimal features.
- Write only what reading the code once wouldn't tell you. That applies to docs, READMEs, PR descriptions and this file.
- No comments or doc comments. Exception: a comment on a non-obvious constraint or solution that would greatly help another developer. Suggest it to the human first.
- No tutorial READMEs or `examples/` demos unless asked.
- Use clear variable names that say what they're for. Abbreviate only when it's really obvious.
- Changes to `../shared/` affect both Amp and Maestro.
- No magic numbers

## Git
- Never commit, push or force-push unless asked.
- Never merge or rebase unless asked. When asked, prefer rebase so there are no merge commits.

## Testing (Amp)
- Test what Maestro or another crate relies on: gRPC calls, the shared state, the DSP. Skip private helpers and code that only passes data along.
- gRPC and anything across crates: integration tests in `crates/<crate>/tests/`. Start the server on a free local port and call it with the generated client. Shared helpers go in `tests/common/mod.rs`.
- Pure logic (DSP, state): unit tests in a `#[cfg(test)] mod tests` at the end of the same file.
- No test may need a sound card, a Pi, a screen or the internet. CI has none of them. Use the embedded snapserver from `amp-control/tests/common` instead of a real one.
- No QML tests in Rust for now (see #139).
- CI runs `cargo clippy --all-targets -- -D warnings` and `cargo test` on every pull request.

## Done means (Amp)
- `cargo build`, `cargo test` and `cargo clippy --all-targets -- -D warnings` pass.
- You re-read your whole diff and deleted every line that isn't needed.
- You reported exactly what you ran. Don't claim sound, LAN or UI works unless a human checked it.

