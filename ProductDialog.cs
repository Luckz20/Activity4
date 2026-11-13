namespace form1
{
    internal sealed class ProductDialog : Form
    {
        private readonly TextBox txtName = new();
        private readonly ComboBox cboCategory = new();
        private readonly NumericUpDown numQty = new();
        private readonly NumericUpDown numPrice = new();
        private readonly int productId;

        public Product Result { get; private set; } = new Product();

        public ProductDialog(Product? existing, IEnumerable<string> categories)
        {
            productId = existing?.Id ?? 0;

            Text = existing == null ? "Add Product" : "Edit Product";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(430, 290);

            txtName.MaxLength = 100;

            cboCategory.DropDownStyle = ComboBoxStyle.DropDown;
            cboCategory.MaxLength = 50;
            var cats = new List<string> { "Food", "Canned Goods", "Personal Care", "Household", "Snacks" };
            foreach (var c in categories)
            {
                if (!cats.Contains(c, StringComparer.OrdinalIgnoreCase)) cats.Add(c);
            }
            cats.Sort(StringComparer.OrdinalIgnoreCase);
            cboCategory.Items.AddRange(cats.ToArray());

            numQty.Minimum = 0;
            numQty.Maximum = 1000000;

            numPrice.Minimum = 0;
            numPrice.Maximum = 100000000;
            numPrice.DecimalPlaces = 2;
            numPrice.Increment = 1;

            if (existing != null)
            {
                txtName.Text = existing.Name;
                cboCategory.Text = existing.Category;
                numQty.Value = Math.Min(numQty.Maximum, Math.Max(0, existing.Quantity));
                numPrice.Value = Math.Min(numPrice.Maximum, Math.Max(0m, existing.Price));
            }

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(16),
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 4; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AddRow(tbl, 0, "Product Name:", txtName);
            AddRow(tbl, 1, "Category:", cboCategory);
            AddRow(tbl, 2, "Quantity:", numQty);
            AddRow(tbl, 3, "Price:", numPrice);

            var btnOk = new Button { Text = "Save", Size = new Size(100, 36) };
            var btnCancel = new Button { Text = "Cancel", Size = new Size(100, 36), DialogResult = DialogResult.Cancel };
            btnOk.Click += (s, e) => Save();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
            };
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnOk);
            tbl.SetColumnSpan(buttons, 2);
            tbl.Controls.Add(buttons, 0, 4);

            Controls.Add(tbl);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private static void AddRow(TableLayoutPanel tbl, int row, string label, Control input)
        {
            tbl.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
            }, 0, row);

            input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbl.Controls.Add(input, 1, row);
        }

        private void Save()
        {
            string name = txtName.Text.Trim();
            if (name == "")
            {
                MessageBox.Show(this, "Please enter the product name.");
                txtName.Focus();
                return;
            }

            string category = cboCategory.Text.Trim();
            if (category == "") category = "Uncategorized";

            Result = new Product
            {
                Id = productId,
                Name = name,
                Category = category,
                Quantity = (int)numQty.Value,
                Price = numPrice.Value,
            };

            DialogResult = DialogResult.OK;
        }
    }
}
