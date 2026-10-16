using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LocalJsonModule.DTOs.Users;
using LocalJsonModule.Models;
using LocalJsonModule.Services;

namespace NoteApp.Windows;

public partial class RegisterWindow : Window
{
    private readonly IRegisterService _register;
    
    
    
    public User? AuthenticatedUser { get; private set; }
    
    public RegisterWindow(IRegisterService register, IAuthService auth)
    {
        InitializeComponent();
        _register = register;

        this.FindControl<Button>("RegisterButton")!.Click += onRegisterClick;
        this.FindControl<Button>("LoginButton")!.Click += onLoginClick;
    }

    private async void onRegisterClick(object sender, RoutedEventArgs e)
    {
        var email = this.FindControl<TextBox>("EmailTextBox")!.Text ?? "";
        var login = this.FindControl<TextBox>("LoginTextBox")!.Text ?? "";
        var pass  = this.FindControl<TextBox>("PasswordTextBox")!.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(pass))
        {
            await ShowMessageAsync("Заполните все поля регистрации.");
            return;
        }
        
        try
        {
            var user = await _register.RegisterAsync(new RegisterUserRequest
            {
                Login    = login,
                Email    = email,
                Password = pass
            });

            if (user is null)
            {
                await ShowMessageAsync("Регистрация не удалась");
                return;
            }
            
            AuthenticatedUser = user;
            Close();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Ошибка регистрации: " + ex.Message);
        }
    }
    private void onLoginClick(object sender, RoutedEventArgs e)
    {
        AuthenticatedUser = null;
        Close();
    }
    private Task ShowMessageAsync(string message)
    {
        MessageText.Text = message;
        MessageOverlay.IsVisible = true;

        return Task.CompletedTask;
    }

    private void OnMessageCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MessageOverlay.IsVisible = false;
    }
}