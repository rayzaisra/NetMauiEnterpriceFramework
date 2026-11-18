# ryzcore-skill

Skill untuk agent AI yang memahami **RyzCore** — framework enterprise .NET 10
yang digenerate oleh [`create-ryzcore`](https://www.npmjs.com/package/create-ryzcore).

Dengan skill ini agent tidak menebak-nebak: ia membaca konvensi framework
sebelum menulis kode, sehingga hasilnya mengikuti style coding, struktur
database, kontrak API, dan pola UI yang sudah ada.

## Kenapa perlu skill terpisah

- `create-ryzcore` tetap murni generator. Skill **tidak** ikut masuk ke project
  hasil generate dan **tidak** ikut ke dalam paket npm generator.
- Skill berisi pengetahuan untuk agent, bukan kode aplikasi. Ia dipasang di
  direktori skill agent yang Anda pakai.
- Formatnya mengikuti standar terbuka
  [Agent Skills](https://agentskills.io), jadi portabel antar agent.

## Isi paket

```text
ryzcore-skill/
└── ryzcore/
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

Yang diajarkan skill ini:

- **Alur kerja wajib** — analisa, petakan flow UI → API → database, periksa
  kode/tabel/stored procedure terdampak, ajukan impact analysis, tunggu
  persetujuan, baru implementasi (`change-workflow.md`).
- **Lapisan backend** — controller tipis, service untuk logic,
  query service untuk paging/proyeksi, repository hanya akses database dan
  tidak pernah `SaveChanges`.
- **Kontrak API** — envelope `ApiResponse<T>` untuk transaksi,
  `PaginatedResponse<T>` untuk list, `ApiResponseFactory`, kode `00/98/99`.
- **Database** — `BaseEntity`/`BaseAuditEntity`, audit terpusat di
  `DbContext.SaveChanges`, migration, dan `IUnitOfWork`.
- **Menu & parameter** — tabel master `rf_*`, registrasi ke
  `tb_parameter_system`/`tb_parameter_system_detail`, engine allowlist, maker
  dan approval, serta menu berbasis `um_menu`/`um_group_menu`.
- **UI** — standar data page User/Group/Role, dialog Add/Edit, approval page,
  dan layout Web vs MAUI.

## Instalasi

Skill ini berupa file markdown, jadi tidak butuh runtime. Node.js hanya
diperlukan jika Anda memasangnya lewat npm.

### Lewat npm

```bash
npm install -D ryzcore-skill
```

Lalu salin foldernya ke direktori skill agent:

```bash
# hanya project ini (dibaca Command Code, Codex, OpenCode, Cursor)
mkdir -p .agents/skills
cp -R node_modules/ryzcore-skill/ryzcore .agents/skills/

# untuk semua project di mesin ini
cp -R node_modules/ryzcore-skill/ryzcore ~/.agents/skills/
```

Jika tidak ingin menyalin, arahkan agent ke foldernya. Untuk Command Code:

```json
// .commandcode/settings.json
{
  "skills": ["node_modules/ryzcore-skill"]
}
```

Untuk OpenCode, gunakan `opencode.json`:

```json
{
  "skills": ["./node_modules/ryzcore-skill"]
}
```

### Lewat clone repo

```bash
git clone https://github.com/rayzaisra/NetMauiEnterpriceFramework.git
mkdir -p .agents/skills
cp -R NetMauiEnterpriceFramework/packages/ryzcore-skill/ryzcore .agents/skills/
```

### Dari GitHub (Command Code)

```bash
cmdc skills add rayzaisra/NetMauiEnterpriceFramework -s ryzcore
cmdc skills add rayzaisra/NetMauiEnterpriceFramework -s ryzcore --global
```

## Lokasi yang didukung tiap agent

| Agent | Project | Global | Cara panggil |
|---|---|---|---|
| Command Code | `.commandcode/skills/`, `.agents/skills/` | `~/.commandcode/skills/`, `~/.agents/skills/` | `/ryzcore`, `/skill:ryzcore` |
| OpenAI Codex CLI | `.agents/skills/` | `~/.agents/skills/` | mention `$`, atau `/skills` |
| OpenCode | `.opencode/skills/`, `.agents/skills/` | `~/.config/opencode/skills/`, `~/.agents/skills/` | `@ryzcore` atau tool `skill` |
| Cursor | `.cursor/skills/`, `.agents/skills/` | `~/.cursor/skills/`, `~/.agents/skills/` | `/ryzcore` |

`.agents/skills/` adalah lokasi paling portabel karena dibaca oleh keempatnya.
Codex, OpenCode, dan Cursor juga membaca `.claude/skills/` untuk kompatibilitas.

## Cara pakai

Alur yang disarankan:

```bash
# 1. buat aplikasi dengan generator
npx create-ryzcore

# 2. pasang skill (sekali saja)
mkdir -p .agents/skills
cp -R node_modules/ryzcore-skill/ryzcore .agents/skills/

# 3. buka agent di folder project hasil generate
# contoh prompt:
```

Contoh prompt yang memicu skill:

```text
Tambah master table kategori produk beserta UI parameter maintenance-nya.
Buat data page untuk approval cuti, ikuti standar User/Group/Role.
Tambah endpoint list employee dengan paging dan envelope yang benar.
Perbaiki modul ini agar repository tidak memanggil SaveChanges.
```

Agent akan memakai skill untuk:

1. menganalisa permintaan dan memetakan flow UI → backend → database;
2. memeriksa entity, DTO, service, controller, menu, migration, dan stored
   procedure yang terdampak;
3. menyajikan impact analysis dan rencana perubahan per file;
4. menunggu persetujuan sebelum mengubah kode;
5. mengimplementasi, build, dan melaporkan hasil verifikasi.

## Hubungan dengan `create-ryzcore`

| Paket | Fungsi |
|---|---|
| `create-ryzcore` | Generator framework. Menghasilkan solusi .NET 10 siap pakai. |
| `ryzcore-skill` | Pengetahuan untuk agent. Membuat agent paham cara kerja framework. |

Keduanya independen: Anda bisa memakai generator tanpa skill, atau memakai
skill untuk project yang sudah ada.

## Versioning

Versi `ryzcore-skill` bergerak terpisah dari `create-ryzcore`. Jika aturan
framework berubah, `FRAMEWORK-REFERENCE.md` di repo utama dan file `references/`
di paket ini harus diperbarui bersama.

## Lisensi

MIT — lihat [LICENSE](LICENSE).
