CREATE TABLE tags (
    id INT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_tags PRIMARY KEY,
    user_id INT NOT NULL CONSTRAINT fk_tags_users_user_id REFERENCES users (id) ON DELETE CASCADE,
    name NVARCHAR(30) NOT NULL,
    created_at DATETIME2 NOT NULL CONSTRAINT df_tags_created_at DEFAULT SYSUTCDATETIME()
);

CREATE UNIQUE INDEX ix_tags_user_id_name ON tags (user_id, name);

CREATE TABLE note_tags (
    note_id INT NOT NULL CONSTRAINT fk_note_tags_notes_note_id REFERENCES notes (id) ON DELETE CASCADE,
    tag_id INT NOT NULL CONSTRAINT fk_note_tags_tags_tag_id REFERENCES tags (id),
    CONSTRAINT pk_note_tags PRIMARY KEY (note_id, tag_id)
);

CREATE INDEX ix_note_tags_tag_id ON note_tags (tag_id);
