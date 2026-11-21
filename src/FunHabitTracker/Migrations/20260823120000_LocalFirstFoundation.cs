using FunHabitTracker.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunHabitTracker.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260823120000_LocalFirstFoundation")]
public sealed class LocalFirstFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "ArchivedAt", table: "Habits", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsArchived", table: "Habits", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<int>(name: "RecurrenceType", table: "Habits", type: "INTEGER", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "ScheduledDays", table: "Habits", type: "INTEGER", nullable: false, defaultValue: 127);
        migrationBuilder.AddColumn<DateOnly>(name: "StartDate", table: "Habits", type: "TEXT", nullable: false, defaultValue: new DateOnly(2000, 1, 1));
        migrationBuilder.AddColumn<DateTime>(name: "UpdatedAt", table: "Habits", type: "TEXT", nullable: false, defaultValue: DateTime.UnixEpoch);

        migrationBuilder.Sql("""
            UPDATE Habits
            SET StartDate = date(CreatedAt),
                UpdatedAt = CreatedAt,
                IsArchived = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END,
                ArchivedAt = CASE WHEN IsActive = 1 THEN NULL ELSE CreatedAt END,
                RecurrenceType = 0,
                ScheduledDays = 127;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE HabitCompletions_LocalFirst (
                Id INTEGER NOT NULL CONSTRAINT PK_HabitCompletions PRIMARY KEY AUTOINCREMENT,
                HabitId INTEGER NOT NULL,
                Date TEXT NOT NULL,
                IsDone INTEGER NOT NULL,
                Outcome INTEGER NOT NULL CONSTRAINT CK_HabitCompletions_Outcome CHECK (Outcome IN (1, 2)),
                CONSTRAINT FK_HabitCompletions_Habits_HabitId FOREIGN KEY (HabitId) REFERENCES Habits (Id) ON DELETE CASCADE
            );
            INSERT INTO HabitCompletions_LocalFirst (Id, HabitId, Date, IsDone, Outcome)
            SELECT Id, HabitId, Date, 1, 1 FROM HabitCompletions WHERE IsDone = 1;
            DROP TABLE HabitCompletions;
            ALTER TABLE HabitCompletions_LocalFirst RENAME TO HabitCompletions;
            CREATE UNIQUE INDEX IX_HabitCompletions_HabitId_Date ON HabitCompletions (HabitId, Date);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE HabitCompletions_Legacy (
                Id INTEGER NOT NULL CONSTRAINT PK_HabitCompletions PRIMARY KEY AUTOINCREMENT,
                HabitId INTEGER NOT NULL,
                Date TEXT NOT NULL,
                IsDone INTEGER NOT NULL,
                CONSTRAINT FK_HabitCompletions_Habits_HabitId FOREIGN KEY (HabitId) REFERENCES Habits (Id) ON DELETE CASCADE
            );
            INSERT INTO HabitCompletions_Legacy (Id, HabitId, Date, IsDone)
            SELECT Id, HabitId, Date, CASE WHEN Outcome = 1 THEN 1 ELSE 0 END FROM HabitCompletions;
            DROP TABLE HabitCompletions;
            ALTER TABLE HabitCompletions_Legacy RENAME TO HabitCompletions;
            CREATE UNIQUE INDEX IX_HabitCompletions_HabitId_Date ON HabitCompletions (HabitId, Date);
            """);

        migrationBuilder.DropColumn(name: "ArchivedAt", table: "Habits");
        migrationBuilder.DropColumn(name: "IsArchived", table: "Habits");
        migrationBuilder.DropColumn(name: "RecurrenceType", table: "Habits");
        migrationBuilder.DropColumn(name: "ScheduledDays", table: "Habits");
        migrationBuilder.DropColumn(name: "StartDate", table: "Habits");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "Habits");
    }
}
