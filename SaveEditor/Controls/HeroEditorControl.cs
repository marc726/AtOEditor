using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using AtOSaveEditor.Models;

namespace AtOSaveEditor.Controls
{
    public class HeroEditorControl : UserControl
    {
        public AtOSaveEditor.Models.Hero HeroData { get; private set; }
        private readonly List<CardInfo> availableCards;
        private readonly string assetsPath;

        // UI controls - making them nullable to fix non-nullable field errors
        private Label? lblDetails;
        private ListBox? lstDeck;
        private TextBox? txtDeckSearch;
        private ListBox? lstPool;
        private TextBox? txtPoolSearch;
        private PictureBox? picCard;
        private Button? btnAdd, btnRemove;
        private FlowLayoutPanel? filterPanel;
        private readonly List<CheckBox> categoryCheckBoxes = new List<CheckBox>();

        public HeroEditorControl(AtOSaveEditor.Models.Hero hero, List<CardInfo> availableCards, string assetsPath)
        {
            this.HeroData = hero ?? throw new ArgumentNullException(nameof(hero));
            this.availableCards = availableCards ?? new List<CardInfo>();
            this.assetsPath = assetsPath ?? string.Empty;
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 3
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            this.Controls.Add(mainLayout);

            Panel detailsPanel = new Panel { Dock = DockStyle.Fill, AutoSize = true };
            lblDetails = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
            };
            detailsPanel.Controls.Add(lblDetails);

            mainLayout.SetColumnSpan(detailsPanel, 3);
            mainLayout.Controls.Add(detailsPanel, 0, 0);

            Panel deckPanel = new Panel { Dock = DockStyle.Fill };
            Label lblDeck = new Label { Text = "Deck", Dock = DockStyle.Top };
            txtDeckSearch = new TextBox { Dock = DockStyle.Top };
            txtDeckSearch.SetPlaceholderText("Search Deck...");

            lstDeck = new ListBox { Dock = DockStyle.Fill };
            btnRemove = new Button { Text = "Remove Card", Dock = DockStyle.Bottom };

            deckPanel.Controls.Add(lstDeck);
            deckPanel.Controls.Add(txtDeckSearch);
            deckPanel.Controls.Add(lblDeck);
            deckPanel.Controls.Add(btnRemove);

            Panel cardPanel = new Panel { Dock = DockStyle.Fill };
            picCard = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            cardPanel.Controls.Add(picCard);

            Panel poolPanel = new Panel { Dock = DockStyle.Fill };
            Label lblPool = new Label { Text = "Available Cards", Dock = DockStyle.Top };

            filterPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            string[] categories = { "Boon", "Healer", "Injury", "Item", "Mage", "MagicKnight", "Monster", "Scout", "Special", "Warrior" };
            foreach (var cat in categories)
            {
                CheckBox cb = new CheckBox { Text = cat, AutoSize = true };
                cb.CheckedChanged += (s, e) => { UpdatePoolList(); };
                categoryCheckBoxes.Add(cb);
                filterPanel.Controls.Add(cb);
            }

            txtPoolSearch = new TextBox { Dock = DockStyle.Top };
            txtPoolSearch.SetPlaceholderText("Search Pool...");

            lstPool = new ListBox { Dock = DockStyle.Fill };
            btnAdd = new Button { Text = "Add Card", Dock = DockStyle.Bottom };

            poolPanel.Controls.Add(lstPool);
            poolPanel.Controls.Add(txtPoolSearch);
            poolPanel.Controls.Add(filterPanel);
            poolPanel.Controls.Add(lblPool);
            poolPanel.Controls.Add(btnAdd);

            mainLayout.Controls.Add(deckPanel, 0, 1); // Left
            mainLayout.Controls.Add(cardPanel, 1, 1); // Center
            mainLayout.Controls.Add(poolPanel, 2, 1); // Right

            // Fix event handler signature by using object? instead of object
            if (txtDeckSearch != null) txtDeckSearch.TextChanged += TxtDeckSearch_TextChanged;
            if (txtPoolSearch != null) txtPoolSearch.TextChanged += TxtPoolSearch_TextChanged;
            if (lstDeck != null) lstDeck.SelectedIndexChanged += LstDeck_SelectedIndexChanged;
            if (lstPool != null) lstPool.SelectedIndexChanged += LstPool_SelectedIndexChanged;
            if (btnRemove != null) btnRemove.Click += BtnRemove_Click;
            if (btnAdd != null) btnAdd.Click += BtnAdd_Click;
        }

