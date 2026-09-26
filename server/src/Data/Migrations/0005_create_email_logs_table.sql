CREATE TABLE email_logs (
    id INT IDENTITY(1, 1) NOT NULL CONSTRAINT pk_email_logs PRIMARY KEY,
    recipient NVARCHAR(255) NOT NULL,
    subject NVARCHAR(255) NOT NULL,
    template_type NVARCHAR(50) NOT NULL,
    payload NVARCHAR(MAX) NOT NULL CONSTRAINT ck_email_logs_payload_is_json CHECK (ISJSON(payload) = 1),
    status NVARCHAR(20) NOT NULL,
    retry_count INT NOT NULL CONSTRAINT df_email_logs_retry_count DEFAULT 0,
    max_retries INT NOT NULL CONSTRAINT df_email_logs_max_retries DEFAULT 3,
    next_retry_at DATETIME2 NULL,
    error_message NVARCHAR(MAX) NULL,
    sent_at DATETIME2 NULL,
    created_at DATETIME2 NOT NULL CONSTRAINT df_email_logs_created_at DEFAULT SYSUTCDATETIME()
);

CREATE INDEX ix_email_logs_recipient ON email_logs (recipient);
CREATE INDEX ix_email_logs_status_retry ON email_logs (status, next_retry_at);
CREATE INDEX ix_email_logs_created_at ON email_logs (created_at);
