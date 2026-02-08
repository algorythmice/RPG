using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RPG;

public sealed class SpeechManager
{
    private readonly EntityManager _entityManager;
    private readonly Dictionary<Guid, SpeechInfo> _speeches = new();
    private Border? _speechPanel;
    private TextBlock? _speechTextBlock;
    private bool _displayResolved;

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
    
    private void EnsureDisplay()
    {
        if (_displayResolved) return;
        _speechPanel = Application.Current.MainWindow?.FindName("SpeechPanel") as Border;
        _speechTextBlock = Application.Current.MainWindow?.FindName("SpeechTextBlock") as TextBlock;
        _displayResolved = true;
    }
    
    public string? ShowSpeech(Guid entityId, string idSpeech)
    {
        var text = GetSpeechText(entityId, idSpeech);

        if (text != null)
        {
            EnsureDisplay();
            if (_speechPanel != null && _speechTextBlock != null)
            {
                _speechTextBlock.Text = text;
                _speechPanel.Visibility = Visibility.Visible;
            }
        }

        return text;
    }

    public bool HideSpeech(Guid entityId)
    {
        if (!_speeches.TryGetValue(entityId, out var info)) return false;
        info.CurrentText = null;
        Console.WriteLine($"[Speech] {entityId}: hidden");
        EnsureDisplay();
        if (_speechPanel != null && _speechTextBlock != null)
        {
            _speechTextBlock.Text = string.Empty;
            _speechPanel.Visibility = Visibility.Collapsed;
        }
        return true;
    }
    
    public bool UpdatePosition(Guid entityId)
    {
        return _speeches.ContainsKey(entityId);
    }

    public string? GetSpeechText(Guid entityId, string idSpeech)
    {
        if (idSpeech == null) throw new ArgumentNullException(nameof(idSpeech));
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
                    if (speech.GetProperty("id").GetString() == idSpeech)
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
        }

        return text;
    }

    public bool RemoveSpeech(Guid entityId)
    {
        return _speeches.Remove(entityId);
    }
}
