CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Courses" (
    "Id" int NOT NULL,
    "Name" nvarchar(100) NOT NULL,
    "Code" nvarchar(20),
    "Description" nvarchar(500),
    "Semester" nvarchar(50),
    "CreditHours" int NOT NULL,
    "CreatedAt" datetime2 NOT NULL,
    CONSTRAINT "PK_Courses" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251017123711_InitialCreate', '8.0.11');

COMMIT;

START TRANSACTION;

CREATE TABLE "Schools" (
    "Id" int NOT NULL,
    "Name" nvarchar(100) NOT NULL,
    "Address" nvarchar(500) NOT NULL,
    CONSTRAINT "PK_Schools" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251022091605_AddSchoolModel', '8.0.11');

COMMIT;

START TRANSACTION;

CREATE TABLE "Subjects" (
    "Id" int NOT NULL,
    "Name" nvarchar(100) NOT NULL,
    "Description" nvarchar(500),
    "AverageTimeInMinutes" int NOT NULL,
    "CreatedAt" datetime2 NOT NULL,
    CONSTRAINT "PK_Subjects" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251105162352_AddSubjectsTable', '8.0.11');

COMMIT;

