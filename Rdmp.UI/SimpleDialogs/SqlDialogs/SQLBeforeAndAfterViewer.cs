// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.UI.ScintillaHelper;
using ScintillaNET;

namespace Rdmp.UI.SimpleDialogs.SqlDialogs;

/// <summary>
/// Shows two pieces of SQL and the differences between them.  This is used by the RDMP for example to show you what the audited extraction SQL for a dataset was and what you 
/// last extracted it (e.g. before the weekend) and what the active configuration looks like today (e.g. if somebody snuck in a couple of extra columns into a data extraction
/// after the extract file had already been generated).
/// </summary>
public partial class SQLBeforeAndAfterViewer : Form
{
    private Scintilla QueryEditorBefore;
    private Scintilla QueryEditorAfter;


    public SQLBeforeAndAfterViewer(string sqlBefore, string sqlAfter, string headerTextForBefore,
        string headerTextForAfter, string caption, MessageBoxButtons buttons,
        SyntaxLanguage language = SyntaxLanguage.SQL)
    {
        InitializeComponent();

        var designMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        if (designMode) //don't add the QueryEditor if we are in design time (visual studio) because it breaks
            return;

        QueryEditorBefore = new ScintillaTextEditorFactory().Create();
        QueryEditorBefore.Text = sqlBefore;
        QueryEditorBefore.ReadOnly = true;

        splitContainer1.Panel1.Controls.Add(QueryEditorBefore);


        QueryEditorAfter = new ScintillaTextEditorFactory().Create();
        QueryEditorAfter.Text = sqlAfter;
        QueryEditorAfter.ReadOnly = true;

        splitContainer1.Panel2.Controls.Add(QueryEditorAfter);


        //compute difference
        var highlighter = new ScintillaLineHighlightingHelper();
        ScintillaLineHighlightingHelper.ClearAll(QueryEditorAfter);
        ScintillaLineHighlightingHelper.ClearAll(QueryEditorBefore);

        sqlBefore ??= "";
        sqlAfter ??= "";

        var diff = new Diff();

        foreach (var item in Diff.DiffText(sqlBefore, sqlAfter))
        {
            for (var i = item.StartA; i < item.StartA + item.deletedA; i++)
                ScintillaLineHighlightingHelper.HighlightLine(QueryEditorBefore, i, Color.Pink);

            for (var i = item.StartB; i < item.StartB + item.insertedB; i++)
                ScintillaLineHighlightingHelper.HighlightLine(QueryEditorAfter, i, Color.LawnGreen);
        }

        switch (buttons)
        {
            case MessageBoxButtons.OK:
                btnYes.Visible = true;
                btnYes.Text = "Ok";
                btnNo.Visible = false;
                break;
            case MessageBoxButtons.YesNo:
                btnYes.Visible = true;
                btnNo.Visible = true;
                break;
            default:
                throw new NotSupportedException("buttons");
        }

        lblBefore.Text = headerTextForBefore;
        lblAfter.Text = headerTextForAfter;

        Text = caption;
    }

    private void btnYes_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Yes;
        Close();
    }

    private void btnNo_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.No;
        Close();
    }
}