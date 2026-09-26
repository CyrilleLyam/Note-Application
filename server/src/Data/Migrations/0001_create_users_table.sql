CREATE TABLE users (
    id INT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_users PRIMARY KEY,
    username NVARCHAR(50) NOT NULL,
    email NVARCHAR(255) NOT NULL,
    password NVARCHAR(255) NOT NULL,
    created_at DATETIME2 NOT NULL CONSTRAINT df_users_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL CONSTRAINT df_users_updated_at DEFAULT SYSUTCDATETIME()
);

CREATE UNIQUE INDEX ix_users_email ON users (email);
