using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LocalJsonModule.DTOs.Folders;
using LocalJsonModule.DTOs.Notes;
using LocalJsonModule.Models;
using LocalJsonModule.Services;

namespace NoteApp;

public partial class MainWindow : Window
{
    private readonly IFolderService _folders;
    private readonly INoteService _notes;
    private readonly User _user;

    public MainWindow(IFolderService folders, INoteService notes, User user)
    {
        InitializeComponent();
        _folders = folders;
        _notes = notes;
        _user = user;

        this.FindControl<Button>("AddNoteButton")!.Click += onAddNoteClick;
        this.FindControl<Button>("AddFolderButton")!.Click += onAddFolderClick;
        this.FindControl<Button>("DeleteButton")!.Click += onDeleteClick;
        this.FindControl<Button>("EditButton")!.Click += onEditClick;
        
        Loaded += async (_, _) => await LoadTreeAsync();
    }

    private async Task LoadTreeAsync()
    {
        var tree = this.FindControl<TreeView>("NotesTreeView");
        tree.Items.Clear();
        
        var roots = await _folders.GetByParentFolderIdAsync(_user.Id, null);
        foreach (var root in roots)
        {
            tree.Items.Add(await BuildFolderNote(root));
        }

        var noteWithoutFolder = (await _notes.GetByOwnerIdAsync(_user.Id)).Where(n => n.FolderId == null);
        foreach (var note in noteWithoutFolder)
        {
            tree.Items.Add(new TreeViewItem
            {
                Header = note.Title,
                Tag = note,
            });
        }
    }

    private async Task<TreeViewItem> BuildFolderNote(Folder folder)
    {
        var item = new TreeViewItem
        {
            Header = folder.Title,
            IsExpanded = true,
            Tag = folder
        };
        
        var childFolder = await _folders.GetByParentFolderIdAsync(_user.Id, folder.Id);
        foreach (var child in childFolder)
        {
            item.Items.Add(await BuildFolderNote(child));
        }
        
        var notes = await _notes.GetByFolderIdAsync(folder.Id);
        foreach (var note in notes)
        {
            item.Items.Add(new TreeViewItem
            {
                Header = note.Title,
                Tag = note
            });
        }
        return item;
    }

    private async void onAddNoteClick(object sender, RoutedEventArgs e)
    {
        var name = "Новая заметка " + DateTime.Now.ToString("HH:mm:ss");

        await _notes.CreateAsync(new CreateNoteRequest
        {
            Title = name,
            Content = "",
            OwnerId = _user.Id,
            FolderId = null
        });
        
        await LoadTreeAsync();
    }
    private async void onAddFolderClick(object sender, RoutedEventArgs e)
    {
        var name = "Новая папка " + DateTime.Now.ToString("HH:mm:ss");

        await _folders.CreateAsync(new CreateFolderRequest
        {
            Title = name,
            OwnerId = _user.Id,
            ParentFolderId = null
        });
        
        await LoadTreeAsync();
    }

    private async void onDeleteClick(object sender, RoutedEventArgs e)
    {
        var tree = this.FindControl<TreeView>("NotesTreeView");
        if (tree.SelectedItem is not TreeViewItem treeItem) return;

        try
        {
            if (treeItem.Tag is Folder folder)
            {
                await _folders.DeleteAsync(folder.Id);
            }
            else if (treeItem.Tag is Note note)
            {
                await _notes.DeleteAsync(note.Id);
            }

            await LoadTreeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Ошибка удаления: " + ex.Message);
        }
    }

    private async void onEditClick(object sender, RoutedEventArgs e)
    {
        var tree = this.FindControl<TreeView>("NotesTreeView");
        if(tree.SelectedItem is not TreeViewItem treeItem) return;

        try
        {
            if (treeItem.Tag is Folder folder)
            {
                var dialog = new InputDialog(folder.Title);
                var newTitle = await dialog.ShowDialog<string>(this);

                if (string.IsNullOrWhiteSpace(newTitle) ||
                    newTitle == folder.Title)
                    return;
                
                await _folders.UpdateAsync(folder.Id, new UpdateFolderRequest
                {
                    Title = newTitle,
                    ParentFolderId = folder.ParentFolderId
                });
            }
            else if (treeItem.Tag is Note note)
            {
                var dialog = new InputDialog(note.Title);
                var newTitle = await dialog.ShowDialog<string>(this);
                
                if (string.IsNullOrWhiteSpace(newTitle) ||
                    newTitle == note.Title)
                    return;
                
                await _notes.UpdateAsync(note.Id, new UpdateNoteRequest
                {
                    Title = newTitle,
                    Content = note.Content,
                    FolderId = note.FolderId,
                    IsFavorite = note.IsFavorite,
                    IsArchive = note.IsArchive,
                    NoteType = note.NoteType
                });
            }

            await LoadTreeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Ошибка редактирования: " + ex.Message);
        }
    }
}