# BeasiswaIn-Hackaton-2026

## ScholarBot

Halaman `/scholarbot` menyediakan konsultasi beasiswa baca-saja untuk pengguna yang sudah masuk. Tautan tersedia di halaman utama.

System prompt membatasi percakapan ke pencarian/pendaftaran beasiswa, kategori, persyaratan, dokumen, deadline, persiapan esai/wawancara beasiswa, dan penggunaan fitur beasiswa di BeasiswaIn. Curhat pribadi umum dan permintaan lain di luar topik ditolak singkat tanpa tool call. Untuk pertanyaan campuran, hanya bagian beasiswa yang dilayani. Roleplay, dalih esai, atau riwayat chat yang sebelumnya melenceng tidak mengubah batas ini.

### Kemampuan

- `GetUserProfile`: membaca preferensi kategori milik pengguna yang sedang login dan kategori aktif yang tersedia. Tidak mengasumsikan profil akademik atau sertifikat tersimpan.
- `GetRecommendations`: memakai filtering kategori dan `OpenAiService.AnalyzeBestScholarshipChoices` yang sudah ada. Hanya kandidat dalam periode pendaftaran yang dipertimbangkan. Maksimal 10 kandidat dengan deadline terdekat dianalisis; maksimal 5 rekomendasi ditampilkan. Cakupan terbatas ini diberikan kepada agent.
- `GetScholarshipDetails`: membaca detail berdasarkan ID atau pencarian nama, termasuk status pendaftaran, persyaratan, dan tautan panduan/pendaftaran. Nama ambigu perlu dipilih terlebih dahulu.

ID dan skor hasil matching divalidasi; nama dan tautan sumber berasal dari database, bukan keluaran model. Skor adalah estimasi AI, bukan peluang diterima atau jaminan kelayakan. Detail panjang dapat diringkas/dibatasi untuk konteks AI; panduan sumber tetap perlu dibaca.

Tidak ada missing-fields tool, gap analysis, bookmark, perubahan profil, unggah sertifikat, riset web, migration, atau seeding baru. Informasi akademik dalam chat hanya konteks percakapan.

### Review esai di chat yang sama

Tempel draf esai langsung di chat dan sebutkan beasiswa tujuan, instruksi/fokus penilaian, serta batas kata jika ada. Tidak ada tab, formulir, endpoint, atau service review terpisah. Contoh: “Review esai ini untuk beasiswa X: kritik isi, proofread, sarankan alur, jelaskan perubahan, lalu tulis revisi utuh maksimal 500 kata,” diikuti draf.

ScholarBot memakai `GetScholarshipDetails` untuk konteks program. Fokus leadership, akademik, atau pengabdian tidak boleh dianggap kriteria resmi tanpa dasar sumber; instruksi yang ditempel pengguna ditandai sebagai arahan pengguna yang belum diverifikasi. Hasil review berupa teks chat: kritik, kutipan asli dan perbaikan beralasan, alur yang disarankan, revisi lengkap dalam bahasa asli, dan pertanyaan untuk fakta yang kurang.

Pengalaman/prestasi tidak boleh dikarang; esai tidak disimpan ke database. Batas kata merupakan arahan kepada model, bukan validasi hitungan kata otomatis. Penulis tetap perlu memeriksa fakta, persyaratan resmi, dan jumlah kata sebelum memakai revisi.


### Menjalankan lokal

Gunakan .NET 10 dan database PostgreSQL existing. Untuk database VPS melalui tunnel lokal, arahkan connection string ke `127.0.0.1:15432`; jangan memakai host/port internal Docker VPS sebagai alamat lokal.

Konfigurasi rahasia menggunakan user-secrets atau environment variables, bukan file yang di-commit:

```powershell
dotnet user-secrets set "OpenAi:ApiKey" "<OPENROUTER_API_KEY>" --project src/WebApp/WebApp.csproj
dotnet user-secrets set "ConnectionStrings:Default" "Host=127.0.0.1;Port=15432;Database=<DATABASE>;Username=<USER>;Password=<PASSWORD>" --project src/WebApp/WebApp.csproj
dotnet run --project src/WebApp/WebApp.csproj --launch-profile http
```

Konfigurasi Cloudinary existing tetap diperlukan saat startup aplikasi, walaupun ScholarBot tidak melakukan upload. Masuk dengan akun existing, lalu buka `/scholarbot`.

ScholarBot memakai client terpisah dengan model default `openai/gpt-6-luna`. API key dan endpoint mengikuti `OpenAi:ApiKey` / `OpenAi:Endpoint` (OpenRouter: `https://openrouter.ai/api/v1`). `OpenAi:Model` tetap digunakan pipeline matching/onboarding existing dan tidak diganti oleh registrasi ScholarBot.

Opsi berikut dapat diatur lewat konfigurasi `ScholarBot` atau environment variables, misalnya `ScholarBot__Model`:

| Opsi | Default |
|---|---:|
| `Model` | `openai/gpt-6-luna` |
| `MaxIterations` | 6 |
| `MaxToolCalls` | 8 |
| `TimeoutSeconds` | 90 |
| `MaxOutputTokens` | 6000 |
| `MaxTotalTokens` | 48000 |
| `MaxToolResultCharacters` | 16000 |

Pemanggilan matching bersarang ikut dicatat dalam penggunaan token/biaya. Batas token kumulatif diperiksa setelah usage diterima; ini batas penghentian eksekusi, bukan hard billing limit dari provider. Retry otomatis client ScholarBot dimatikan. Error layanan tidak diganti dengan rekomendasi buatan.

### Keamanan dan konteks

- Semua tool mengikat identitas dari sesi server; argumen `userId` tidak diterima. Tidak ada tool tulis atau SQL bebas.
- Endpoint chat membutuhkan login, antiforgery token, dan rate limit. Browser hanya boleh mengirim riwayat berperan `user` atau `assistant`, bukan `system`/`tool`.
- Pesan/esai maksimal 12.000 karakter. Riwayat hanya berada di memori halaman: maksimal 12 pesan historis dan 48.000 karakter total konteks termasuk pesan saat ini. Giliran lama dilepas sebagai pasangan utuh bila penuh; draf/revisi dalam giliran yang masih dipakai tidak dipotong sebagian. Memuat ulang atau memulai percakapan baru menghapus konteks halaman, bukan data akun.
- Jawaban ditampilkan sebagai teks, bukan HTML/Markdown aktif. Kartu sumber hanya menerima URL HTTP/HTTPS.
- Log aplikasi mencatat nama/status/durasi tool dan usage AI, bukan isi percakapan, argumen tool, atau kredensial. Biaya ditampilkan hanya jika provider melaporkannya; nilai tidak tersedia bukan berarti gratis.
- Konteks yang dipakai AI dikirim ke OpenRouter/provider model. Hindari data sensitif dan ikuti kebijakan retensi provider.
- Deployment Production tetap membutuhkan HTTPS sesuai konfigurasi cookie aplikasi.

Contoh uji manual: beri konteks S1 dan minat AI, minta satu beasiswa yang masih buka beserta checklist. Buka bagian aktivitas untuk melihat pemanggilan profil, rekomendasi, dan detail; periksa tanggal serta tautan sumber. Permintaan menyimpan profil atau bookmark harus ditolak.
