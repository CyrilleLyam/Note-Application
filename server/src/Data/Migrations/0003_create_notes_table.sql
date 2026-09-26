CREATE TABLE notes (
    id INT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_notes PRIMARY KEY,
    user_id INT NOT NULL CONSTRAINT fk_notes_users_user_id REFERENCES users (id) ON DELETE CASCADE,
    title NVARCHAR(200) NOT NULL,
    content NVARCHAR(MAX) NULL,
    is_pinned BIT NOT NULL CONSTRAINT df_notes_is_pinned DEFAULT 0,
    created_at DATETIME2 NOT NULL CONSTRAINT df_notes_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    deleted_at DATETIME2 NULL,
    row_version ROWVERSION NOT NULL
);

CREATE INDEX ix_notes_user_id_deleted_at_created_at ON notes (user_id, deleted_at, created_at DESC);
