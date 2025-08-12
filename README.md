# User Management Service – User Manual

## Current Progress

- User registration, login, verification, availability management, professional status, job claiming, and Elo rating update APIs are functional.  
- Three-way resolution logic is implemented.  
- Admin-level APIs for setting professional status are complete.
- All APIs from the doc, including the additional "change-password", are functional.
- Pending tasks: Kafka, more testing

---

## Workflow Overview

### User Lifecycle APIs

| Method | Endpoint                          | Description                                                     |
|--------|---------------------------------|-----------------------------------------------------------------|
| POST   | `/api/v1/users/register`         | Registers a new user with personal details and consent flags. Returns `201 Created` with user ID. |
| POST   | `/api/v1/users/login`            | Authenticates a user and returns a JWT token for subsequent requests. |
| POST   | `/api/v1/users/change-password` | Updates user password after validating the current password.    |
| POST   | `/api/v1/users/{userId}/verify` | Verifies user documents and contact info based on validation rules. |

---

### Availability & Job Claiming

| Method | Endpoint                                  | Description                                                           |
|--------|-------------------------------------------|----------------------------------------------------------------------|
| GET    | `/api/v1/users/available-for-work`        | Lists all users available for job assignment.                        |
| GET    | `/api/v1/users/{userId}/availability`     | Retrieves availability status of a user.                             |
| PATCH  | `/api/v1/users/{userId}/availability`     | Updates availability parameters for a user.                          |
| POST   | `/api/v1/users/{userId}/claim-job`         | Claims a job for the user, stores claim details, validates job limits.|
| POST   | `/api/v1/users/{userId}/validate-tiebreaker-claim` | Checks if user meets tiebreaker requirements. If so, stores the claim                       |

---

### Profile & Professional Status

| Method | Endpoint                               | Description                                                        |
|--------|----------------------------------------|-------------------------------------------------------------------|
| GET    | `/api/v1/users/{userId}/profile`       | Fetches full profile details of the user.                         |
| GET    | `/api/v1/users/{userId}/professional-status` | Returns professional status and verification level.               |
| POST   | `/api/v1/users/check-professional-status` | Batch check of multiple users’ professional status.               |
| POST   | `/api/v1/admin/users/{userId}/set-professional` | Admin-only endpoint to mark a user as professional.               |

---

### Elo Rating & History

| Method | Endpoint                        | Description                                                        |
|--------|--------------------------------|-------------------------------------------------------------------|
| GET    | `/api/v1/users/{userId}/elo-history` | Retrieves Elo rating history for a user.                           |
| POST   | `/api/v1/elo-update`            | Applies Elo updates based on QA comparison results.                |
| POST   | `/api/v1/elo-update/three-way-resolution` | Processes Elo updates in three-way comparison scenarios.           |

---

## Processing Flow

### Registration & Verification

- User data is stored in PostgreSQL.  
- If verification is required, validation rules from `ValidationOptions` are applied (e.g. ID document, photo, phone, email).

### Availability & Job Assignment

- Availability state is cached in Redis with a TTL configured by `RedisCacheTtlMinutes`.  
- Job claim requests are validated against `MaxConcurrentJobs` and Elo thresholds.

### Elo Rating Updates

- Updates are triggered by the workflow engine via `WorkflowEngineOptions.EloNotifyUri`.  
- New Elo value is computed based on K-factor rules (`EloKFactorNew`, `EloKFactorEstablished`, `EloKFactorExpert`).  
- Ratings are persisted in PostgreSQL and cached in Redis.

---

*For further details or pending features, refer to the project documentation or contact the development team.*
