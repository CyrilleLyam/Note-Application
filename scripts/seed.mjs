import { execFileSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const API_URL = process.env.API_URL ?? 'http://localhost:5142'
const ROOT = fileURLToPath(new URL('..', import.meta.url))

const USERS = [
  {
    username: 'dara',
    email: 'dara@example.com',
    password: 'Password123!',
    displayName: 'Dara Sok',
    bio: 'Backend engineer. .NET, SQL Server and keeping production boring.',
    notes: [
      {
        title: 'Sprint 14 planning: backend tasks',
        tags: ['sprint', 'planning', 'todo'],
        pinned: true,
        content: `
## Goals
Ship note sharing and harden the auth flow.

## Tasks
- [x] Share link table + migration
- [x] Public endpoint with rate limiting
- [ ] Audit log for share / revoke
- [ ] Load test \`GET /api/notes\` with 50k rows
- [ ] Review refresh token rotation

**Risk:** search is slow on big accounts, see the postmortem note.
`,
      },
      {
        title: 'Dapper vs EF Core: when to pick which',
        tags: ['dotnet', 'dapper', 'ef-core', 'database'],
        content: `
| | Dapper | EF Core |
|---|---|---|
| SQL control | full | generated |
| Change tracking | none | built in |
| Migrations | hand-written SQL | code first |
| Speed | very fast | fast enough |

Pick **Dapper** when queries are hand-tuned or reporting-heavy.
Pick **EF Core** for CRUD-heavy domains where productivity matters more than SQL control.
`,
      },
      {
        title: 'SQL Server indexing cheat sheet',
        tags: ['sql-server', 'database', 'performance'],
        pinned: true,
        content: `
- Clustered index = the table's physical order. Keep it narrow and ever-increasing.
- Put **equality** columns first, then **range** columns in composite indexes.
- Use \`INCLUDE\` for columns you only read, not filter on.
- Filtered index for sparse data: \`WHERE deleted_at IS NULL\`.
- Check \`sys.dm_db_index_usage_stats\` before adding more indexes.

~~~sql
CREATE INDEX ix_notes_user_id_deleted_at_created_at
    ON notes (user_id, deleted_at, created_at DESC);
~~~
`,
      },
      {
        title: 'Optimistic concurrency with rowversion',
        tags: ['sql-server', 'dotnet', 'concurrency'],
        content: `
Every update sends the \`row_version\` the client loaded:

~~~sql
UPDATE notes
SET title = @Title
WHERE id = @Id AND row_version = @RowVersion;
~~~

- 0 rows updated + row still exists = someone else saved first, so return **409**.
- The client offers "Load latest" or "Overwrite with mine".
- Remember: rowversion changes on *any* column update, so keep unrelated data in other tables.
`,
      },
      {
        title: 'JWT + refresh token flow',
        tags: ['security', 'auth', 'api-design'],
        content: `
1. Login returns a short-lived **access token** (15 min) and a **refresh token** (7 days).
2. The client sends the access token as \`Authorization: Bearer ...\`.
3. On 401, the client calls \`/api/auth/refresh\` once and retries queued requests.
4. Refresh tokens are stored hashed and can be revoked on logout.

Open question: rotate refresh tokens on every use?
`,
      },
      {
        title: 'REST API naming conventions',
        tags: ['api-design', 'rest'],
        content: `
- Plural nouns: \`/api/notes\`, not \`/api/getNotes\`
- Sub-resources for actions: \`POST /api/notes/{id}/restore\`
- snake_case JSON, ISO 8601 UTC dates
- 201 + Location header on create, 204 on delete
- Errors: \`{ error, status, message }\`
- Pagination: \`page\`, \`page_size\`, and a \`meta\` object in the response
`,
      },
      {
        title: 'Docker Compose for local dev',
        tags: ['docker', 'devops'],
        content: `
~~~yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports: ["1433:1433"]
  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
~~~

Tips:
- Set \`COMPOSE_PROJECT_NAME\` so two clones don't share volumes.
- Use healthchecks + \`docker compose up -d --wait\`.
- \`docker compose down -v\` wipes the data. Be careful.
`,
      },
      {
        title: 'Git branching strategy for the team',
        tags: ['git', 'workflow'],
        content: `
- \`main\` is always deployable.
- Short-lived feature branches: \`feat/share-links\`, \`fix/toast-offset\`.
- Squash merge with a Conventional Commit title.
- Rebase on main before opening a PR; no merge commits from main.
- Tag releases: \`v1.4.0\`.
`,
      },
      {
        title: 'Code review checklist',
        tags: ['code-review', 'quality'],
        pinned: true,
        content: `
- [ ] Does it do what the ticket asks, and nothing extra?
- [ ] Are errors handled and logged with context?
- [ ] Any N+1 queries or missing indexes?
- [ ] Are inputs validated at the API boundary?
- [ ] Is every DB call passing the \`CancellationToken\`?
- [ ] Tests for the unhappy path?
- [ ] Is the migration safe to run on a big table?
`,
      },
      {
        title: 'Incident postmortem: slow notes search',
        tags: ['incident', 'performance', 'sql-server'],
        content: `
**Impact:** search took 4-6 s for users with 20k+ notes.

**Root cause:** \`LIKE '%term%'\` on \`content NVARCHAR(MAX)\` forced a full scan.

**Fix:**
1. Search the title first, content second.
2. Limit results and paginate server-side.
3. Evaluate full-text search (not available in our Linux image).

**Follow-up:** add a p95 latency alert on \`GET /api/notes\`.
`,
      },
      {
        title: 'Background jobs with PeriodicTimer',
        tags: ['dotnet', 'background-jobs'],
        content: `
~~~csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await RetryFailedEmails(stoppingToken);
    }
}
~~~

- Create a scope per tick for scoped services.
- Never let one exception kill the loop.
`,
      },
      {
        title: 'Structured logging with Serilog',
        tags: ['logging', 'observability', 'dotnet'],
        content: `
- Log **templates**, not string interpolation: \`Log.Information("Note {NoteId} shared", id)\`
- Enrich with a correlation id per request.
- Never log tokens, passwords or full request bodies.
- Rolling daily files locally, a log backend (Seq / Loki) in production.
`,
      },
      {
        title: 'Rate limiting login endpoints',
        tags: ['security', 'dotnet', 'api-design'],
        content: `
Fixed window: **5 attempts per minute** per IP + endpoint.

- Return 429 with a \`Retry-After\` header.
- Public read endpoints get a looser policy (60/min).
- Behind a proxy, use the forwarded client IP, not the proxy's.
`,
      },
      {
        title: 'Argon2 vs bcrypt for password hashing',
        tags: ['security', 'auth'],
        content: `
- **Argon2id** is the current recommendation (memory-hard, resists GPUs).
- **bcrypt** is still fine, but limited to 72 bytes of input.
- Tune parameters so hashing takes ~250 ms on production hardware.
- Store the algorithm and parameters with the hash so you can upgrade later.
`,
      },
      {
        title: 'CI pipeline ideas (GitHub Actions)',
        tags: ['ci-cd', 'devops', 'github-actions'],
        content: `
1. \`dotnet build\` + \`dotnet test\` with a SQL Server service container
2. \`npm ci\`, \`npm run lint\`, \`npm run build\` for the client
3. Cache NuGet and npm
4. Fail the PR on new warnings
5. Build a Docker image on \`main\` only
`,
      },
      {
        title: 'Health checks and readiness probes',
        tags: ['observability', 'kubernetes', 'devops'],
        content: `
- \`/health\` checks the database with a cheap \`SELECT 1\`.
- **Liveness** = process is alive. **Readiness** = dependencies are reachable.
- Don't check third parties in liveness, or one outage restarts everything.
`,
      },
      {
        title: 'Object storage layout for uploads',
        tags: ['minio', 'storage', 'architecture'],
        content: `
One bucket, grouped by user:

~~~text
note-bucket/{shard}/{userId}/{uuid}.{ext}
~~~

- \`shard\` = first 2 hex chars of sha256(userId), for 256 prefixes.
- File names are random UUIDs, never the uploaded name.
- The database stores the key; files are always found through it.
`,
      },
      {
        title: 'SOLID principles: quick refresher',
        tags: ['clean-code', 'oop'],
        content: `
- **S**ingle responsibility: one reason to change
- **O**pen/closed: extend without editing
- **L**iskov substitution: subtypes keep the contract
- **I**nterface segregation: small, focused interfaces
- **D**ependency inversion: depend on abstractions

Use them as smells to notice, not rules to force.
`,
      },
      {
        title: 'Async/await pitfalls in C#',
        tags: ['dotnet', 'async'],
        content: `
- Never \`.Result\` or \`.Wait()\` in ASP.NET. It causes thread-pool starvation.
- Avoid \`async void\` except for event handlers.
- Pass \`CancellationToken\` all the way down to the database.
- \`ConfigureAwait(false)\` isn't needed in ASP.NET Core apps.
- Use \`Task.WhenAll\` for independent calls.
`,
      },
      {
        title: 'Meeting notes: architecture sync',
        tags: ['meeting', 'architecture'],
        content: `
**Attendees:** Dara, Sophea, Vanna

- Agreed: keep the modular monolith for now; split only when a module needs its own scaling.
- Storage service stays generic (upload / get / delete only).
- Next: design "share with a user" with view / edit permissions.

**Action items**
- [ ] Dara: draft the \`note_shares\` schema
- [ ] Sophea: mock the share dialog with the People tab
`,
      },
      {
        title: 'Database migration rules',
        tags: ['database', 'migrations'],
        content: `
1. Never edit a migration that already ran anywhere.
2. One change per file, numbered: \`0010_add_color_to_notes.sql\`.
3. Add columns as NULL first, backfill, then add NOT NULL.
4. Renames need a data step, and code deployed in the right order.
5. Test on a copy of production-sized data.
`,
      },
      {
        title: 'Interview questions for backend candidates',
        tags: ['interview', 'hiring'],
        content: `
- How would you design pagination for a table with 50M rows?
- What happens between typing a URL and seeing the page?
- Explain a race condition you fixed.
- When would you add an index, and when would you remove one?
- Walk me through a production incident you handled.
`,
      },
      {
        title: 'Caching strategies',
        tags: ['performance', 'caching', 'redis'],
        content: `
- **Cache-aside:** read cache, fall back to DB, then fill the cache.
- **Write-through:** update the cache on every write.
- Always set a TTL and plan invalidation first.
- Avatars: \`Cache-Control: public, max-age=86400\` plus random file names = safe long caching.
`,
      },
      {
        title: 'Learning goals Q4',
        tags: ['learning', 'career'],
        content: `
- [ ] Finish "Designing Data-Intensive Applications"
- [ ] Build a small SignalR demo for live note updates
- [ ] Get comfortable with query plans in SQL Server
- [ ] Give one internal talk on API design
`,
      },
    ],
  },
  {
    username: 'sophea',
    email: 'sophea@example.com',
    password: 'Password123!',
    displayName: 'Sophea Chan',
    bio: 'Frontend engineer. Vue, TypeScript and accessible UIs.',
    notes: [
      {
        title: 'Frontend roadmap: this month',
        tags: ['planning', 'todo', 'frontend'],
        pinned: true,
        content: `
- [x] Share dialog with copy button
- [x] Public shared note page
- [ ] "Shared with me" tab
- [ ] Image attachments in the editor
- [ ] Lighthouse score above 95 on the notes page
`,
      },
      {
        title: 'Vue 3 Composition API patterns',
        tags: ['vue', 'frontend'],
        content: `
- Keep components thin; move logic into composables (\`useKeyboardShortcuts\`).
- \`defineModel\` for v-model props.
- \`watch\` with \`{ immediate: true }\` instead of duplicating onMounted logic.
- Prefer \`computed\` over watchers that set refs.

~~~ts
const open = defineModel<boolean>('open', { required: true })
~~~
`,
      },
      {
        title: 'Pinia store conventions',
        tags: ['vue', 'pinia', 'state-management'],
        pinned: true,
        content: `
- Setup stores (\`defineStore('notes', () => { ... })\`).
- Server data + loading/error flags live in the store; components keep UI-only state.
- Guard against stale responses with a request id counter.
- \`storeToRefs\` when destructuring state.
- Persist only preferences (sort, filters), never server data.
`,
      },
      {
        title: 'TypeScript utility types I keep forgetting',
        tags: ['typescript', 'cheat-sheet'],
        content: `
| Type | Use |
|---|---|
| \`Partial<T>\` | all props optional |
| \`Pick<T, K>\` | keep some props |
| \`Omit<T, K>\` | drop some props |
| \`Record<K, V>\` | typed dictionary |
| \`ReturnType<F>\` | type of a function's result |
| \`NonNullable<T>\` | remove null / undefined |

\`satisfies\` checks a value against a type without widening it.
`,
      },
      {
        title: 'Form validation with vee-validate + zod',
        tags: ['vue', 'forms', 'validation'],
        content: `
~~~ts
const { handleSubmit } = useForm({
  validationSchema: toTypedSchema(noteSchema),
})
~~~

- Keep zod on v3: \`@vee-validate/zod\` doesn't support zod 4 yet.
- Messages are i18n keys, translated in \`FormMessage\`.
- Validate eagerly only after the first error.
`,
      },
      {
        title: 'Accessibility checklist for new components',
        tags: ['accessibility', 'a11y', 'frontend'],
        pinned: true,
        content: `
- [ ] Works with keyboard only (Tab, Enter, Escape)
- [ ] Visible focus ring
- [ ] Icon-only buttons have \`aria-label\`
- [ ] Color contrast at least 4.5:1
- [ ] Dialogs trap focus and return it on close
- [ ] Loading states use \`aria-busy\`
`,
      },
      {
        title: 'Tailwind CSS tips',
        tags: ['css', 'tailwind'],
        content: `
- \`cn()\` = clsx + tailwind-merge, so later classes win.
- Use design tokens (\`bg-muted\`, \`text-muted-foreground\`), not raw colors.
- \`line-clamp-3\` for previews, \`break-words\` for long titles.
- \`has-[...]\` and \`group-hover:\` remove a lot of JavaScript.
`,
      },
      {
        title: 'Axios interceptors: refresh token queue',
        tags: ['http', 'auth', 'frontend'],
        content: `
1. A request gets 401.
2. If no refresh is running, start one; otherwise wait in a queue.
3. When refresh succeeds, replay queued requests with the new token.
4. If refresh fails, clear the session and redirect to login.

Don't refresh for the refresh call itself, or it loops forever.
`,
      },
      {
        title: 'Component testing with Vitest',
        tags: ['testing', 'vitest', 'vue'],
        content: `
~~~ts
it('shows the empty state', async () => {
  const wrapper = mount(NotesPage, { global: { plugins: [createTestingPinia()] } })
  expect(wrapper.text()).toContain('No notes yet')
})
~~~

- Test behavior, not implementation details.
- Mock the API layer, not axios internals.
`,
      },
      {
        title: 'E2E testing plan with Playwright',
        tags: ['testing', 'e2e', 'playwright'],
        content: `
Critical paths:
1. Register, then create the first note
2. Search + tag filter
3. Trash, then undo
4. Share a note and open the link in a private window
5. Edit conflict (two tabs)

Run against Docker Compose in CI; seed data with \`scripts/seed.mjs\`.
`,
      },
      {
        title: 'Web performance: Core Web Vitals',
        tags: ['performance', 'web-vitals'],
        content: `
- **LCP** < 2.5 s: preload fonts, lazy-load routes
- **INP** < 200 ms: debounce search (300 ms), avoid long tasks
- **CLS** < 0.1: skeletons with the same size as the real content

Measure with Lighthouse and the web-vitals library in production.
`,
      },
      {
        title: 'i18n workflow (en, km, zh)',
        tags: ['i18n', 'vue'],
        content: `
- \`en.ts\` defines the \`MessageSchema\` type, so missing keys in \`km.ts\` / \`zh.ts\` fail the build.
- Use \`{name}\` placeholders; never concatenate translated strings.
- Plurals: \`'{n} note | {n} notes'\`.
- Check Khmer text in the UI: it's usually longer than English.
`,
      },
      {
        title: 'Dark mode implementation notes',
        tags: ['css', 'ui', 'theming'],
        content: `
- \`useDark()\` from VueUse toggles the \`dark\` class on html.
- All colors come from CSS variables, so components need no changes.
- Test both modes for contrast, especially muted text and borders.
`,
      },
      {
        title: 'Git commit message convention',
        tags: ['git', 'conventions'],
        content: `
~~~text
feat: share notes with a public read-only link
fix: move toasts below the navbar
refactor: generalize storage service
docs: add simple readme
chore: add zh locale
~~~

Imperative mood, lowercase, no period at the end.
`,
      },
      {
        title: 'Debugging checklist',
        tags: ['debugging', 'productivity'],
        content: `
1. Reproduce reliably first.
2. Read the actual error and the network tab.
3. Check what changed recently (\`git log -p\`).
4. Binary search: comment out half, repeat.
5. Explain it to someone (or a rubber duck).
6. Write a test that fails before the fix.
`,
      },
      {
        title: 'Design review feedback: notes page',
        tags: ['ux', 'design', 'meeting'],
        content: `
- Card menu is only visible on hover on desktop, fine, but keep it visible on touch.
- Pinned notes need a stronger visual difference.
- Empty state copy is good.
- Consider a grid / list toggle.

**Next:** prototype list view in Figma.
`,
      },
      {
        title: 'Rendering Markdown safely (XSS)',
        tags: ['security', 'xss', 'frontend'],
        content: `
- markdown-it with \`html: false\`, so raw HTML shows as text.
- Sanitize the output with DOMPurify anyway (defense in depth).
- Links: add \`rel="noopener noreferrer"\` for \`target="_blank"\`.
- Never use \`v-html\` on anything that skipped sanitizing.
`,
      },
      {
        title: 'Vite config snippets',
        tags: ['vite', 'tooling'],
        content: `
~~~ts
server: {
  proxy: {
    '/api': { target: 'http://localhost:5142', changeOrigin: true },
  },
},
~~~

- Path aliases: \`@\`, \`@core\`, \`@modules\`.
- Lazy routes with \`() => import(...)\` give automatic code splitting.
`,
      },
      {
        title: 'Keyboard shortcuts UX',
        tags: ['ux', 'accessibility'],
        content: `
| Key | Action |
|---|---|
| \`n\` | new note |
| \`/\` | focus search |
| \`?\` | shortcuts help |
| \`Ctrl + Enter\` | save |
| \`Esc\` | close dialog |

Ignore shortcuts while the user is typing in an input.
`,
      },
      {
        title: 'Reading list: frontend architecture',
        tags: ['learning', 'reading-list'],
        content: `
- [x] Vue docs: Composables and Reactivity in depth
- [ ] "Refactoring UI"
- [ ] web.dev: Learn Accessibility
- [ ] "Micro Frontends in Action" (chapters 1-3)
`,
      },
      {
        title: '1:1 notes with mentor',
        tags: ['career', 'mentoring'],
        content: `
- Write short design docs before big changes.
- Pair on one backend task per sprint to learn the API side.
- Goal for next quarter: own the release of the sharing feature.
`,
      },
      {
        title: 'Bug: toast covers navbar buttons',
        tags: ['bug', 'ui', 'sonner'],
        content: `
**Symptom:** toasts "never close".

**Cause:** top-right toasts sat on top of the navbar's account and theme buttons. Hovering a toast pauses its timer, which is by design.

**Fix:**
~~~vue
<Toaster position="top-right" :offset="{ top: 80 }" />
~~~
`,
      },
      {
        title: 'Responsive layout breakpoints',
        tags: ['css', 'responsive'],
        content: `
| Breakpoint | Width | Notes grid |
|---|---|---|
| default | < 640px | 1 column |
| sm | 640px | 2 columns |
| lg | 1024px | 3 columns |

Dialogs: full-width on mobile, \`max-w-2xl\` on desktop.
`,
      },
      {
        title: 'ESLint + antfu config setup',
        tags: ['tooling', 'linting'],
        content: `
~~~js
import antfu from '@antfu/eslint-config'

export default antfu()
~~~

- \`npm run lint:fix\` before every commit.
- It also sorts imports and exports, and formats Vue templates.
`,
      },
    ],
  },
]

function readEnvValue(file, key) {
  try {
    const line = readFileSync(file, 'utf8').split(/\r?\n/).find(l => l.startsWith(`${key}=`))
    return line?.slice(key.length + 1).trim() || undefined
  }
  catch {
    return undefined
  }
}

async function request(method, path, { token, body } = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    method,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(body ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  })
  const text = await response.text()
  return { status: response.status, body: text ? JSON.parse(text) : null }
}

