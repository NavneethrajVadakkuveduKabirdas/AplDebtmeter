using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace DebtMeter.Gui
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SetupChart();
        }

        // ---------- Button handler wired in Form1.Designer.cs ----------
        private void btnRun_Click(object sender, EventArgs e)
        {
            txtLog.Clear();

            try
            {
                Log("Loading DebtData.csv...");

                // Load CSV from the app's runtime directory (bin\Debug\netX-windows\)
                // Make sure DebtData.csv Properties:
                //   Build Action = Content
                //   Copy to Output Directory = Copy if newer
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string csvPath = Path.Combine(exeDir, "DebtData.csv");

                if (!File.Exists(csvPath))
                {
                    Log($"ERROR: File not found: {csvPath}");
                    MessageBox.Show($"DebtData.csv was not found next to the .exe.\n\nExpected:\n{csvPath}\n\n" +
                                    "Fix: set DebtData.csv -> Build Action: Content, Copy to Output Directory: Copy if newer.");
                    return;
                }

                var rows = LoadDebtCsv(csvPath);
                Log($"Loaded {rows.Count} rows.");

                if (rows.Count == 0)
                {
                    Log("No data rows found.");
                    return;
                }

                // Build numeric arrays for ASM:
                // publicDebtUSD = GDP * (PublicDebt_pct_GDP / 100)
                // interestRate  = InterestPayments / publicDebtUSD
                int n = rows.Count;
                var debt = new double[n];
                var rate = new double[n];
                var years = new int[n];

                for (int i = 0; i < n; i++)
                {
                    years[i] = rows[i].Year;

                    double publicDebt = rows[i].GDP_USD * (rows[i].PublicDebt_pct_GDP / 100.0);
                    double r = publicDebt != 0.0 ? (rows[i].InterestPayments_USD / publicDebt) : 0.0;

                    debt[i] = publicDebt;
                    rate[i] = r;
                }

                // Call ASM: annual projection (periodDivisor = 1.0)
                var projectedAnnual = new double[n];
                Log("Calling ASM: ProjectDebt(...)");

                int rc = NativeMethods.ProjectDebt(debt, rate, n, 1.0, projectedAnnual);
                Log($"ASM return code: {rc}");

                if (rc != 0)
                {
                    MessageBox.Show($"ASM ProjectDebt failed. rc={rc}");
                    return;
                }

                // Visualize in grid + chart
                ShowInGrid(rows, debt, rate, projectedAnnual);
                ShowInChart(rows, debt, projectedAnnual);

                Log("Done.");
            }
            catch (DllNotFoundException ex)
            {
                Log("ERROR: DLL not found. Ensure DebtMeter.Native.dll is next to the WinForms exe.");
                Log(ex.Message);
                MessageBox.Show(ex.Message);
            }
            catch (EntryPointNotFoundException ex)
            {
                Log("ERROR: Export not found. Ensure the DLL exports ProjectDebt with the exact name.");
                Log(ex.Message);
                MessageBox.Show(ex.Message);
            }
            catch (BadImageFormatException ex)
            {
                Log("ERROR: x86/x64 mismatch. Ensure WinForms target == DLL target (both x64).");
                Log(ex.Message);
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                Log("ERROR:");
                Log(ex.ToString());
                MessageBox.Show(ex.ToString());
            }
        }

        // ---------- Chart setup ----------
        private void SetupChart()
        {
            chart1.Series.Clear();

            var sDebt = new Series("PublicDebt")
            {
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.Int32
            };

            var sProj = new Series("Projected (Annual)")
            {
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.Int32
            };

            chart1.Series.Add(sDebt);
            chart1.Series.Add(sProj);
        }

        // ---------- Visualization ----------
        private void ShowInGrid(List<DebtRow> rows, double[] debt, double[] rate, double[] projectedAnnual)
        {
            var dt = new DataTable();
            dt.Columns.Add("Country");
            dt.Columns.Add("Year", typeof(int));
            dt.Columns.Add("GDP_USD", typeof(double));
            dt.Columns.Add("PublicDebt_USD", typeof(double));
            dt.Columns.Add("InterestRate", typeof(double));
            dt.Columns.Add("ProjectedDebt_Annual", typeof(double));

            for (int i = 0; i < rows.Count; i++)
            {
                dt.Rows.Add(
                    rows[i].Country,
                    rows[i].Year,
                    rows[i].GDP_USD,
                    debt[i],
                    rate[i],
                    projectedAnnual[i]);
            }

            dataGridView1.DataSource = dt;
        }

        private void ShowInChart(List<DebtRow> rows, double[] debt, double[] projectedAnnual)
        {
            var sDebt = chart1.Series["PublicDebt"];
            var sProj = chart1.Series["Projected (Annual)"];

            sDebt.Points.Clear();
            sProj.Points.Clear();

            // Plot by year (x-axis = Year)
            for (int i = 0; i < rows.Count; i++)
            {
                int year = rows[i].Year;
                sDebt.Points.AddXY(year, debt[i]);
                sProj.Points.AddXY(year, projectedAnnual[i]);
            }
        }

        // ---------- CSV parsing ----------
        private static List<DebtRow> LoadDebtCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return new List<DebtRow>();

            // Header expected:
            // Country,Year,GDP_USD,ExternalDebt_USD,InterestPayments_USD,PublicDebt_pct_GDP
            var list = new List<DebtRow>();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                var parts = line.Split(',');
                if (parts.Length < 6)
                    throw new Exception($"Bad CSV line {i + 1}: expected 6 columns, got {parts.Length}\n{line}");

                list.Add(new DebtRow
                {
                    Country = parts[0],
                    Year = int.Parse(parts[1], CultureInfo.InvariantCulture),
                    GDP_USD = double.Parse(parts[2], CultureInfo.InvariantCulture),
                    ExternalDebt_USD = double.Parse(parts[3], CultureInfo.InvariantCulture),
                    InterestPayments_USD = double.Parse(parts[4], CultureInfo.InvariantCulture),
                    PublicDebt_pct_GDP = double.Parse(parts[5], CultureInfo.InvariantCulture),
                });
            }

            // Optional: sort for nicer charts
            return list.OrderBy(r => r.Country).ThenBy(r => r.Year).ToList();
        }

        private sealed class DebtRow
        {
            public string Country { get; set; } = "";
            public int Year { get; set; }
            public double GDP_USD { get; set; }
            public double ExternalDebt_USD { get; set; }
            public double InterestPayments_USD { get; set; }
            public double PublicDebt_pct_GDP { get; set; }
        }

        // ---------- Logging ----------
        private void Log(string msg) => txtLog.AppendText(msg + Environment.NewLine);

        // ---------- P/Invoke ----------
        internal static class NativeMethods
        {
            // Must match your native export:
            // int ProjectDebt(const double* debt, const double* rate, int n, double periodDivisor, double* outDebt)
            [DllImport("DebtMeter.Native.dll", ExactSpelling = true)]
            internal static extern int ProjectDebt(
                [In] double[] debt,
                [In] double[] rate,
                int n,
                double periodDivisor,
                [Out] double[] outDebt);
        }
    }
}
