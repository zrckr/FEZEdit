using System;
using FEZEdit.Core;
using Godot;

namespace FEZEdit.Main;

public partial class StatusBar : Control
{
    [Export] private IconsResource _icons;

    [Export] private ulong _progressIconFps;

    private TextureRect _statusIcon;

    private Label _statusLabel;

    private ProgressBar _progressBar;

    private Label _versionLabel;

    private Control _progressIcon;

    private Sprite2D _progressSprite;

    public override void _EnterTree()
    {
        EventBus.MessageSent += (@event, message) => Callable.From(() => OnMessageSent(@event, message)).CallDeferred();
        EventBus.ProgressUpdated += (progress, message) =>
            Callable.From(() => OnProgressUpdated(progress, message)).CallDeferred();
    }

    public override void _Ready()
    {
        _statusIcon = GetNode<TextureRect>("%StatusIcon");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _progressBar = GetNode<ProgressBar>("%ProgressBar");
        _versionLabel = GetNode<Label>("%VersionLabel");
        _progressIcon = GetNode<Control>("%ProgressIcon");
        _progressSprite = _progressIcon.GetNode<Sprite2D>("Sprite");
        _progressBar.Value = 0;
        _progressBar.Visible = false;
        _progressIcon.Visible = false;
        _versionLabel.Text = ProjectSettings.GetSetting("application/config/version").AsString();
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (Engine.GetProcessFrames() % _progressIconFps == 0)
        {
            _progressSprite.Frame = Mathf.Wrap(_progressSprite.Frame + 1, 0, _progressSprite.Hframes - 1);
        }
    }

    private void OnMessageSent(EventBus.EventType @event, string message)
    {
        if (@event == EventBus.EventType.Progress)
        {
            return;
        }

        _statusLabel.Text = message;
        _statusIcon.Texture = @event switch
        {
            EventBus.EventType.Information => _icons.Info,
            EventBus.EventType.Success => _icons.Success,
            EventBus.EventType.Warning => _icons.Warning,
            EventBus.EventType.Error => _icons.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, null)
        };
    }

    private void OnProgressUpdated(ProgressValue progress, string message)
    {
        if (progress.Completed)
        {
            _statusIcon.Visible = true;
            _statusLabel.Text = string.Empty;
            _progressIcon.Visible = false;
            _progressBar.Visible = false;
            _progressBar.Value = 0;
            SetProcess(false);
            return;
        }

        _statusIcon.Visible = false;
        _statusLabel.Text = message;
        _progressIcon.Visible = true;
        _progressBar.Visible = true;
        _progressBar.MinValue = progress.Minimum;
        _progressBar.MaxValue = progress.Maximum;
        _progressBar.Step = progress.Step;
        _progressBar.Value = progress.Value;
        SetProcess(true);
    }
}