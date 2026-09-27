CREATE TABLE note_shares (
    note_id INT NOT NULL CONSTRAINT fk_note_shares_notes_note_id REFERENCES notes (id) ON DELETE CASCADE,
    user_id INT NOT NULL CONSTRAINT fk_note_shares_users_user_id REFERENCES users (id),
    permission NVARCHAR(10) NOT NULL CONSTRAINT ck_note_shares_permission CHECK (permission IN (N'view', N'edit')),
    created_at DATETIME2 NOT NULL CONSTRAINT df_note_shares_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT pk_note_shares PRIMARY KEY (note_id, user_id)
);

CREATE INDEX ix_note_shares_user_id ON note_shares (user_id);
