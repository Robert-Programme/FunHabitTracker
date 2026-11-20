CREATE TABLE "Habits" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Habits" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Frequency" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);


CREATE TABLE "HabitCompletions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_HabitCompletions" PRIMARY KEY AUTOINCREMENT,
    "HabitId" INTEGER NOT NULL,
    "Date" TEXT NOT NULL,
    "IsDone" INTEGER NOT NULL,
    CONSTRAINT "FK_HabitCompletions_Habits_HabitId" FOREIGN KEY ("HabitId") REFERENCES "Habits" ("Id") ON DELETE CASCADE
);


CREATE UNIQUE INDEX "IX_HabitCompletions_HabitId_Date" ON "HabitCompletions" ("HabitId", "Date");
