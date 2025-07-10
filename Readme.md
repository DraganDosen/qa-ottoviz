Testovi se pokrecu iz terminala sa komandom: dotnet test u Visual Studio Code programu.
Reporti su u vidu tekstualnih fajlova za oba testa: report-events-number.txt, report-metriks.txt u folderu:
 PlaywrightTests\bin\Debug\net8.0. Vama ce se tamo praviti txt fajlovi, ja sam kopirao odatle u folder reporti
Ovo je Playwright sa C# Projekat
U folderu rezultati su moja dva txt fajla sa reportima.
Ovde pisem na nasem jeziku, nadam se da necete zameriti. Inace uvek se pise na engleskom.
Klonirajte i otvorite folder sa Visual Studio Code programom

Projekat koristi:
- .NET SDK 8.0+
- Playwright sa NUnit integracijom
- Microsoft.Playwright.NUnit (verzija 1.52.0)
- coverlet.collector za code coverage (opciono)
- Nema potrebe za ručnim dodavanjem NUnit.Framework — već je uključeno u .csproj



