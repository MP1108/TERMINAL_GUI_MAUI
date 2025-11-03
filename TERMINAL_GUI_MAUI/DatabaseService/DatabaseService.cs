using SQLite;
using TERMINAL_GUI_MAUI.Models_Maui;

namespace TERMINAL_GUI_MAUI.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                var databasePath = Path.Combine(FileSystem.AppDataDirectory, "produkty.db3");
                _database = new SQLiteAsyncConnection(databasePath);
                
                await _database.CreateTableAsync<Produkt>();
                _isInitialized = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<Produkt>> GetProduktyAsync()
        {
            await InitializeAsync();
            return await _database.Table<Produkt>().OrderBy(p => p.Nazwa).ToListAsync();
        }

        public async Task<int> SaveProduktAsync(Produkt produkt)
        {
            await InitializeAsync();
            return await _database.InsertAsync(produkt);
        }

        public async Task<int> UpdateProduktAsync(Produkt produkt)
        {
            await InitializeAsync();
            return await _database.UpdateAsync(produkt);
        }

        public async Task<int> DeleteProduktAsync(Produkt produkt)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(produkt);
        }
    }
}