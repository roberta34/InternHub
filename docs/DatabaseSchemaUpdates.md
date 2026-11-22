# Database schema updates

The API currently initializes SQLite with `Database.EnsureCreated()`. `EnsureCreated`
creates the current model only when the database schema does not already exist; it
does not apply later model changes to an existing `internhub.db`.

The active-candidacy uniqueness index must therefore be applied to an existing
database with a non-destructive schema operation:

```sql
SELECT StudentId, OfferId, COUNT(*) AS ActiveCount
FROM InternshipCandidacies
WHERE Status IN ('Submitted', 'Shortlisted', 'Accepted', 'Waitlisted')
GROUP BY StudentId, OfferId
HAVING COUNT(*) > 1;
```

The query must return no rows before creating the index. Resolve any reported
duplicates using an approved data-retention decision; do not delete or recreate
the database automatically. After the duplicate check succeeds, apply:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS IX_InternshipCandidacies_StudentId_OfferId
ON InternshipCandidacies (StudentId, OfferId)
WHERE Status IN ('Submitted', 'Shortlisted', 'Accepted', 'Waitlisted');
```

Back up the database first and verify the index afterward with:

```sql
SELECT name, sql
FROM sqlite_master
WHERE type = 'index'
  AND name = 'IX_InternshipCandidacies_StudentId_OfferId';
```

This index protects the one-active-candidacy rule for each student/offer pair. It
does not resolve the separate maximum-five-active-candidacies concurrency issue.

## Identity tables for existing databases

`EnsureCreated()` does not add the Identity schema to an existing database. The
following additive SQLite script was generated from the current EF Core model
and uses `IF NOT EXISTS`; it creates only the `AspNet*` tables and their indexes.
It does not replace existing application tables.

Before applying:

1. Stop the API and make a backup copy of `internhub.db`.
2. Verify the current Identity tables and indexes. If any are already present,
   compare their definitions with the script below; `IF NOT EXISTS` does not
   update an incompatible or partial schema.
3. Apply the script to the backed-up database, then verify the resulting tables
   and indexes.

Inspect the existing schema:

```sql
SELECT type, name, tbl_name, sql
FROM sqlite_master
WHERE (type = 'table' AND name LIKE 'AspNet%')
   OR (type = 'index' AND tbl_name LIKE 'AspNet%')
ORDER BY type, name;
```

Apply this script only after the inspection:

```sql
CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "StudentId" TEXT NULL,
    "RecruiterId" TEXT NULL,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId"
        FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId"
        FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId"
        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId"
    ON "AspNetRoleClaims" ("RoleId");
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex"
    ON "AspNetRoles" ("NormalizedName");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId"
    ON "AspNetUserClaims" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId"
    ON "AspNetUserLogins" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId"
    ON "AspNetUserRoles" ("RoleId");
CREATE INDEX IF NOT EXISTS "EmailIndex"
    ON "AspNetUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex"
    ON "AspNetUsers" ("NormalizedUserName");
```

Verify after applying:

```sql
SELECT type, name, tbl_name, sql
FROM sqlite_master
WHERE (type = 'table' AND name LIKE 'AspNet%')
   OR (type = 'index' AND tbl_name LIKE 'AspNet%')
ORDER BY type, name;
```

The script intentionally does not alter or remove existing user, student, offer,
or candidacy data.
