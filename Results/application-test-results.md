# Application Test Results

- **Generated:** 2026-10-02 11:32:18 +02:00
- **Command:** `dotnet test PhotoAlbum.sln --logger "console;verbosity=normal"`
- **.NET SDK:** 10.0.401
- **Exit code:** 1
- **Overall status:** Failed

## Summary

| Result | Count |
| --- | ---: |
| Passed | 10 |
| Failed | 2 |
| Skipped | 0 |
| Total | 12 |

Test duration reported by the runner: 2.1558 seconds. The complete command, including restore and build, finished in 8.7 seconds.

## Failure Details

### `PhotoAlbum.Tests.Unit.Services.PhotoServiceTests.UpdatePhotoAsync_WithExistingPhoto_UpdatesDimensions`

```text
Assert.Equal() Failure: Values differ
Expected: 1920
Actual:   1080

at PhotoAlbum.Tests.Unit.Services.PhotoServiceTests.UpdatePhotoAsync_WithExistingPhoto_UpdatesDimensions()
in C:\repos\PhotoAlbum-Dotnet\PhotoAlbum.Tests\Unit\Services\PhotoServiceTests.cs:line 265
--- End of stack trace from previous location ---
```

### `PhotoAlbum.Tests.Unit.Services.PhotoServiceTests.UpdatePhotoAsync_DoesNotChangeFileOrUploadMetadata`

```text
Assert.Equal() Failure: Values differ
Expected: 800
Actual:   600

at PhotoAlbum.Tests.Unit.Services.PhotoServiceTests.UpdatePhotoAsync_DoesNotChangeFileOrUploadMetadata()
in C:\repos\PhotoAlbum-Dotnet\PhotoAlbum.Tests\Unit\Services\PhotoServiceTests.cs:line 323
--- End of stack trace from previous location ---
```

## Warnings And Test-Run Errors

- The test command failed because two tests failed; restore and compilation succeeded before test execution.
- The console logger emitted duplicate xUnit discovery, execution, and failure lines. The final test summary was internally consistent and is the source for the counts above.
- No skipped tests or additional test-run warnings were reported.