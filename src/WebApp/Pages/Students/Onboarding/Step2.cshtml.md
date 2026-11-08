@page "/profil-beasiswa/sertifikat"
@model BeasiswaIn.Web.Pages.Students.Onboarding.Step2Model
@{
    ViewData["Title"] = "Unggah Sertifikat";
}

<main class="flex min-h-dvh flex-col justify-between bg-white text-neutral-800">
    <div class="px-5 pb-2 pt-4">
        <header class="mb-5 flex items-center gap-3.5 pt-1">
            <a href="/profil-beasiswa" aria-label="Kembali" class="p-1 text-neutral-900">
                <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" stroke-linecap="round" stroke-linejoin="round"></path></svg>
            </a>
            <h1 class="text-xl font-bold tracking-tight text-[#1C1F16]">Unggah Sertifikat</h1>
        </header>
        <section class="mb-5" aria-label="Progress Langkah">
            <div class="mb-2 flex items-center justify-between text-[11px] tracking-wide">
                <span class="font-extrabold uppercase tracking-wider text-[#00613a]">LANGKAH 2 DARI 2</span>
                <span class="font-medium text-[#70766B]">Unggah Sertifikat</span>
            </div>
            <div class="flex w-full items-center gap-2">
                <div class="h-1.5 flex-1 rounded-full bg-[#00613a]"></div>
                <div class="h-1.5 flex-1 rounded-full bg-[#00613a]"></div>
            </div>
        </section>
        <section class="mb-5">
            <h2 class="mb-1.5 text-xl font-bold tracking-tight text-[#161A10]">Unggah Dokumen Sertifikat</h2>
            <p class="text-[13px] leading-relaxed text-[#5F6359]">Lampirkan bukti sertifikat prestasi, TOEFL/IELTS, atau sertifikat organisasi terbaru untuk memperkuat profil beasiswamu.</p>
        </section>
        <section class="mb-6">
            <label for="certificateInput" class="block w-full cursor-pointer rounded-2xl border-2 border-dashed border-[#00613a] bg-white px-4 py-8 text-center transition hover:bg-[#F2F9F6]">
                <input id="certificateInput" class="hidden" type="file" accept=".pdf,.png,.jpg,.jpeg,image/png,image/jpeg,application/pdf" />
                <div class="relative mb-3.5 flex items-center justify-center">
                    <div class="flex h-14 w-14 items-center justify-center rounded-full bg-[#E6F4EA]">
                        <svg class="h-7 w-7 text-[#00613a]" fill="currentColor" viewBox="0 0 24 24"><path d="M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96zM14 13v4h-4v-4H7l5-5 5 5h-3z"></path></svg>
                    </div>
                    <div class="absolute -bottom-0.5 -right-0.5 flex h-5 w-5 items-center justify-center rounded-full border-2 border-white bg-[#00613a] text-white">
                        <svg class="h-3 w-3" fill="none" stroke="currentColor" stroke-width="3" viewBox="0 0 24 24"><path d="M12 4.5v15m7.5-7.5h-15" stroke-linecap="round"></path></svg>
                    </div>
                </div>
                <p class="mb-1 text-[14.5px] font-bold text-[#1A1E14]">Pilih dokumen atau seret ke sini</p>
                <p class="mx-auto max-w-[215px] text-[11.5px] leading-tight text-[#787D72]">Format didukung: PDF, JPG, PNG (Maks. 5MB per berkas)</p>
            </label>
            <p id="certificateError" class="mt-2 hidden text-xs text-red-600"></p>
        </section>
        <section class="mb-5" aria-label="Daftar Berkas Terunggah">
            <div class="mb-3 flex items-center justify-between px-0.5">
                <div class="flex items-center gap-2">
                    <h3 class="text-sm font-bold text-[#1B1E14]">File Terunggah</h3>
                    <span id="fileCount" class="inline-flex h-4 w-4 items-center justify-center rounded-full bg-[#E6F4EA] text-[11px] font-bold text-[#00613a]">1</span>
                </div>
                <span class="text-xs text-[#70766B]">Maks 1 berkas</span>
            </div>
            <div id="fileList" class="space-y-2.5">
                <div data-file-row class="flex items-center justify-between rounded-2xl border border-[rgba(0,78,46,0.12)] bg-white p-3.5 shadow-[0_10px_25px_-5px_rgba(0,78,46,0.12)]">
                    <div class="flex min-w-0 items-center gap-3">
                        <div class="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-[#E6F4EA] text-[#00613a]">
                            <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="1.8" viewBox="0 0 24 24"><path d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" stroke-linecap="round"></path></svg>
                        </div>
                        <div class="truncate">
                            <p data-file-name class="mb-0.5 truncate text-[13px] font-bold leading-tight text-[#1C1F16]">Sertifikat_Prestasi.png</p>
                            <p data-file-meta class="text-[11.5px] text-[#7B8074]">1.9 MB • Berhasil diunggah</p>
                        </div>
                    </div>
                    <button id="removeFile" type="button" aria-label="Hapus file" class="flex-shrink-0 p-1 text-[#D93838]">
                        <svg class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="1.6" viewBox="0 0 24 24"><path d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" stroke-linecap="round"></path></svg>
                    </button>
                </div>
            </div>
        </section>
    </div>
    <div class="px-5 pb-6 pt-2">
        <form method="post">
            <button class="flex w-full items-center justify-center gap-2 rounded-full bg-[#004e2e] px-6 py-3.5 text-sm font-bold text-white shadow-[0_4px_14px_0_rgba(0,78,46,0.35)]" type="submit">
                <span>Submit dan Analisa</span>
                <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" stroke-linecap="round"></path></svg>
            </button>
        </form>
        <div class="mt-4 flex items-center justify-center gap-1.5 text-[12px] text-[#696F63]">
            <svg class="h-4 w-4 text-[#00613a]" fill="none" stroke="currentColor" stroke-width="1.8" viewBox="0 0 24 24"><path d="M12 16.5V9.75m0 0l3 3m-3-3l-3 3" stroke-linecap="round"></path></svg>
            <span>Draf tersimpan secara otomatis</span>
        </div>
    </div>
</main>

@section Scripts {
    <script src="~/js/mobile/profil-beasiswa.js" asp-append-version="true"></script>
}
