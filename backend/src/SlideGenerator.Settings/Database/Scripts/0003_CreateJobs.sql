CREATE TABLE IF NOT EXISTS Jobs (
    RequestId  TEXT NOT NULL,
    JobId      INTEGER NOT NULL,
    Status     TEXT NOT NULL,
    Phase      TEXT NOT NULL DEFAULT 'CreatingOutput',
    CurrentIndex INTEGER NOT NULL DEFAULT 0,

    WorkbookPath   TEXT NOT NULL,
    WorksheetName  TEXT NOT NULL,
    UsedColumnsJson TEXT NULL,
    RowFilterType  TEXT NULL,
    RowFilterStart INTEGER NULL, RowFilterEnd INTEGER NULL,
    RowFilterPartitionIndex INTEGER NULL, RowFilterPartitionCount INTEGER NULL,

    TemplatePresentationPath TEXT NOT NULL,
    TemplateSlideIndex       INTEGER NOT NULL,
    TextInstructionsJson     TEXT NOT NULL,
    ImageInstructionsJson    TEXT NOT NULL,

    OutputPath TEXT NOT NULL,
    Timestamp  TEXT NOT NULL,
    PRIMARY KEY (RequestId, JobId)
);
