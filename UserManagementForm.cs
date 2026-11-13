namespace form1
{
    internal sealed class UserManagementForm : Form
    {
        private const string SearchPlaceholder = "Search user.....";

        private readonly DataGridView grid = new();
        private readonly TextBox txtSearch = new();
        private List<UserRecord> allUsers = new();

        public UserManagementForm()
        {
            Text = "User Management";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 10F);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 520);
            MinimumSize = new Size(700, 400);

            var title = new Label
            {
                Text = "Users",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 12),
            };

            txtSearch.Text = SearchPlaceholder;
            txtSearch.Width = 260;
            txtSearch.Location = new Point(16, 62);
            txtSearch.Enter += (s, e) =>
            {
                if (txtSearch.Text == SearchPlaceholder) txtSearch.Text = "";
            };
            txtSearch.Leave += (s, e) =>
            {
                if (txtSearch.Text.Trim() == "") txtSearch.Text = SearchPlaceholder;
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            var btnAdd = new Button { Text = "+ Add User", Size = new Size(110, 32) };
            var btnEdit = new Button { Text = "Edit", Size = new Size(80, 32) };
            var btnDelete = new Button { Text = "Delete", Size = new Size(80, 32) };
            var btnRefresh = new Button { Text = "Refresh", Size = new Size(80, 32) };
            btnAdd.Click += (s, e) => AddUser();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnRefresh.Click += (s, e) =>
            {
                txtSearch.Text = SearchPlaceholder;
                LoadUsers();
            };

            var bar = new FlowLayoutPanel
            {
                Location = new Point(290, 58),
                Size = new Size(594, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
            };
            bar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh });

            grid.Location = new Point(16, 108);
            grid.Size = new Size(868, 396);
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.AutoGenerateColumns = false;
            grid.BackgroundColor = SystemColors.Window;
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserRecord.Id), HeaderText = "ID", Width = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserRecord.FullName), HeaderText = "Full Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserRecord.Username), HeaderText = "Username", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(UserRecord.Email), HeaderText = "Email", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(UserRecord.DateCreated),
                HeaderText = "Date Created",
                Width = 160,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd hh:mm tt" },
            });
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) EditSelected();
            };

            Controls.Add(title);
            Controls.Add(txtSearch);
            Controls.Add(bar);
            Controls.Add(grid);

            Load += (s, e) => LoadUsers();
        }

        private string SearchText
        {
            get
            {
                string t = txtSearch.Text.Trim();
                return t == SearchPlaceholder ? "" : t;
            }
        }

        private void LoadUsers()
        {
            try
            {
                allUsers = Db.GetUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Database Error: " + ex.Message);
                return;
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string q = SearchText;

            grid.DataSource = allUsers.Where(u =>
                q == "" ||
                u.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(q, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        private UserRecord? SelectedUser()
        {
            return grid.CurrentRow?.DataBoundItem as UserRecord;
        }

        private void SelectById(int id)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.DataBoundItem is UserRecord u && u.Id == id)
                {
                    row.Selected = true;
                    grid.CurrentCell = row.Cells[1];
                    return;
                }
            }
        }

        private void AddUser()
        {
            using var dlg = new UserDialog(null);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            int newId;
            try
            {
                newId = Db.AddUser(dlg.Result, dlg.NewPassword ?? "");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Add Error: " + ex.Message);
                return;
            }

            txtSearch.Text = SearchPlaceholder;
            LoadUsers();
            SelectById(newId);
        }

        private void EditSelected()
        {
            var u = SelectedUser();
            if (u == null)
            {
                MessageBox.Show(this, "Select a user first.");
                return;
            }

            using var dlg = new UserDialog(u);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Db.UpdateUser(dlg.Result, dlg.NewPassword);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Update Error: " + ex.Message);
                return;
            }

            LoadUsers();
            SelectById(u.Id);
        }

        private void DeleteSelected()
        {
            var u = SelectedUser();
            if (u == null)
            {
                MessageBox.Show(this, "Select a user first.");
                return;
            }

            try
            {
                if (Db.CountUsers() <= 1)
                {
                    MessageBox.Show(this, "You cannot delete the last remaining user.");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Database Error: " + ex.Message);
                return;
            }

            var answer = MessageBox.Show(
                this,
                "Delete user \"" + u.Username + "\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes) return;

            try
            {
                Db.DeleteUser(u.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Delete Error: " + ex.Message);
                return;
            }

            LoadUsers();
        }
    }
}
