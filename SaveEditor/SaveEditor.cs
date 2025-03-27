using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;
using AtOSaveEditor.Helpers;



namespace AtOSaveEditor

{
    // Minimal classes for the save data.
    [Serializable]
    public class SaveData
    {
        public string GameDate { get; set; }
        public string CurrentMapNode { get; set; }
        // The TeamAtO field is stored as a JSON string.
        public string TeamAtO { get; set; }
        public int GameMode { get; set; }
        // ... add other fields as needed.
    }

    public class TeamAtO
    {
        public List<Hero> Items { get; set; }
    }

    public class Hero
    {
        public string gameName { get; set; }
        public string owner { get; set; }
        public string className { get; set; }
        public int level { get; set; }
        public int experience { get; set; }
        public int hp { get; set; }
        public int energy { get; set; }
        public int speed { get; set; }
        public string weapon { get; set; }
        public string armor { get; set; }
        public string jewelry { get; set; }
        public string accesory { get; set; }
        public List<string> traits { get; set; }
        public List<string> cards { get; set; }
    }

    public class CardInfo
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string FilePath { get; set; }
        public override string ToString() => Name;
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // UI controls
        private Button btnOpen, btnSave;
        private TabControl tabControl;

        // Current loaded save data
        private SaveData currentSaveData;
        private TeamAtO currentTeam;
        private string currentAtoPath;

        // DES Key/IV (as provided)
        private readonly byte[] key = new byte[] { 18, 54, 100, 160, 190, 148, 136, 3 };
        private readonly byte[] iv = new byte[] { 82, 242, 164, 132, 119, 197, 179, 20 };

        // Folder where card images are stored (relative to exe)
        private string assetsPath;

        // Pool of available card names (derived from image filenames)
        private List<CardInfo> availableCards;

        // Cache file path
        private string cacheFilePath;

        public MainForm()
        {
            this.Text = "ATO Save Editor";
            this.MinimumSize = new Size(1000, 600);  // enforce minimum size
            this.Size = new Size(1000, 600);         // initial size

            // Create a MenuStrip and add the File menu with Open and Save items.
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

            // Add Tools menu with Recache option
            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Tools");
            menuStrip.Items.Add(toolsMenu);
            ToolStripMenuItem recacheToolItem = new ToolStripMenuItem("Recache Card Images");
            recacheToolItem.Click += RecacheToolItem_Click;
            toolsMenu.DropDownItems.Add(recacheToolItem);
            // ADD: Decrypt to JSON tool
            ToolStripMenuItem decryptJsonToolItem = new ToolStripMenuItem("Decrypt to JSON");
            decryptJsonToolItem.Click += DecryptToJsonToolItem_Click;
            toolsMenu.DropDownItems.Add(decryptJsonToolItem);
            // ADD: Encrypt JSON to ATO tool
            ToolStripMenuItem encryptJsonToolItem = new ToolStripMenuItem("Encrypt JSON to ATO");
            encryptJsonToolItem.Click += EncryptJsonToAtoToolItem_Click;
            toolsMenu.DropDownItems.Add(encryptJsonToolItem);

            // Create the TabControl for hero tabs with owner-draw settings.
            // In your MainForm constructor, after initializing the TabControl:
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Multiline = true;
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.ItemSize = new Size(150, 60);  // Increased height for more room
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += tabControl_DrawItem;

            this.Controls.Add(tabControl);

            // Set up assets path using the actual EXE directory.
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string exeDir = Path.GetDirectoryName(exePath);
            assetsPath = Path.Combine(exeDir, "assets", "cardimg");

            // Set cache file path in exe directory.
            cacheFilePath = Path.Combine(exeDir, "cardCache.json");
            LoadAvailableCards();
        }

        // Custom DrawItem event handler for the TabControl
        private void tabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tc = sender as TabControl;
            if (tc == null)
                return;

            // Retrieve the text for the current tab.
            string tabText = tc.TabPages[e.Index].Text;

            // Draw the tab background.
            e.DrawBackground();

            // Create a rectangle for drawing the text with minimal padding.
            Rectangle rect = e.Bounds;
            rect.Inflate(-1, -1);  // Reduce the padding so more text fits

