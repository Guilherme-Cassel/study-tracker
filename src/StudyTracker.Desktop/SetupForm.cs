using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.Desktop;

// First run (or broken settings): pick the study folder and the deliverable prefix, saved to appsettings.Local.json.
internal sealed class SetupForm : Form
{
    private readonly TextBox _folder = new() { Dock = DockStyle.Fill };
    private readonly TextBox _prefix = new() { Dock = DockStyle.Fill, PlaceholderText = "ex.: guilherme_cassel" };
    private readonly Label _error = new() { AutoSize = true, ForeColor = Color.Firebrick, MaximumSize = new Size(500, 0) };

    public SetupForm(Icon icon, string? currentFolder, string? currentPrefix)
    {
        Text = "Study Tracker · Configuração";
        Icon = icon;
        Font = new Font("Segoe UI", 9.75f);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        _folder.Text = currentFolder ?? "";
        _prefix.Text = currentPrefix ?? "";

        var browse = new Button { Text = "Escolher…", AutoSize = true };
        browse.Click += (_, _) => Browse();

        var save = new Button { Text = "Salvar", AutoSize = true, DialogResult = DialogResult.None };
        save.Click += (_, _) => Save();
        var cancel = new Button { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };
        AcceptButton = save;
        CancelButton = cancel;

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill, Width = 520 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        layout.Controls.Add(Note($"Escolha a pasta da faculdade: a que tem o {StatusDataStore.FileName}.", bold: true), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);
        layout.Controls.Add(_folder, 0, 1);
        layout.Controls.Add(browse, 1, 1);
        layout.Controls.Add(Note("Prefixo dos arquivos de entrega (opcional)", bold: false, top: 12), 0, 2);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 2)!, 2);
        layout.Controls.Add(_prefix, 0, 3);
        layout.SetColumnSpan(_prefix, 2);
        layout.Controls.Add(Note("Usado para conferir os nomes, como guilherme_cassel_DR3_TP1.pdf.", bold: false, muted: true), 0, 4);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 4)!, 2);
        layout.Controls.Add(_error, 0, 5);
        layout.SetColumnSpan(_error, 2);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        layout.Controls.Add(buttons, 0, 6);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
    }

    public string FolderPath => _folder.Text.Trim();

    public string Prefix => _prefix.Text.Trim();

    private static Label Note(string text, bool bold, int top = 0, bool muted = false) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(500, 0),
        Margin = new Padding(0, top, 0, 6),
        Font = bold ? new Font("Segoe UI", 9.75f, FontStyle.Bold) : null,
        ForeColor = muted ? SystemColors.GrayText : SystemColors.ControlText
    };

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = $"Pasta da faculdade (com o {StatusDataStore.FileName})",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (Directory.Exists(FolderPath)) dialog.InitialDirectory = FolderPath;
        if (dialog.ShowDialog(this) == DialogResult.OK) _folder.Text = dialog.SelectedPath;
    }

    private void Save()
    {
        var folderProblem = LocalSettings.CheckStudyFolder(FolderPath);
        _error.Text = folderProblem switch
        {
            StudyFolderProblem.Missing => "Escolha a pasta da faculdade.",
            StudyFolderProblem.NotAbsolute => "Use o caminho completo da pasta, como G:\\Meu Drive\\Faculdade.",
            StudyFolderProblem.NotFound => "Essa pasta não existe. Se ela está no Google Drive, confira se ele está aberto.",
            StudyFolderProblem.NoDataFile => $"Essa pasta não tem o {StatusDataStore.FileName}.",
            _ => LocalSettings.CheckPrefix(Prefix) is not null
                ? "O prefixo não pode ter espaços nem os caracteres \\ / : * ? \" < > |."
                : ""
        };
        if (_error.Text.Length > 0) return;

        DialogResult = DialogResult.OK;
        Close();
    }
}
