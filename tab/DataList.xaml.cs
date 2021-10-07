using ScottPlot;
using ScottPlot.Plottable;
using ScottPlot.Renderable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.ComponentModel;

namespace ApexVisIns.tab
{
    /// <summary>
    /// DataList.xaml 的互動邏輯
    /// </summary>
    public partial class DataList : StackPanel, INotifyPropertyChanged
    {
        public MainWindow MainWindow { get; set; }

        private readonly double[] dataX = new double[10_000];
        private readonly double[] dataY = new double[10_000];
        //private SignalPlotXY signal; // => 之後改 scatter

        private ScatterPlot signal; // => 之後改 scatter


        /// <summary>
        /// xslx export
        /// </summary>
        private XSLXExport XSLXExport;


        #region Binding
        private bool _exporting;

        public bool Exporting
        {
            get => _exporting;
            set
            {
                if (value != _exporting)
                {
                    _exporting = value;
                    OnPropertyChanged(nameof(Exporting));
                }
            }
        }
        #endregion

        private BFR.Trail BFRTrail { get; set; }

        public DataList()
        {
            InitializeComponent();

            InitializePlot(dataX, dataY);
        }

        private void SelfControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Data List Loaded");
            XSLXExport = new(typeof(BFR.Record));
        }

        /// <summary>
        /// 初始化 Chart
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="dataY"></param>
        private void InitializePlot(double[] dataX, double[] dataY)
        {
            try
            {
                WpfPlot.Reset();

                signal = WpfPlot.Plot.AddScatter(dataX, dataY, System.Drawing.Color.DarkCyan);

                //signal = wpfPlot1.Plot.AddSignal(dataY, 1, System.Drawing.Color.DarkCyan);
                signal.LineStyle = LineStyle.Solid;
                signal.LineWidth = 1;
                signal.MarkerSize = 0;
                signal.MinRenderIndex = 0;
                signal.MaxRenderIndex = 0;

                // Change Style of Plot and Layout
                WpfPlot.Plot.Style(ScottPlot.Style.Light2);
                WpfPlot.Plot.Layout(left: 50, top: 50, bottom: 20, right: 50);


                #region Title and Axis Labels
                _ = WpfPlot.Plot.XAxis2.Label("溫度-變形量曲線", System.Drawing.Color.DarkGreen, 24, true); // Title
                                                                                                     //wpfPlot1.Plot.XLabel("Temperature (℃)");
                _ = WpfPlot.Plot.XAxis.Label("Temperature (℃)", System.Drawing.Color.DarkCyan, 16, true, "consolas");
                WpfPlot.Plot.XAxis.TickLabelStyle(System.Drawing.Color.DarkBlue, "consolas", 12, true);
                WpfPlot.Plot.XAxis.MinimumTickSpacing(0.5);

                _ = WpfPlot.Plot.YAxis.Label("Transformation (px)", System.Drawing.Color.DarkCyan, 16, true, "consolas");
                WpfPlot.Plot.YAxis.TickLabelStyle(System.Drawing.Color.DarkBlue, "consolas", 12, true);
                WpfPlot.Plot.YAxis.MinimumTickSpacing(0.1);
                #endregion

                #region Legend
                Legend legend = WpfPlot.Plot.Legend(location: Alignment.UpperCenter);
                legend.ShadowColor = System.Drawing.Color.Transparent;
                legend.OutlineColor = System.Drawing.Color.Transparent;
                legend.FillColor = System.Drawing.Color.Transparent;
                legend.FontSize = 16;
                #endregion

                // Set Axis Ticks
                // wpfPlot1.Plot.AxisAuto(0.5, 0.1);
                WpfPlot.Plot.AxisAutoX(0.5);
                WpfPlot.Plot.AxisAutoY(0.1);

                // Refresh
                WpfPlot.Refresh();
            }
            catch (Exception ex)
            {
                MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.CHART, ex.Message, MsgInformer.Message.MessageType.Error);
            }
        }

        private void ResultSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            int idx = e.NewStartingIndex;

            if (idx > -1)
            {
                signal.MinRenderIndex = 1;  // index: 0 is (0, 0), ignore it
                signal.MaxRenderIndex = idx + 1;

                dataX[idx + 1] = BFRTrail.ResultSource[idx].Temperature;
                dataY[idx + 1] = BFRTrail.ResultSource[idx].AvgX;
            }
            else
            {
                signal.MinRenderIndex = 0;
                signal.MaxRenderIndex = 0;
            }
        }

        /// <summary>
        /// 刷新 Chart
        /// </summary>
        public void RenderPlot()
        {
            if ((bool)AutoCheckBox.IsChecked)
            {
                WpfPlot.Plot.AxisAuto();
            }
            WpfPlot.Refresh();
        }

        private void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WpfPlot.Plot.AxisAuto(verticalMargin: 0.5);
            AxisLimits axisLimits = WpfPlot.Plot.GetAxisLimits();
            WpfPlot.Plot.SetAxisLimits(xMax: axisLimits.XMax + 500);
        }

        private void StackPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Get Source from DataContext
            BFRTrail = DataContext as BFR.Trail;
            BFRTrail.ResultSource.CollectionChanged += ResultSource_CollectionChanged;
        }

        private void ExcelExportBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new();

            dialog.FileName = $"{DateTime.Now:HHmmss}";
            dialog.DefaultExt = ".xlsx";
            dialog.Filter = "Excel Worksheets|*.xls;*.xlsx";

            if (dialog.ShowDialog() == true)
            {
                Exporting = true;
                _ = Task.Run(() =>
                  {
                      try
                      {
                          // 測試輸出 Excel
                          XLWorkbook workbook = XSLXExport.Export(BFRTrail.ResultSource.ToList());
                          
                          workbook.SaveAs(dialog.FileName);
                      }
                      catch (InvalidOperationException op)
                      {
                          Dispatcher.Invoke(() =>
                          {
                              MainWindow.MsgInformer.AddError(MsgInformer.Message.MsgCode.IO, op.Message, MsgInformer.Message.MessageType.Warning);
                          });
                      }
                  }).ContinueWith(t =>
                  {
                      Exporting = false;
                  });
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //int count = BFRTrail.ResultSource.Count;

            //for (int i = 0; i < count; i++)
            //{
            //    dataX[i] = BFRTrail.ResultSource[i].Temperature;
            //    dataY[i] = BFRTrail.ResultSource[i].AvgX;
            //}

            //RenderPlot();

            //for (int i = 1; i <= BFRTrail.ResultSource.Count; i++)
            //{
            //    Debug.WriteLine($"{dataX[i]} {dataY[i]}");
            //}


            BFRTrail.ResultSource.Clear();

            //wpfPlot1.Refresh();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < BFRTrail.ResultSource.Count; i++)
            {
                Debug.WriteLine($"{BFRTrail.ResultSource[i].PosX1} , {BFRTrail.ResultSource[i].PosX2}");
            }
        }


        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
