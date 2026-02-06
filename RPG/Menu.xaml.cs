using System;
using System.Windows;
using System.Windows.Controls;

namespace RPG;

public partial class Menu : UserControl
{
    public event EventHandler? StartGameRequested;
    public event EventHandler? OptionsRequested;
    public event EventHandler? QuitRequested;

    public Menu()
    {
        InitializeComponent();
    }

    private void OnPlayClicked(object sender, RoutedEventArgs e)
    {
        StartGameRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnOptionsClicked(object sender, RoutedEventArgs e)
    {
        OptionsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnQuitClicked(object sender, RoutedEventArgs e)
    {
        QuitRequested?.Invoke(this, EventArgs.Empty);
    }
}
