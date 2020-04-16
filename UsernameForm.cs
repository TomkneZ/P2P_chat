using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace P2P_Chat_on_Sockets
{
    public partial class UsernameForm : Form
    {
        public UsernameForm()
        {
            InitializeComponent();
        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            if (tbUsername.Text.Length > 0)
            {
                btnConnect.Enabled = true;
            }
            else
            {
                btnConnect.Enabled = false;
            }

        }
    }
}
