
namespace form1
{
    public partial class Form1 : Form
    {
        private const int PageSize = 6;
        private const int LowStockLimit = 10;
        private const string SearchPlaceholder = "Search product.....";
        private const string AllCategories = "All Categories";

        private static readonly Color InStockColor = Color.Lime;
        private static readonly Color LowStockColor = Color.FromArgb(255, 192, 128);
        private static readonly Color OutStockColor = Color.FromArgb(255, 128, 128);
        private static readonly Color SelectedColor = Color.LightSkyBlue;

        private TextBox[] rowNo = null!;
        private TextBox[] rowName = null!;
        private TextBox[] rowCategory = null!;
        private TextBox[] rowQty = null!;
        private TextBox[] rowPrice = null!;
        private Button[] rowStatus = null!;
        private FlowLayoutPanel[] rowActions = null!;
        private Button[] rowEdit = null!;
        private Button[] rowDelete = null!;

        private Button btnPrev = null!;
        private Button btnNext = null!;
        private Label lblPage = null!;

        private Color cellBack;
        private List<Product> allProducts = new();
        private List<Product> view = new();
        private int page;
        private int selectedId = -1;
        private string categoryFilter = AllCategories;

        public Form1()
        {
            InitializeComponent();

            FormClosed += (s, e) => Application.Exit();

            SetupRows();
            SetupPager();

            button27.Click += (s, e) => AddProduct();
            button28.Click += (s, e) => RefreshAll();
            button29.Click += (s, e) => EditSelected();

            button6.Text = "Users";
            button6.Click += (s, e) =>
            {
                using var users = new UserManagementForm();
                users.ShowDialog(this);
            };

            textBox2.Enter += (s, e) =>
            {
                if (textBox2.Text == SearchPlaceholder) textBox2.Text = "";
            };
            textBox2.Leave += (s, e) =>
            {
                if (textBox2.Text.Trim() == "") textBox2.Text = SearchPlaceholder;
            };
            textBox2.TextChanged += (s, e) =>
            {
                page = 0;
                ApplyFilter();
            };

            Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                Db.EnsureProductsTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
                return;
            }

            LoadProducts();
        }

        private void SetupRows()
        {
            rowNo = new[] { textBox10, textBox17, textBox24, textBox31, textBox38, textBox45 };
            rowName = new[] { textBox11, textBox18, textBox25, textBox32, textBox39, textBox46 };
            rowCategory = new[] { textBox12, textBox19, textBox26, textBox33, textBox40, textBox47 };
            rowQty = new[] { textBox13, textBox20, textBox27, textBox34, textBox41, textBox48 };
            rowPrice = new[] { textBox14, textBox21, textBox28, textBox35, textBox42, textBox49 };
            rowStatus = new[] { button9, button11, button13, button15, button17, button19 };
            rowActions = new[] { flowLayoutPanel3, flowLayoutPanel2, flowLayoutPanel5, flowLayoutPanel6, flowLayoutPanel7, flowLayoutPanel4 };
            rowEdit = new[] { button10, button14, button21, button23, button25, button18 };
            rowDelete = new[] { button12, button16, button22, button24, button26, button20 };

            cellBack = textBox11.BackColor;

            for (int i = 0; i < PageSize; i++)
            {
                int row = i;

                foreach (var tb in new[] { rowNo[i], rowName[i], rowCategory[i], rowQty[i], rowPrice[i] })
                {
                    tb.ReadOnly = true;
                    tb.BackColor = cellBack;
                    tb.Cursor = Cursors.Default;
                    tb.Click += (s, e) => SelectRow(row);
                }

                rowStatus[i].Click += (s, e) => SelectRow(row);

                rowEdit[i].Click += (s, e) =>
                {
                    var p = ProductAt(row);
                    if (p != null) EditProduct(p);
                };

                rowDelete[i].Click += (s, e) =>
                {
                    var p = ProductAt(row);
                    if (p != null) DeleteProduct(p);
                };
            }
        }

        private void SetupPager()
        {
            int gap = LogicalToDeviceUnits(10);
            int h = LogicalToDeviceUnits(32);
            int y = button27.Bottom + gap;

            btnPrev = new Button
            {
                Text = "◀ Prev",
                Size = new Size(LogicalToDeviceUnits(90), h),
                Location = new Point(tableLayoutPanel1.Left, y),
            };
            btnPrev.Click += (s, e) => { page--; Render(); };

            lblPage = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(LogicalToDeviceUnits(300), h),
                Location = new Point(btnPrev.Right + gap, y),
            };

            btnNext = new Button
            {
                Text = "Next ▶",
                Size = new Size(LogicalToDeviceUnits(90), h),
                Location = new Point(lblPage.Right + gap, y),
            };
            btnNext.Click += (s, e) => { page++; Render(); };

