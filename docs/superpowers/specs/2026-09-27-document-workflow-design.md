# DocumentWorkflow design

## Goal

Move deterministic document preparation orchestration out of `MainWindow` while preserving current preview, export, validation, warning, tie-break, PDF, and print behavior. Keep this as a small architectural step after the `Save_Click` integration merged in PR #18.

## Current flow

`MainWindow.Documents.cs` currently handles document selection, unsupported-type messages, export readiness checks, advisory validation, date warnings, user confirmation, tie-winner selection, `OfficialDocumentService.BuildDocument`, PDF save dialog/file writing, preview window, and print dialog. `DocumentReadinessService` and `DocumentValidationService` already contain reusable deterministic checks. `OfficialDocumentService` builds WPF `FixedDocument` instances and is exercised by Windows tests. `MainWindow.DataHome.cs` independently computes readiness and advisory summaries for the home screen.

## Boundary and first increment

Add a WPF-layer `DocumentWorkflow` that coordinates document readiness, document validation, tie detection, and document construction through the existing services. It returns a result that lets the caller distinguish blocked export requirements, advisories requiring user confirmation, unresolved winner selection, successful generation, and generation failure. Preserve validation messages and severity values.

The workflow accepts a `ProcurementData` snapshot, selected implemented document types, preview/export intent, and any already confirmed advisories or selected preferred winner. It must not read WPF controls or show dialogs. It must not mutate the caller's data snapshot. The UI remains responsible for collecting selections, showing messages and confirmations, asking the user to select a tied winner, and applying that winner to UI state before retrying preparation.

Preview continues to allow incomplete information. Export continues to block on the existing core readiness rules, ask before continuing when advisory/date issues exist, and ask the user to resolve a tie only for documents that require a winner. Generation occurs only after the same checks and confirmations as today.

## UI and output responsibilities

In the first increment, `MainWindow` remains responsible for WPF interaction and output destinations: preview window, `SaveFileDialog`, PDF folder selection/default, PDF writing, `PrintDialog`, printing, success notifications, and user-facing exception messages. A save dialog's chosen path is user input and must not be guessed by the workflow. A later increment may move the non-interactive PDF write/result handling behind a separate output service, without moving the dialog into the workflow.

## Errors and result handling

Expected incomplete data is represented in the workflow result, not thrown as an exception. Unexpected generation errors are returned with their exception/message so the UI can preserve the existing localized action-specific error messages. Cancellation of a dialog or tie selection remains a normal UI cancellation and does not invoke generation. The workflow does not absorb or alter existing validation text.

## Compatibility and scope limits

- Keep `OfficialDocumentService`, its `FixedDocument` output, and all document layouts unchanged.
- Keep `ProcurementEngine` calculation and tie-break semantics unchanged.
- Keep document type selection and supported-type checks in the UI for this first increment.
- Do not change PDF path defaults, output file naming, print behavior, or home-screen readiness presentation.
- Do not fold archive, backup/recovery, or ViewModel work into this change.

## Verification criteria

- Preview with incomplete data remains available when the document generator supports the selected type.
- Export is blocked by the same core readiness issues and produces the same user warning/confirmation sequence.
- Advisory/date validation messages and severity remain unchanged.
- Winner selection is requested for the same document types and remains persisted through the existing UI state path.
- PDF save location, default filename/folder, print dialog, and success/error messages remain unchanged.
- Windows CI passes, including existing document layout/PDF tests and focused `DocumentWorkflow` result/branch tests.

## Rollout

Implement as one focused PR after this design is reviewed. Keep output dialogs and PDF/print mechanics in `MainWindow` for that PR. After merge, perform the scheduled real Windows app flow check before taking on archive or output-service changes.
