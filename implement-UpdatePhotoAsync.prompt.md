## Plan: Implement Photo Metadata Update

Implement [UpdatePhotoAsync](PhotoAlbum/Services/IPhotoService.cs#L42) using EF Core’s tracked-entity pattern. Only `Width` and `Height` will be editable.

**Steps**

1. Add `UpdatePhotoAsync(Photo photo)` to [PhotoService.cs](PhotoAlbum/Services/PhotoService.cs), preferably after `DeletePhotoAsync`.

2. Wrap the method body in `try/catch`, matching the existing service methods.

3. Load the persisted entity with `_context.Photos.FindAsync(photo.Id)`. This produces a tracked entity.

4. If no entity exists:
   - Log a warning using `{PhotoId}`.
   - Return `false`.

5. Update only:
   - `existingPhoto.Width = photo.Width`
   - `existingPhoto.Height = photo.Height`

   Do not use `_context.Update(photo)` because it could overwrite filenames, paths, MIME type, file size, and upload timestamp supplied by an untrusted caller.

6. Persist changes with `_context.SaveChangesAsync()`.

7. Log successful completion and return `true`.

8. In the `catch` block, log the exception with the photo ID and rethrow it, consistent with the other service methods.

**Tests**

Add these cases to [PhotoServiceTests.cs](PhotoAlbum.Tests/Unit/Services/PhotoServiceTests.cs):

1. Existing photo updates both dimensions and returns `true`.
2. Unknown ID returns `false`.
3. File identity and upload metadata remain unchanged when conflicting values are passed.
4. Null dimensions clear previously stored dimensions.

**Verification**

Run:

- `dotnet test PhotoAlbum.Tests/PhotoAlbum.Tests.csproj --filter "FullyQualifiedName~PhotoServiceTests.UpdatePhotoAsync"`
- `dotnet test PhotoAlbum.Tests/PhotoAlbum.Tests.csproj`
- `dotnet build PhotoAlbum.sln`

No database migration or UI changes are required because [Photo.cs](PhotoAlbum/Models/Photo.cs#L54) already contains nullable `Width` and `Height` fields.
