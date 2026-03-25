using LeoniRFID.Models;
using Newtonsoft.Json;
using System.Text;

namespace LeoniRFID.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private string? _token;

    public ApiService(HttpClient http) => _http = http;

    public void SetAuthToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    // ── Auth ──────────────────────────────────────────────────────────────────
    public async Task<(bool, string?)> LoginAsync(string email, string password)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { email, password });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("auth/login", content);
            if (!response.IsSuccessStatusCode) return (false, null);

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(json);
            string? token = result?.token;
            if (!string.IsNullOrEmpty(token)) SetAuthToken(token);
            return (true, token);
        }
        catch { return (false, null); }
    }

    // ── Machines ──────────────────────────────────────────────────────────────
    public async Task<List<Machine>?> GetMachinesAsync()
    {
        try
        {
            var json = await _http.GetStringAsync("machines");
            return JsonConvert.DeserializeObject<List<Machine>>(json);
        }
        catch { return null; }
    }

    public async Task<bool> UpdateMachineAsync(Machine machine)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(machine);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync($"machines/{machine.Id}", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Scan Events ───────────────────────────────────────────────────────────
    public async Task<bool> PostScanEventAsync(ScanEvent scanEvent)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(scanEvent);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("events", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Health Check ──────────────────────────────────────────────────────────
    public async Task<bool> IsServerReachableAsync()
    {
        try
        {
            var response = await _http.GetAsync("health");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
