namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Theme.Setup(this, "UIT Chat • NT106", new Size(720, 460));
            FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
            var brand = Theme.Label("uit / chat", 32, Theme.Teal, true); brand.Location = new Point(48, 42);
            var title = Theme.Label("Kết nối nhóm bạn của bạn.", 23, bold: true); title.Location = new Point(48, 125);
            var desc = Theme.Label("Ứng dụng nhắn tin desktop cho môn Lập trình mạng căn bản.\nKhởi động server trước, sau đó mở các client để trò chuyện.", 11, Theme.Muted); desc.Location = new Point(50, 184);
            var server = Theme.Button("Quản lý server", false, 245); server.Location = new Point(50, 270); server.Height = 54;
            var client = Theme.Button("Mở client  →", true, 245); client.Location = new Point(315, 270); client.Height = 54;
            ServerForm? serverForm = null;
            server.Click += (_, _) => { if (serverForm is null || serverForm.IsDisposed) serverForm = new ServerForm(); serverForm.Show(); serverForm.Activate(); };
            client.Click += (_, _) => new LoginForm().Show();
            var footer = Theme.Label("C# WINFORMS     /     TCP + TLS     /     REALTIME", 9, Theme.Muted, true); footer.Location = new Point(50, 385);
            Controls.AddRange([brand, title, desc, server, client, footer]);
            FormClosing += (_, e) =>
            {
                if (Application.OpenForms.Count > 1 && MessageBox.Show(this, "Thoát ứng dụng và đóng các cửa sổ client/server?", "UIT Chat", MessageBoxButtons.YesNo) != DialogResult.Yes) e.Cancel = true;
            };
        }
    }
}
