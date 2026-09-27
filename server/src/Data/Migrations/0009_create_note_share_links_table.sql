CREATE TABLE note_share_links (
    note_id INT NOT NULL CONSTRAINT pk_note_share_links PRIMARY KEY
        CONSTRAINT fk_note_share_links_notes_note_id REFERENCES notes (id) ON DELETE CASCADE,
    token NVARCHAR(64) NOT NULL,
    created_at DATETIME2 NOT NULL CONSTRAINT df_note_share_links_created_at DEFAULT SYSUTCDATETIME()
);

CREATE UNIQUE INDEX ix_note_share_links_token ON note_share_links (token);