async function signIn(user) {
  const registered = await request('POST', '/api/auth/register', {
    body: { username: user.username, email: user.email, password: user.password },
  })
  if (registered.status === 200) {
    return { token: registered.body.data.access_token, created: true }
  }

  const loggedIn = await request('POST', '/api/auth/login', {
    body: { email: user.email, password: user.password },
  })
  if (loggedIn.status !== 200) {
    throw new Error(`Could not register or sign in ${user.email}: ${registered.body?.message ?? registered.status} / ${loggedIn.body?.message ?? loggedIn.status}`)
  }
  return { token: loggedIn.body.data.access_token, created: false }
}

function spreadNoteDates(emails) {
  const database = readEnvValue(`${ROOT}server/.env`, 'DB_DATABASE') ?? 'note_app'
  const emailList = emails.map(email => `'${email}'`).join(', ')
  const sql = `
    SET NOCOUNT ON;
    WITH seeded AS (
      SELECT n.id, ROW_NUMBER() OVER (PARTITION BY n.user_id ORDER BY n.id) AS rn
      FROM notes n
      INNER JOIN users u ON u.id = n.user_id
      WHERE u.email IN (${emailList})
    )
    UPDATE n
    SET created_at = c.created_at,
        updated_at = CASE WHEN s.rn % 3 = 0 AND DATEADD(DAY, s.rn % 5 + 1, c.created_at) < SYSUTCDATETIME()
                          THEN DATEADD(DAY, s.rn % 5 + 1, c.created_at) END
    FROM notes n
    INNER JOIN seeded s ON s.id = n.id
    CROSS APPLY (SELECT DATEADD(MINUTE, -((s.rn - 1) * 2 * 1440 + (s.rn * 53) % 480 + 5), SYSUTCDATETIME()) AS created_at) c;`
  const command = `/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -d ${database} -Q "${sql.replace(/\s+/g, ' ')}"`
  execFileSync('docker', ['compose', '--project-directory', ROOT, 'exec', '-T', 'sqlserver', 'sh', '-c', command], { stdio: 'pipe' })
}

