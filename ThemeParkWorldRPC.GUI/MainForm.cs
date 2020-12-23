using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace ThemeParkWorldRPC.GUI
{
    public partial class MainForm : Form
    {
        private Thread bgThread;
        private delegate void SafeCallDelegate();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            bgThread = new Thread(TpwThread);
            bgThread.Start();
        }

        private void TpwThread()
        {
            var tpwRpc = new TpwRpc();
            tpwRpc.onMessage += (s, eventArgs) =>
            {
                // Needs to be run on main WinForms thread
                statusLabel.Invoke(new SafeCallDelegate(() =>
                {
                    statusLabel.Text = eventArgs.Message;
                }));
            };
            tpwRpc.Run();
        }
    }
}
