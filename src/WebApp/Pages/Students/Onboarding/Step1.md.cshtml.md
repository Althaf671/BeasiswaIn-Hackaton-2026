@page "/profil-beasiswa"
@model BeasiswaIn.Web.Pages.Students.Onboarding.Step1Model
@{
    ViewData["Title"] = "Profil Beasiswa";
}

<div class="flex min-h-dvh flex-col justify-between bg-white px-5 pb-6 pt-3">
    <div>
        <header class="mb-3 flex items-center gap-4 py-3">
            <a href="/masuk" aria-label="Kembali" class="p-1 text-[#142713]">
                <svg class="h-6 w-6" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" stroke-linecap="round" stroke-linejoin="round"></path></svg>
            </a>
            <h1 class="text-[20px] font-bold tracking-tight text-[#172714]">Profil Beasiswa</h1>
        </header>

        <section class="mb-6">
            <div class="mb-2 flex items-center justify-between text-[11px] tracking-wide">
                <span class="font-extrabold uppercase tracking-wider text-[#004e2e]">LANGKAH 1 DARI 2</span>
                <span class="font-medium text-[#6D7769]">Esai Diri</span>
            </div>
            <div class="flex w-full items-center gap-2">
                <div class="h-1.5 flex-1 rounded-full bg-[#004e2e]"></div>
                <div class="h-1.5 flex-1 rounded-full bg-[#E4E9DE]"></div>
            </div>
        </section>

        <section class="mb-5">
            <h2 class="mb-2 text-[26px] font-extrabold leading-tight tracking-tight text-[#112411]">Jelaskan Tentang Dirimu</h2>
        </section>

        <section class="mb-5 rounded-2xl border border-[#ECEFE6] bg-white p-4 shadow-[0_2px_12px_rgba(0,0,0,0.03)]">
            <div class="flex items-center justify-between pb-3">
                <div class="flex items-center gap-1.5 text-[13px] font-semibold text-[#004e2e]">
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="2.2" viewBox="0 0 24 24"><path d="M3.75 6.75h16.5M3.75 12h10.5m-10.5 5.25h16.5" stroke-linecap="round"></path></svg>
                    <span>Esai Singkat</span>
                </div>
                <span class="text-[12px] font-medium text-[#697566]"><span id="essayCount">0</span> / 500</span>
            </div>
            <textarea id="essay" maxlength="500" rows="7" class="min-h-[170px] w-full resize-none border-0 p-0 text-[13.5px] leading-relaxed text-[#112411] outline-none placeholder:text-[#9EA79C] focus:ring-0" placeholder="Contoh : Saya mahasiswa baru, memiliki pengalaman organisasi....."></textarea>
            <div class="my-3 h-px w-full bg-[#F1F3ED]"></div>
            <div class="flex items-start gap-2 text-[11.5px] leading-snug text-[#626F5E]">
                <svg class="mt-0.5 h-4 w-4 shrink-0 text-[#004e2e]" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M12 18v-5.25m0 0a6.01 6.01 0 001.5-.189m-1.5.189a6.01 6.01 0 01-1.5-.189m3.75 7.688a4.5 4.5 0 01-4.5 0" stroke-linecap="round"></path></svg>
                <p>Tips: Ceritakan minat spesifik dan kontribusi yang ingin kamu berikan.</p>
            </div>
        </section>

        <section>
            <div class="mb-2.5 flex items-center justify-between px-0.5">
                <div class="flex items-center gap-1.5 text-[14px] font-bold text-[#142614]">
                    <svg class="h-4 w-4 text-[#004e2e]" fill="none" stroke="currentColor" stroke-width="2.2" viewBox="0 0 24 24"><path d="M9.568 3H5.25A2.25 2.25 0 003 5.25v4.318c0 .597.237 1.17.659 1.591l9.581 9.581c.699.699 1.78.872 2.607.386l3.41-2.006" stroke-linecap="round"></path></svg>
                    <span>Kategori Beasiswa</span>
                </div>
                <span class="rounded-full bg-[#e6f2eb] px-2.5 py-0.5 text-[11px] font-bold text-[#004e2e]"><span id="chipCount">3</span> Terpilih</span>
            </div>
            <div id="chipBox" class="min-h-44 rounded-2xl border-2 border-[#004e2e] bg-white p-3.5 shadow-sm">
                <div id="chipList" class="mb-3 flex flex-wrap items-center gap-2">
                    @foreach (var chip in new[] { "Fully Funded", "Ptn", "Swasta" })
                    {
                        <span data-chip data-label="@chip" class="inline-flex items-center gap-1.5 rounded-full border border-[#b8e2cb] bg-[#e6f2eb] px-3 py-1.5 text-[12px] font-semibold text-[#004e2e]">
                            @chip
                            <button type="button" data-remove-chip aria-label="Hapus @chip" class="text-xs font-bold leading-none">×</button>
                        </span>
                    }
                </div>
                <input id="chipInput" list="categoryOptions" class="w-full border-0 p-0 text-[13.5px] font-medium text-[#112411] outline-none placeholder:text-[#9EA79C] focus:ring-0" placeholder="Ketik kategori lalu Enter" />
                <datalist id="categoryOptions">
                    @foreach (var option in Model.SuggestedCategories)
                    {
                        <option value="@option"></option>
                    }
                </datalist>
            </div>
        </section>
    </div>

    <footer class="mt-6 flex flex-col items-center gap-2.5 pt-1">
        <button id="saveStep" type="button" class="flex w-full items-center justify-center gap-2 rounded-full bg-[#004e2e] px-6 py-3.5 text-[15px] font-bold text-white shadow-[0_4px_14px_rgba(0,78,46,0.35)] transition active:scale-[0.99]">
            <span>Simpan dan Lanjut</span>
            <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" stroke-linecap="round" stroke-linejoin="round"></path></svg>
        </button>
        <div class="flex items-center justify-center gap-1.5 pb-1 text-[11.5px] text-[#556350]">
            <svg class="h-3.5 w-3.5 text-[#004e2e]" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M12 16.5V9.75m0 0l3 3m-3-3l-3 3M6.75 19.5a4.5 4.5 0 01-1.41-8.775 5.25 5.25 0 0110.233-2.33 3 3 0 013.758 3.848A3.752 3.752 0 0118 19.5H6.75z" stroke-linecap="round"></path></svg>
            <span>Draf tersimpan secara otomatis</span>
        </div>
    </footer>
</div>

@section Scripts {
    <script src="~/js/mobile/profil-beasiswa.js" asp-append-version="true"></script>
}
