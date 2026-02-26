using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Goobstation.Common.CCVar;
using Content.Server.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Goobstation.Server.Verify;

public sealed class VerifyApiController : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    private HttpListener? _listener;
    private string _apiSecret = string.Empty;
    private ISawmill _sawmill = default!;

    private const int VerifyMinutes = 9999;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("verify");

        _cfg.OnValueChanged(GoobCVars.VerifyApiSecret,
            s => _apiSecret = s, true);

        var port = _cfg.GetCVar(GoobCVars.VerifyApiPort);
        StartListener(port);
    }

    private void StartListener(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://*:{port}/");
        _listener.Start();
        _ = HandleRequests();
    }

    private async Task HandleRequests()
    {
        while (_listener is { IsListening: true })
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = HandleRequest(ctx);
            }
            catch { break; }
        }
    }

    private async Task HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        if (req.Url?.AbsolutePath != "/verify" || req.HttpMethod != "POST")
        {
            res.StatusCode = 404;
            res.Close();
            return;
        }

        if (req.Headers["X-Secret"] != _apiSecret)
        {
            res.StatusCode = 403;
            res.Close();
            return;
        }

        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<VerifyRequest>(body);

        if (data?.Username == null)
        {
            await WriteJson(res, 400, new { success = false, error = "no_username" });
            return;
        }

        if (!VerifySystem.OnlinePlayers.TryGetValue(data.Username, out var userId))
        {
            await WriteJson(res, 200, new { success = false, error = "not_online" });
            return;
        }

        try
        {
            var result = await GrantPlaytime(userId);
            await WriteJson(res, 200, new
            {
                success = true,
                username = data.Username,
                totalMinutes = result.TotalMinutes,
                unlockedJobs = result.UnlockedJobs
            });
        }
        catch (Exception e)
        {
            _sawmill.Error($"Ошибка выдачи playtime для {data.Username}: {e}");
            await WriteJson(res, 200, new { success = false, error = "db_error" });
        }
    }

    private async Task<GrantResult> GrantPlaytime(NetUserId userId)
    {
        var playTimes = await _db.GetPlayTimes(userId);
        var playTimeDict = new Dictionary<string, TimeSpan>();
        foreach (var pt in playTimes)
            playTimeDict[pt.Tracker] = pt.TimeSpent;

        var totalBefore = playTimeDict.TryGetValue("Overall", out var overall)
            ? (int) overall.TotalMinutes
            : 0;

        var targetTime = TimeSpan.FromMinutes(VerifyMinutes);
        var updateList = new List<PlayTimeUpdate>();
        var unlockedJobs = new List<string>();

        var jobTrackers = new Dictionary<string, string>
        {
            { "Overall", "Общее время" },
            { "JobPassenger", "Пассажир" },
            { "JobTechnicalAssistant", "Технический ассистент" },
            { "JobStationEngineer", "Станционный инженер" },
            { "JobAtmosphericTechnician", "Атмосферный техник" },
            { "JobChiefEngineer", "Старший инженер" },
            { "JobMedicalDoctor", "Врач" },
            { "JobMedicalIntern", "Интерн" },
            { "JobParamedic", "Парамедик" },
            { "JobChemist", "Химик" },
            { "JobPsychologist", "Психолог" },
            { "JobChiefMedicalOfficer", "Главный врач" },
            { "JobResearchDirector", "Директор исследований" },
            { "JobScientist", "Учёный" },
            { "JobResearchAssistant", "Ассистент исследователя" },
            { "JobSecurityOfficer", "Офицер безопасности" },
            { "JobSecurityCadet", "Кадет безопасности" },
            { "JobDetective", "Детектив" },
            { "JobHeadOfSecurity", "Глава безопасности" },
            { "JobWarden", "Смотритель" },
            { "JobHeadOfPersonnel", "Глава персонала" },
            { "JobCaptain", "Капитан" },
            { "JobQuartermaster", "Квартирмейстер" },
            { "JobCargoTechnician", "Грузовой техник" },
            { "JobSalvageSpecialist", "Спасатель" },
            { "JobJanitor", "Уборщик" },
            { "JobChef", "Повар" },
            { "JobBartender", "Бармен" },
            { "JobBotanist", "Ботаник" },
            { "JobLawyer", "Юрист" },
            { "JobMime", "Мим" },
            { "JobClown", "Клоун" },
            { "JobChaplain", "Капеллан" },
            { "JobLibrarian", "Библиотекарь" },
            { "JobMusician", "Музыкант" },
            { "JobReporter", "Репортёр" },
            { "JobServiceWorker", "Сервисный работник" },
            { "JobBoxer", "Боксёр" },
            { "JobZookeeper", "Смотритель зоопарка" },
            { "JobBorg", "Борг" },
            { "JobStationAi", "Станционный ИИ" },
        };

        foreach (var (tracker, jobName) in jobTrackers)
        {
            if (playTimeDict.TryGetValue(tracker, out var existing) && existing >= targetTime)
                continue;

            updateList.Add(new PlayTimeUpdate(userId, tracker, targetTime));
            if (tracker != "Overall")
                unlockedJobs.Add(jobName);
        }

        if (updateList.Count > 0)
            await _db.UpdatePlayTimes(updateList);

        return new GrantResult
        {
            TotalMinutes = totalBefore,
            UnlockedJobs = unlockedJobs
        };
    }

    private static async Task WriteJson(HttpListenerResponse res, int status, object body)
    {
        res.StatusCode = status;
        res.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body));
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    private sealed class VerifyRequest
    {
        public string? Username { get; set; }
    }

    private sealed class GrantResult
    {
        public int TotalMinutes { get; set; }
        public List<string> UnlockedJobs { get; set; } = new();
    }
}
