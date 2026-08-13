PRAGMA journal_mode=WAL;

CREATE TABLE IF NOT EXISTS Recipes (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    Name      TEXT NOT NULL,
    Recipe            TEXT NOT NULL,
    CreatedTimestamp TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    UpdatedTimestamp TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);
