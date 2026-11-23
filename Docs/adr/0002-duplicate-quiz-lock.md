# ADR 0002: Prevent duplicate quizzes with a lock in memory

## Status

Accepted
2026-11-22

## Context

A quiz is a duplicate when another quiz the user can see (a public quiz or one of their own private quizzes) has the same title, category and language. Titles are compared case-insensitively and ignoring leading and trailing spaces. Another user's private quiz with the same title is not a duplicate.

The duplicate check and the save are two separate steps. When two identical create requests arrive at the same time, both can run the check before either has saved, so both pass and two identical quizzes are saved.

## Decision

- The duplicate check and the save run under a `SemaphoreSlim` with a count of 1, so only one request at a time can do them.
- Each category and language pair has its own lock. A duplicate is only possible within the same pair, so creating quizzes in other pairs does not wait.
- The locks are kept in a `ConcurrentDictionary` inside the singleton service `QuizCreationLocks`, so all requests use the same locks.
- The lock is released in `finally`, so it is also released when a duplicate is found (409) or an error happens.

## Alternatives considered

- **Unique index in the DB:** the usual way to prevent duplicates. However, an index cannot express the visibility rule: another user's private quiz with the same title is allowed. Comparing titles ignoring case and leading and trailing spaces would also need an extra normalized column.
- **One lock for all quiz creation:** simpler, because no dictionary is needed. But every create request would wait for the others, even when a duplicate is impossible (different category or language).
- **C# `lock`:** the standard way to synchronize threads. It does not fit, because `await` cannot be used inside a `lock`, and the duplicate check and the save are async.
- **PostgreSQL advisory lock or a serializable transaction:** the lock would live in a database, so it would also work with several server instances. It is more complex: a serializable transaction fails on a conflict, so the request has to be retried. The project runs on one server instance, so a lock in memory is enough.

## Consequences

- Good, because two identical quizzes can no longer be saved when they are created at the same time. An integration test sends identical create requests at once and checks that only one is created.
- Good, because creating quizzes in different categories or languages does not wait, and requests for the same pair wait only for the duplicate check and the save.
- Bad, because the lock only works within one server instance. If the API ran as several instances, each would have its own locks, and a lock in the database would be needed instead.
- Neutral, because the dictionary keeps one lock for every category and language pair that was used. The number of pairs is limited (categories * languages), so this does not grow without limit.