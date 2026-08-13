CREATE TABLE IF NOT EXISTS Requests (
    RequestId       TEXT PRIMARY KEY,
    RecipeId        INTEGER NOT NULL,
    Name            TEXT NOT NULL,
    OutputType      TEXT NOT NULL,
    SaveFolder      TEXT NOT NULL,
    AllowLocalPaths INTEGER NOT NULL,
    LogPath         TEXT NOT NULL,
    CreatedAt       TEXT NOT NULL
);