        private void LoadData()
        {
            if (lblDetails == null) return;

            lblDetails.Text =
                $"Hero Name: {HeroData?.gameName ?? "Unknown"}\n" +
                $"Owner: {HeroData?.owner ?? "Unknown"}\n" +
                $"Class: {HeroData?.className ?? "Unknown"}\n" +
                $"Level: {HeroData?.level}\n" +
                $"XP: {HeroData?.experience}\n" +
                $"HP: {HeroData?.hp}\n" +
                $"Energy: {HeroData?.energy}\n" +
                $"Speed: {HeroData?.speed}\n" +
                $"Items: Weapon: {HeroData?.weapon ?? "None"}, Armor: {HeroData?.armor ?? "None"}, " +
                $"Jewelry: {HeroData?.jewelry ?? "None"}, Accessory: {HeroData?.accesory ?? "None"}\n" +
                $"Traits: {string.Join(", ", HeroData?.traits?.Select(t => t ?? "Unknown") ?? Array.Empty<string>())}";

            UpdateDeckList();
            UpdatePoolList();
        }

        private void UpdateDeckList()
        {
            if (lstDeck == null || txtDeckSearch == null) return;

            string search = txtDeckSearch.Text?.ToLower() ?? "";
            lstDeck.Items.Clear();

            foreach (var card in HeroData?.cards ?? new List<string>())
            {
                if (card != null && (string.IsNullOrEmpty(search) || card.ToLower().Contains(search)))
                    lstDeck.Items.Add(card);
            }
        }

        private void UpdatePoolList()
        {
            if (lstPool == null || txtPoolSearch == null) return;

            string search = txtPoolSearch.Text?.ToLower() ?? "";
            var selectedCategories = categoryCheckBoxes
                .Where(cb => cb?.Checked == true)
                .Select(cb => cb.Text)
                .ToList();

            lstPool.Items.Clear();

            foreach (var card in availableCards)
            {
                // if any category is checked, filter by it; otherwise show all
                if (card == null) continue;

                if (selectedCategories.Any() && !selectedCategories.Contains(card.Category))
                    continue;

                if (card.Name != null && (string.IsNullOrEmpty(search) || card.Name.ToLower().Contains(search)))
                    lstPool.Items.Add(card);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (lstPool?.SelectedItem is CardInfo selectedCard && selectedCard.Name != null && HeroData?.cards != null)
            {
                HeroData.cards.Add(selectedCard.Name);
                UpdateDeckList();
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            if (lstDeck?.SelectedItem is string card && HeroData?.cards != null)
            {
                HeroData.cards.Remove(card);
                UpdateDeckList();
            }
        }

        private void LstDeck_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstDeck?.SelectedItem is string card)
                DisplayCardImage(card);
        }

        private void LstPool_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstPool?.SelectedItem is CardInfo card)
                DisplayCardImage(card.Name);
        }

        private void TxtDeckSearch_TextChanged(object? sender, EventArgs e)
        {
            UpdateDeckList();
        }

        private void TxtPoolSearch_TextChanged(object? sender, EventArgs e)
        {
            UpdatePoolList();
        }

        private void DisplayCardImage(string cardName)
        {
            if (string.IsNullOrEmpty(cardName))
            {
                SafeSetImage(null);
                return;
            }

            // Try to find the card with the exact name (case-sensitive)
            var card = availableCards.FirstOrDefault(c => c?.Name == cardName);
            if (card == null || string.IsNullOrEmpty(card.FilePath))
            {
                SafeSetImage(null);
                return;
            }

            // Get the exe directory
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
            string fullPath = Path.Combine(exeDir, card.FilePath);

            if (!File.Exists(fullPath))
            {
                SafeSetImage(null);
                return;
            }

            try
            {
                using var stream = File.OpenRead(fullPath);
                SafeSetImage(Image.FromStream(stream));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                SafeSetImage(null);
            }
        }

        private void SafeSetImage(Image? newImage)
        {
            if (picCard == null) return;

            var oldImage = picCard.Image;
            picCard.Image = newImage;

            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }
    }

    // Extension for backwards compatibility
    public static class ControlExtensions
    {
        public static void SetPlaceholderText(this TextBox textBox, string placeholder)
        {
            try
            {
                // Try to use the PlaceholderText property if available (.NET 5.0+)
                var prop = typeof(TextBox).GetProperty("PlaceholderText");
                if (prop != null)
                {
                    prop.SetValue(textBox, placeholder);
                }
                // Otherwise, the placeholder won't be set, but the application will continue working
            }
            catch
            {
                // Ignore any errors
            }
        }
    }
}