const results = []
const seededEmails = []

for (const user of USERS) {
  const { token, created } = await signIn(user)

  await request('PUT', '/api/user/profile', { token, body: { display_name: user.displayName, bio: user.bio } })

  const existing = await request('GET', '/api/notes?page_size=1', { token })
  const existingCount = existing.body?.meta?.total_count ?? 0
  if (existingCount > 0) {
    results.push({ email: user.email, account: created ? 'created' : 'existing', notes: `skipped (already has ${existingCount})` })
    continue
  }

  let pinned = 0
  for (const note of user.notes) {
    const response = await request('POST', '/api/notes', {
      token,
      body: { title: note.title, content: note.content.trim(), tags: note.tags },
    })
    if (response.status !== 201) {
      throw new Error(`Failed to create "${note.title}": ${response.body?.message ?? response.status}`)
    }
    if (note.pinned) {
      await request('PATCH', `/api/notes/${response.body.data.id}/pin`, { token, body: { is_pinned: true } })
      pinned++
    }
  }

  seededEmails.push(user.email)
  results.push({ email: user.email, account: created ? 'created' : 'existing', notes: `${user.notes.length} created, ${pinned} pinned` })
}

if (seededEmails.length > 0) {
  try {
    spreadNoteDates(seededEmails)
  }
  catch (err) {
    console.warn(`Notes were created, but spreading their dates failed: ${err.stderr?.toString().trim() || err.message}`)
  }
}

console.table(results)
console.log('Sign in with the emails and passwords in scripts/seed.mjs.')
