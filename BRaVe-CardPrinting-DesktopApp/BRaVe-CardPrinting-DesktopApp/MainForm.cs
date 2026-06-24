using BRaVe_CardPrinting_DesktopApp.Api;
using BRaVe_CardPrinting_DesktopApp.Auth;
using BRaVe_CardPrinting_DesktopApp.Db;
using BRaVe_CardPrinting_DesktopApp.Helpers;
using BRaVe_CardPrinting_DesktopApp.Helpers.Mappers;
using BRaVe_CardPrinting_DesktopApp.Models;
using BRaVe_CardPrinting_DesktopApp.Models.Request;
using BRaVe_CardPrinting_DesktopApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;


namespace BRaVe_CardPrinting_DesktopApp
{
    public partial class MainForm : Form
    {

        private MockUserService _userService = new MockUserService();
        private User _selectedUser;
        private Label lblName, lblSize, lblHousehold, lblProgram, lblActivity;
        private PictureBox picPhoto;
        private Guna.UI2.WinForms.Guna2ComboBox cmbPrinters;
        private Guna.UI2.WinForms.Guna2Panel previewCard;
        private Label lblStatus;
        private PrintDocument _printDocument;
        private Bitmap _currentBitmap;
        private Label lblPrinter;
        private ComboBox cmbTemplates;
        private List<CardTemplate> _templates;
        private CardTemplate _selectedTemplate;
        private Guna.UI2.WinForms.Guna2DataGridView gridSearch;
        private Guna.UI2.WinForms.Guna2DataGridView gridHistory;
        private Guna.UI2.WinForms.Guna2TextBox txtInput;
        private Guna.UI2.WinForms.Guna2TextBox txtFilePath;
        private List<User> _previewUsers;
        private int _previewIndex = 0;
        private PrintService _printService = new PrintService();
        private readonly CardPrintingApiClient _api;
        private string _currentUser;
        private readonly QueueCacheService _queueCache =new QueueCacheService();
        public MainForm(CardPrintingApiClient api)
        {
            InitializeComponent();
            _api = api;
            _printDocument = new PrintDocument();

            _printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            _printDocument.DefaultPageSettings.Landscape = true;
            _currentUser = Environment.UserDomainName + "\\" + Environment.UserName;

            _printDocument.PrintPage += (sender, e) =>
            {
                if (_previewUsers == null || _previewUsers.Count == 0)
                    return;

                if (_previewIndex >= _previewUsers.Count)
                {
                    e.HasMorePages = false;
                    return;
                }

                var user = _previewUsers[_previewIndex];



                using (var card = RenderCard(Mapper.ToCardData(user)))
                {
                    e.Graphics.DrawImage(card, 0, 0);
                }

                _previewIndex++;

                e.HasMorePages = _previewIndex < _previewUsers.Count;
            };
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            DbInitializer.Initialize();
            BuildUI();

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 3000; // every 3 seconds

            timer.Tick += (s, e) =>
            {
                var printerName = cmbPrinters.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(printerName))
                {
                    var status = GetPrinterStatus(printerName);
                    lblStatus.Text = $"Status: {status}";
                    lblStatus.ForeColor = status == "Ready" ? Color.Green : Color.Red;
                }
            };

            timer.Start();
        }


        private void BindHistoryGrid()
        {
            gridHistory.DataSource = null;
            gridHistory.DataSource = _printService.GetAll();
        }
        private void UpdatePreview(User user)
        {
            lblName.Text = $"Name: {user.FullName}";
            lblSize.Text = $"Family Size: {user.FamilySize}";
            lblHousehold.Text = $"Household Id: {user.HouseholdId}";
            lblProgram.Text = $"Program: {user.Program}";
            lblActivity.Text = $"Activity: {user.Activity}";

            if (user.Photo != null)
            {
                using (var ms = new System.IO.MemoryStream(user.Photo))
                {
                    picPhoto.Image = Image.FromStream(ms);
                }
            }
            else
            {
                picPhoto.Image = Properties.Resources.user_placeholder;
            }
        }


        private void AddHeaderCheckbox()
        {
            CheckBox headerCheckBox = new CheckBox();

            // Get header cell location
            Rectangle rect = gridSearch.GetCellDisplayRectangle(0, -1, true);

            headerCheckBox.Size = new Size(18, 18);
            headerCheckBox.Location = new Point(
                rect.Location.X + (rect.Width / 2) - 9,
                rect.Location.Y + (rect.Height / 2) - 9
            );

            headerCheckBox.CheckedChanged += (s, e) =>
            {
                bool isChecked = headerCheckBox.Checked;

                foreach (DataGridViewRow row in gridSearch.Rows)
                {
                    var user = row.DataBoundItem as User;
                    if (user == null) continue;

                    //  skip printed
                    if (user.CardPrinted) continue;

                    //  skip locked by others
                    if (user.IsLocked && user.LockedBy != _currentUser)
                        continue;

                    row.Cells["Select"].Value = isChecked;
                }

                gridSearch.RefreshEdit();
            };

            gridSearch.Controls.Add(headerCheckBox);
        }


