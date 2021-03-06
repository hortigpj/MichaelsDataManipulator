﻿using MathNet.Numerics.Statistics;
using MichaelsDataManipulator.SimplotDatabaseDataSetTableAdapters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.Charting;
using Telerik.WinControls.UI;
using UnitsNet;

namespace MichaelsDataManipulator
{
    public partial class Form1 : Form
    {

        string logfile = @"\\prod\root\S_Drive\USGR-Shared\SimPlot DATA\logfile.txt";

        bool ignore_events = true;

        bool gridview_cell_value_changed = false;
        
        Form_Wait wait_form = new Form_Wait();

        public int allow_for_1_event_per_seconds
        {
            get
            {
                return (int)radSpinEditor_allow_for_1_event_per_seconds.Value;
            }
        }


        public double? current_event_time
        {
            get
            {
                if (current_datafile != null)
                {

                    return double.Parse(current_datafile.time_data[current_file_index]);
                }

                return null;
            }
        }

        public double LocalStdDevTrigger
        {
            get
            {
                return (double)radSpinEditor_local_std_dev_trigger.Value;
            }
        }

        public CDataFile.SCAN_TYPE current_scan_type_for_viewing
        {
            get
            {
                return (CDataFile.SCAN_TYPE)radDropDownList_scan_type_for_viewing.SelectedItem.Tag;
            }
        }



        public double SamplingRate
        {
            get
            {
                return (double)radSpinEditor_sampling_rate.Value;
            }
        }


        public bool DoFFT
        {
            get
            {
                return radCheckBox_do_fft.Checked;
            }
        }


        public DateTime IgnoreBeforeDate
        {
            get
            {
                return radDateTimePicker_ignore_before_date.Value;
            }
        }


        public double running_average_length_in_seconds
        {
            get
            {
                return (double)radSpinEditor_running_avr_size.Value;
            }
        }

        public double local_standard_deviation_length_in_seconds
        {
            get
            {
                return (double)radSpinEditor_local_standard_deviation_size.Value;
            }
        }



        public double speed_drop_in_percent
        {
            get
            {
                return (double)radSpinEditor_running_avr_decrease_trigger_value.Value * 0.01;
            }
        }


        public double spike_filter_accel
        {
            get
            {
                return (double)radSpinEditor_spike_filter_trigger_accel.Value * 9.81;

            }
            set
            {
                radSpinEditor_spike_filter_trigger_accel.Value = (decimal)(value / 9.81);
            }
        }
        public double low_pass_filter_frequency
        {
            get
            {
                return (double)radSpinEditor_low_pass_filter_frequency.Value;

            }
            set
            {
                radSpinEditor_low_pass_filter_frequency.Value = (decimal)value;
            }
        }



        public CDataFile current_datafile
        {
            get; set;
        }


        public int event_file_index
        {
            get
            {
                if (current_datafile != null)
                {
                    int index = (int)this.radGridView_data.CurrentRow.Cells["EVENT_INDEX"].Value;

                    if (index >= 0 && index < current_datafile.n_of_good_data_points)
                    {
                        return index;
                    }
                }

                return -1;
            }
        }

        public int current_file_index
        {
            get
            {
                decimal v = (decimal)(radHScrollBar_index.Value / SamplingRate);

                if (v < radSpinEditor_position.Minimum)
                {
                    v = radSpinEditor_position.Minimum;
                }
                if (v > radSpinEditor_position.Maximum)
                {
                    v = radSpinEditor_position.Maximum;
                }



                radSpinEditor_position.Value =v;
                //radTextBox_position.Text = (radHScrollBar_index.Value * 0.01).ToString();

                return (int)radHScrollBar_index.Value;
            }
            set
            {
                radHScrollBar_index.Maximum = current_datafile.n_of_good_data_points;
                radHScrollBar_index.Minimum = 0;

                if (value > radHScrollBar_index.Maximum)
                {
                    value = radHScrollBar_index.Maximum;
                }

                if (value < radHScrollBar_index.Minimum)
                {
                    value = radHScrollBar_index.Minimum;
                }

                radHScrollBar_index.Value = value;

                decimal v = (decimal)(value / SamplingRate);

                if (v < radSpinEditor_position.Minimum)
                {
                    v = radSpinEditor_position.Minimum;
                }
                if (v > radSpinEditor_position.Maximum)
                {
                    v = radSpinEditor_position.Maximum;
                }
                
                radSpinEditor_position.Value = v;
                
            }
        }



        public List<string> drive_letters
        {
            get
            {
                List<string> list = new List<string>();

                foreach (RadCheckedListDataItem item in radCheckedDropDownList_drives.Items)
                {
                    if (item.Checked)
                    {
                        list.Add(item.Text);
                    }
                }

                return list;
            }
        }


        LinearAxis horizontalAxis_seconds;
        LinearAxis horizontalAxis_minutes;


        LinearAxis verticalAxis_speed_in_ft_min;
        LinearAxis verticalAxis_frequency_in_Hz;
        LinearAxis verticalAxis_std_dev;


        public double delta_t_min
        {
            get
            {
                return (double)radSpinEditor_delta_t_min.Value * 60;
            }
        }
        public double delta_t_max
        {
            get
            {
                return (double)radSpinEditor_delta_t_max.Value * 60;
            }
        }


        public int chart_sample_size
        {
            get;
            set;
        } = 10 * 100;


