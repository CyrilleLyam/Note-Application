CREATE TABLE notifications (
    id INT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_notifications PRIMARY KEY,
    user_id INT NOT NULL CONSTRAINT fk_notifications_users_user_id REFERENCES users (id) ON DELETE CASCADE,
    type NVARCHAR(50) NOT NULL,
    note_id INT NULL,
    note_title NVARCHAR(200) NULL,
    actor_name NVARCHAR(100) NULL,
    permission NVARCHAR(10) NULL,
    created_at DATETIME2 NOT NULL CONSTRAINT df_notifications_created_at DEFAULT SYSUTCDATETIME(),
    read_at DATETIME2 NULL
);

CREATE INDEX ix_notifications_user_id_created_at ON notifications (user_id, created_at DESC) INCLUDE (read_at);
