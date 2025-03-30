using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using AtOSaveEditor.Helpers;
using AtOSaveEditor.Models;
using AtOSaveEditor.Controls;

namespace AtOSaveEditor
{
    public class MainForm : Form
    {
        // UI controls
        private TabControl tabControl;

        private SaveData currentSaveData;
        private TeamAtO currentTeam;
        private string currentAtoPath;

        // DES Key/IV 
        private readonly byte[] key = new byte[] { 18, 54, 100, 160, 190, 148, 136, 3 };
        private readonly byte[] iv = new byte[] { 82, 242, 164, 132, 119, 197, 179, 20 };

        private string assetsPath;

        private List<CardInfo> availableCards = new List<CardInfo>();

        private string cacheFilePath;

        public MainForm()
        {
            this.Text = "ATO Save Editor";
            this.MinimumSize = new Size(1000, 600);
            this.Size = new Size(1000, 600);

            MenuStrip menuStrip = new MenuStrip();
            this.MainMenuStrip = menuStrip;
            menuStrip.Dock = DockStyle.Top;
            this.Controls.Add(menuStrip);

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            menuStrip.Items.Add(fileMenu);

            ToolStripMenuItem openAtoItem = new ToolStripMenuItem("Open .ato");
            openAtoItem.Click += BtnOpen_Click;
            fileMenu.DropDownItems.Add(openAtoItem);

            ToolStripMenuItem saveAtoItem = new ToolStripMenuItem("Save .ato");
            saveAtoItem.Click += BtnSave_Click;
            fileMenu.DropDownItems.Add(saveAtoItem);

            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Tools");
            menuStrip.Items.Add(toolsMenu);
            ToolStripMenuItem recacheToolItem = new ToolStripMenuItem("Recache Card Images");
            recacheToolItem.Click += RecacheToolItem_Click;
            toolsMenu.DropDownItems.Add(recacheToolItem);

            ToolStripMenuItem decryptJsonToolItem = new ToolStripMenuItem("Decrypt to JSON");
            decryptJsonToolItem.Click += DecryptToJsonToolItem_Click;
            toolsMenu.DropDownItems.Add(decryptJsonToolItem);

            ToolStripMenuItem encryptJsonToolItem = new ToolStripMenuItem("Encrypt JSON to ATO");
            encryptJsonToolItem.Click += EncryptJsonToAtoToolItem_Click;
            toolsMenu.DropDownItems.Add(encryptJsonToolItem);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Multiline = false;
            tabControl.SizeMode = TabSizeMode.Normal;
            tabControl.Padding = new Point(3, 30); // Top padding of 30px

            this.Controls.Add(tabControl);

            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? Application.ExecutablePath;
            string exeDir = Path.GetDirectoryName(exePath) ?? ".";
            assetsPath = Path.Combine(exeDir, "assets", "cardimg");

            cacheFilePath = Path.Combine(exeDir, "cardCache.json");
            LoadAvailableCards();
        }

