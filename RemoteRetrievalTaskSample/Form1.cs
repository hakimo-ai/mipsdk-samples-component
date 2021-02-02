﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using VideoOS.Platform.SDK.RemoteRetrievalTasks;
using VideoOS.Platform;
using VideoOS.Platform.UI;

namespace RemoteRetrievalTaskSample
{
	public partial class Form1 : Form
	{
		RetrievalTaskManager _manager;
		IList<RetrievalTask> _tasks;
		Guid _selectedCamera;
		public Form1()
		{
			InitializeComponent();
			dtpStartTime.CustomFormat = CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
			dtpEndTime.CustomFormat = CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			try
			{
                _manager = new RetrievalTaskManager(EnvironmentManager.Instance.MasterSite);
			    _manager.Connect();
				btnConnect.Enabled = false;
                btnSelectCamera.Enabled = true;
			}
			catch (NotAuthorizedMIPException ex)
			{
				// The was a problem with login credentials. Check user name and password
				lblLoginError.Text = ex.Message;
			}
            catch (MIPException ex)
            {
                // There was a problem contacting management server. Check the address.
                lblLoginError.Text = ex.Message;
            }
        }

		private void btnRetrieve_Click(object sender, EventArgs e)
		{
			DateTime utcStartTime = dtpStartTime.Value.ToUniversalTime();
			DateTime utcEndTime = dtpEndTime.Value.ToUniversalTime();
			try
			{
				var task = _manager.Retrieve(_selectedCamera, utcStartTime, utcEndTime);
                if (_tasks == null)
                {
                    _tasks = new List<RetrievalTask>();
                }
                _tasks.Add(task);
                dataGridTasks.DataSource = null;
                dataGridTasks.AutoGenerateColumns = false;
                dataGridTasks.DataSource = _tasks;
                dataGridTasks.Rows[dataGridTasks.Rows.Count - 1].Selected = true;
            }
            catch (UnauthorizedAccessException ex)
			{
				// User doesn't have rights to retrieve.
				lblRetrieveError.Text = ex.Message;
			}
			catch (ArgumentException ex)
			{
				// Device does not exist or does not support remote recordings or
				// Start time is before end time.
				lblRetrieveError.Text = ex.Message;
			}
			catch (InvalidOperationException ex)
			{
				// Not logged in.
				lblRetrieveError.Text = ex.Message;
			}
		}

		private void btnGet_Click(object sender, EventArgs e)
		{
			lblRetrieveError.Text = string.Empty;
			try
			{
				_tasks = _manager.GetTasks();
				dataGridTasks.AutoGenerateColumns = false;
				dataGridTasks.DataSource = _tasks;
			}
			catch (InvalidOperationException ex)
			{
				// Not logged in.
				lblRetrieveError.Text = ex.Message;
			}
			
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			lblRetrieveError.Text = string.Empty;
			try
			{
				_manager.RefreshTasks();
				dataGridTasks.Refresh();
			}
			catch (InvalidOperationException ex)
			{
				// Not logged in.
				lblRetrieveError.Text = ex.Message;
			}
		}

		private void dataGridTasks_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex != dataGridTasks.Columns["Cancel"].Index) {
				return;
			}
			try
			{
				_tasks[e.RowIndex].Stop();
			}
			catch (UnauthorizedAccessException ex)
			{
				// User doesn't have rights to stop retrieval.
				lblRetrieveError.Text = ex.Message;
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			ItemPickerForm form = new ItemPickerForm();
			form.KindFilter = Kind.Camera;
			form.AutoAccept = true;
			form.Init(Configuration.Instance.GetItems());
			if (form.ShowDialog() == DialogResult.OK)
			{
				txtDevice.Text = form.SelectedItem.Name;
				_selectedCamera = form.SelectedItem.FQID.ObjectId;

                btnGet.Enabled = true;
                btnRefresh.Enabled = true;
                btnRetrieve.Enabled = true;
            }
        }
	}
}