            Controls.Add(btnPrev);
            Controls.Add(lblPage);
            Controls.Add(btnNext);
        }

        private void LoadProducts()
        {
            try
            {
                allProducts = Db.GetProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
                return;
            }

            if (categoryFilter != AllCategories &&
                !allProducts.Any(p => string.Equals(p.Category, categoryFilter, StringComparison.OrdinalIgnoreCase)))
            {
                categoryFilter = AllCategories;
                button7.Text = AllCategories + " ∨";
            }

            ApplyFilter();
        }

        private string SearchText
        {
            get
            {
                string t = textBox2.Text.Trim();
                return t == SearchPlaceholder ? "" : t;
            }
        }

        private void ApplyFilter()
        {
            string q = SearchText;

            view = allProducts.Where(p =>
                (categoryFilter == AllCategories ||
                    string.Equals(p.Category, categoryFilter, StringComparison.OrdinalIgnoreCase)) &&
                (q == "" ||
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            Render();
        }

        private List<string> Categories()
        {
            return allProducts
                .Select(p => p.Category)
                .Where(c => c != "")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private Product? ProductAt(int row)
        {
            int idx = page * PageSize + row;
            return idx >= 0 && idx < view.Count ? view[idx] : null;
        }

        private Product? SelectedProduct()
        {
            return allProducts.FirstOrDefault(p => p.Id == selectedId);
        }

        private void Render()
        {
            int pages = Math.Max(1, (int)Math.Ceiling(view.Count / (double)PageSize));
            if (page >= pages) page = pages - 1;
            if (page < 0) page = 0;

            tableLayoutPanel1.SuspendLayout();

            for (int i = 0; i < PageSize; i++)
            {
                var p = ProductAt(i);
                bool has = p != null;

                foreach (Control c in new Control[]
                {
                    rowNo[i], rowName[i], rowCategory[i], rowQty[i], rowPrice[i],
                    rowStatus[i], rowActions[i]
                })
                {
                    c.Visible = has;
                }

                if (p == null) continue;

                rowNo[i].Text = (page * PageSize + i + 1).ToString();
                rowName[i].Text = p.Name;
                rowCategory[i].Text = p.Category;
                rowQty[i].Text = p.Quantity.ToString();
                rowPrice[i].Text = p.Price.ToString("N2");

                if (p.Quantity <= 0)
                {
                    rowStatus[i].Text = "Out of Stock";
                    rowStatus[i].BackColor = OutStockColor;
                }
                else if (p.Quantity <= LowStockLimit)
                {
                    rowStatus[i].Text = "Low Stock";
                    rowStatus[i].BackColor = LowStockColor;
                }
                else
                {
                    rowStatus[i].Text = "In Stock";
                    rowStatus[i].BackColor = InStockColor;
                }
            }

            tableLayoutPanel1.ResumeLayout();

            HighlightRows();

            lblPage.Text = view.Count == 0
                ? "No products found"
                : $"Page {page + 1} of {pages}   •   {view.Count} item(s)";
            btnPrev.Enabled = page > 0;
            btnNext.Enabled = page < pages - 1;
        }

        private void HighlightRows()
        {
            for (int i = 0; i < PageSize; i++)
            {
                var p = ProductAt(i);
                Color back = (p != null && p.Id == selectedId) ? SelectedColor : cellBack;

                foreach (var tb in new[] { rowNo[i], rowName[i], rowCategory[i], rowQty[i], rowPrice[i] })
                {
                    tb.BackColor = back;
                }
            }
        }

        private void SelectRow(int row)
        {
            var p = ProductAt(row);
            if (p == null) return;

            selectedId = p.Id;
            HighlightRows();
        }

        private void ResetFilters()
        {
            categoryFilter = AllCategories;
            button7.Text = AllCategories + " ∨";
            textBox2.Text = SearchPlaceholder;
        }

        private void RefreshAll()
        {
            ResetFilters();
            page = 0;
            LoadProducts();
        }

        private void AddProduct()
        {
            using var dlg = new ProductDialog(null, Categories());
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                selectedId = Db.AddProduct(dlg.Result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Add Error: " + ex.Message);
                return;
            }

            ResetFilters();
            LoadProducts();

            int idx = view.FindIndex(p => p.Id == selectedId);
            if (idx >= 0)
            {
                page = idx / PageSize;
                Render();
            }
        }

        private void EditProduct(Product p)
        {
            using var dlg = new ProductDialog(p, Categories());
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Db.UpdateProduct(dlg.Result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update Error: " + ex.Message);
                return;
            }

            selectedId = p.Id;
            LoadProducts();
        }

        private void DeleteProduct(Product p)
        {
            var answer = MessageBox.Show(
                "Delete \"" + p.Name + "\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes) return;

            try
            {
                Db.DeleteProduct(p.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete Error: " + ex.Message);
                return;
            }

            if (selectedId == p.Id) selectedId = -1;
            LoadProducts();
        }

        private void EditSelected()
        {
            var p = SelectedProduct();
            if (p == null)
            {
                MessageBox.Show("Click a product row first, then press UPDATE.");
                return;
            }

            EditProduct(p);
        }

        private void ShowCategoryMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(AllCategories, null, (s, e) => SetCategory(AllCategories));

            foreach (var c in Categories())
            {
                string cat = c;
                menu.Items.Add(cat, null, (s, e) => SetCategory(cat));
            }

            menu.Closed += (s, e) => BeginInvoke(new Action(menu.Dispose));
            menu.Show(button7, new Point(0, button7.Height));
        }

        private void SetCategory(string cat)
        {
            categoryFilter = cat;
            button7.Text = cat + " ∨";
            page = 0;
            ApplyFilter();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            ShowCategoryMenu();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            AddProduct();
        }

        private void button30_Click(object sender, EventArgs e)
        {
            var p = SelectedProduct();
            if (p == null)
            {
                MessageBox.Show("Click a product row first, then press DELETE.");
                return;
            }

            DeleteProduct(p);
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
