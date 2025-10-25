# Contracts: Electron IPC

Defines IPC channels and schemas used between Electron (shell) and the ASP.NET Core/Blazor app.

## Channels

- `ipc:openFileDialog`
- `ipc:saveFileDialog`
- `ipc:persistUnsaved`
- `ipc:restoreSession`
- `ipc:app:window`

See JSON Schemas in this folder for request/response shapes.

## Schema compatibility

The JSON Schemas use draft 2020-12 by default for richer validation. If your CI validator does not support 2020-12, either:

- Switch `$schema` to `https://json-schema.org/draft-07/schema#`, or
- Provide draft-07 mirror schemas for CI only and keep 2020-12 locally.

Contract tests should validate messages against the active draft in CI to prevent drift.
