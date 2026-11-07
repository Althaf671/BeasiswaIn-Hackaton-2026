using BeasiswaIn.Web.Models.Chatbot;

namespace BeasiswaIn.Web.Common.Constants;

internal sealed class OpenAiConstant
{
    public string PersonalizationPrompt(
        string certificateUrl,
        ScholarshipPersonlizationPromptRequest request,
        CancellationToken cancellationToken)
    {
        var extractedCertificate = $"""
            Kamu adalah AI Student Opportunity Advisor.
            Pada bagian ini kamu hanya akan membaca sertifikat dari link berikut dan ekstrak
            data sesuai ketentuan.

            ## PARAMETERS
            Berikut adalah daftar parameter yang harus ekstrak:
            1. certificateName
            2. certificateType
            3. ownerName
            4. institution
            5. eventName
            6. category
            7. achievement
            8. competitionLevel
            9. rank
            10. score
            11. grade
            12. scoreType
            13. documentLanguage

            ## INSTRUKSI
            1. Ekstrak lah sertifkat dari gambar pada link di bawah
            2. Parameter yang di ekstrek sesuai ketentuan di atas
            3. Jika ada parameter yang gagal di ekstrak maka isi dengan unknown
            
            ## SERTIFIKAT YANG DI EXTRACT
            {certificateUrl}

            ## FORMAT OUTPUT DALAM JSON (DIBAWAH NI HANYA FIELDS YANG HARUS KAMU IKUTI, FORMATNYA HARUS JSON)
            ## VALUE DI SINI HANYA CONTOH
            "extractionCertificate": (
                "certificateName": "ETS TOEFL ITP Certificate of Achievement",
                "certificateType": "English Proficiency Certificate",
                "ownerName": "unknown",
                "institution": "PROGRESS ENGLISH CENTER",
                "eventName": "TOEFL ITP Test",
                "category": "Language",
                "achievement": "Achieved a total TOEFL ITP score of 517",
                "competitionLevel": "unknown",
                "rank": "unknown",
                "score": "517",
                "grade": "unknown",
                "scoreType": "TOEFL ITP Scaled Score",
                "documentLanguage": "English"
            )

        """;

        return $"""
            Kamu adalah AI Student Opportunity Advisor.
            Pada bagian ini kamu akan memberikan daftar beasiswa yang dipersonalisasikan 
            sesuai parameter di bawah.

            Analisis Mahasiswa berikut dan tentukan beasiswa yang paling sesaui berdasarkan
            data yang tersedia.

            ## DATA DIRI {request.Role}
            {request.StudentProfile}

            ## PREFERENSI {request.Role}
            {request.StudentPreference}

            ## TAG KATEGORI {request.Role}
            {request.SelectedScholarshipCategoryTags}

            ## HASIL EKSTRASI SERTIFIKAT MILIK {request.Role}
            {request.CertificateExtraction}

            ## DAFTAR BEASISWA DARI DATABASE BERDASARKAN PILIHAN TAGS {request.Role}
            {request.SelectedScholarshipsBasedOnCategory}

            ## INSTRUKSI
            1. Analisis kesesuaian profile {request.Role}.
            2. Pertimbangkan preferensi dan prestasi.
            3. Pilih beasiswa paling sesuai dari daftar yang diberikan.
            4. Jangan membuat ID beasiswa sendiri.
            5. Berikan alasan pemilihan.
            6. Berikan feedback yang relevan untuk mahasiswa.
            7. Jangan mengarang informasi yang tidak tersedia.
            8. Diharuskan beri maksimal 5 beasiswa, jika tidak cukup apa ada nya.
            9. Beri nilai match score dari asumsi mu sendiri beserta alasannya yang bisa dibuktikan secara matematis.
            10. Pada hasil ekstraksi, jika ada parameter dengan nilai unknown maka hiraukan.

            ## FORMAT OUTPUT DALAM JSON (DIBAWAH NI HANYA FIELDS YANG HARUS KAMU IKUTI, FORMATNYA HARUS JSON)
            ## VALUE DI SINI HANYA CONTOH
            "recommendations": [
                (
                    "opportunityId": "ID_BEASISWA",
                    "matchReason": "Alasan pemilihan",
                    "feedback": "Pesan untuk {request.Role}",
                    "matchScore": 95,
                    "matchScoreSource": "Bukti pemilihan match score yang dapat dipertanggung jawabkan secara matematis"
                )
            ]

        """;
    }
}