        public Form1()
        {
            

            Application.DoEvents();

            InitializeComponent();

            CartesianArea area = this.radChartView_speed_over_time.GetArea<CartesianArea>();

            area.ShowGrid = true;
            CartesianGrid grid = area.GetGrid<CartesianGrid>();
            grid.DrawHorizontalStripes = true;
            grid.DrawVerticalStripes = true;

            area.Axes.Clear();

            horizontalAxis_seconds = new LinearAxis();
            horizontalAxis_seconds.LabelFitMode = AxisLabelFitMode.MultiLine;


            horizontalAxis_seconds.Title = "Time [sec]";

            horizontalAxis_seconds.Minimum = 0;
            horizontalAxis_seconds.Maximum = 1;

            horizontalAxis_seconds.MajorStep = 1;


            area.Axes.Add(horizontalAxis_seconds);


            horizontalAxis_minutes = new LinearAxis();
            horizontalAxis_minutes.LabelFitMode = AxisLabelFitMode.MultiLine;


            horizontalAxis_minutes.Title = "Time [min]";

            horizontalAxis_minutes.Minimum = 0;
            horizontalAxis_minutes.Maximum = 1;

            horizontalAxis_minutes.MajorStep = 1;


            area.Axes.Add(horizontalAxis_minutes);


            verticalAxis_speed_in_ft_min = new LinearAxis();
            verticalAxis_speed_in_ft_min.AxisType = AxisType.Second;
            verticalAxis_speed_in_ft_min.HorizontalLocation = AxisHorizontalLocation.Left;
            verticalAxis_speed_in_ft_min.Title = "Speed [ft/min]";
            verticalAxis_speed_in_ft_min.Minimum = 0;
            verticalAxis_speed_in_ft_min.Maximum = 200;

            verticalAxis_frequency_in_Hz = new LinearAxis();
            verticalAxis_frequency_in_Hz.AxisType = AxisType.Second;
            verticalAxis_frequency_in_Hz.HorizontalLocation = AxisHorizontalLocation.Right;
            verticalAxis_frequency_in_Hz.Title = "Frequency [Hz]";
            verticalAxis_frequency_in_Hz.Minimum = 0;
            verticalAxis_frequency_in_Hz.Maximum = 50;

            verticalAxis_std_dev = new LinearAxis();
            verticalAxis_std_dev.AxisType = AxisType.Second;
            verticalAxis_std_dev.HorizontalLocation = AxisHorizontalLocation.Right;
            verticalAxis_std_dev.Title = "Std.Dev. [ft/min]";
            verticalAxis_std_dev.Minimum = 0;
            verticalAxis_std_dev.Maximum = 100;

            area.Axes.Add(verticalAxis_speed_in_ft_min);
            area.Axes.Add(verticalAxis_frequency_in_Hz);
            area.Axes.Add(verticalAxis_std_dev);

            RadMenuItem radmenuitem_std_dev_scan = new RadMenuItem("Std. Dev. Scan");
            radmenuitem_std_dev_scan.Click += Radmenuitem_std_dev_scan_Click;

            RadMenuItem radmenuitem_running_average_scan = new RadMenuItem("Run. Avr. Scan");
            radmenuitem_running_average_scan.Click += Radmenuitem_running_average_scan_Click;

            RadMenuItem radmenuitem_multiple_running_average_scan = new RadMenuItem("Multiple Run. Avr. Scan");
            radmenuitem_multiple_running_average_scan.Click += Radmenuitem_multiple_running_average_scan_Click;

            RadMenuItem radmenuitem_max_speed_scan = new RadMenuItem("Max. Speed Scan");
            radmenuitem_max_speed_scan.Click += Radmenuitem_max_speed_scan_Click;


            radDropDownButton_scans.Items.Add(radmenuitem_std_dev_scan);
            radDropDownButton_scans.Items.Add(radmenuitem_running_average_scan);
            radDropDownButton_scans.Items.Add(radmenuitem_multiple_running_average_scan);
            radDropDownButton_scans.Items.Add(radmenuitem_max_speed_scan);

            // add range buttons

            string[] ranges = { "1s", "2s", "5s", "10s", "20s", "1m", "2m", "5m", "10m", "1h" };

            foreach (string range in ranges)
            {
                RadButton range_button = new RadButton();
                range_button.Text = range;
                range_button.Click += Range_button_Click; 
                flowLayoutPanel_range_buttons.Controls.Add(range_button);
            }

            foreach (CDataFile.SCAN_TYPE scan_type in Enum.GetValues(typeof(CDataFile.SCAN_TYPE)))
            {
                RadListDataItem item = new RadListDataItem(scan_type.ToString());
                radDropDownList_scan_type_for_viewing.Items.Add(item);
                item.Tag = scan_type;
            }

            radDropDownList_scan_type_for_viewing.SelectedIndex = 1;

            // add video player



            /// tests
            /// 


        }

        private void Radmenuitem_max_speed_scan_Click(object sender, EventArgs e)
        {
            Scan(CDataFile.SCAN_TYPE.MAX_SPEED, false);
        }

        private void Range_button_Click(object sender, EventArgs e)
        {
            if (ignore_events)
                return;

            RadButton button = sender as RadButton;

            if (button != null)
            {
                Duration t = Duration.Parse(button.Text);

                chart_sample_size = (int)(t.Seconds * SamplingRate);

                if (chart_sample_size>0 && chart_sample_size < 360000)
                {
                    radHScrollBar_index.LargeChange = chart_sample_size;
                    radSpinEditor_position.Increment = chart_sample_size;
                }

                UpdateChart();
            }
        }

        private void UpdateChart()
        {
            if (current_datafile != null)
            {
                ChartLogic();
                current_datafile.ChartData(radChartView_speed_over_time, horizontalAxis_seconds, verticalAxis_speed_in_ft_min, verticalAxis_frequency_in_Hz, verticalAxis_std_dev,current_file_index - chart_sample_size / 2, chart_sample_size);

                userControl_Spectrum1.datafile = current_datafile;
                userControl_Spectrum1.index_from = current_file_index - chart_sample_size / 2;
                userControl_Spectrum1.index_to = userControl_Spectrum1.index_from + chart_sample_size;

                Application.DoEvents();
            }
        }


