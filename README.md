# RyzCore

RyzCore adalah generator framework enterprise **.NET 10** untuk menghasilkan aplikasi siap pakai dengan:

- ASP.NET Core Web API
- Blazor WebAssembly
- .NET MAUI Blazor Hybrid (Windows/Android/iOS/MacCatalyst)
- MudBlazor UI
- SQL Server + EF Core
- JWT access/refresh token + session validation
- Dynamic menu, screen, report, dan parameter engine
- Maker-checker approval
- SignalR notification + Hangfire background job
- Import Excel dan export Excel/PDF
- Stored procedure + synonym deployment

Nama paket npm: `create-ryzcore`
Repository: [rayzaisra/RyzCore](https://github.com/rayzaisra/RyzCore)

Referensi arsitektur, aturan coding, database, dan guardrail untuk agent AI: **[FRAMEWORK-REFERENCE.md](FRAMEWORK-REFERENCE.md)**

## Prasyarat

- Node.js 20+
- .NET 10 SDK
- SQL Server dengan TCP 1433 aktif
- .NET MAUI workload bila tidak memakai `--no-maui`
- Docker Desktop bila memakai `--docker`

Panduan lengkap: **[INSTALL.md](INSTALL.md)**

## Instalasi

Instal global:

```bash
npm install -g create-ryzcore
```

Atau jalankan tanpa instalasi global:

```bash
npx create-ryzcore
```

Alternatif dari source GitHub:

```bash
npm install -g github:rayzaisra/RyzCore
npx github:rayzaisra/RyzCore
```

## Membuat aplikasi

Mode interaktif:

```bash
create-ryzcore
```

Mode config:

```bash
create-ryzcore --config scaffold.example.json
```

Contoh non-interaktif:

```bash
create-ryzcore \
  --name Procurement \
  --db procurement_db \
  --audit-db procurement_log_audit \
  --server localhost,1433 \
  --user sa \
  --password "YourStrong!Passw0rd" \
  --style neo \
  --palette fintech \
  --no-maui \
  --tests \
  --docker
```

## Opsi generator

| Opsi | Keterangan |
|---|---|
| `--name` | Nama project/solution |
| `--db` | Nama database utama |
| `--audit-db` | Nama database audit/log |
| `--server` | SQL Server host/port |
| `--user`, `--password` | Kredensial SQL Server |
| `--admin-*` | Data admin awal |
| `--logo` | Path logo PNG/JPG/SVG |
| `--style` | Style awal: `default` atau `neo` |
| `--palette` | Palette awal: `default`, `fintech`, `monochrome`, `creative`, `wellness` |
| `--config` | Config JSON non-interaktif |
| `--output` | Direktori output |
| `--no-maui` | Tanpa project MAUI |
| `--tests` | Tambah project xUnit dan jalankan test |
| `--docker` | Tambah Dockerfile + docker-compose |
| `--skip-build` | Lewati `dotnet build` |
| `--skip-seed` | Lewati pembuatan/seed database |
| `--force` | Timpa output yang tidak kosong |
| `--dry-run` | Validasi dan tampilkan rencana saja |
| `--yes` | Non-interaktif |

Palette awal yang tersedia:

| Nilai | Nama |
|---|---|
| `default` | Default |
| `fintech` | FinTech |
| `monochrome` | Black & White |
| `creative` | Creative Colorful |
| `wellness` | Wellness |

Setelah aplikasi berjalan, style dan palette dapat diganti dari topbar Web.
Pilihan disimpan di browser; nilai install hanya menjadi default awal.

## Struktur hasil generate

| Project | Fungsi |
|---|---|
| `<Project>.Domain` | Entity dan konstanta domain |
| `<Project>.Application` | DTO, interface, service kontrak |
| `<Project>.Infrastructure` | EF Core, service implementasi, seeder |
| `<Project>.Api` | Web API, auth, SignalR, Hangfire, export |
| `<Project>.Shared` | Komponen Razor, halaman, service HTTP |
| `<Project>.Web` | Blazor WebAssembly host |
| `<Project>.App` | .NET MAUI Blazor Hybrid host |

Database yang dibuat:

- database utama aplikasi
- database audit/log terpisah
- 19 stored procedure
- 3 synonym audit

## Setelah generate

```bash
cd <ProjectName>

dotnet run --project <ProjectName>.Api
dotnet run --project <ProjectName>.Web
dotnet run --project <ProjectName>.App
```

Jika memakai `--docker`:

```bash
docker compose up --build
```

## Skill AI (terpisah dari generator)

Panduan untuk agent AI tersedia sebagai skill terpisah di `skills/ryzcore/`.
Skill ini **tidak** dibundel ke project hasil generate dan **tidak** perlu
dipublish ke npm — cukup diarahkan ke folder skill agent.

```text
skills/ryzcore/
├── SKILL.md
└── references/
    ├── change-workflow.md
    ├── coding-rules.md
    ├── backend-api-style.md
    ├── database.md
    ├── api.md
    ├── ui-pages.md
    ├── menus-and-parameters.md
    ├── adding-a-feature.md
    └── guardrails.md
```

### Cara pakai (Command Code)

Ada dua cara. Paling sederhana, tunjuk foldernya lewat
`.commandcode/settings.json` di root project:

```json
{
  "skills": ["./skills"]
}
```

Dengan begitu skill `ryzcore` langsung terdeteksi tanpa copy dan tanpa publish.
Alternatifnya salin foldernya seperti pada bagian berikut.

Verifikasi:

```bash
cmdc skills list      # ryzcore muncul di daftar
```

Di sesi Command Code, panggil dengan:

```text
/ryzcore tambah master table untuk kategori produk
/ryzcore buat data page untuk approval cuti
/skill:ryzcore <perintah>       # bentuk namespaced, selalu resolve ke skill
```

Skill juga bisa terpanggil otomatis saat prompt cocok dengan description-nya
(entity, migration, repository, data page, menu, parameter, dashboard).

### Install ke project lain

Pilihan 1 — salin folder:

```bash
# hanya project ini
mkdir -p .commandcode/skills
cp -R skills/ryzcore .commandcode/skills/

# semua project di mesin ini
cp -R skills/ryzcore ~/.commandcode/skills/
```

Pilihan 2 — tunjuk folder tanpa menyalin, lewat `.commandcode/settings.json`:

```json
{
  "skills": ["./skills"]
}
```

Pilihan 3 — hanya untuk satu sesi:

```bash
cmdc --skill ./skills
```

### Install dari GitHub

Setelah repo di-push, skill bisa dipasang dengan:

```bash
cmdc skills add rayzaisra/NetMauiEnterpriceFramework -s ryzcore
cmdc skills add rayzaisra/NetMauiEnterpriceFramework -s ryzcore --global   # untuk semua project
```

`.agents/skills/` juga didukung (badge `[.agents]`), untuk agent lain yang
memakai standar Agent Skills.

### Agent lain (Codex, OpenCode, Cursor, dan sejenisnya)

`SKILL.md` mengikuti standar terbuka Agent Skills, jadi formatnya sudah
kompatibel. Yang berbeda hanya lokasi folder dan cara memanggil:

| Agent | Lokasi project | Lokasi global | Cara panggil |
|---|---|---|---|
| Command Code | `.commandcode/skills/` atau `.agents/skills/` | `~/.commandcode/skills/`, `~/.agents/skills/` | `/ryzcore`, `/skill:ryzcore` |
| OpenAI Codex CLI | `.agents/skills/` (dipindai dari CWD sampai root repo) | `~/.agents/skills/` | `$` untuk mention, atau `/skills` |
| OpenCode | `.opencode/skills/` atau `.agents/skills/` | `~/.config/opencode/skills/`, `~/.agents/skills/` | `@ryzcore` atau via tool `skill` |
| Cursor | `.cursor/skills/` atau `.agents/skills/` | `~/.cursor/skills/`, `~/.agents/skills/` | `/ryzcore` |

Paling praktis: pakai `.agents/skills/` karena dibaca oleh Command Code, Codex,
OpenCode, Cursor, dan juga `.claude/skills/` kompatibel di OpenCode/Cursor.

```bash
mkdir -p .agents/skills
cp -R skills/ryzcore .agents/skills/
```

Untuk semua project di mesin ini:

```bash
cp -R skills/ryzcore ~/.agents/skills/
```

Catatan:

- **Codex** membaca `.agents/skills/` di setiap level direktori hingga root repo;
  tidak perlu konfigurasi tambahan.
- **OpenCode** juga menerima array `skills` di `opencode.json`:
  `{"skills": ["./skills"]}` — sama seperti pendekatan `settings.json` di
  Command Code.
- **Cursor** memasang skill dari repo GitHub lewat plugin
  (`.cursor-plugin/marketplace.json`), tetapi menyalin folder ke
  `.cursor/skills/` atau `.agents/skills/` sudah cukup.
- **Harness lain** yang mengikuti standar Agent Skills dapat memakai folder yang
  sama. Harness yang hanya mendukung file instruksi tunggal (bukan skills)
  dapat memakai `SKILL.md` sebagai isi file instruksinya.

Skill tetap **tidak** perlu dipublish ke npm. npm hanya untuk generator
(`create-ryzcore`).

Dengan skill ini agent memahami style coding, struktur database, alur backend,
konvensi UI, dan cara membuat menu/parameter sebelum mengubah kode, sehingga
tidak mengikuti template .NET generik.

## Keamanan

- Ganti password admin awal setelah login pertama.
- Jangan commit `appsettings.Development.json` berisi kredensial nyata.
- Gunakan SQL login dengan hak minimum yang dibutuhkan.

## Lisensi

MIT — lihat [LICENSE](LICENSE).

---

