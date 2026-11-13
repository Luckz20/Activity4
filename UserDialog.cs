namespace form1
{
    internal sealed class UserDialog : Form
    {
        private readonly TextBox txtFullName = new();
        private readonly TextBox txtUsername = new();
        private readonly TextBox txtEmail = new();
        private readonly TextBox txtPassword = new();
        private readonly TextBox txtConfirm = new();
        private readonly int userId;
        private readonly bool isEdit;

        public UserRecord Result { get; private set; } = new UserRecord();
        public string? NewPassword { get; private set; }

        public UserDialog(UserRecord? existing)
        {
            userId = existing?.Id ?? 0;
            isEdit = existing != null;

            Text = isEdit ? "Edit User" : "Add User";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 340);

            txtFullName.MaxLength = 100;
            txtUsername.MaxLength = 50;
            txtEmail.MaxLength = 100;
            txtPassword.UseSystemPasswordChar = true;
            txtConfirm.UseSystemPasswordChar = true;

            if (existing != null)
            {
                txtFullName.Text = existing.FullName;
                txtUsername.Text = existing.Username;
                txtEmail.Text = existing.Email;
            }

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(16),
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            AddRow(tbl, 0, "Full Name:", txtFullName);
            AddRow(tbl, 1, "Username:", txtUsername);
            AddRow(tbl, 2, "Email:", txtEmail);
            AddRow(tbl, 3, isEdit ? "New Password:" : "Password:", txtPassword);
            AddRow(tbl, 4, "Confirm Password:", txtConfirm);

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
            tbl.Controls.Add(buttons, 0, 5);

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
            string fullName = txtFullName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirm = txtConfirm.Text;

            if (fullName == "" || username == "" || email == "")
            {
                MessageBox.Show(this, "Please complete Full Name, Username and Email.");
                return;
            }

            if (!email.Contains('@') || !email.Contains('.'))
            {
                MessageBox.Show(this, "Please enter a valid email address.");
                txtEmail.Focus();
                return;
            }

            if (!isEdit || password != "" || confirm != "")
            {
                if (password.Length < 8)
                {
                    MessageBox.Show(this, "Password must be at least 8 characters.");
                    txtPassword.Focus();
                    return;
                }

                if (password != confirm)
                {
                    MessageBox.Show(this, "Passwords do not match.");
                    txtConfirm.Focus();
                    return;
                }
            }

            try
            {
                if (Db.UserExists(username, email, userId))
                {
                    MessageBox.Show(this, "Username or Email already exists.");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Database Error: " + ex.Message);
                return;
            }

            Result = new UserRecord
            {
                Id = userId,
                FullName = fullName,
                Username = username,
                Email = email,
            };
            NewPassword = password == "" ? null : password;

            DialogResult = DialogResult.OK;
        }
    }
}