        private void BindGrid(List<User> users)
        {
            // Remove existing header checkbox if any
            foreach (Control ctrl in gridSearch.Controls)
            {
                if (ctrl is CheckBox)
                {
                    gridSearch.Controls.Remove(ctrl);
                    break;
                }
            }

            gridSearch.AutoGenerateColumns = true;

            // CLEAR OLD COLUMNS FIRST
            gridSearch.Columns.Clear();

            // THEN BIND FRESH DATA
            gridSearch.DataSource = null;
            gridSearch.DataSource = users;

            // ==========================================
            // SHOW / HIDE COLUMNS
            // ==========================================

            // Rename headers
            gridSearch.Columns["FullName"].HeaderText = "Full Name";
            gridSearch.Columns["FamilySize"].HeaderText = "Family Size";
            gridSearch.Columns["HouseholdId"].HeaderText = "Household ID";
            gridSearch.Columns["Activity"].HeaderText = "Activity";
            gridSearch.Columns["LocationInformation"].HeaderText = "Location";
            gridSearch.Columns["IsLocked"].HeaderText = "Locked?";
            gridSearch.Columns["LockedBy"].HeaderText = "Locked By";
            gridSearch.Columns["CardPrinted"].HeaderText = "Printed";
            gridSearch.Columns["PrintedOn"].HeaderText = "Printed On";

            // Hide unwanted columns
          
            gridSearch.Columns["barcodeId"].Visible = false;
            gridSearch.Columns["Mission"].Visible = false;
            gridSearch.Columns["Program"].Visible = false;
            gridSearch.Columns["AdditionalInformation"].Visible = false;
            gridSearch.Columns["Photo"].Visible = false;
            gridSearch.Columns["RegDate"].Visible = false;

            // ==========================================
            // CHECKBOX COLUMN 
            // ==========================================

            if (!gridSearch.Columns.Contains("Select"))
            {
                var chkColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "Select",
                    HeaderText = "",
                    Width = 40
                };

                gridSearch.Columns.Insert(0, chkColumn);
            }

            foreach (DataGridViewRow row in gridSearch.Rows)
            {
                row.Cells["Select"].Value = false;
            }

            foreach (DataGridViewColumn col in gridSearch.Columns)
            {
                if (col.Name == "Select")
                    col.ReadOnly = false;
                else
                    col.ReadOnly = true;
            }

            // ==========================================
            //  ROW COLORING 
            // ==========================================

