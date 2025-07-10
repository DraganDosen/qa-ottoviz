using Microsoft.Playwright;
using NUnit.Framework;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OttovizTests
{
    [TestFixture]
    public class MetriksInspektor32
    {
        private async Task ScrollajDoKrajaTabeleAsync(IPage page)
        {
            await page.EvaluateAsync(@"() => {
                const tabela = document.querySelector('table[data-testid=""table-center""]');
                if (tabela) {
                    tabela.style.minWidth = '3000px';
                }
            }");

            await page.WaitForTimeoutAsync(800);

            await page.EvaluateAsync(@"() => {
                const kontejner = document.querySelector('div[class*=""MuiTableContainer-root""]');
                if (kontejner) {
                    kontejner.scrollLeft = kontejner.scrollWidth;
                }
            }");

            await page.WaitForTimeoutAsync(800);
        }

        [Test]
        public async Task IzracunajZbirIProsekZaSveKolone()
        {
            var regex = new Regex(@"[+-]?\d+(\.\d+)?\s?%");
            var culture = new CultureInfo("en-US");
            var reportBuilder = new StringBuilder();

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
            var page = await browser.NewPageAsync();

            // Login
            await page.GotoAsync("https://qa-ottoviz.ominf.net/");
            await page.FillAsync("[name=\"email\"]", "test_user_123@test.com");
            await page.FillAsync("[name=\"password\"]", "rusaiilzsmtvnhet");
            await page.ClickAsync("[data-testid=\"otto-login-btn\"]");
            await page.WaitForTimeoutAsync(2000);

            // Navigacija
            await page.ClickAsync("[role=\"combobox\"]");
            await page.ClickAsync("text=Camera System VT1");
            await page.ClickAsync("[data-testid=\"KPI Sensor-drawer\"]");
            await page.WaitForTimeoutAsync(2000);
            await page.ClickAsync("[data-testid=\"FCM-drawer\"]");
            await page.WaitForTimeoutAsync(2000); 
            await page.ClickAsync("[data-testid=\"Lanes-drawer\"]");

            // Scrollovanje
            await ScrollajDoKrajaTabeleAsync(page);

            await page.WaitForSelectorAsync("table[data-testid='table-center'] tbody tr");
            await page.WaitForTimeoutAsync(1000);

            var prviRed = await page.QuerySelectorAsync("table[data-testid='table-center'] tbody tr");
            var brojKolona = await prviRed.EvalOnSelectorAllAsync<int>("td", "tds => tds.length");
            Console.WriteLine($" Detektovano kolona: {brojKolona}");
            reportBuilder.AppendLine($" Detektovano kolona: {brojKolona}\n");

            var rows = await page.QuerySelectorAllAsync("table[data-testid='table-center'] tbody tr");
            Console.WriteLine($" Renderovanih redova: {rows.Count}");
            reportBuilder.AppendLine($" Renderovanih redova: {rows.Count}\n");

            for (int col = 0; col < brojKolona; col++)
            {
                var vrednosti = new List<double>();
                var vrednostiLog = new StringBuilder();

                foreach (var row in rows)
                {
                    var cells = await row.QuerySelectorAllAsync("td");
                    if (col < cells.Count)
                    {
                        var div = await cells[col].QuerySelectorAsync("div");
                        var text = await div?.TextContentAsync();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var raw = text.Replace("%", "").Trim();
                            if (double.TryParse(raw, NumberStyles.Any, culture, out double val))
                            {
                                vrednosti.Add(val);
                                Console.WriteLine($"        vrednost: {val:F2}%");
                                vrednostiLog.AppendLine($"        vrednost: {val:F2}%");
                            }
                        }
                    }
                }

                double zbir = vrednosti.Sum();
                double prosek = vrednosti.Count > 0 ? zbir / vrednosti.Count : 0;

                // Footer
                string totalText = "";
                double total = 0;
                bool found = false;

                try
                {
                    var xpath = $"//table[@data-testid='table-center']/tfoot/tr/td[{col + 1}]/div";
                    var locator = page.Locator($"xpath={xpath}");
                    await locator.WaitForAsync(new() { Timeout = 3000 });
                    totalText = await locator.TextContentAsync();
                    found = double.TryParse(totalText?.Replace("%", "").Trim(), NumberStyles.Any, culture, out total);
                }
                catch
                {
                    found = false;
                }

                string log = $"\n Kolona [{col + 1}]\n" +
                             $"    Zbir: {zbir:F2}  |  Broj: {vrednosti.Count}  |  Prosek: {prosek:F2}%\n" +
                             vrednostiLog.ToString();

                if (found)
                {
                    log += $"    Footer TOTAL: {total:F2}%  |  Razlika: {Math.Abs(total - prosek):F2}%\n";
                    if (Math.Abs(total - prosek) > 0.5)
                        log += "  Razlika veća od tolerancije (> 0.5%)\n";
                }
                else
                {
                    log += " TOTAL vrednost nije pronađena.\n";
                }

                Console.WriteLine(log);
                reportBuilder.AppendLine(log);
            }

            // Zapiši report
            var fajlPutanja = Path.Combine(Directory.GetCurrentDirectory(), "report-metriks.txt");
            File.WriteAllText(fajlPutanja, reportBuilder.ToString());
            Console.WriteLine($"\n Report snimljen na: {fajlPutanja}");

            await browser.CloseAsync();
        }
    }
}
