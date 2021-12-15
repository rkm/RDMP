﻿// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.CommandExecution;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rdmp.UI.SimpleDialogs
{
    public partial class TaskDescriptionLabel : UserControl
    {
        public TaskDescriptionLabel()
        {
            InitializeComponent();
        }

        public void SetupFor(DialogArgs args)
        {
            var task = args.TaskDescription;
            var entryLabel = args.EntryLabel;

            tbTaskDescription.Visible = !string.IsNullOrWhiteSpace(task);
            tbTaskDescription.Text = task;

            tbEntryLabel.Visible = !string.IsNullOrWhiteSpace(entryLabel);

            if (entryLabel != null && entryLabel.Length > WideMessageBox.MAX_LENGTH_BODY)
                entryLabel = entryLabel.Substring(0, WideMessageBox.MAX_LENGTH_BODY);

            // set prompt text. If theres a TaskDescription too then leave a bit of extra space
            this.tbEntryLabel.Text = !string.IsNullOrWhiteSpace(task) ? Environment.NewLine + entryLabel : entryLabel;

            this.Height = (!string.IsNullOrWhiteSpace(entryLabel) ? tbEntryLabel.Height : 0) + 
                          (!string.IsNullOrWhiteSpace(task) ? tbTaskDescription.Height : 0);
        }

        /// <summary>
        /// Returns the width this control would ideally like to take up
        /// </summary>
        public int PreferredWidth => Math.Max(tbEntryLabel.Width, tbTaskDescription.Width);

        private void textBox1_Resize(object sender, EventArgs e)
        {
            SizeF MessageSize = tbTaskDescription.CreateGraphics()
                                            .MeasureString(tbTaskDescription.Text,
                                                            tbTaskDescription.Font,
                                                            tbTaskDescription.Width,
                                                            new StringFormat(0));
            tbTaskDescription.Height = (int)MessageSize.Height + 3;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbEntryLabel_Resize(object sender, EventArgs e)
        {
            SizeF MessageSize = tbEntryLabel.CreateGraphics()
                                            .MeasureString(tbEntryLabel.Text,
                                                            tbEntryLabel.Font,
                                                            tbEntryLabel.Width,
                                                            new StringFormat(0));
            tbEntryLabel.Height = (int)MessageSize.Height + 3;
        }
    }
}
