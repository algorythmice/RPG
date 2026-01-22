using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace RPG;

public sealed class SpeechManager
{
    private readonly EntityManager _entityManager;
    private readonly Dictionary<Guid, SpeechInfo> _speeches = new();

    private sealed class SpeechInfo
    {
        public string? CurrentText { get; set; }
    }

    public SpeechManager(EntityManager entityManager)
    {
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
    }
    
    private EntityManager.EntityHandle? FindEntityById(Guid? entityId) => _entityManager.FindEntityById(entityId);
    public void RegisterEntity(Guid entityId, FrameworkElement anchor, TranslateTransform transform, double width, double x, double y)
    {
        _speeches[entityId] = new SpeechInfo { CurrentText = null };
    }
    
    public string? ShowSpeech(Guid entityId, string idSearch)
    {
        if (idSearch == null) throw new ArgumentNullException(nameof(idSearch));
        if (!_speeches.TryGetValue(entityId, out var info)) return null;
        
        var entity = FindEntityById(entityId);
        if (entity == null) return null;
        
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var speechesDir = Path.Combine(exeDir, "speeches");

        string filePath = Path.Combine(speechesDir, entity.Name + ".json");
        if (!File.Exists(filePath)) return null;

        var json = File.ReadAllText(filePath);
        string? text = null;

        using (var doc = JsonDocument.Parse(json))
        {
            if (doc.RootElement.TryGetProperty("speeches", out var speeches))
            {
                foreach (var speech in speeches.EnumerateArray())
                {
                    if (speech.GetProperty("id").GetString() == idSearch)
                    {
                        text = speech.GetProperty("text").GetString();
                        break;
                    }
                }
            }
        }

        if (text != null)
        {
            info.CurrentText = text;
            Console.WriteLine($"[Speech] {entityId}: {text}");
        }

        return text;
    }

    public bool HideSpeech(Guid entityId)
    {
        if (!_speeches.TryGetValue(entityId, out var info)) return false;
        info.CurrentText = null;
        Console.WriteLine($"[Speech] {entityId}: hidden");
        return true;
    }

    // Compat: nothing to update visually in console mode
    public bool UpdatePosition(Guid entityId)
    {
        return _speeches.ContainsKey(entityId);
    }

    public string? GetSpeechText(Guid entityId)
    {
        return _speeches.TryGetValue(entityId, out var info) ? info.CurrentText : null;
    }

    public bool RemoveSpeech(Guid entityId)
    {
        return _speeches.Remove(entityId);
    }
}