        private void Radmenuitem_multiple_running_average_scan_Click(object sender, EventArgs e)
        {
            Scan(CDataFile.SCAN_TYPE.MULTIPLE_RUNNING_AVR, true);
            //MultipleRunningAverageScanParallel();
        }


        private void Radmenuitem_running_average_scan_Click(object sender, EventArgs e)
        {
        }

        private void Radmenuitem_std_dev_scan_Click(object sender, EventArgs e)
        {
            Scan(CDataFile.SCAN_TYPE.STD_DEV, true);
        }


        private double FeetPerMinuteToMetersPerSecond(double ft_per_min)
        {
            return Speed.FromFeetPerSecond(ft_per_min / 60).MetersPerSecond;
        }

        private void radGridView_events_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'eventDatabaseDataSet.Events' table. You can move, or remove it, as needed.

            ignore_events = true;

            // do the sql stuff

            //string strConnection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=\\\\prod\\root\\S_Drive\\USGR - Shared\\SimPlot DATA\\SimplotDatabase.accdb";
            //string strConnection = "Data Source=\\\\prod\\root\\S_Drive\\USGR - Shared\\SimPlot DATA\\SimplotDatabase.accdb";

            //SqlConnection con = new SqlConnection(strConnection);


            //SqlCommand sqlCmd = new SqlCommand();
            //sqlCmd.Connection = con;
            //sqlCmd.CommandType = CommandType.Text;
            //sqlCmd.CommandText = "Select * from data";

            //SqlDataAdapter sqlDataAdap = new SqlDataAdapter(sqlCmd);

            //DataTable dtRecord = new DataTable();
            //sqlDataAdap.Fill(dtRecord);
            //radGridView_data.DataSource = dtRecord;



            SimplotDatabaseDataSetTableAdapters.dataTableAdapter a = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();
            a.Fill(simplotDatabaseDataSet.data);


            List<string> drives = GetAvailableDriveLetters();

            foreach (string drive in drives)
            {
                radCheckedDropDownList_drives.Items.Add(drive);

                if (drive.Contains("E:"))
                {
                    (radCheckedDropDownList_drives.Items.Last as RadCheckedListDataItem).Checked = true;
                }
                if (drive.Contains("F:"))
                {
                    (radCheckedDropDownList_drives.Items.Last as RadCheckedListDataItem).Checked = true;
                }
                if (drive.Contains("G:"))
                {
                    (radCheckedDropDownList_drives.Items.Last as RadCheckedListDataItem).Checked = true;
                }

            }
            ignore_events = false;

            Application.DoEvents();


        }

        private void ListVideo(double? event_time, CDataFile datafile)
        {
            ignore_events = true;

            if (event_time.HasValue && datafile != null)
            {
                axWindowsMediaPlayer1.URL = "";

                this.radListView_video.Items.Clear();

                this.radSpinEditor_event_time_code.Value = (decimal)event_time;

                string path = Path.GetPathRoot(datafile.filename);

                List<string> all_file_names = new List<string>();

                Form_searching_for_video_files search_form = new Form_searching_for_video_files();

                if (drive_letters.Count > 0)
                {

                    search_form.radProgressBar_search_for_video_files_drive_letters.Maximum = drive_letters.Count - 1;

                    search_form.Show();

                    int n_letters = 0;
                    foreach (string drive_letter in drive_letters)
                    {
                        try
                        {
                            string[] file_names = Directory.GetFiles(drive_letter + "\\DATA", "*.mp4", SearchOption.AllDirectories);
                            all_file_names.AddRange(file_names.ToList<string>());

                            search_form.radProgressBar_search_for_video_files_drive_letters.Text = "scanning drive letter: " + drive_letter;
                            search_form.radProgressBar_search_for_video_files_drive_letters.Value1 = ++n_letters;

                           Application.DoEvents();
                        }
                        catch (Exception ex)
                        { }
                    }

                    int n_files = 0;

                    search_form.radProgressBar_search_for_video_files_files.Maximum = all_file_names.Count;

                    foreach (string video_file in all_file_names)
                    {
                        if (n_files % 10 == 0)
                        {
                            search_form.radProgressBar_search_for_video_files_files.Text = "scanning file " + n_files.ToString() + " of " + all_file_names.Count().ToString();
                            search_form.radProgressBar_search_for_video_files_files.Value1 = ++n_files;

                            Application.DoEvents();
                        }

                        if (video_file.Contains(datafile.conveyor))
                        {
                            string fn = Path.GetFileNameWithoutExtension(video_file);

                            string[] subs = fn.Split('-');

                            if (subs.GetUpperBound(0) == 5)
                            {
                                string time_code = subs[subs.GetUpperBound(0)];

                                time_code = time_code.Substring(0, 10);

                                double start_time_of_video = double.Parse(time_code);

                                double time_min = event_time.Value + delta_t_min;
                                double time_max = event_time.Value + delta_t_max;



                                if (start_time_of_video > time_min && start_time_of_video < time_max)
                                {
                                    double event_delta_t = event_time.Value - start_time_of_video;

                                    TimeSpan event_delta_time_span = new TimeSpan(0, 0, (int)event_delta_t);

                                    ListViewDataItem item = new ListViewDataItem();
                                    this.radListView_video.Items.Add(item);
                                    item.Text = video_file;

                                    item[0] = video_file;
                                    item[1] = start_time_of_video.ToString();
                                    item[2] = event_delta_time_span.ToString();

                                    if (video_file.Contains("AXIS - P1425-LE - 1"))
                                    {
                                        item[3] = "Camera 1";
                                    }
                                    if (video_file.Contains("AXIS - P1425-LE - 2"))
                                    {
                                        item[3] = "Camera 2";
                                    }
                                    if (video_file.Contains("AXIS - P1425-LE - 3"))
                                    {
                                        item[3] = "Camera 3";
                                    }


                                }
                            }
                        }
                    }
                    search_form.Hide();
                }
            }


            ignore_events = false;
        }

