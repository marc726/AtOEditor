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

        // UI controls
        private TabControl tabControl = new(); // Initialize to fix non-nullable warning
        private Label? lblDetails;
        private ListBox? lstDeck;
        private TextBox? txtDeckSearch;
        private ListBox? lstPool;
        private TextBox? txtPoolSearch;
        private PictureBox? picCard;
        private Button? btnAdd, btnRemove;
        private FlowLayoutPanel? filterPanel;
        private readonly List<CheckBox> categoryCheckBoxes = new List<CheckBox>();

        // Items controls
        private ComboBox? cmbWeapon;
        private ComboBox? cmbArmor;
        private ComboBox? cmbJewelry;
        private ComboBox? cmbAccessory;

        // Stats controls
        private NumericUpDown? nudHp;
        private NumericUpDown? nudEnergy;
        private NumericUpDown? nudSpeed;
        private NumericUpDown? nudGold;
        private NumericUpDown? nudDust;
        private Dictionary<string, NumericUpDown> resistControls;

        public HeroEditorControl(AtOSaveEditor.Models.Hero hero, List<CardInfo> availableCards, string assetsPath)
        {
            this.HeroData = hero ?? throw new ArgumentNullException(nameof(hero));
            this.availableCards = availableCards ?? new List<CardInfo>();
            this.assetsPath = assetsPath ?? string.Empty;
            this.resistControls = new Dictionary<string, NumericUpDown>();
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;

            // Create main tab control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            // Details panel at top
            Panel detailsPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            lblDetails = new Label { AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };
            detailsPanel.Controls.Add(lblDetails);

            // Create main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainLayout.Controls.Add(detailsPanel, 0, 0);
            mainLayout.Controls.Add(tabControl, 0, 1);

            this.Controls.Add(mainLayout);

            // Create tabs
            TabPage deckTab = CreateDeckTab();
            TabPage itemsTab = CreateItemsTab();
            TabPage statsTab = CreateStatsTab();

            tabControl.TabPages.Add(deckTab);
            tabControl.TabPages.Add(itemsTab);
            tabControl.TabPages.Add(statsTab);

            // Wire up events
            if (txtDeckSearch != null) txtDeckSearch.TextChanged += TxtDeckSearch_TextChanged;
            if (txtPoolSearch != null) txtPoolSearch.TextChanged += TxtPoolSearch_TextChanged;
            if (lstDeck != null) lstDeck.SelectedIndexChanged += LstDeck_SelectedIndexChanged;
            if (lstPool != null) lstPool.SelectedIndexChanged += LstPool_SelectedIndexChanged;
            if (btnRemove != null) btnRemove.Click += BtnRemove_Click;
            if (btnAdd != null) btnAdd.Click += BtnAdd_Click;

            // Wire up item change events
            if (cmbWeapon != null) cmbWeapon.SelectedIndexChanged += (s, e) => { if (cmbWeapon.SelectedItem != null) HeroData.weapon = cmbWeapon.SelectedItem.ToString(); };
            if (cmbArmor != null) cmbArmor.SelectedIndexChanged += (s, e) => { if (cmbArmor.SelectedItem != null) HeroData.armor = cmbArmor.SelectedItem.ToString(); };
            if (cmbJewelry != null) cmbJewelry.SelectedIndexChanged += (s, e) => { if (cmbJewelry.SelectedItem != null) HeroData.jewelry = cmbJewelry.SelectedItem.ToString(); };
            if (cmbAccessory != null) cmbAccessory.SelectedIndexChanged += (s, e) => { if (cmbAccessory.SelectedItem != null) HeroData.accesory = cmbAccessory.SelectedItem.ToString(); };

            // Wire up stats change events
            if (nudHp != null) nudHp.ValueChanged += (s, e) => { if (HeroData != null) HeroData.hp = (int)nudHp.Value; };
            if (nudEnergy != null) nudEnergy.ValueChanged += (s, e) => { if (HeroData != null) HeroData.energy = (int)nudEnergy.Value; };
            if (nudSpeed != null) nudSpeed.ValueChanged += (s, e) => { if (HeroData != null) HeroData.speed = (int)nudSpeed.Value; };
            if (nudGold != null) nudGold.ValueChanged += (s, e) => { if (HeroData != null) HeroData.heroGold = (int)nudGold.Value; };
            if (nudDust != null) nudDust.ValueChanged += (s, e) => { if (HeroData != null) HeroData.heroDust = (int)nudDust.Value; };
        }

        private TabPage CreateDeckTab()
        {
            TabPage deckTab = new TabPage("Deck");
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            // Left panel (Deck)
            Panel deckPanel = new Panel { Dock = DockStyle.Fill };
            Label lblDeck = new Label { Text = "Deck", Dock = DockStyle.Top };
            txtDeckSearch = new TextBox { Dock = DockStyle.Top };
            txtDeckSearch.SetPlaceholderText("Search Deck...");
            lstDeck = new ListBox { Dock = DockStyle.Fill };
            btnRemove = new Button { Text = "Remove Card", Dock = DockStyle.Bottom };
            deckPanel.Controls.AddRange(new Control[] { lstDeck, txtDeckSearch, lblDeck, btnRemove });

            // Center panel (Card preview)
            Panel cardPanel = new Panel { Dock = DockStyle.Fill };
            picCard = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
            cardPanel.Controls.Add(picCard);

            // Right panel (Card pool)
            Panel poolPanel = new Panel { Dock = DockStyle.Fill };
            Label lblPool = new Label { Text = "Available Cards", Dock = DockStyle.Top };
            filterPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };

            string[] categories = { "Boon", "Healer", "Injury", "Item", "Mage", "MagicKnight", "Monster", "Scout", "Special", "Warrior" };
            foreach (var cat in categories)
            {
                CheckBox cb = new CheckBox { Text = cat, AutoSize = true };
                cb.CheckedChanged += (s, e) => UpdatePoolList();
                categoryCheckBoxes.Add(cb);
                filterPanel.Controls.Add(cb);
            }

            txtPoolSearch = new TextBox { Dock = DockStyle.Top };
            txtPoolSearch.SetPlaceholderText("Search Pool...");
            lstPool = new ListBox { Dock = DockStyle.Fill };
            btnAdd = new Button { Text = "Add Card", Dock = DockStyle.Bottom };
            poolPanel.Controls.AddRange(new Control[] { lstPool, txtPoolSearch, filterPanel, lblPool, btnAdd });

            layout.Controls.AddRange(new Control[] { deckPanel, cardPanel, poolPanel });
            deckTab.Controls.Add(layout);
            return deckTab;
        }

        private TabPage CreateItemsTab()
        {
            TabPage itemsTab = new TabPage("Items");
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10),
                AutoSize = true
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            // Add item selection dropdowns
            int currentRow = 0;

            // Center everything in a panel with padding
            Panel contentPanel = new Panel
            {
                AutoSize = true,
                Padding = new Padding(10),
                Dock = DockStyle.Top
            };

            AddItemRow(layout, "Weapon:", ref cmbWeapon, currentRow++);
            AddItemRow(layout, "Armor:", ref cmbArmor, currentRow++);
            AddItemRow(layout, "Jewelry:", ref cmbJewelry, currentRow++);
            AddItemRow(layout, "Accessory:", ref cmbAccessory, currentRow++);

            // Populate dropdowns with items from card pool
            var items = availableCards.Where(c => c.Category == "Item").Select(c => c.Name).ToList();
            foreach (var combo in new[] { cmbWeapon, cmbArmor, cmbJewelry, cmbAccessory })
            {
                if (combo != null)
                {
                    combo.Items.AddRange(items.Cast<object>().ToArray());
                }
            }

            contentPanel.Controls.Add(layout);
            itemsTab.Controls.Add(contentPanel);
            return itemsTab;
        }

        private void AddItemRow(TableLayoutPanel layout, string labelText, ref ComboBox? combo, int row)
        {
            Label label = new Label
            {
                Text = labelText,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            layout.Controls.Add(label, 0, row);

            if (combo == null)
            {
                combo = new ComboBox();
            }
            combo.Dock = DockStyle.Fill;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Margin = new Padding(0, 4, 0, 4);
            layout.Controls.Add(combo, 1, row);
        }

        private TabPage CreateStatsTab()
        {
            TabPage statsTab = new TabPage("Stats");

            // Create a container panel for scrolling
            Panel scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // Create content panel that will hold our layout
            Panel contentPanel = new Panel
            {
                AutoSize = true,
                Padding = new Padding(10),
                Dock = DockStyle.Top
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 4,
                Padding = new Padding(10)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            int row = 0;

            // Basic stats
            AddStatControl(layout, "HP:", ref nudHp, 0, row, 1, 999);
            AddResistControl(layout, "Fire Resist:", "fire", 2, row++);

            AddStatControl(layout, "Energy:", ref nudEnergy, 0, row, 0, 10);
            AddResistControl(layout, "Cold Resist:", "cold", 2, row++);

            AddStatControl(layout, "Speed:", ref nudSpeed, 0, row, 1, 99);
            AddResistControl(layout, "Lightning Resist:", "lightning", 2, row++);

            AddStatControl(layout, "Gold:", ref nudGold, 0, row, 0, 99999);
            AddResistControl(layout, "Mind Resist:", "mind", 2, row++);

            AddStatControl(layout, "Dust:", ref nudDust, 0, row, 0, 99999);
            AddResistControl(layout, "Holy Resist:", "holy", 2, row++);

            AddResistControl(layout, "Slashing Resist:", "slashing", 0, row);
            AddResistControl(layout, "Shadow Resist:", "shadow", 2, row++);

            AddResistControl(layout, "Blunt Resist:", "blunt", 0, row);
            AddResistControl(layout, "Piercing Resist:", "piercing", 2, row);

            contentPanel.Controls.Add(layout);
            scrollPanel.Controls.Add(contentPanel);
            statsTab.Controls.Add(scrollPanel);

            return statsTab;
        }

        private void AddResistControl(TableLayoutPanel layout, string labelText, string resistKey, int col, int row)
        {
            Label label = new Label
            {
                Text = labelText,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            layout.Controls.Add(label, col, row);

            var resist = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Width = 100,
                Margin = new Padding(0, 4, 0, 4)
            };
            resistControls[resistKey] = resist;

            // Wire up the event handler
            resist.ValueChanged += (s, e) =>
            {
                if (HeroData != null)
                {
                    var property = typeof(Hero).GetProperty($"resist{char.ToUpper(resistKey[0]) + resistKey.Substring(1)}");
                    if (property != null)
                    {
                        property.SetValue(HeroData, (int)resist.Value);
                    }
                }
            };

            layout.Controls.Add(resist, col + 1, row);
        }

        private void AddStatControl(TableLayoutPanel layout, string labelText, ref NumericUpDown? control, int col, int row, decimal min, decimal max)
        {
            Label label = new Label
            {
                Text = labelText,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            layout.Controls.Add(label, col, row);

            if (control == null)
            {
                control = new NumericUpDown();
            }

            control.Minimum = min;
            control.Maximum = max;
            control.Value = min;
            control.Width = 100;
            control.Margin = new Padding(0, 4, 0, 4);
            layout.Controls.Add(control, col + 1, row);
        }

        private void LoadData()
        {
            if (lblDetails == null || HeroData == null) return;

            lblDetails.Text = $"Hero: {HeroData.gameName ?? "Unknown"} ({HeroData.className ?? "Unknown"})";

            // Load deck
            UpdateDeckList();
            UpdatePoolList();

            // Load items
            if (cmbWeapon != null) cmbWeapon.SelectedItem = HeroData.weapon;
            if (cmbArmor != null) cmbArmor.SelectedItem = HeroData.armor;
            if (cmbJewelry != null) cmbJewelry.SelectedItem = HeroData.jewelry;
            if (cmbAccessory != null) cmbAccessory.SelectedItem = HeroData.accesory;

            // Load stats
            if (nudHp != null && HeroData.hp != null) nudHp.Value = HeroData.hp.Value;
            if (nudEnergy != null && HeroData.energy != null) nudEnergy.Value = HeroData.energy.Value;
            if (nudSpeed != null && HeroData.speed != null) nudSpeed.Value = HeroData.speed.Value;
            if (nudGold != null) nudGold.Value = (decimal)HeroData.heroGold;
            if (nudDust != null) nudDust.Value = (decimal)HeroData.heroDust;

            // Load resists with direct property access
            if (resistControls.TryGetValue("slashing", out var slashingControl))
                slashingControl.Value = HeroData.resistSlashing;
            if (resistControls.TryGetValue("blunt", out var bluntControl))
                bluntControl.Value = HeroData.resistBlunt;
            if (resistControls.TryGetValue("piercing", out var piercingControl))
                piercingControl.Value = HeroData.resistPiercing;
            if (resistControls.TryGetValue("fire", out var fireControl))
                fireControl.Value = HeroData.resistFire;
            if (resistControls.TryGetValue("cold", out var coldControl))
                coldControl.Value = HeroData.resistCold;
            if (resistControls.TryGetValue("lightning", out var lightningControl))
                lightningControl.Value = HeroData.resistLightning;
            if (resistControls.TryGetValue("mind", out var mindControl))
                mindControl.Value = HeroData.resistMind;
            if (resistControls.TryGetValue("holy", out var holyControl))
                holyControl.Value = HeroData.resistHoly;
            if (resistControls.TryGetValue("shadow", out var shadowControl))
                shadowControl.Value = HeroData.resistShadow;
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
                .Select(cb => cb?.Text)
                .Where(text => text != null)
                .ToList();

            lstPool.Items.Clear();

            foreach (var card in availableCards)
            {
                if (card?.Category == null) continue;

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
            if (lstPool?.SelectedItem is CardInfo card && card.Name != null)
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