        private void LoadAvailableCards()
        {
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    string json = File.ReadAllText(cacheFilePath);
                    var deserializedCards = JsonSerializer.Deserialize<List<CardInfo>>(json);
                    if (deserializedCards != null)
                    {
                        availableCards = deserializedCards;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading card cache: {ex.Message}. Recreating cache.",
                        "Cache Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // If we got here, either the cache doesn't exist or loading it failed
            RecacheCardImages();
        }

        private void RecacheCardImages()
        {
            availableCards = new List<CardInfo>();
            string[] categories = { "Boon", "Healer", "Injury", "Item", "Mage", "MagicKnight", "Monster", "Scout", "Special", "Warrior" };
            foreach (var cat in categories)
            {
                string subPath = Path.Combine(assetsPath, cat);
                if (Directory.Exists(subPath))
                {
                    foreach (var file in Directory.GetFiles(subPath))
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif")
                        {
                            string name = Path.GetFileNameWithoutExtension(file);
                            // Store relative path (e.g.: "assets\\cardimg\\Warrior\\card.png")
                            string relativePath = Path.Combine("assets", "cardimg", cat, Path.GetFileName(file));
                            availableCards.Add(new CardInfo { Name = name, Category = cat, FilePath = relativePath });
                        }
                    }
                }
            }
            availableCards = availableCards.OrderBy(c => c.Name).ToList();

            try
            {
                File.WriteAllText(cacheFilePath, JsonSerializer.Serialize(availableCards,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving card cache: {ex.Message}",
                    "Cache Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RecacheToolItem_Click(object sender, EventArgs e)
        {
            RecacheCardImages();
            MessageBox.Show("Card image cache has been updated.", "Cache Updated",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnOpen_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "ATO Files (*.ato)|*.ato";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (!File.Exists(dlg.FileName))
                {
                    MessageBox.Show("The selected file does not exist.", "File Not Found",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                currentAtoPath = dlg.FileName;

                using var progressForm = new ProgressForm("Opening save file...", "Please wait while the save file is being decrypted...");
                progressForm.Show(this);

                try
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromMinutes(2)); // Set a reasonable timeout

                    var progressHandler = new Progress<int>(value => progressForm.UpdateProgress(value));

                    currentSaveData = await CryptoHelper.DecryptAndDeserializeAsync(
                        currentAtoPath, key, iv, progressHandler, cts.Token);

                    if (string.IsNullOrEmpty(currentSaveData?.TeamAtO))
                    {
                        MessageBox.Show("The save file does not contain valid team data.", "Invalid Save Data",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = false,
                            WriteIndented = true
                        };

                        currentTeam = JsonSerializer.Deserialize<TeamAtO>(currentSaveData.TeamAtO, jsonOptions);
                        if (currentTeam == null)
                        {
                            throw new JsonException("TeamAtO deserialized to null");
                        }

                        PopulateTabs();
                    }
                    catch (JsonException jsonEx)
                    {
                        MessageBox.Show($"Error parsing team data: {jsonEx.Message}", "JSON Parsing Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("The operation was canceled.", "Operation Canceled",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "File Open Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PopulateTabs()
        {
            tabControl.TabPages.Clear();
            if (currentTeam == null || currentTeam.Items == null)
            {
                MessageBox.Show("No team data found in save.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            foreach (var hero in currentTeam.Items)
            {
                TabPage page = new TabPage(hero.gameName ?? "Hero");
                HeroEditorControl editor = new HeroEditorControl(hero, availableCards, assetsPath);
                editor.Dock = DockStyle.Fill;
                page.Controls.Add(editor);
                tabControl.TabPages.Add(page);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (currentSaveData == null || currentTeam == null || string.IsNullOrEmpty(currentAtoPath))
            {
                MessageBox.Show("No save data loaded to save.", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Generate the JSON for the team
            currentSaveData.TeamAtO = JsonSerializer.Serialize(currentTeam,
                new JsonSerializerOptions { WriteIndented = true });

            // Create a backup of the original file
            string backupPath = currentAtoPath + ".backup";
            try
            {
                if (File.Exists(currentAtoPath))
                {
                    File.Copy(currentAtoPath, backupPath, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create backup file: {ex.Message}",
                    "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Continue without backup
            }

            using SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "ATO Files (*.ato)|*.ato";
            dlg.FileName = Path.GetFileNameWithoutExtension(currentAtoPath) + ".new.ato";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                using var progressForm = new ProgressForm("Saving changes...", "Please wait while the save file is being encrypted...");
                progressForm.Show(this);

                try
                {
                    using var cts = new CancellationTokenSource();
                    var progressHandler = new Progress<int>(value => progressForm.UpdateProgress(value));

                    await CryptoHelper.SerializeAndEncryptAsync(
                        currentSaveData, dlg.FileName, key, iv, progressHandler, cts.Token);

                    MessageBox.Show($"File saved: {dlg.FileName}", "Save Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("The operation was canceled.", "Operation Canceled",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void DecryptToJsonToolItem_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog { Filter = "ATO Files (*.ato)|*.ato" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                using var progressForm = new ProgressForm("Decrypting file...", "Please wait while the save file is being decrypted...");
                progressForm.Show(this);

                try
                {
                    using var cts = new CancellationTokenSource();
                    var progressHandler = new Progress<int>(value => progressForm.UpdateProgress(value));

                    SaveData data = await CryptoHelper.DecryptAndDeserializeAsync(dlg.FileName, key, iv, progressHandler, cts.Token);
                    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

                    string exeDir = Path.GetDirectoryName(cacheFilePath) ?? ".";
                    string newFilePath = Path.Combine(exeDir, Path.GetFileNameWithoutExtension(dlg.FileName) + ".json");
                    await File.WriteAllTextAsync(newFilePath, json, cts.Token);

                    MessageBox.Show($"Decrypted JSON saved to: {newFilePath}", "Decryption Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error decrypting file: {ex.Message}", "Decryption Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void EncryptJsonToAtoToolItem_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                using var progressForm = new ProgressForm("Encrypting file...", "Please wait while the file is being encrypted...");
                progressForm.Show(this);

                try
                {
                    using var cts = new CancellationTokenSource();
                    var progressHandler = new Progress<int>(value => progressForm.UpdateProgress(value));

                    string jsonText = await File.ReadAllTextAsync(dlg.FileName, cts.Token);
                    SaveData data = JsonSerializer.Deserialize<SaveData>(jsonText)
                        ?? throw new InvalidOperationException("Failed to parse JSON data");

                    string exeDir = Path.GetDirectoryName(cacheFilePath) ?? ".";
                    string newFilePath = Path.Combine(exeDir, Path.GetFileNameWithoutExtension(dlg.FileName) + ".ato");

                    await CryptoHelper.SerializeAndEncryptAsync(data, newFilePath, key, iv, progressHandler, cts.Token);

                    MessageBox.Show($"Encrypted ATO saved to: {newFilePath}", "Encryption Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error encrypting file: {ex.Message}", "Encryption Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    // Add this progress form class
    public class ProgressForm : Form
    {
        private readonly ProgressBar progressBar;

        public ProgressForm(string title, string message)
        {
            this.Text = title;
            this.Size = new Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };

            Label lblMessage = new Label
            {
                Text = message,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.None
            };

            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Blocks,
                Height = 23,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Anchor = AnchorStyles.None,
                Width = 250
            };

            layout.Controls.Add(lblMessage);
            layout.Controls.Add(progressBar);

            this.Controls.Add(layout);
        }

        public void UpdateProgress(int value)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<int>(UpdateProgressInternal), value);
            }
            else
            {
                UpdateProgressInternal(value);
            }
        }

        private void UpdateProgressInternal(int value)
        {
            int safeValue = Math.Min(100, Math.Max(0, value));
            if (progressBar.Value != safeValue)
            {
                progressBar.Value = safeValue;
                progressBar.Refresh();
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_NOCLOSE = 0x200;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_NOCLOSE;
                return cp;
            }
        }
    }
}