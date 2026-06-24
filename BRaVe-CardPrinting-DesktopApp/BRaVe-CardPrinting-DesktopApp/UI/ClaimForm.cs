using BRaVe_CardPrinting_DesktopApp.Api;
using BRaVe_CardPrinting_DesktopApp.Auth;
using BRaVe_CardPrinting_DesktopApp.Helpers;
using BRaVe_CardPrinting_DesktopApp.Models.Request;
using Guna.UI2.WinForms;
using Microsoft.Extensions.Configuration;
using System;
using System.Buffers.Text;
using System.Drawing;
using System.Windows.Forms;

namespace BRaVe_CardPrinting_DesktopApp.UI
{
    public class ClaimForm : Form
    {
        private Guna2TextBox txtSessionCode;
        private Guna2Button btnClaim;
        private Label lblError;
        private Label lblInfo;

        private readonly CardPrintingApiClient _apiClient;
        private readonly DeviceService _deviceService = new DeviceService();
        private readonly ITokenStore _tokenStore;

        public ClaimForm(ITokenStore tokenStore)
        {
            _tokenStore = tokenStore;

            var config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

            string baseUrl = config["Api:BaseUrl"];
            _apiClient = new CardPrintingApiClient(baseUrl);

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "BRaVe Card Printing - Provision Device";
            this.Size = new Size(700, 700);
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

            // Logo
            var logo = new PictureBox
            {
                Image = Properties.Resources.iombrave_logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Height = 80,
                Dock = DockStyle.Fill
            };

            // Info / Instruction
            lblInfo = new Label
            {
                Text = "Enter the claim code provided from the portal to provision this device for card printing.\n\n" +
                       "• Each device can only be registered once\n" +
                       "• Do not share your claim code\n" +
                       "• Contact admin if claim fails",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DimGray,
                AutoSize = false,
                Height = 100,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Claim Code Input
            txtSessionCode = new Guna2TextBox
            {
                PlaceholderText = "Enter Claim Code",
                BorderRadius = 8,
                Height = 45,
                Dock = DockStyle.Fill
            };

            txtSessionCode.Margin = new Padding(0, 20, 0, 20);

            // Button
            btnClaim = new Guna2Button
            {
                Text = "CONNECT DEVICE",
                Height = 55,
                BorderRadius = 8,
                FillColor = Color.FromArgb(0, 45, 114),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Dock = DockStyle.Fill
            };

            btnClaim.Click += BtnClaim_Click;

            // Error label
            lblError = new Label
            {
                ForeColor = Color.Red,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            // Add controls
            layout.Controls.Add(logo, 0, 0);
            layout.Controls.Add(lblInfo, 0, 1);
            layout.Controls.Add(txtSessionCode, 0, 2);
            layout.Controls.Add(btnClaim, 0, 3);
            layout.Controls.Add(lblError, 0, 4);

            container.Controls.Add(layout);
            this.Controls.Add(container);
        }

        private async void BtnClaim_Click(object sender, EventArgs e)
        {
            try
            {
                btnClaim.Enabled = false;
                lblError.Text = "";

                var sessionCode = txtSessionCode.Text.Trim();

                if (string.IsNullOrEmpty(sessionCode))
                {
                    lblError.Text = "Please enter claim code";
                    return;
                }

                var deviceId = _deviceService.GetDeviceId();

                var request = new PrintClaimRequest
                {
                    SessionCode = sessionCode,
                    DeviceId = deviceId
                };

                var result = await _apiClient.ClaimAsync(request);

                if (result == null)
                {
                    lblError.Text = "Claim failed";
                    return;
                }

                // Save tokens
              
                
                _tokenStore.SaveRefreshToken(result.RefreshToken);
                

                SessionManager.AccessToken = result.AccessToken;

                _apiClient.SetAccessToken(result.AccessToken);

                MessageBox.Show("Device successfully registered", "Success");

                var main = new MainForm(_apiClient);
                main.Show();

                this.Hide();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
            finally
            {
                btnClaim.Enabled = true;
            }
        }
    }
}