            // Set up a StringFormat for centered text with ellipsis if necessary.
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                using (Brush brush = new SolidBrush(Color.Black))
                {
                    e.Graphics.DrawString(tabText, tc.Font, brush, rect, sf);
                }
            }

            e.DrawFocusRectangle();
        }


        private void LoadAvailableCards()
        {
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    string json = File.ReadAllText(cacheFilePath);
                    availableCards = JsonSerializer.Deserialize<List<CardInfo>>(json);
                }
                catch
                {
                    // if cache fails, recache
                    RecacheCardImages();
                }
            }
            else
            {
                RecacheCardImages();
            }
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
                            // Store relative path (e.g.: "assets\cardimg\Warrior\card.png")
                            string relativePath = Path.Combine("assets", "cardimg", cat, Path.GetFileName(file));
                            availableCards.Add(new CardInfo { Name = name, Category = cat, FilePath = relativePath });
                        }
                    }
                }
            }
            // Sort availableCards by Name if needed.
            availableCards = availableCards.OrderBy(c => c.Name).ToList();
            // Save to cache file.
            File.WriteAllText(cacheFilePath, JsonSerializer.Serialize(availableCards, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void RecacheToolItem_Click(object sender, EventArgs e)
        {
            RecacheCardImages();
            MessageBox.Show("Card image cache has been updated.");
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "ATO Files (*.ato)|*.ato";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                currentAtoPath = dlg.FileName;
                try
                {
                    // Decrypt and deserialize .ato file
                    currentSaveData = DecryptAndDeserialize(currentAtoPath);
                    // currentSaveData.TeamAtO is a JSON string; parse it to TeamAtO
                    currentTeam = JsonSerializer.Deserialize<TeamAtO>(currentSaveData.TeamAtO);
                    PopulateTabs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening file: " + ex.Message);
                }
            }
        }

        private SaveData DecryptAndDeserialize(string filePath)
        {
            return CryptoHelper.DecryptAndDeserialize(filePath, key, iv);
        }

        private void PopulateTabs()
        {
            tabControl.TabPages.Clear();
            if (currentTeam == null || currentTeam.Items == null)
            {
                MessageBox.Show("No team data found in save.");
                return;
            }
            foreach (var hero in currentTeam.Items)
            {
                TabPage page = new TabPage(hero.gameName ?? "Hero");
                // Create a HeroEditorControl for this hero
                HeroEditorControl editor = new HeroEditorControl(hero, availableCards, assetsPath);
                editor.Dock = DockStyle.Fill;
                page.Controls.Add(editor);
                tabControl.TabPages.Add(page);
            }
            // ADD: Center the tabs if there are exactly 4 heroes.
            if (tabControl.TabCount == 4)
            {
                int totalTabsWidth = tabControl.ItemSize.Width * 4;
                int offset = (tabControl.Width - totalTabsWidth) / 2;
                tabControl.Padding = new Point(offset, 3);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // (Optional) Update the currentTeam data from each HeroEditorControl if needed.
            // In this example, HeroEditorControl edits the Hero object in place.

            // Convert currentTeam back to JSON string and update currentSaveData
            currentSaveData.TeamAtO = JsonSerializer.Serialize(currentTeam, new JsonSerializerOptions { WriteIndented = true });
            // Ask for a file location to save the new .ato
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "ATO Files (*.ato)|*.ato";
            dlg.FileName = Path.GetFileNameWithoutExtension(currentAtoPath) + ".new.ato";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SerializeAndEncrypt(currentSaveData, dlg.FileName);
                    MessageBox.Show("File saved: " + dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving file: " + ex.Message);
                }
            }
        }

        private void SerializeAndEncrypt(SaveData data, string filePath)
        {
            CryptoHelper.SerializeAndEncrypt(data, filePath, key, iv);
        }

        // ADD event handler for "Decrypt to JSON"
        private void DecryptToJsonToolItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "ATO Files (*.ato)|*.ato" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                SaveData data = DecryptAndDeserialize(dlg.FileName);
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                string exeDir = Path.GetDirectoryName(cacheFilePath);
                string newFilePath = Path.Combine(exeDir, Path.GetFileNameWithoutExtension(dlg.FileName) + ".json");
                File.WriteAllText(newFilePath, json);
                MessageBox.Show("Decrypted JSON saved to: " + newFilePath);
            }
        }

        // ADD event handler for "Encrypt JSON to ATO"
        private void EncryptJsonToAtoToolItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string jsonText = File.ReadAllText(dlg.FileName);
                SaveData data = JsonSerializer.Deserialize<SaveData>(jsonText);
                string exeDir = Path.GetDirectoryName(cacheFilePath);
                string newFilePath = Path.Combine(exeDir, Path.GetFileNameWithoutExtension(dlg.FileName) + ".ato");
                SerializeAndEncrypt(data, newFilePath);
                MessageBox.Show("Encrypted ATO saved to: " + newFilePath);
            }
        }
    }

    // A UserControl that displays hero details and deck editing UI
    public class HeroEditorControl : UserControl
    {
        public Hero HeroData { get; private set; }
        private List<CardInfo> availableCards;
        private string assetsPath;

        // UI controls
        private Label lblDetails;
        private ListBox lstDeck;
        private TextBox txtDeckSearch;
        private ListBox lstPool;
        private TextBox txtPoolSearch;
        private PictureBox picCard;
        private Button btnAdd, btnRemove;

        // Field for category filter checkboxes
        private FlowLayoutPanel filterPanel;
        private List<CheckBox> categoryCheckBoxes = new List<CheckBox>();

        public HeroEditorControl(Hero hero, List<CardInfo> availableCards, string assetsPath)
        {
            this.HeroData = hero;
            this.availableCards = availableCards;
            this.assetsPath = assetsPath;
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;

            // 1) Create the main TableLayoutPanel with 2 rows and 3 columns
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 3
            };
            // Row 0: Auto-size for hero details
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Row 1: Fills the remaining space for the deck/card/pool
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Column 0 = 30% width, Column 1 = 40%, Column 2 = 30%
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            this.Controls.Add(mainLayout);

            // 2) Create a Panel for hero details in Row 0 (spanning all 3 columns)
            Panel detailsPanel = new Panel { Dock = DockStyle.Fill, AutoSize = true };
            lblDetails = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                // If your text is long, consider setting MaximumSize or using a multi-line approach.
            };
            detailsPanel.Controls.Add(lblDetails);

            // Place detailsPanel in row=0, col=0, spanning all 3 columns
            mainLayout.SetColumnSpan(detailsPanel, 3);
            mainLayout.Controls.Add(detailsPanel, 0, 0);

            // 3) Deck panel (left column, row=1)
            Panel deckPanel = new Panel { Dock = DockStyle.Fill };
            Label lblDeck = new Label { Text = "Deck", Dock = DockStyle.Top };
            txtDeckSearch = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search Deck..." };
            lstDeck = new ListBox { Dock = DockStyle.Fill };
            btnRemove = new Button { Text = "Remove Card", Dock = DockStyle.Bottom };

            // Add controls to deckPanel
            deckPanel.Controls.Add(lstDeck);
            deckPanel.Controls.Add(txtDeckSearch);
            deckPanel.Controls.Add(lblDeck);
            deckPanel.Controls.Add(btnRemove);

            // 4) Card image panel (center column, row=1)
            Panel cardPanel = new Panel { Dock = DockStyle.Fill };
            picCard = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            cardPanel.Controls.Add(picCard);

            // 5) Pool panel (right column, row=1)
            Panel poolPanel = new Panel { Dock = DockStyle.Fill };
            Label lblPool = new Label { Text = "Available Cards", Dock = DockStyle.Top };

            // Create filterPanel with checkboxes for subfolders
            filterPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            string[] categories = { "Boon", "Healer", "Injury", "Item", "Mage", "MagicKnight", "Monster", "Scout", "Special", "Warrior" };
            foreach (var cat in categories)
            {
                CheckBox cb = new CheckBox { Text = cat, AutoSize = true };
                cb.CheckedChanged += (s, e) => { UpdatePoolList(); };
                categoryCheckBoxes.Add(cb);
                filterPanel.Controls.Add(cb);
            }

            txtPoolSearch = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search Pool..." };
            lstPool = new ListBox { Dock = DockStyle.Fill };
            btnAdd = new Button { Text = "Add Card", Dock = DockStyle.Bottom };

            // Add controls to poolPanel in order: label, filterPanel, search, list, button.
            poolPanel.Controls.Add(lstPool);
            poolPanel.Controls.Add(txtPoolSearch);
            poolPanel.Controls.Add(filterPanel);
            poolPanel.Controls.Add(lblPool);
            poolPanel.Controls.Add(btnAdd);

            // 6) Add these three panels to row=1
            mainLayout.Controls.Add(deckPanel, 0, 1); // Left
            mainLayout.Controls.Add(cardPanel, 1, 1); // Center
            mainLayout.Controls.Add(poolPanel, 2, 1); // Right

            // 7) Hook up events
            txtDeckSearch.TextChanged += TxtDeckSearch_TextChanged;
            txtPoolSearch.TextChanged += TxtPoolSearch_TextChanged;
            lstDeck.SelectedIndexChanged += LstDeck_SelectedIndexChanged;
            lstPool.SelectedIndexChanged += LstPool_SelectedIndexChanged;
            btnRemove.Click += BtnRemove_Click;
            btnAdd.Click += BtnAdd_Click;
        }


        private void LoadData()
        {
            lblDetails.Text =
                $"Hero Name: {HeroData.gameName}\n" +
                $"Owner: {HeroData.owner}\n" +
                $"Class: {HeroData.className}\n" +
                $"Level: {HeroData.level}\n" +
                $"XP: {HeroData.experience}\n" +
                $"HP: {HeroData.hp}\n" +
                $"Energy: {HeroData.energy}\n" +
                $"Speed: {HeroData.speed}\n" +
                $"Items: Weapon: {HeroData.weapon}, Armor: {HeroData.armor}, Jewelry: {HeroData.jewelry}, Accessory: {HeroData.accesory}\n" +
                $"Traits: {string.Join(", ", HeroData.traits)}";
            // Ensure the deck list is not null
            if (HeroData.cards == null)
                HeroData.cards = new List<string>();
            UpdateDeckList();
            UpdatePoolList();
        }

        private void UpdateDeckList()
        {
            string search = txtDeckSearch.Text.ToLower();
            lstDeck.Items.Clear();
            foreach (var card in HeroData.cards)
            {
                if (card.ToLower().Contains(search))
                    lstDeck.Items.Add(card);
            }
        }

        private void UpdatePoolList()
        {
            string search = txtPoolSearch.Text.ToLower();
            var selectedCategories = categoryCheckBoxes.Where(cb => cb.Checked).Select(cb => cb.Text).ToList();
            lstPool.Items.Clear();
            foreach (var card in availableCards)
            {
                // if any category is checked, filter by it; otherwise show all
                if (selectedCategories.Any() && !selectedCategories.Contains(card.Category))
                    continue;
                if (card.Name.ToLower().Contains(search))
                    lstPool.Items.Add(card);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (lstPool.SelectedItem != null)
            {
                CardInfo selectedCard = lstPool.SelectedItem as CardInfo;
                if (selectedCard != null)
                {
                    HeroData.cards.Add(selectedCard.Name);
                    UpdateDeckList();
                }
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (lstDeck.SelectedItem != null)
            {
                string card = lstDeck.SelectedItem.ToString();
                HeroData.cards.Remove(card);
                UpdateDeckList();
            }
        }

        private void LstDeck_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstDeck.SelectedItem != null)
                DisplayCardImage(lstDeck.SelectedItem.ToString());
        }

        private void LstPool_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstPool.SelectedItem != null)
                DisplayCardImage((lstPool.SelectedItem as CardInfo)?.Name);
        }

        private void TxtDeckSearch_TextChanged(object sender, EventArgs e)
        {
            UpdateDeckList();
        }

        private void TxtPoolSearch_TextChanged(object sender, EventArgs e)
        {
            UpdatePoolList();
        }

        private void DisplayCardImage(string cardName)
        {
            // Try to find the card with the exact name (case-sensitive)
            var card = availableCards.FirstOrDefault(c => c.Name == cardName);
            if (card != null)
            {
                // Get the exe directory via Application.ExecutablePath
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                string fullPath = Path.Combine(exeDir, card.FilePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        picCard.Image = System.Drawing.Image.FromFile(fullPath);
                    }
                    catch
                    {
                        picCard.Image = null;
                    }
                }
                else
                {
                    picCard.Image = null;
                }
            }
            else
            {
                picCard.Image = null;
            }
        }
    }
}