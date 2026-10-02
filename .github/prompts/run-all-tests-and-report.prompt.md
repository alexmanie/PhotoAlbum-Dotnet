---
name: "Run All Tests and Report"
description: "Run every application test and write a Markdown results report in the project-root Results folder"
agent: "agent"
---

Run all tests in [PhotoAlbum.sln](../../PhotoAlbum.sln) from the project root and create or replace `Results/application-test-results.md`.

Requirements:

1. Run `dotnet test PhotoAlbum.sln` without a test filter so every test project and test case is included.
2. Capture the command's complete outcome, including its exit code and the passed, failed, skipped, and total test counts.
3. Create the `Results` directory when it does not exist.
4. Write a concise Markdown report containing:
   - Report generation date and time, including the time zone
   - Exact command executed
   - .NET SDK version
   - Overall `Passed` or `Failed` status based on the test command's exit code
   - Passed, failed, skipped, and total counts
   - A failure-details section with failed test names and useful error messages or stack traces; write `None` when no tests failed
   - Any warnings or test-run errors that may affect interpretation of the results
5. Report observed results only. Do not invent missing counts or claim success when the command fails or cannot run.
6. Do not modify application or test source code. Finish by linking to the generated report and briefly stating the overall status.