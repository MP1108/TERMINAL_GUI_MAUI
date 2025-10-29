using System.ComponentModel;

namespace TERMINAL_GUI_MAUI.Models_Maui;

public class Produkt : INotifyPropertyChanged
{
    private string _nazwa;
    private decimal _cena;
    private string _kategoria;

    public int Id { get; set; }

    public string Nazwa
    {
        get => _nazwa;
        set
        {
            _nazwa = value;
            OnPropertyChanged(nameof(Nazwa));
        }
    }

    public decimal Cena
    {
        get => _cena;
        set
        {
            _cena = value;
            OnPropertyChanged(nameof(Cena));
        }
    }

    public string Kategoria
    {
        get => _kategoria;
        set
        {
            _kategoria = value;
            OnPropertyChanged(nameof(Kategoria));
        }
    }

    public DateTime DataUtworzenia { get; set; }
    public DateTime DataModyfikacji { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => $"{Nazwa} - {Cena:C}";
}