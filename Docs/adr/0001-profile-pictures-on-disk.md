# ADR 0001: Store profile pictures on disk instead of in the database

## Status

Accepted
2026-11-25

## Context

All data of the application is stored in the PostgreSQL database: users, quizzes, scores and answers. Profile pictures are the one exception, so this needs a clear reason.

A user can upload one JPEG or PNG picture of at most 2 MB. The picture is shown on the home screen and on the profile page, so it is read much more often than it is written. Unlike the other data, the picture is not queried, filtered or joined with anything. It is only read and written as a whole.

## Decision

- The picture is saved as a file in a folder on the server (`Uploads` by default, changed with the `Uploads:Path` setting). The folder is in `.gitignore`.
- The file is named `{userId}{extension}`. The name from the client is never used, so it cannot be used for path tricks.
- The database stores only the file name, in `User.ProfilePictureFilename`.
- A new upload replaces the old file. If the extension changes, the old file is deleted.
- The file is saved and sent as a stream, so the whole picture is not kept in memory.
- The picture is only available through `GET /api/profile/picture`, which needs a login and uses the user from the token.

## Alternatives considered

- **Store the image bytes in the database (`bytea` column):** this keeps "all data is in the database". Backups and deleting a user stay consistent, and no extra folder has to be managed. It was not chosen because every picture request would go through the database connection and memory of the API, and the bytes would be loaded with the user row unless every query avoids that column. Pictures of up to 2 MB would also make the database and its backups larger for data that is never queried.
- **Store the image in a separate table:** this avoids loading the bytes with the user row, but the pictures still make the database larger and still go through the database connection.
- **Object storage (S3, Azure Blob Storage):** this is the usual choice when an application has several servers. It adds an external service, an account and a cost, which is too much for one server and small pictures.

## Consequences

- Good, because the database stays small and fast, and its backups do not grow with every upload.
- Good, because no extra infrastructure is needed. A folder on the server is enough, with no external service, account or cost.
- Good, because the database stores only the file name. If the project later moves to shared storage, only the code that reads and writes the file changes.
- Bad, because a database backup does not include the `Uploads` folder, so the folder has to be backed up separately. If a file is missing, the API returns 404 and the client shows the placeholder.
- Bad, because the files live on the disk of one server. With several instances the pictures would have to move to shared storage. The project runs on one instance, so this is acceptable for now.
- Neutral, because deleting a user does not delete the file. The application has no user deletion yet, and when it is added it also has to delete the file.
