using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace BRaVe_CardPrinting_DesktopApp
{
    public class LoginForm : Form
    {
        private Guna2TextBox txtUsername, txtPassword;
        private Guna2Button btnLogin;
        private Label lblError;

        public bool IsAuthenticated { get; private set; }

        public LoginForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Login";
            this.Size = new Size(800, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            var container = new Guna2Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 10));

         

            var logo = new PictureBox
            {
                Image = Properties.Resources.iombrave_logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Height = 10,
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(logo, 0, 0);

            //// Title
            //var lblTitle = new Label
            //{
            //    Text = "BRaVe Card Printing Login",
            //    Font = new Font("Segoe UI", 16, FontStyle.Bold),
            //    Dock = DockStyle.Top,
            //    TextAlign = ContentAlignment.MiddleCenter
            //};

            // Username
            txtUsername = new Guna2TextBox
            {
                PlaceholderText = "Username",
                BorderRadius = 8,
                Dock = DockStyle.Fill
            };

            // Password
            txtPassword = new Guna2TextBox
            {
                PlaceholderText = "Password",
                BorderRadius = 8,
                PasswordChar = '●',
                Dock = DockStyle.Fill
            };

            txtUsername.Margin = new Padding(0, 100, 0, 30);
            txtPassword.Margin = new Padding(0, 0, 0, 30);

            // Login button
            btnLogin = new Guna2Button
            {
                Text = "LOGIN",
                Height = 60,
                BorderRadius = 8,
                FillColor = Color.FromArgb(0, 45, 114),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Dock = DockStyle.Fill
            };

            // Error label
            lblError = new Label
            {
                ForeColor = Color.Red,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            btnLogin.Click += BtnLogin_Click;

            //layout.Controls.Add(lblTitle, 0, 1);
            layout.Controls.Add(txtUsername, 0, 2);
            layout.Controls.Add(txtPassword, 0, 3);
            layout.Controls.Add(btnLogin, 0, 4);
            layout.Controls.Add(lblError, 0, 5);

            container.Controls.Add(layout);
            this.Controls.Add(container);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (txtUsername.Text == "admin" && txtPassword.Text == "admin")
            {
                IsAuthenticated = true;
                this.Close();
            }
            else
            {
                lblError.Text = "Invalid username or password";
            }
        }
    }
}