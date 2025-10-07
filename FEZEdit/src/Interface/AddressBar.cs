using System;
using System.Collections.Generic;
using Godot;

namespace FEZEdit.Interface;

public partial class AddressBar : Control
{
    public event Action<string> AddressChanged;
    
    public Predicate<string> IsPathValid { get; set; } 
    
    private Button _backButton;

    private Button _forwardButton;

    private LineEdit _pathEdit;
    
    private readonly Stack<string> _backHistory = new();

    private readonly Stack<string> _forwardHistory = new();

    private string _currentDirectory = "";

    public override void _Ready()
    {
        _backButton = GetNode<Button>("%BackButton");
        _backButton.Pressed += OnBackButtonPressed;

        _forwardButton = GetNode<Button>("%ForwardButton");
        _forwardButton.Pressed += OnForwardButtonPressed;

        _pathEdit = GetNode<LineEdit>("%PathEdit");
        _pathEdit.TextSubmitted += OnPathChanged;

        UpdateNavigationButtons();
    }

    public void SetCurrentDirectory(string path, bool addToHistory = true)
    {
        if (_currentDirectory == path)
        {
            return;
        }

        if (addToHistory && !string.IsNullOrEmpty(_currentDirectory))
        {
            _backHistory.Push(_currentDirectory);
            _forwardHistory.Clear();
        }

        _currentDirectory = path ?? "";
        _pathEdit.Text = _currentDirectory;
        
        UpdateNavigationButtons();
        AddressChanged?.Invoke(_currentDirectory);
    }
    
    private void UpdateNavigationButtons()
    {
        _backButton.Disabled = _backHistory.Count == 0;
        _forwardButton.Disabled = _forwardHistory.Count == 0;
    }
    
    private void OnBackButtonPressed()
    {
        if (_backHistory.Count <= 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_currentDirectory))
        {
            _forwardHistory.Push(_currentDirectory);
        }
        
        var previousPath = _backHistory.Pop();
        _currentDirectory = previousPath;
        _pathEdit.Text = _currentDirectory;
        
        UpdateNavigationButtons();
        AddressChanged?.Invoke(_currentDirectory);
    }

    private void OnForwardButtonPressed()
    {
        if (_forwardHistory.Count <= 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_currentDirectory))
        {
            _backHistory.Push(_currentDirectory);
        }

        var nextPath = _forwardHistory.Pop();
        _currentDirectory = nextPath;
        _pathEdit.Text = _currentDirectory;
        
        UpdateNavigationButtons();
        AddressChanged?.Invoke(_currentDirectory);
    }
    
    private void OnPathChanged(string newText)
    {
        if (IsPathValid(newText))
        {
            SetCurrentDirectory(newText);
        }
        else
        {
            _pathEdit.Text = _currentDirectory;
        }
    }
}