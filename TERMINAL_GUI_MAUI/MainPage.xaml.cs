using TERMINAL_GUI_MAUI.Models_Maui;
using TERMINAL_GUI_MAUI.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace TERMINAL_GUI_MAUI;

public partial class MainPage : ContentPage
{
    public ObservableCollection<Produkt> Produkty { get; set; } = new();
    private int _nextId = 1;
    private const string DefaultCategory = "Inne";
    
    private DatabaseService _databaseService;

    public MainPage()
    {
        InitializeComponent();
        ProduktyList.ItemsSource = Produkty;
        
        _databaseService = new DatabaseService();
        _ = LoadDataFromDatabase();
    
        UpdateProductCount();
        DodajLog("Aplikacja uruchomiona pomyślnie.");
    }

    private async Task LoadDataFromDatabase()
    {
        try
        {
            var produktyFromDb = await _databaseService.GetProduktyAsync();
        
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Produkty.Clear();
                foreach (var produkt in produktyFromDb)
                {
                    Produkty.Add(produkt);
                }
            
                _nextId = Produkty.Any() ? Produkty.Max(p => p.Id) + 1 : 1;
            
                UpdateProductCount();
                DodajLog($"Załadowano {Produkty.Count} produktów z bazy danych.");
            });
        }
        catch (Exception ex)
        {
            DodajLog($"BŁĄD ładowania z bazy: {ex.Message}");
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        if (!Produkty.Any())
        {
            Produkty.Add(new Produkt 
            { 
                Id = _nextId++, 
                Nazwa = "Przykładowy produkt", 
                Cena = 29.99m, 
                Kategoria = "Demo", 
                DataUtworzenia = DateTime.Now,
                DataModyfikacji = DateTime.Now
            });
            UpdateProductCount();
            DodajLog("Załadowano dane demonstracyjne.");
        }
    }

    private void UpdateProductCount()
    {
        LiczbaProduktowLabel.Text = $"Liczba produktów: {Produkty.Count}";
    }

    private async void DodajProdukt_Clicked(object sender, EventArgs e)
    {
        try
        {
            var nazwa = await DisplayPromptAsync("Nowy produkt",
                "Podaj nazwę produktu:", "Dodaj", "Anuluj", "Nazwa produktu");

            if (string.IsNullOrWhiteSpace(nazwa))
            {
                DodajLog("Anulowano dodawanie produktu.");
                return;
            }

            if (nazwa.Trim().Length < 2)
            {
                await DisplayAlert("Błąd", "Nazwa produktu musi mieć co najmniej 2 znaki.", "OK");
                return;
            }

            var cenaText = await DisplayPromptAsync("Cena produktu",
                "Podaj cenę produktu:", "Dodaj", "Anuluj", "0,00", -1, Keyboard.Numeric);

            if (!decimal.TryParse(cenaText, out decimal cena) || cena < 0)
            {
                await DisplayAlert("Błąd", "Podaj prawidłową cenę (liczba nieujemna).", "OK");
                return;
            }

            var kategoria = await DisplayPromptAsync("Kategoria produktu",
                "Podaj kategorię:", "Dodaj", "Pomiń", DefaultCategory);

            if (string.IsNullOrWhiteSpace(kategoria))
                kategoria = DefaultCategory;

            var now = DateTime.Now;
            var produkt = new Produkt
            {
                Nazwa = nazwa.Trim(),
                Cena = Math.Round(cena, 2),
                Kategoria = kategoria.Trim(),
                DataUtworzenia = now,
                DataModyfikacji = now
            };

            var result = await _databaseService.SaveProduktAsync(produkt);

            if (result > 0)
            {
                await LoadDataFromDatabase();
                DodajLog($"Dodano produkt: '{produkt.Nazwa}' za {produkt.Cena:C}");
            }
            else
            {
                await DisplayAlert("Błąd", "Nie udało się zapisać produktu do bazy", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Wystąpił nieoczekiwany błąd: {ex.Message}", "OK");
            DodajLog($"BŁĄD: {ex.Message}");
        }
    }

    private async void EdytujProdukt_Clicked(object sender, EventArgs e)
    {
        if (ProduktyList.SelectedItem is not Produkt produkt)
        {
            await DisplayAlert("Informacja", "Wybierz produkt do edycji z listy.", "OK");
            return;
        }

        try
        {
            var nowaNazwa = await DisplayPromptAsync("Edycja produktu", 
                "Nowa nazwa:", "Zapisz", "Anuluj", produkt.Nazwa);

            if (string.IsNullOrWhiteSpace(nowaNazwa))
                return;

            if (nowaNazwa.Trim().Length < 2)
            {
                await DisplayAlert("Błąd", "Nazwa produktu musi mieć co najmniej 2 znaki.", "OK");
                return;
            }

            var nowaCenaText = await DisplayPromptAsync("Edycja ceny", 
                "Nowa cena:", "Zapisz", "Anuluj", produkt.Cena.ToString("F2"), -1, Keyboard.Numeric);

            if (!decimal.TryParse(nowaCenaText, out decimal nowaCena) || nowaCena < 0)
            {
                await DisplayAlert("Błąd", "Podaj prawidłową cenę (liczba nieujemna).", "OK");
                return;
            }

            var nowaKategoria = await DisplayPromptAsync("Edycja kategorii", 
                "Nowa kategoria:", "Zapisz", "Pomiń", produkt.Kategoria);

            var staraNazwa = produkt.Nazwa;
            
            produkt.Nazwa = nowaNazwa.Trim();
            produkt.Cena = Math.Round(nowaCena, 2);
            produkt.Kategoria = string.IsNullOrWhiteSpace(nowaKategoria) ? produkt.Kategoria : nowaKategoria.Trim();
            produkt.DataModyfikacji = DateTime.Now;

            var result = await _databaseService.UpdateProduktAsync(produkt);
            if (result > 0)
            {
                UpdateProductCount();
                DodajLog($"Zaktualizowano produkt: '{staraNazwa}' -> '{produkt.Nazwa}'");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Wystąpił błąd podczas edycji: {ex.Message}", "OK");
            DodajLog($"BŁĄD edycji: {ex.Message}");
        }
    }

    private async void UsunProdukt_Clicked(object sender, EventArgs e)
    {
        if (ProduktyList.SelectedItem is not Produkt produkt)
        {
            await DisplayAlert("Informacja", "Wybierz produkt do usunięcia z listy.", "OK");
            return;
        }

        try
        {
            bool potwierdzenie = await DisplayAlert("Potwierdzenie usunięcia", 
                $"Czy na pewno chcesz usunąć produkt:\n\"{produkt.Nazwa}\"?", 
                "Tak, usuń", "Nie, anuluj");

            if (!potwierdzenie) 
            {
                DodajLog("Anulowano usuwanie produktu.");
                return;
            }

            var nazwaProduktu = produkt.Nazwa;
            var result = await _databaseService.DeleteProduktAsync(produkt);
            
            if (result > 0)
            {
                Produkty.Remove(produkt);
                UpdateProductCount();
                DodajLog($"Usunięto produkt: '{nazwaProduktu}'");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd", $"Wystąpił błąd podczas usuwania: {ex.Message}", "OK");
            DodajLog($"BŁĄD usuwania: {ex.Message}");
        }
    }

    private async void Eksport_Clicked(object sender, EventArgs e)
    {
        if (!Produkty.Any())
        {
            await DisplayAlert("Brak danych", "Brak produktów do eksportu.", "OK");
            return;
        }

        try
        {
            var fileName = $"produkty_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var path = Path.Combine(FileSystem.AppDataDirectory, fileName);

            var csv = new StringBuilder();
            csv.AppendLine("Id;Nazwa;Cena;Kategoria;DataUtworzenia;DataModyfikacji");

            foreach (var produkt in Produkty.OrderBy(p => p.Id))
            {
                csv.AppendLine(
                    $"{produkt.Id};" +
                    $"{EscapeCsvField(produkt.Nazwa)};" +
                    $"{produkt.Cena:F2};" +
                    $"{EscapeCsvField(produkt.Kategoria)};" +
                    $"{produkt.DataUtworzenia:yyyy-MM-dd HH:mm:ss};" +
                    $"{produkt.DataModyfikacji:yyyy-MM-dd HH:mm:ss}"
                );
            }

            await File.WriteAllTextAsync(path, csv.ToString(), Encoding.UTF8);
        
            DodajLog($"Wyeksportowano {Produkty.Count} produktów do: {fileName}");
        
            await DisplayAlert("Eksport zakończony", 
                $"Pomyślnie wyeksportowano {Produkty.Count} produktów do pliku:\n{fileName}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd eksportu", $"Nie udało się wyeksportować danych: {ex.Message}", "OK");
            DodajLog($"BŁĄD eksportu: {ex.Message}");
        }
    }
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private void DodajLog(string wiadomosc)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogBox.Text += $"{DateTime.Now:HH:mm:ss} → {wiadomosc}\n";
        });
    }

    // TEST SQLite - możesz usunąć później
    private async void TestSQLite_Clicked(object sender, EventArgs e)
    {
        try
        {
            var testProdukt = new Produkt 
            { 
                Nazwa = "TEST SQLite", 
                Cena = 99.99m, 
                Kategoria = "Test",
                DataUtworzenia = DateTime.Now,
                DataModyfikacji = DateTime.Now
            };
            
            var saveResult = await _databaseService.SaveProduktAsync(testProdukt);
            DodajLog($"SQLite ZAPIS: {saveResult} (1 = sukces)");
            
            var produkty = await _databaseService.GetProduktyAsync();
            DodajLog($"SQLite ODCZYT: {produkty.Count} produktów");
            
            if (saveResult > 0)
            {
                var deleteResult = await _databaseService.DeleteProduktAsync(testProdukt);
                DodajLog($"SQLite USUWANIE: {deleteResult} (1 = sukces)");
            }
            
            await DisplayAlert("Test SQLite", 
                $"Zapis: {saveResult}\nOdczyt: {produkty.Count}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Błąd SQLite", ex.Message, "OK");
        }
    }
}