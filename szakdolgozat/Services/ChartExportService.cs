using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Wpf;
using System.Diagnostics;
using System.IO;

namespace szakdolgozat.Services
{
    public class ChartExportService
    {
        public void ExportPiechart(PlotModel plotModel)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"piechart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var outputFilePath = saveFileDialog.FileName;
                ExportChartToPng(plotModel, outputFilePath);
                OpenImage(outputFilePath);
            }
        }

        public void ExportColumnChart(PlotModel plotModel)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"columnchart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var outputFilePath = saveFileDialog.FileName;
                ExportChartToPng(plotModel, outputFilePath);
                OpenImage(outputFilePath);
            }
        }

        private void ExportChartToPng(PlotModel plotModel, string filePath)
        {
            plotModel.Background = OxyColors.White;

            var exporter = new PngExporter
            {
                Width = 1500,
                Height = 1000
            };

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                exporter.Export(plotModel, fileStream);
            }
        }

        private void OpenImage(string filePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(filePath)
                {
                    UseShellExecute = true
                }
            };
            process.Start();
        }
    }
}
