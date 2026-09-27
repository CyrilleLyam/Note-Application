UPDATE users
SET avatar_url = 'avatars/' + SUBSTRING(avatar_url, LEN('/api/user/avatar/') + 1, 500)
WHERE avatar_url LIKE '/api/user/avatar/%';

EXEC sp_rename 'users.avatar_url', 'avatar_key', 'COLUMN';