            foreach (DataGridViewRow row in gridSearch.Rows)
            {
                var user = row.DataBoundItem as User;
                if (user == null) continue;

                var cell = (DataGridViewCheckBoxCell)row.Cells["Select"];

                //  Printed
                if (user.CardPrinted)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                    cell.ReadOnly = true;
                    cell.Style.ForeColor = Color.DarkGray;
                }
                //  Locked by others
                else if (user.IsLocked && user.LockedBy != _currentUser)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                    cell.ReadOnly = true;
                    cell.Style.ForeColor = Color.DarkGray;
                }
                //  Locked by user
                else if (user.IsLocked)
                {
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
                    cell.ReadOnly = false;
                }
                else
                {
                    cell.ReadOnly = false;
                }
            }


            AddHeaderCheckbox();
        }

        private List<User> GetCheckedUsers()
        {
            var selectedUsers = new List<User>();

            foreach (DataGridViewRow row in gridSearch.Rows)
            {
                var cellValue = row.Cells["Select"].Value;

                bool isChecked = cellValue != null && (bool)cellValue;

                if (isChecked && row.DataBoundItem is User user)
                {
                    selectedUsers.Add(user);
                }
            }

            return selectedUsers;
        }



        private string GetPrinterStatus(string printerName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer"))
                {
                    foreach (ManagementObject printer in searcher.Get())
                    {
                        var name = printer["Name"]?.ToString();

                        if (string.IsNullOrEmpty(name)) continue;

                        if (!name.Contains(printerName) && !printerName.Contains(name))
                            continue;

                    
                        bool isOffline = Convert.ToBoolean(printer["WorkOffline"] ?? false);
                 

                        int errorState = 0;
                        if (printer["DetectedErrorState"] != null)
                            errorState = Convert.ToInt32(printer["DetectedErrorState"]);

                        if (isOffline) return "Offline";
               
                        if (errorState != 0) return "Error";

                        return "Ready";
                    }
                }

                return "Not Found";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }




        //CARD TEMPLATE RENDER
        private Bitmap RenderCard(CardData data)
        {
            int width = 1013;
            int height = 638;

            Bitmap bmp = new Bitmap(width, height);
            bmp.SetResolution(300, 300);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                var template = _selectedTemplate;

                if (!File.Exists(template.BackgroundPath))
                {
                    throw new Exception($"Image not found: {template.BackgroundPath}");
                }

                using (var fs = new FileStream(template.BackgroundPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var bg = new Bitmap(template.BackgroundPath))
                {
                    g.DrawImage(bg, 0, 0, width, height);
                }


                template.RenderAction(g, data);
            }

            return bmp;
        }

        private CardData BuildCardData(User user)
        {
            return new CardData
            {
                UserName = user.FullName,
                LocationInformation = user.LocationInformation,
                FamilySize=user.FamilySize,
                AdditionalInformation=user.AdditionalInformation,
                RegDate=user.RegDate,
                HouseholdId =user.HouseholdId,       
                Photo = user.Photo,
                PrintedOn = DateTime.Now
            };
        }

        private void BuildUI()
        {
            {

                this.SuspendLayout();
                this.Text = "BRaVe Card Printing";
                this.WindowState = FormWindowState.Maximized;

                // ================= HEADER =================
                var header = BuildHeader();

                // ================= MAIN SPLIT =================
                var mainSplit = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    FixedPanel = FixedPanel.None,
                    IsSplitterFixed = false
                };

                this.Shown += (s, e) =>
                {
                    mainSplit.SplitterDistance = (int)(this.ClientSize.Width * 0.67);
                };

                // ================= LEFT PANEL =================
                var leftPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10),
                    BackColor = Color.WhiteSmoke
                };

                var leftLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2
                };

                leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var searchPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    FlowDirection = FlowDirection.TopDown,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = false,
                    Padding = new Padding(5)
                };

                // ================= RADIO BUTTONS =================
                var radioPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    WrapContents = false,
                    Margin = new Padding(0, 0, 0, 10)
                };

                var rdoHousehold = new RadioButton
                {
                    Text = "Household ID",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Checked = true
                };

                var rdoActivity = new RadioButton
                {
                    Text = "Activity",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };

                var rdoFile = new RadioButton
                {
                    Text = "Upload File",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };

                // spacing
                rdoHousehold.Margin = new Padding(0, 0, 20, 0);
                rdoActivity.Margin = new Padding(0, 0, 20, 0);

                radioPanel.Controls.Add(rdoHousehold);
                radioPanel.Controls.Add(rdoActivity);
                radioPanel.Controls.Add(rdoFile);



                // ================= INPUT PANEL =================
                var inputPanel = new Panel
                {
                    Height = 40,
                    Width = 600,
                    Margin = new Padding(0, 0, 0, 10)
                };

                txtInput = new Guna.UI2.WinForms.Guna2TextBox
                {
                    PlaceholderText = "Enter value...",
                    Dock = DockStyle.Fill,
                    BorderRadius = 8,
                    Height = 40

                };


                var filePanel = new Guna.UI2.WinForms.Guna2Panel
                {
                    Dock = DockStyle.Fill,
                    Visible = false
                };

              txtFilePath = new Guna.UI2.WinForms.Guna2TextBox
                {
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    BorderRadius = 8,
                    PlaceholderText = "Select file...",
                    FillColor = Color.White,
                    Height = 40
                };

                var btnBrowse = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Browse",
                    Dock = DockStyle.Right,
                    Width = 120,
                    Height = 40,
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(59, 130, 246),
                    ForeColor = Color.White
                };

                filePanel.Controls.Add(txtFilePath);
                filePanel.Controls.Add(btnBrowse);

                inputPanel.Controls.Add(txtInput);
                inputPanel.Controls.Add(filePanel);

                searchPanel.Padding = new Padding(10);
                inputPanel.Padding = new Padding(0);

                // ================= LOAD BUTTON =================
                var btnLoad = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "LOAD RECORDS",
                    Dock = DockStyle.None,
                    Margin = new Padding(0, 5, 0, 0),
                    Height = 40,
                    Width = 200,
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White
                };

                btnLoad.Click += async (s, e) =>
                {

                    //DbInitializer.Reset();
                    //MessageBox.Show("Database reset complete");

                    try
                    {

                        // ==========================================
                        // LOADING STATE
                        // ==========================================
                        btnLoad.Enabled = false;
                        btnLoad.Text = "Loading...";

                        var deviceId = new DeviceService().GetDeviceId();
                        var windowsUser = Environment.UserDomainName + "\\" + Environment.UserName;

                        var request = new CardPrintQueueRequest
                        {
                            DeviceId = deviceId,
                            WindowsUser = windowsUser
                        };

                        // ================= HOUSEHOLD =================
                        if (rdoHousehold.Checked)
                        {
                            var value = txtInput.Text.Trim();

                            if (string.IsNullOrEmpty(value))
                            {
                                MessageBox.Show("Enter Household ID");
                                return;
                            }

                            request.HouseholdIds = new List<string> { value };
                        }

                        // ================= ACTIVITY =================
                        else if (rdoActivity.Checked)
                        {
                            var value = txtInput.Text.Trim();

                            if (string.IsNullOrEmpty(value))
                            {
                                MessageBox.Show("Enter Activity");
                                return;
                            }

                            request.ActivityCode = value;
                        }

                        // ================= FILE =================
                        else if (rdoFile.Checked)
                        {
                            var path = txtFilePath.Text;

                            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                            {
                                MessageBox.Show("Select a valid file");
                                return;
                            }

                            var cleaned = File.ReadAllLines(path)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .Select(l => l.Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            if (cleaned.Count == 0)
                            {
                                MessageBox.Show("File is empty");
                                return;
                            }

                            request.HouseholdIds = cleaned;
                        }



                        // ================= API CALL =================


                        var apiResult = await _api.LoadQueueAsync(request);

                        if (apiResult == null || apiResult.Count == 0)
                        {
                            MessageBox.Show("No records found");
                            return;
                        }

                        // ==========================================
                        // SAVE TO SQLITE CACHE
                        // ==========================================
                        _queueCache.Save(apiResult);

                        // ==========================================
                        // LOAD FROM SQLITE CACHE
                        // ==========================================
                        var cachedItems = _queueCache.GetAll();

                        // ================= MAP + BIND =================
                        var users = QueueMapper.ToUsers(cachedItems);

                        // Get locally printed records
                        var printedIds = _printService.GetAll()
                            .Select(x => x.HouseholdId)
                            .ToHashSet();

                        // Override API results using local history
                        foreach (var user in users)
                        {
                            if (printedIds.Contains(user.HouseholdId))
                            {
                                user.CardPrinted = true;
                            }
                        }

                        BindGrid(users);
                    }
                    catch (Exception ex)
                    {
                        // ==========================================
                        // OFFLINE FALLBACK
                        // ==========================================
                        var cachedItems = _queueCache.GetAll();

                        if (cachedItems.Count == 0)
                        {
                            MessageBox.Show(
                                "Unable to load records.\n\n" +
                                ex.Message);

                            return;
                        }

                        MessageBox.Show(
                            "Offline mode: showing cached records.");

                        // ================= MAP + BIND =================
                        var users = QueueMapper.ToUsers(cachedItems);

                        // Get locally printed records
                        var printedIds = _printService.GetAll()
                            .Select(x => x.HouseholdId)
                            .ToHashSet();

                        // Override API results using local history
                        foreach (var user in users)
                        {
                            if (printedIds.Contains(user.HouseholdId))
                            {
                                user.CardPrinted = true;
                            }
                        }

                        BindGrid(users);
                    }

                    finally
                    {
                        btnLoad.Enabled = true;
                        btnLoad.Text = "LOAD RECORDS";
                    }
                };

                // ================= ADD TO PANEL =================
                searchPanel.Controls.Add(radioPanel);
                searchPanel.Controls.Add(inputPanel);
                searchPanel.Controls.Add(btnLoad);

                void ToggleInputMode()
                {
                    txtInput.Visible = !rdoFile.Checked;
                    filePanel.Visible = rdoFile.Checked;
                }

                rdoHousehold.CheckedChanged += (s, e) => ToggleInputMode();
                rdoActivity.CheckedChanged += (s, e) => ToggleInputMode();
                rdoFile.CheckedChanged += (s, e) => ToggleInputMode();

                btnBrowse.Click += (s, e) =>
                {
                    var dialog = new OpenFileDialog
                    {
                        Filter = "Text Files (*.txt)|*.txt"
                    };

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        txtFilePath.Text = dialog.FileName;
                    }
                };

                var gridsSplit = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterWidth = 5
                };

                // Top search results
                gridSearch = new Guna.UI2.WinForms.Guna2DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White
                };
                gridSearch.SelectionChanged += (s, e) =>
                {
                    if (gridSearch.CurrentRow?.DataBoundItem is User user)
                    {
                        _selectedUser = user;
                        UpdatePreview(user);
                    }
                };

                // FORCE HEADERS VISIBLE
                gridSearch.ColumnHeadersVisible = true;

                // SET HEIGHT
                gridSearch.ColumnHeadersHeight = 30;
                gridSearch.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;

                // FIX STYLE (Guna override)
                gridSearch.EnableHeadersVisualStyles = false;

                gridSearch.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(37, 99, 235);
                gridSearch.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                gridSearch.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

                // FORCE GUNA THEME HEADER
                gridSearch.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(37, 99, 235);
                gridSearch.ThemeStyle.HeaderStyle.ForeColor = Color.White;
                gridSearch.ThemeStyle.HeaderStyle.Height = 30;



                // Bottom  print history
                gridHistory = new Guna.UI2.WinForms.Guna2DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    BackgroundColor = Color.White
                };

                var lblSearchResults = new Label
                {
                    Text = "Loaded Records",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Dock = DockStyle.Top
                };

                var lblHistory = new Label
                {
                    Text = "Print History",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Dock = DockStyle.Top
                };

                // Add to split panels
                var searchContainer = new Panel { Dock = DockStyle.Fill };
                lblSearchResults.Height = 50;

                // CLEAR CACHE BUTTON
                var btnClearCache = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Release Records",
                    Width = 180,
                    Height = 40,
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(239, 68, 68), // red
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };

                btnClearCache.Click += async (s, e) =>
                {
                    var confirm = MessageBox.Show(
                        "This action will clear all locally stored records? Make sure you are connected to the internet",
                        "Confirm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.Yes)
                        return;

                    try
                    {
                        //--------------------------------------------------
                        // GET CACHED RECORDS
                        //--------------------------------------------------
                        var cachedItems = _queueCache.GetAll();

                        if (cachedItems == null || cachedItems.Count == 0)
                        {
                            MessageBox.Show(
                                "No local records found.",
                                "Info",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            return;
                        }

                        //--------------------------------------------------
                        // EXTRACT IDS
                        //--------------------------------------------------
                        var householdIds = cachedItems
                            .Select(x => x.HouseholdId)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Distinct()
                            .ToList();

                        //--------------------------------------------------
                        // CALL API UNLOCK
                        //--------------------------------------------------
                        var deviceId =
                            new DeviceService().GetDeviceId();

                        var windowsUser =
                            Environment.UserDomainName + "\\" +
                            Environment.UserName;

                        btnClearCache.Enabled = false;
                        btnClearCache.Text = "Clearing...";

                        await _api.UnlockQueueAsync(
                            householdIds,
                            deviceId,
                            windowsUser
                        );

                        //--------------------------------------------------
                        // ONLY CLEAR LOCAL CACHE IF API SUCCEEDS
                        //--------------------------------------------------
                        _queueCache.Clear();

                        //--------------------------------------------------
                        // CLEAR GRID
                        //--------------------------------------------------
                        gridSearch.DataSource = null;

                        MessageBox.Show(
                            "Records unlocked and cleared from local storage.",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        //--------------------------------------------------
                        // DO NOT CLEAR SQLITE ON FAILURE
                        //--------------------------------------------------
                        MessageBox.Show(
                            "Failed to unlock records and clear local storage. Check internet\n\n" +
                            ex.Message,
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnClearCache.Enabled = true;
                        btnClearCache.Text = "Release Records";
                    }
                };

                searchContainer.Controls.Add(gridSearch);
                searchContainer.Controls.Add(btnClearCache);
                searchContainer.Controls.Add(lblSearchResults);


                var historyContainer = new Panel { Dock = DockStyle.Fill };
                lblHistory.Height = 50;

                // SYNC BUTTON
                var btnSync = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "SYNC TO SERVER",
                    Width = 200, 
                    Height = 40,
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(16, 185, 129), 
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };

                // ADD IN CORRECT ORDER (TOP  DOWN)
                historyContainer.Controls.Add(gridHistory);
                historyContainer.Controls.Add(btnSync);
                historyContainer.Controls.Add(lblHistory);

                btnSync.Click += async (s, e) =>
                {
                    try
                    {
                        // ==========================================
                        // GET LOCAL PRINTED RECORDS
                        // ==========================================
                        var records = _printService.GetAll();

                        if (records == null || records.Count == 0)
                        {
                            MessageBox.Show(
                                "No records to sync.",
                                "Sync",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                            return;
                        }

                        var householdIds = records
                            .Select(x => x.HouseholdId)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Distinct()
                            .ToList();

                        // ==========================================
                        // CONFIRMATION
                        // ==========================================
                        var confirm = MessageBox.Show(
                            $"Sync {householdIds.Count} printed records to server?",
                            "Confirm Sync",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (confirm != DialogResult.Yes)
                            return;

                        // ==========================================
                        // PREP DATA
                        // ==========================================
                        var deviceId = new DeviceService().GetDeviceId();
                        var windowsUser = _currentUser;

                        // ==========================================
                        // CALL API
                        // ==========================================
                        btnSync.Enabled = false;
                        btnSync.Text = "Syncing...";

                        await _api.SyncPrintedAsync(householdIds, deviceId, windowsUser);

                        // ==========================================
                        // SUCCESS CLEAR LOCAL DB
                        // ==========================================
                        DbInitializer.ClearPrintedRecords();

                        BindHistoryGrid();
                        gridSearch.DataSource = null; // refresh main grid

                        MessageBox.Show(
                            $"Successfully synced {householdIds.Count} records.",
                            "Sync Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        // ==========================================
                        // FAILURE  DO NOT CLEAR LOCAL DB
                        // ==========================================
                        MessageBox.Show(
                            "Sync failed.\n\n" + ex.Message,
                            "Sync Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                    finally
                    {
                        btnSync.Enabled = true;
                        btnSync.Text = "SYNC TO SERVER";
                    }
                };

                gridsSplit.Panel1.Controls.Add(searchContainer);
                gridsSplit.Panel2.Controls.Add(historyContainer);

                this.Shown += (s, e) =>
                {
                    gridsSplit.SplitterDistance = (int)(gridsSplit.Height * 0.65);
                };

                



                gridHistory.DataSource = _printService.GetAll();




                // Add to layout
                leftLayout.Controls.Add(searchPanel, 0, 0);
                leftLayout.Controls.Add(gridsSplit, 0, 1);

                leftPanel.Controls.Add(leftLayout);

                gridSearch.CellBeginEdit += (s, e) =>
                {
                    if (gridSearch.Columns[e.ColumnIndex].Name != "Select")
                        return;

                    var row = gridSearch.Rows[e.RowIndex];
                    var user = row.DataBoundItem as User;

                    if (user == null) return;

                    //  block printed
                    if (user.CardPrinted)
                    {
                        e.Cancel = true;
                        return;
                    }

                    // block locked by others
                    if (user.IsLocked && user.LockedBy != _currentUser)
                    {
                        e.Cancel = true;
                        return;
                    }
                };


                // ================= RIGHT PANEL =================
                var rightSplit = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,

                };

                this.Shown += (s, e) =>
                {
                    rightSplit.SplitterDistance = (int)(rightSplit.Height * 0.5);
                };

                rightSplit.Resize += (s, e) =>
                {
                    rightSplit.SplitterDistance = (int)(rightSplit.Height * 0.5);
                };

                // ===== PREVIEW PANEL =====
                previewCard = new Guna.UI2.WinForms.Guna2Panel
                {
                    Dock = DockStyle.Fill,
                    BorderRadius = 12,
                    FillColor = Color.White,
                    Padding = new Padding(20),
                    ShadowDecoration = { Enabled = false }
                };

                // Layout inside card
                var previewLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2
                };

                previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));


                gridSearch.CellContentClick += (s, e) =>
                {
                    if (gridSearch.Columns[e.ColumnIndex].Name != "Select")
                        return;

                    var row = gridSearch.Rows[e.RowIndex];
                    var user = row.DataBoundItem as User;

                    if (user == null) return;

                    // BLOCK PRINTED
                    if (user.CardPrinted)
                    {
                        MessageBox.Show("Already printed");
                        row.Cells["Select"].Value = false;
                        return;
                    }

                    // BLOCK LOCKED BY OTHER USER
                    if (user.IsLocked && user.LockedBy != _currentUser)
                    {
                        MessageBox.Show($"Locked by {user.LockedBy}");
                        row.Cells["Select"].Value = false;
                        return;
                    }
                };
                // Photo
                picPhoto = new Guna.UI2.WinForms.Guna2PictureBox
                {
                    Size = new Size(120, 140),
                    BorderRadius = 8,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    FillColor = Color.LightGray
                };

                // Right info layout
                var infoLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 4
                };

                lblName = new Label
                {
                    Text = "Name:   ---",
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    AutoSize = true
                };

                lblSize = new Label
                {
                    Text = "Family Size:   ---",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DimGray,
                    AutoSize = true
                };


                lblHousehold = new Label
                {
                    Text = "HouseholdId:   ---",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DimGray,
                    AutoSize = true
                };

                lblProgram = new Label
                {
                    Text = "Program:   ---",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DimGray,
                    AutoSize = true
                };

                lblActivity = new Label
                {
                    Text = "Activity:   ---",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DimGray,
                    AutoSize = true
                };




                infoLayout.Controls.Add(lblName);
                infoLayout.Controls.Add(lblSize);
                infoLayout.Controls.Add(lblHousehold);
                infoLayout.Controls.Add(lblProgram);
                infoLayout.Controls.Add(lblActivity);
                infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                infoLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                infoLayout.Padding = new Padding(10, 5, 0, 0);

                previewLayout.Controls.Add(picPhoto, 0, 0);
                previewLayout.Controls.Add(infoLayout, 1, 0);

                previewCard.Controls.Add(previewLayout);

                // ===== ACTION PANEL =====
                var actionsCard = new Guna.UI2.WinForms.Guna2Panel
                {
                    Dock = DockStyle.Fill,
                    BorderRadius = 12,
                    FillColor = Color.White,
                    Padding = new Padding(20),
                    ShadowDecoration = { Enabled = false }
                };
                rightSplit.Panel1.Controls.Add(previewCard);
                rightSplit.Panel2.Controls.Add(actionsCard);

                var actionsLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 6
                };


                actionsLayout.RowStyles.Clear();

                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // label template
                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // dropdown template

                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // label printer
                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // dropdown printer

                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // status
                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // spacer

                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); // preview
                actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // print


                // TEMPLATE DROPDOWN
                cmbTemplates = new Guna.UI2.WinForms.Guna2ComboBox
                {
                    Dock = DockStyle.Fill,
                    BorderRadius = 8,
                    Font = new Font("Segoe UI", 10),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                //cmbTemplates.Margin = new Padding(0, 0, 0, 15);

                var templateService = new TemplateService();
                _templates = templateService.GetTemplates();

                cmbTemplates.DataSource = _templates;
                cmbTemplates.DisplayMember = "Name";

                cmbTemplates.BindingContext = new BindingContext();

                if (_templates != null && _templates.Count > 0)
                {
                    cmbTemplates.SelectedIndex = 0;
                    _selectedTemplate = (CardTemplate)cmbTemplates.SelectedItem;
                }

                cmbTemplates.SelectedIndexChanged += (s, e) =>
                {
                    _selectedTemplate = (CardTemplate)cmbTemplates.SelectedItem;
                };

                // PRINTER DROPDOWN
                cmbPrinters = new Guna.UI2.WinForms.Guna2ComboBox
                {
                    Dock = DockStyle.Fill,
                    BorderRadius = 8,
                    Font = new Font("Segoe UI", 10),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                //cmbPrinters.Margin = new Padding(0, 0, 0, 15);

                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printer);
                }

                if (cmbPrinters.Items.Count > 0)
                {
                    cmbPrinters.SelectedIndex = 0;

                }

                cmbPrinters.SelectedIndexChanged += (s, e) =>
                {
                    var printerName = cmbPrinters.SelectedItem?.ToString();

                    lblPrinter.Text = string.IsNullOrEmpty(printerName)
                        ? "None selected"
                        : printerName;
                };

                // Status
                lblStatus = new Label
                {
                    Text = "Status: Ready",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.Gray,
                    AutoSize = true
                };
                //lblStatus.Margin = new Padding(0, 0, 0, 20);


                var printerName = cmbPrinters.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(printerName))
                {
                    var status = GetPrinterStatus(printerName);
                    lblStatus.Text = $"Status: {status}";
                    lblStatus.ForeColor = status == "Ready" ? Color.Green : Color.Red;
                }

                // Print & preview buttons
                var btnPreview = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "PREVIEW CARD",
                    Dock = DockStyle.Top,
                    Height = 50,
                    BorderRadius = 10,
                    FillColor = Color.FromArgb(59, 130, 246), // blue
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };

                //btnPreview.Margin = new Padding(0, 0, 0, 10);

                var btnPrint = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "PRINT CARD",
                    Dock = DockStyle.Top,
                    Height = 50,
                    BorderRadius = 10,
                    FillColor = Color.FromArgb(16, 185, 129), // green
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };

                // TEMPLATE LABEL
                var lblTemplate = new Label
                {
                    Text = "Select Template",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.Black,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft
                };

                // PRINTER LABEL
                var lblPrinterSelect = new Label
                {
                    Text = "Select Printer",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.Black,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.BottomLeft
                };

                // Add to layout (label and dropdown pairs)
                actionsLayout.Controls.Add(lblTemplate, 0, 0);
                actionsLayout.Controls.Add(cmbTemplates, 0, 1);

                actionsLayout.Controls.Add(lblPrinterSelect, 0, 2);
                actionsLayout.Controls.Add(cmbPrinters, 0, 3);

                actionsLayout.Controls.Add(lblStatus, 0, 4);
                actionsLayout.Controls.Add(new Label(), 0, 5); // spacer
                actionsLayout.Controls.Add(btnPreview, 0, 6);
                actionsLayout.Controls.Add(btnPrint, 0, 7);

                actionsCard.Controls.Add(actionsLayout);

                btnPreview.Click += (s, e) =>
                {

                    try
                    {
                        var users = GetCheckedUsers()
                            .Where(u =>
                                !u.CardPrinted &&
                                (!u.IsLocked || u.LockedBy == _currentUser)
                            )
                            .ToList();

                    if (users.Count == 0)
                    {
                        MessageBox.Show("Select at least one record");
                        return;
                    }

                    _previewUsers = users;
                    _previewIndex = 0;

                    var previewDialog = new PrintPreviewDialog
                    {
                        Document = _printDocument,
                        Width = 1000,
                        Height = 700
                    };

                        var toolStrip = previewDialog.Controls
                            .OfType<ToolStrip>()
                            .FirstOrDefault();

                        if (toolStrip != null)
                        {
                            foreach (ToolStripItem item in toolStrip.Items)
                            {
                                if (item.ToolTipText == "Print")
                                {
                                    item.Visible = false;
                                }
                            }
                        }

                        previewDialog.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                };

                btnPrint.Click += (s, e) =>
                {
                    var users = GetCheckedUsers();

                    if (users.Count == 0)
                    {
                        MessageBox.Show("Select at least one record");
                        return;
                    }

                    var printerName = cmbPrinters.SelectedItem?.ToString();

                    if (string.IsNullOrEmpty(printerName))
                    {
                        MessageBox.Show("Select a printer");
                        return;
                    }

                    var confirm = MessageBox.Show($"Print {users.Count} cards?", "Confirm", MessageBoxButtons.YesNo);

                    if (confirm != DialogResult.Yes) return;

                    try
                    {
                        _previewUsers = users;
                        _previewIndex = 0;

                        _printDocument.PrinterSettings.PrinterName = printerName;

                        // This handles ALL pages internally
                        _printDocument.Print();

                        // Save records separately
                        foreach (var user in users)
                        {
                            _userService.MarkAsPrinted(user.HouseholdId);

                            _printService.Save(
                                        user,
                                        _selectedTemplate?.Name,
                                        printerName
                                    );

                            // ==========================================
                            // UPDATE SQLITE CACHE
                            // ==========================================
                            _queueCache.MarkAsPrinted(
                                user.HouseholdId);

                            user.CardPrinted = true;

                            //UNCHECK GRID AFTER PRINTING

                            foreach (DataGridViewRow row in gridSearch.Rows)
                            {
                                if (row.DataBoundItem is User u && u.HouseholdId == user.HouseholdId)
                                {
                                    row.Cells["Select"].Value = false;
                                    break;
                                }
                            }
                        }

                        //  REFRESH USER GRID
                        gridSearch.Refresh();

                        // Refresh history 
                        gridHistory.DataSource = _printService.GetAll();

                        MessageBox.Show("Batch print completed");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Print failed: " + ex.Message);
                    }
                };


                this.Controls.Add(mainSplit);
                this.Controls.Add(header);
                printerName = cmbPrinters.SelectedItem?.ToString();

                lblPrinter.Text = string.IsNullOrEmpty(printerName)
                    ? "None selected"
                    : printerName;
                mainSplit.Panel1.Controls.Add(leftPanel);
                mainSplit.Panel2.Controls.Add(rightSplit);

                this.ResumeLayout(true);
            }
        }





        private Control BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = ColorTranslator.FromHtml("#002D72"),
                Padding = new Padding(20, 10, 20, 10)
            };

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = false 
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            // ================= LEFT =================
            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = false
            };

            // Columns: logo and text
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Rows: title and subtitle
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

            // LOGO
            var picLogo = new PictureBox
            {
                Size = new Size(120, 120),
                SizeMode = PictureBoxSizeMode.Zoom,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 10, 5)
            };

            picLogo.Image = Properties.Resources.brave_logo;

            // TITLE
            var lblTitle = new Label
            {
                Text = "BRaVe Card Printing App",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            // SUBTITLE
            var lblSub = new Label
            {
                Text = "Print Beneficiary Cards",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };

            // Add controls
            leftLayout.Controls.Add(picLogo, 0, 0);
            leftLayout.SetRowSpan(picLogo, 2); 

            leftLayout.Controls.Add(lblTitle, 1, 0);
            leftLayout.Controls.Add(lblSub, 1, 1);

            // ================= RIGHT =================
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                AutoSize = false
            };

            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var lblSelected = new Label
            {
                Text = "Selected Printer",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

             lblPrinter = new Label
            {
                Text = "None selected",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold), 
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };


            rightLayout.Controls.Add(lblSelected, 0, 0);
            //rightLayout.Controls.Add(btnDiscover, 1, 0);
            rightLayout.Controls.Add(lblPrinter, 0, 1);
            //rightLayout.Controls.Add(btnManual, 1, 1);

            mainLayout.Controls.Add(leftLayout, 0, 0);
            mainLayout.Controls.Add(rightLayout, 1, 0);

            header.Controls.Add(mainLayout);

            return header;
        }
    }
}