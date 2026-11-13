# Panduan Instalasi RyzCore

Panduan ini menjelaskan cara memasang generator RyzCore dan menjalankan aplikasi hasil generate.

## 1. Prasyarat

| Komponen | Versi/ketentuan |
|---|---|
| Node.js | 20 atau lebih baru |
| npm | Mengikuti Node.js |
| .NET SDK | .NET 10 |
| SQL Server | TCP 1433 aktif, SQL authentication |
| .NET MAUI workload | Diperlukan jika tidak memakai `--no-maui` |
| Docker Desktop | Opsional, untuk `--docker` |
| Git | Opsional, untuk instalasi dari GitHub |

Cek versi:

```bash
node --version
npm --version
dotnet --version
```

Cek SQL Server:

```bash
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -Q "SELECT 1"
```

Login SQL harus memiliki izin membuat database (`dbcreator` atau setara), membuat tabel, migration, dan stored procedure.

## 2. Instalasi generator

### Dari npm (global)

```bash
npm install -g create-ryzcore
create-ryzcore --help
```

### Tanpa instalasi global

```bash
npx create-ryzcore --help
```

### Dari GitHub (global)

```bash
npm install -g github:rayzaisra/RyzCore
create-ryzcore --help
```

### Dari tarball

```bash
npm pack
npm install -g ./create-ryzcore-0.1.0.tgz
```

Jika PowerShell tidak menemukan command setelah install global, jalankan binary langsung:

```powershell
& "$env:APPDATA\npm\create-ryzcore.cmd"
```

Atau tambahkan `%APPDATA%\npm` ke `PATH` lalu buka terminal baru.

### Dari source lokal

```bash
git clone https://github.com/rayzaisra/RyzCore.git
cd RyzCore
npm install
npm link
create-ryzcore --help
```

## 3. Membuat aplikasi

### Mode interaktif

```bash
create-ryzcore
```

Ikuti pertanyaan: nama project, database, kredensial SQL, admin awal, logo, MAUI, test, dan Docker.

### Mode config

Salin `scaffold.example.json`, sesuaikan nilainya, lalu:

```bash
create-ryzcore --config scaffold.example.json
```

Contoh isi config:

```json
{
  "projectName": "MitraPensiun",
  "dbName": "mitra_pensiun_db",
  "auditDbName": "mitra_pensiun_log_audit",
  "server": "localhost,1433",
  "authType": "sql",
  "dbUser": "sa",
  "dbPassword": "YourStrong!Passw0rd",
  "trustServerCertificate": true,
  "adminUser": "admin",
  "adminPassword": "Admin#12345",
  "adminName": "Administrator",
  "adminEmail": "admin@example.com",
  "logoPath": "",
  "includeMaui": true,
  "includeTests": false,
  "includeDocker": false,
  "agentDocs": false
}
```

### Mode argumen

```bash
create-ryzcore \
  --name MyApp \
  --db my_app_db \
  --audit-db my_app_log_audit \
  --server localhost,1433 \
  --user sa \
  --password "YourStrong!Passw0rd" \
  --admin-user admin \
  --admin-password "Admin#12345" \
  --no-maui \
  --tests
```

### Dry-run

```bash
create-ryzcore --config scaffold.example.json --dry-run
```

Dry-run hanya menampilkan rencana; tidak menulis file atau database.

## 4. Yang dilakukan generator

1. Menyalin template solution.
2. Mengganti token project/database/API.
3. Menghapus project MAUI jika `--no-maui`.
4. Menambahkan xUnit project dan menjalankan test jika `--tests`.
5. Menulis Dockerfile/compose jika `--docker`.
6. Menjalankan `dotnet build`.
7. Membuat database utama dan audit.
8. Menjalankan EF migration + seed melalui API `--seed-only`.
9. Deploy stored procedure.

Jika langkah build/seed gagal, generator berhenti dengan pesan error dan tidak melanjutkan ke langkah berikutnya.

## 5. Menjalankan aplikasi hasil generate

```bash
cd <ProjectName>
dotnet run --project <ProjectName>.Api
```

Di terminal lain:

```bash
dotnet run --project <ProjectName>.Web
```

Web default: `https://localhost:44353`  
API default: `https://localhost:7143`

MAUI Windows:

```bash
dotnet build <ProjectName>.App -f net10.0-windows10.0.19041.0
dotnet run --project <ProjectName>.App -f net10.0-windows10.0.19041.0
```

Login awal memakai `adminUser` dan `adminPassword` dari config. Segera ganti password setelah login.

### Build APK Android

APK Debug standalone wajib memakai `EmbedAssembliesIntoApk=true`. Tanpa properti ini, APK memakai Fast Deployment dan akan crash saat dibuka dengan pesan:

```text
No assemblies found in .../.__override__/arm64-v8a
```

Perintah build:

```powershell
dotnet clean <ProjectName>.App.csproj -f net10.0-android -v:minimal
dotnet publish <ProjectName>.App.csproj `
  -f net10.0-android `
  -c Debug `
  -p:AndroidPackageFormats=apk `
  -p:EmbedAssembliesIntoApk=true `
  -p:AndroidEnableProfiledAot=false
```

APK berada di:

```text
<ProjectName>.App/bin/Debug/net10.0-android/<ApplicationId>-Signed.apk
```

Untuk perangkat fisik, isi `ApiBaseUrl` pada `Resources/Raw/appsettings.json` dengan IP LAN komputer, jalankan API pada `0.0.0.0`, dan izinkan port API di Windows Firewall. Build Debug memakai bypass sertifikat development; untuk distribusi produksi gunakan build Release dengan keystore resmi.

## 6. Docker

Jika dibuat dengan `--docker`:

```bash
docker compose up --build
```

Port default:

- API: `7143`
- Web: `44353`
- SQL: `1433` (pastikan tidak bentrok dengan SQL Server host)

Jika SQL Server host sudah memakai 1433, ubah mapping port SQL pada `docker-compose.yml` dan gunakan port tersebut untuk koneksi dari host.

## 7. Test

Jika dibuat dengan `--tests`:

```bash
dotnet test <ProjectName>.sln
```

Generator juga menjalankan test secara otomatis setelah build.

## 8. Troubleshooting

### SQL Server tidak bisa dihubungi

- Pastikan service SQL Server berjalan.
- Aktifkan TCP/IP dan port 1433.
- Pastikan SQL authentication aktif.
- Coba `sqlcmd` dengan server `localhost,1433`.
- Jika memakai instance bernama, gunakan `host\instance` dan pastikan SQL Browser berjalan.

### Build MAUI gagal

- Install workload: `dotnet workload install maui-windows`.
- Atau generate ulang dengan `--no-maui`.
- Untuk Android/iOS diperlukan SDK/emulator/device masing-masing.

### Port sudah dipakai

Ubah port pada `Properties/launchSettings.json` (API/Web) atau mapping port pada compose.

### Database sudah ada

Generator menggunakan nama database dari config. Gunakan nama berbeda atau hapus database lama secara sengaja sebelum generate ulang. Generator tidak menghapus database secara otomatis.

### Login gagal setelah generate

- Pastikan seed selesai tanpa error.
- Periksa `adminUser`/`adminPassword` pada config.
- Periksa koneksi API ke database yang benar.

## 9. Upgrade

```bash
npm update -g create-ryzcore
# atau
npm install -g github:rayzaisra/RyzCore
```

Aplikasi yang sudah digenerate tidak otomatis ter-upgrade. Untuk template terbaru, generate project baru lalu port perubahan bisnis secara terkontrol.

## 10. Uninstall

```bash
npm uninstall -g create-ryzcore
```
