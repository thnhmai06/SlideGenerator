PRAGMA journal_mode= WAL;

CREATE TABLE IF NOT EXISTS Recipes
(
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    Name             TEXT NOT NULL,
    Recipe           TEXT NOT NULL,
    CreatedTimestamp TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    UpdatedTimestamp TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE TABLE IF NOT EXISTS Requests
(
    RequestId       TEXT PRIMARY KEY,
    RecipeId        INTEGER NOT NULL,
    Name            TEXT    NOT NULL,
    OutputType      TEXT    NOT NULL,
    SaveFolder      TEXT    NOT NULL,
    AllowLocalPaths INTEGER NOT NULL,
    LogPath         TEXT    NOT NULL,
    CreatedAt       TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Jobs
(
    RequestId                TEXT    NOT NULL,
    JobId                    INTEGER NOT NULL,
    Status                   TEXT    NOT NULL,
    Phase                    TEXT    NOT NULL DEFAULT 'CreatingOutput',
    CurrentIndex             INTEGER NOT NULL DEFAULT 0,

    WorkbookPath             TEXT    NOT NULL,
    WorksheetName            TEXT    NOT NULL,
    UsedColumnsJson          TEXT    NULL,
    RowFilterType            TEXT    NULL,
    RowFilterStart           INTEGER NULL,
    RowFilterEnd             INTEGER NULL,
    RowFilterPartitionIndex  INTEGER NULL,
    RowFilterPartitionCount  INTEGER NULL,

    TemplatePresentationPath TEXT    NOT NULL,
    TemplateSlideIndex       INTEGER NOT NULL,
    TextInstructionsJson     TEXT    NOT NULL,
    ImageInstructionsJson    TEXT    NOT NULL,

    OutputPath               TEXT    NOT NULL,
    Timestamp                TEXT    NOT NULL,
    PRIMARY KEY (RequestId, JobId)
);