        private void radSpinEditor_delta_t_min_ValueChanged(object sender, EventArgs e)
        {
            ListVideo(current_event_time, current_datafile);
        }

        private void radSpinEditor_delta_t_max_ValueChanged(object sender, EventArgs e)
        {
            ListVideo(current_event_time, current_datafile);
        }

        private void radListView_video_DoubleClick(object sender, EventArgs e)
        {
            if (ignore_events)
                return;

            if (radListView_video.SelectedIndex >= 0)
            {

                ListViewDataItem item = radListView_video.Items[radListView_video.SelectedIndex];

                string filename = item.Text;

                Process.Start(filename);
            }
        }

        private List<string> GetAvailableDriveLetters()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            List<string> list = new List<string>();

            foreach (DriveInfo d in allDrives)
            {
                list.Add(d.Name);
            }

            return list;
        }


        void LocalStdDevScan()
        {
            string[] file_names = Directory.GetFiles(@"\\prod\root\S_Drive\USGR-Shared\SimPlot DATA\DATA", "*.bin", SearchOption.AllDirectories);

            List<string> fn = new List<string>();
            fn.AddRange(file_names);

            fn.Sort();

            file_names = fn.ToArray();

            double min_start_time_code = double.MaxValue;
            double max_end_time_code = double.MinValue;

            double min_max_time_span_in_seconds = 0;

            long counted_data_points = 0;
            long expected_data_points = 0;

            ignore_events = true;

            float i = 0;

            long aver_ticks = 0;
            long old_ticks = 0;

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            long start_ticks = stopwatch.ElapsedMilliseconds;

            foreach (string file_name in file_names)
            {
                Debug.WriteLine("reading:" + file_name);

                long now_ticks = stopwatch.ElapsedMilliseconds;

                long ticks_passed = now_ticks - old_ticks;

                long ticks_passed_total = now_ticks - start_ticks;

                old_ticks = now_ticks;

                aver_ticks += ticks_passed;

                aver_ticks /= 2;

                long ticks_total = aver_ticks * file_names.Count();

                long ticks_left = ticks_total - ticks_passed_total;

                if (ticks_left < 0)
                    ticks_left = 0;

                Duration time_left = Duration.FromMilliseconds((double)(ticks_left));
                Duration time_per = Duration.FromMilliseconds((double)(ticks_passed));


                CDataFile data_file = new CDataFile(file_name, radCheckBox_spike_filter.Checked, spike_filter_accel,
                    radCheckBox_low_pass_filter.Checked, low_pass_filter_frequency,
                    running_average_length_in_seconds, local_standard_deviation_length_in_seconds, LocalStdDevTrigger,
                    speed_drop_in_percent, false, logfile, CDataFile.SCAN_TYPE.STD_DEV
                    , DoFFT, allow_for_1_event_per_seconds,null);

                float progress = ++i / (float)file_names.Count();

                radProgressBar_ReadData.Value1 = (int)(progress * 100);
                radProgressBar_ReadData.Text = i.ToString() + "/" + file_names.Count().ToString() + " "
                    + ((int)time_left.ToTimeSpan().TotalMinutes).ToString() + " min left" +
                    " per : " + ((int)time_per.ToTimeSpan().TotalSeconds).ToString() + " sec";


                if (data_file.n_of_good_data_points > 0)
                {
                    counted_data_points += data_file.n_of_good_data_points;

                    ignore_events = true;

                    if (data_file.start_time_code < min_start_time_code)
                    {
                        min_start_time_code = data_file.start_time_code;
                    }
                    if (max_end_time_code < data_file.end_time_code)
                    {
                        max_end_time_code = data_file.end_time_code;
                    }

                    min_max_time_span_in_seconds = max_end_time_code - min_start_time_code;

                    expected_data_points = (int)(min_max_time_span_in_seconds * 100);

                    AddDataFileToDataBase(data_file, file_name, CDataFile.SCAN_TYPE.STD_DEV);

                    ignore_events = false;

                    Application.DoEvents();

                }
                else
                {
                    AddDataFileToDataBase(data_file, file_name, CDataFile.SCAN_TYPE.STD_DEV);
                }
                data_file = null;
            }
            ignore_events = false;

        }

        void AddDataFileToDataBase(CDataFile datafile, string filename, CDataFile.SCAN_TYPE scan_type)
        {
            SimplotDatabaseDataSetTableAdapters.dataTableAdapter adapter = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();

            if (datafile.n_of_good_data_points > 0 && datafile.IsValid)
            {

                int index_of_event = -1;

                if (scan_type == CDataFile.SCAN_TYPE.STD_DEV)
                {
                    index_of_event = datafile.IndexOfLocalStdDevMax;
                }

                if (scan_type == CDataFile.SCAN_TYPE.RUNNING_AVR)
                {
                    index_of_event = datafile.running_avr_event_index;
                }



                if (radCheckBox_jump_to_event.Checked && index_of_event >= 0)
                {

                    double t_window_min = -10;
                    double t_window_max = 10;

                    double window_min = double.MaxValue;
                    double window_max = double.MinValue;
                    double window_average = 0;

                    int window_min_index = index_of_event + (int)(100 * t_window_min);
                    int window_max_index = index_of_event + (int)(100 * t_window_max);

                    if (window_min_index < 0)
                    {
                        window_min_index = 0;
                    }

                    if (window_max_index > datafile.n_of_good_data_points - 1)
                    {
                        window_max_index = datafile.n_of_good_data_points - 1;
                    }

                    int n = 0;

                    for (int i = window_min_index; i <= window_max_index; i++)
                    {
                        if (window_min > datafile.lower[i])
                        {
                            window_min = datafile.lower[i];
                        }

                        if (window_max < datafile.upper[i])
                        {
                            window_max = datafile.upper[i];
                        }

                        window_average += datafile.average_speed[i];

                        n++;
                    }

                    window_average /= (double)n;

                    double window_min_ft_per_min = Speed.FromMetersPerSecond(window_min).FeetPerSecond * 60;
                    double window_max_ft_per_min = Speed.FromMetersPerSecond(window_max).FeetPerSecond * 60;
                    double window_average_ft_per_min = Speed.FromMetersPerSecond(window_average).FeetPerSecond * 60;



                    current_datafile = datafile;
                    current_file_index = index_of_event;

                    current_datafile.ChartData(radChartView_speed_over_time, horizontalAxis_seconds, verticalAxis_speed_in_ft_min, verticalAxis_frequency_in_Hz, verticalAxis_std_dev, current_file_index - chart_sample_size / 2, chart_sample_size);
                    ChartLogic();


                    radPropertyGrid1.SelectedObject = current_datafile;

                    simplotDatabaseDataSet.data.AdddataRow(datafile.filename, datafile.data_filename, "", datafile.conveyor,
                       index_of_event,
                       datafile.time_stamp_data[index_of_event],
                       datafile.relative_time_data[index_of_event],
                       "",
                       window_min_ft_per_min,
                       window_max_ft_per_min,
                       window_average_ft_per_min,
                       datafile.local_std_dev.Max(),
                       datafile.minimum_ft_per_min,
                       datafile.maximum_ft_per_min,
                       datafile.total_average_ft_per_min,
                       datafile.std_dev_of_average,
                       datafile.n_of_good_data_points,
                       datafile.n_of_data_points, scan_type.ToString(), 0, "", "", 0,0

                       );


                }
                else
                {
                    simplotDatabaseDataSet.data.AdddataRow(datafile.filename, datafile.data_filename, "", datafile.conveyor,
                       index_of_event,
                       System.DateTime.MinValue,
                       0,
                       "",
                       0,
                       0,
                       0,
                       datafile.local_std_dev.Max(),
                       datafile.minimum_ft_per_min,
                       datafile.maximum_ft_per_min,
                       datafile.total_average_ft_per_min,
                       datafile.std_dev_of_average,
                       datafile.n_of_good_data_points,
                       datafile.n_of_data_points, scan_type.ToString(), 0, "", "", 0,0
                       );
                }
            }
            else
            {
                simplotDatabaseDataSet.data.AdddataRow(datafile.filename, datafile.data_filename, "", datafile.conveyor,
                   -1,
                   System.DateTime.MinValue,
                   0,
                   "",
                   0,
                   0,
                   0,
                   0,
                   datafile.minimum_ft_per_min,
                   datafile.maximum_ft_per_min,
                   datafile.total_average_ft_per_min,
                   datafile.std_dev_of_average,
                   datafile.n_of_good_data_points,
                   datafile.n_of_data_points, "BAD_SET", 0, "", "", 0,0);

            }

            adapter.Update(simplotDatabaseDataSet);

            simplotDatabaseDataSet.AcceptChanges();

            Application.DoEvents();

        }




        private void radGridView_data_SelectionChanged(object sender, EventArgs e)
        {
            if (ignore_events)
                return;

            if (gridview_cell_value_changed)
            {
                gridview_cell_value_changed = false;
                return;
            }


            if (radGridView_data.SelectedRows.Count == 1)
            {

                radGridView_data.Enabled = false;
                Cursor = Cursors.WaitCursor;

                wait_form.Show();

                Application.DoEvents();

                this.radDesktopAlert1.Hide();

                string filename = this.radGridView_data.CurrentRow.Cells[1].Value as string;

                string changed_filename = filename;

                Debug.WriteLine(changed_filename);

                if (File.Exists(changed_filename))
                {

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    current_datafile = new CDataFile(changed_filename, radCheckBox_spike_filter.Checked, spike_filter_accel,
                        radCheckBox_low_pass_filter.Checked, low_pass_filter_frequency,
                        running_average_length_in_seconds, local_standard_deviation_length_in_seconds, LocalStdDevTrigger,
                        speed_drop_in_percent, false, null, current_scan_type_for_viewing, DoFFT, allow_for_1_event_per_seconds,
                        wait_form.SetMessage);

                    if (event_file_index != -1)
                    {

                        if (radCheckBox_jump_to_event.Checked)
                        {
                            current_file_index = event_file_index;
                        }

                        Application.DoEvents();
                        current_datafile.ChartData(radChartView_speed_over_time, horizontalAxis_seconds, verticalAxis_speed_in_ft_min, verticalAxis_frequency_in_Hz, verticalAxis_std_dev, current_file_index - chart_sample_size / 2, chart_sample_size);
                        ChartLogic();

                        userControl_Spectrum1.datafile = current_datafile;
                        userControl_Spectrum1.index_from = current_file_index - chart_sample_size / 2;
                        userControl_Spectrum1.index_to = userControl_Spectrum1.index_from + chart_sample_size;

                        //userControl_Spectrum1.Draw();

                        radPropertyGrid1.SelectedObject = current_datafile;

                        // create the video list
                        ListVideo(current_event_time, current_datafile);
                    }
                }
                else
                {
                    this.radDesktopAlert1.CaptionText = "File Error";
                    this.radDesktopAlert1.ContentText = "Data file does not exist at indicated path.\nConsider changing the drive letter.";
                    this.radDesktopAlert1.Show();
                }

                Cursor = Cursors.Default;
                radGridView_data.Enabled = true;
                wait_form.Hide();
                Application.DoEvents();
            }
        }

        private void radGridView_data_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            ignore_events = true;



            SimplotDatabaseDataSetTableAdapters.dataTableAdapter a = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();
            a.Update(simplotDatabaseDataSet);
            simplotDatabaseDataSet.AcceptChanges();

            ignore_events = false;

            gridview_cell_value_changed = true;

        }

        private void radHScrollBar_index_ValueChanged(object sender, EventArgs e)
        {
            if (current_datafile != null)
            {
                current_datafile.ChartData(radChartView_speed_over_time, horizontalAxis_seconds, verticalAxis_speed_in_ft_min, verticalAxis_frequency_in_Hz, verticalAxis_std_dev, current_file_index - chart_sample_size / 2, chart_sample_size);
                ChartLogic();
            }
        }

        private void radButton_transfer_Click(object sender, EventArgs e)
        {

            SimplotDatabaseDataSetTableAdapters.EventsTableAdapter ae = new EventsTableAdapter();

            ae.Fill(simplotDatabaseDataSet.Events);


            radProgressBar_ReadData.Maximum = simplotDatabaseDataSet.Events.Rows.Count;

            SimplotDatabaseDataSetTableAdapters.dataTableAdapter a = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();

            int i = 0;
            foreach (SimplotDatabaseDataSet.EventsRow row in simplotDatabaseDataSet.Events.Rows)
            {
                radProgressBar_ReadData.Value1 = i++;

                Application.DoEvents();

                if (!row.Is_EVENT_TYPENull())
                {
                    if (row._EVENT_TYPE != "")
                    {
                        // find equivalent row in main database

                        string filename = row._DATA_FILENAME;
                        int event_index = row._EVENT_INDEX;

                        foreach (SimplotDatabaseDataSet.dataRow data_row in simplotDatabaseDataSet.data.Rows)
                        {
                            if (data_row.DATA_FILENAME == filename)

                            {
                                if (row._FILENAME.Contains(data_row.CONVEYOR))
                                {
                                    data_row.BeginEdit();

                                    data_row.EVENT_TYPE = row._EVENT_TYPE;

                                    data_row.EndEdit();

                                    a.Update(simplotDatabaseDataSet);
                                    simplotDatabaseDataSet.AcceptChanges();
                                }
                            }
                        }
                    }
                }


            }
        }

        private void radCheckBox_spike_filter_CheckStateChanged(object sender, EventArgs e)
        {
            radGridView_data_SelectionChanged(null, null);
        }

        private void radSpinEditor_spike_filter_trigger_accel_ValueChanged(object sender, EventArgs e)
        {
            radGridView_data_SelectionChanged(null, null);
        }

        private void radCheckBox_low_pass_filter_CheckStateChanged(object sender, EventArgs e)
        {
            radGridView_data_SelectionChanged(null, null);
        }

        private void radSpinEditor_low_pass_filter_frequency_ValueChanged(object sender, EventArgs e)
        {
            radGridView_data_SelectionChanged(null, null);
        }

        private void radButton_script_Click(object sender, EventArgs e)
        {
        }

        public void FillInSTDDEVScript()
        {
            SimplotDatabaseDataSetTableAdapters.dataTableAdapter a = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();

            int i = 0;
            ignore_events = true;

            radProgressBar_ReadData.Maximum = simplotDatabaseDataSet.data.Rows.Count;

            foreach (SimplotDatabaseDataSet.dataRow data_row in simplotDatabaseDataSet.data.Rows)
            {

                radProgressBar_ReadData.Value1 = i++;
                radProgressBar_ReadData.Text = "processing " + i.ToString() + "/" + simplotDatabaseDataSet.data.Rows.Count.ToString();


                Application.DoEvents();

                data_row.BeginEdit();

                data_row.SCAN_TYPE = "STD_DEV";

                data_row.EndEdit();

                a.Update(simplotDatabaseDataSet);
                simplotDatabaseDataSet.AcceptChanges();

            }

            ignore_events = false;

        }


        public void ChartLogic()
        {

            if (current_datafile != null)
            {
                if (radChartView_speed_over_time != null)
                {
                    flowLayoutPanel_chart_checkboxes.Controls.Clear();

                    foreach (ScatterLineSeries s in radChartView_speed_over_time.Series)
                    {
                        RadCheckBox check_box = new RadCheckBox();
                        check_box.Text = s.LegendTitle;
                        check_box.Checked = s.IsVisible;

                        check_box.CheckStateChanged += Check_box_CheckStateChanged;

                        flowLayoutPanel_chart_checkboxes.Controls.Add(check_box);
                    }

                }
            }
        }

        private void Check_box_CheckStateChanged(object sender, EventArgs e)
        {
            if (ignore_events)
                return;

            RadCheckBox check_box = sender as RadCheckBox;

            foreach (ScatterLineSeries s in radChartView_speed_over_time.Series)
            {
                if (s.LegendTitle == check_box.Text)
                {
                    s.IsVisible = check_box.Checked;
                }
            }
        }


        private bool IsFileAndScanTypeAlreadyInDataBase(string filename, CDataFile.SCAN_TYPE scan_type)
        {
            //SimplotDatabaseDataSetTableAdapters.dataTableAdapter a = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();

            string filter = "FILENAME = '" + filename + "'" + " AND SCAN_TYPE = '" + scan_type.ToString() + "'";

            DataRow[] selection = simplotDatabaseDataSet.data.Select(filter);

            if (selection.Length > 0)
            {
                return true;
            }

            return false;

        }



        public void Scan(CDataFile.SCAN_TYPE scan_type, bool test)
        {
            string[] file_names = Directory.GetFiles(@"\\prod\root\S_Drive\USGR-Shared\SimPlot DATA\DATA", "*.bin", SearchOption.AllDirectories);
            //string[] file_names = Directory.GetFiles(@"\\prod\root\S_Drive\USGR-Shared\SimPlot DATA\DATA", "*.bin", SearchOption.TopDirectoryOnly);

            List<string> fn = new List<string>();
            fn.AddRange(file_names);

            fn.Sort();

            file_names = fn.ToArray();

            ignore_events = true;

            float i = 0;

            long aver_ticks = 0;
            long old_ticks = 0;

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            long start_ticks = stopwatch.ElapsedMilliseconds;

            //Parallel.ForEach(file_names, file_name =>
            foreach (string file_name in file_names)
            {
                Debug.WriteLine("reading:" + file_name);

                long now_ticks = stopwatch.ElapsedMilliseconds;

                long ticks_passed = now_ticks - old_ticks;

                long ticks_passed_total = now_ticks - start_ticks;

                old_ticks = now_ticks;

                aver_ticks += ticks_passed;

                aver_ticks /= 2;

                long ticks_total = aver_ticks * file_names.Count();

                long ticks_left = ticks_total - ticks_passed_total;

                if (ticks_left < 0)
                    ticks_left = 0;

                Duration time_left = Duration.FromMilliseconds((double)(ticks_left));
                Duration time_per = Duration.FromMilliseconds((double)(ticks_passed));

                float progress = ++i / (float)file_names.Count();

                radProgressBar_ReadData.Value1 = (int)(progress * 100);
                radProgressBar_ReadData.Text = i.ToString() + "/" + file_names.Count().ToString() + " "
                    + ((int)time_left.ToTimeSpan().TotalMinutes).ToString() + " min left" +
                    " per : " + ((int)time_per.ToTimeSpan().TotalSeconds).ToString() + " sec";

                Application.DoEvents();

                if (CDataFile.DateEncodedInFilename(file_name) < IgnoreBeforeDate)
                {
                    using (StreamWriter log = File.AppendText(logfile))
                    {
                        log.WriteLine("skipped file due to date:" + file_name);
                    }

                    continue;
                }

                if (IsFileAndScanTypeAlreadyInDataBase(file_name, scan_type))
                {
                    continue;
                }

                if (IsFileAndScanTypeAlreadyInDataBase(file_name, CDataFile.SCAN_TYPE.BAD_SET))
                {
                    continue;
                }


                CDataFile data_file = new CDataFile(file_name, radCheckBox_spike_filter.Checked, spike_filter_accel,
                    radCheckBox_low_pass_filter.Checked, low_pass_filter_frequency,
                    running_average_length_in_seconds, local_standard_deviation_length_in_seconds, LocalStdDevTrigger,
                    speed_drop_in_percent, false, logfile, scan_type, DoFFT, allow_for_1_event_per_seconds,null);

                if (data_file.n_of_good_data_points > 0)
                {
                    if (data_file.event_index_list.Count > 0)
                    {
                        foreach (int index_of_event in data_file.event_index_list)
                        {
                            ignore_events = true;
                            AddDataFileToDataBaseForMultipleEvents(data_file, file_name, scan_type, index_of_event, test);
                            ignore_events = false;
                            Application.DoEvents();
                        }
                    }
                    else
                    {
                        ignore_events = true;
                        AddDataFileToDataBaseForMultipleEvents(data_file, file_name, scan_type, -1, test);
                        ignore_events = false;
                        Application.DoEvents();
                    }
                }
                else
                {
                    ignore_events = true;
                    AddDataFileToDataBaseForMultipleEvents(data_file, file_name, scan_type, -1, test);
                    ignore_events = false;
                    Application.DoEvents();

                }


                data_file = null;
            }
            ignore_events = false;


        }

        void AddDataFileToDataBaseForMultipleEvents(CDataFile datafile, string filename, CDataFile.SCAN_TYPE scan_type, int index_of_event, bool test)
        {
            SimplotDatabaseDataSetTableAdapters.dataTableAdapter adapter = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();

            if (datafile.IsValid)
            {
                if (index_of_event >= 0)
                {
                    double t_window_min = -10;
                    double t_window_max = 10;

                    double window_min = double.MaxValue;
                    double window_max = double.MinValue;
                    double window_average = 0;

                    int window_min_index = index_of_event + (int)(100 * t_window_min);
                    int window_max_index = index_of_event + (int)(100 * t_window_max);

                    if (window_min_index < 0)
                    {
                        window_min_index = 0;
                    }

                    if (window_max_index > datafile.n_of_good_data_points - 1)
                    {
                        window_max_index = datafile.n_of_good_data_points - 1;
                    }

                    int n = 0;

                    for (int i = window_min_index; i <= window_max_index; i++)
                    {
                        if (window_min > datafile.lower[i])
                        {
                            window_min = datafile.lower[i];
                        }

                        if (window_max < datafile.upper[i])
                        {
                            window_max = datafile.upper[i];
                        }

                        window_average += datafile.average_speed[i];

                        n++;
                    }

                    window_average /= (double)n;

                    double window_min_ft_per_min = Speed.FromMetersPerSecond(window_min).FeetPerSecond * 60;
                    double window_max_ft_per_min = Speed.FromMetersPerSecond(window_max).FeetPerSecond * 60;
                    double window_average_ft_per_min = Speed.FromMetersPerSecond(window_average).FeetPerSecond * 60;
                    
                    current_datafile = datafile;
                    current_file_index = index_of_event;

                    current_datafile.ChartData(radChartView_speed_over_time, horizontalAxis_seconds, verticalAxis_speed_in_ft_min, verticalAxis_frequency_in_Hz, verticalAxis_std_dev, current_file_index - chart_sample_size / 2, chart_sample_size);
                    ChartLogic();

                    radPropertyGrid1.SelectedObject = current_datafile;


                    double average_running_average_incline_at_event=0;

                    if (datafile.average_running_average_incline.Count > index_of_event)
                    {
                        average_running_average_incline_at_event = Speed.FromMetersPerSecond(datafile.average_running_average_incline[index_of_event]).FeetPerSecond * 60;
                    }
                                                                      

                    simplotDatabaseDataSet.data.AdddataRow(datafile.filename, datafile.data_filename, "", datafile.conveyor,
                        index_of_event,
                        datafile.time_stamp_data[index_of_event],
                        datafile.relative_time_data[index_of_event],
                        "",
                        window_min_ft_per_min,
                        window_max_ft_per_min,
                        window_average_ft_per_min,
                        datafile.local_std_dev.Max(),

                        datafile.minimum_ft_per_min,
                        datafile.maximum_ft_per_min,
                        datafile.total_average_ft_per_min,
                        datafile.std_dev_of_average,
                        datafile.n_of_good_data_points,
                        datafile.n_of_data_points, scan_type.ToString(), 0, "", "", 
                        average_running_average_incline_at_event, 
                        datafile.event_max_speed[index_of_event]
                        );


                }
                else
                {
                    simplotDatabaseDataSet.data.AdddataRow(datafile.filename, datafile.data_filename, "", datafile.conveyor,
                        index_of_event,
                        System.DateTime.MinValue,
                        0,
                        "Nothing detected",
                        0,
                        0,
                        0,
                        datafile.local_std_dev.Max(),
                        datafile.minimum_ft_per_min,
                        datafile.maximum_ft_per_min,
                        datafile.total_average_ft_per_min,
                        datafile.std_dev_of_average,
                        datafile.n_of_good_data_points,
                        datafile.n_of_data_points, scan_type.ToString(), 0, "", "", 0,0);
                }
            }
            else
            {
                simplotDatabaseDataSet.data.AdddataRow(datafile.filename, datafile.data_filename, "", datafile.conveyor,
                   -1,
                   System.DateTime.MinValue,
                   0,
                   "bad set",
                   0,
                   0,
                   0,
                   0,
                   datafile.minimum_ft_per_min,
                   datafile.maximum_ft_per_min,
                   datafile.total_average_ft_per_min,
                   datafile.std_dev_of_average,
                   datafile.n_of_good_data_points,
                   datafile.n_of_data_points, "BAD_SET", 0, "", "", 0,0);
            }

            if (!test)
            {
                adapter.Update(simplotDatabaseDataSet);

                simplotDatabaseDataSet.AcceptChanges();
            }
            Application.DoEvents();

        }

        

        private void radSpinEditor_position_ValueChanged(object sender, EventArgs e)
        {
            if (ignore_events)
                return;

            current_file_index = (int)(SamplingRate * (double)radSpinEditor_position.Value);
        }

        private void radListView_video_Click(object sender, EventArgs e)
        {
            if (ignore_events)
                return;

            string video_file = radListView_video.SelectedItem.Text;
            
           
            axWindowsMediaPlayer1.URL = video_file;
        }

        private void radChartView_speed_over_time_MouseClick(object sender, MouseEventArgs e)
        {
            CartesianArea area = this.radChartView_speed_over_time.GetArea<CartesianArea>();


            Point p = radChartView_speed_over_time.PointToClient(e.Location);
            


        }

        private void radChartView_speed_over_time_SelectedPointChanged(object sender, ChartViewSelectedPointChangedEventArgs e)
        {

        }

        private void radButton_max_speed_script_Click(object sender, EventArgs e)
        {
            SimplotDatabaseDataSetTableAdapters.dataTableAdapter a = new SimplotDatabaseDataSetTableAdapters.dataTableAdapter();

            int i = 0;
            ignore_events = true;

            radProgressBar_ReadData.Maximum = simplotDatabaseDataSet.data.Rows.Count;

            foreach (SimplotDatabaseDataSet.dataRow data_row in simplotDatabaseDataSet.data.Rows)
            {

                radProgressBar_ReadData.Value1 = i++;
                radProgressBar_ReadData.Text = "processing " + i.ToString() + "/" + simplotDatabaseDataSet.data.Rows.Count.ToString();

                Application.DoEvents();

                if (data_row != null)
                {
                    if (data_row.SCAN_TYPE != null)
                    {
                        if (data_row.SCAN_TYPE == CDataFile.SCAN_TYPE.MAX_SPEED.ToString())
                        {
                            int event_index = data_row.EVENT_INDEX;

                            if (event_index != -1)
                            {


                                CDataFile data_file = new CDataFile(data_row.FILENAME, radCheckBox_spike_filter.Checked, spike_filter_accel,
                                    radCheckBox_low_pass_filter.Checked, low_pass_filter_frequency,
                                    running_average_length_in_seconds, local_standard_deviation_length_in_seconds, LocalStdDevTrigger,
                                    speed_drop_in_percent, false, logfile, CDataFile.SCAN_TYPE.MAX_SPEED, false, allow_for_1_event_per_seconds, null);

                                if (data_file.IsValid)
                                {
                                    double max_speed =
                                        Math.Max(
                                            data_file.head_speed[event_index],
                                            Math.Max(data_file.mid_speed[event_index], data_file.tail_speed[event_index]));

                                    data_row.BeginEdit();

                                    data_row.EVENT_MAX_SPEED = Speed.FromMetersPerSecond(max_speed).FeetPerSecond * 60;

                                    data_row.EndEdit();
                                    a.Update(simplotDatabaseDataSet);
                                    simplotDatabaseDataSet.AcceptChanges();


                                }

                            }

                        }
                    }
                }

            }

            ignore_events = false;

        }
    }
}
