CREATE TABLE refresh_tokens (
    id INT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_refresh_tokens PRIMARY KEY,
    token NVARCHAR(255) NOT NULL,
    expires_at DATETIME2 NOT NULL,
    is_revoked BIT NOT NULL CONSTRAINT df_refresh_tokens_is_revoked DEFAULT 0,
    user_id INT NOT NULL CONSTRAINT fk_refresh_tokens_users_user_id REFERENCES users (id) ON DELETE CASCADE,
    created_at DATETIME2 NOT NULL CONSTRAINT df_refresh_tokens_created_at DEFAULT SYSUTCDATETIME()
);

CREATE UNIQUE INDEX ix_refresh_tokens_token ON refresh_tokens (token);
CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
