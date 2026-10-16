using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LocalJsonModule.Data;
using LocalJsonModule.Repositories;
using LocalJsonModule.Repositories.postgresSQL;
using LocalJsonModule.Services;
using NoteApp.Windows;

namespace NoteApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var connectionString = "Host=94.41.85.115;Port=54321;Database=webnotes;Username=postgres;Password=1111";
        var connectionFactory =
            new DbConnectionFactory(connectionString);

        IUserRepository userRepo = new UserPostgresRepository(connectionFactory);
        var pathProvider = new DataPathProvider();
        var folderRepo = new FolderJsonRepository(pathProvider);
        var noteRepo = new NoteJsonRepository(pathProvider);

        var password = new PasswordService();
        var auth = new AuthService(userRepo, password);
        var register = new RegisterService(userRepo, password);
        var folders = new FolderService(folderRepo, userRepo, noteRepo);
        var notes = new NoteService(noteRepo, userRepo, folderRepo);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var login = new LoginWindow(auth, () => new RegisterWindow(register, auth));
            desktop.MainWindow = login;
    
            login.Closed += (_, _) =>
            {
                if (login.AuthenticatedUser is null) return;
        
                var main = new MainWindow(folders, notes, login.AuthenticatedUser);
                desktop.MainWindow = main;
                main.Show();
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}