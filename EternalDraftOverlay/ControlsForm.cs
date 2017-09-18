using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EternalDraftOverlay
{
    public partial class ControlsForm : Form
    {
        public ControlsForm()
        {
            InitializeComponent();
        }

        private void rescanButton_Click(object sender, EventArgs e)
        {
            if(Application.OpenForms["yourForm"] != null)
            {
                (Application.OpenForms["yourForm"] as Overlay).RescanPage();
            }
        }
    }
}
