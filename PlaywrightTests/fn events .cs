using Microsoft.Playwright;
using NUnit.Framework;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace PlaywrightTests {
  [TestFixture]
  public class PrikazBrowseraTest {
    [Test]
    public async Task LoginPrikazTesta() {
      using var playwright = await Playwright.CreateAsync();
      await using var browser = await playwright.Chromium.LaunchAsync(
          new BrowserTypeLaunchOptions { Headless = false });

      var page = await browser.NewPageAsync();
      await page.GotoAsync("https://qa-ottoviz.ominf.net/");

      // Login
      await page.FillAsync("[name=\"email\"]", "test_user_123@test.com");
      await page.FillAsync("[name=\"password\"]", "rusaiilzsmtvnhet");
      await page.ClickAsync("[data-testid=\"otto-login-btn\"]");

      // Navigacija
      await page.ClickAsync("[role=\"combobox\"]");
      await page.ClickAsync("text=Camera System VI1");
      await page.WaitForTimeoutAsync(1000);
      await page.ClickAsync("text=KPI Feature");
      await page.WaitForTimeoutAsync(1000);
      await page.ClickAsync("text=ISA");
      await page.ClickAsync("text=Zone1");
      await page.WaitForTimeoutAsync(1000);

      // Aktiviraj checkbox-e
      for (int i = 1; i <= 7; i++) {
        await page.Locator($"(//input[@type='checkbox'])[{i}]").CheckAsync();
      }

      await page.Locator("[data-testid='sendToDetails']").ClickAsync();
      await page.WaitForTimeoutAsync(5000);

      // Klik i hvatanje novog taba
      int brojPreKlik = page.Context.Pages.Count;
      await Task.Delay(1000);  // malo buffer
      var noviTab = page.Context.Pages.Last();
      await noviTab.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

      // Screenshot za proveru
      await noviTab.ScreenshotAsync(new() { Path = "noviTab.png" });

      // Čitanje vrednosti iz vis-item-content
      var reportBuilder = new StringBuilder();
      var vrednosti = new List<double>();

      var elementi =
          noviTab.Locator("div.vis-item-overflow div.vis-item-content");
      int count = await elementi.CountAsync();

      Console.WriteLine($"\nPronađeno vrednosti u DOM-u: {count}");
      reportBuilder.AppendLine($"Pronađeno vrednosti u DOM-u: {count}");

      for (int i = 0; i < count; i++) {
        var text = await elementi.Nth(i).TextContentAsync();
        var raw = text?.Trim();

        if (!string.IsNullOrWhiteSpace(raw) &&
            double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture,
                            out double broj)) {
          reportBuilder.AppendLine($"Vrednost: {broj}");
          vrednosti.Add(broj);
        }
      }

      //  Ukupan zbir
      double zbir = vrednosti.Sum();
      reportBuilder.AppendLine($"\n Ukupna suma svih vrednosti: {zbir:F2}");
      Console.WriteLine($"\n Ukupna suma svih vrednosti: {zbir:F2}");

      //  Statistika pojavljivanja
      var statistika =
          vrednosti.GroupBy(v => v)
              .OrderBy(g => g.Key)
              .Select(g => new { Vrednost = g.Key, Pojavljivanja = g.Count() });

      reportBuilder.AppendLine("\n Statistika učestalosti:");
      Console.WriteLine("\n Statistika učestalosti:");
      foreach (var s in statistika) {
        string linija =
            $"Vrednost: {s.Vrednost}  | Pojavljivanja: {s.Pojavljivanja} puta";
        reportBuilder.AppendLine(linija);
        Console.WriteLine(linija);
      }

      //  Zapis u fajl
      var putanja = Path.Combine(Directory.GetCurrentDirectory(),
                                 "report-events_number.txt");
      File.WriteAllText(putanja, reportBuilder.ToString());
      Console.WriteLine($"\n Report snimljen na: {putanja}");

      await browser.CloseAsync();
    }
  }
}