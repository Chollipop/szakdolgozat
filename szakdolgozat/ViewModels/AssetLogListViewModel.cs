using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using szakdolgozat.Models;

namespace szakdolgozat.ViewModels
{
    public class AssetLogListViewModel : BaseViewModel
    {
        public ObservableCollection<AssetLog> AssetLogs { get; set; }

        public event EventHandler AssetLogsChangedReapplyFilters;

        public AssetLogListViewModel()
        {
            LoadAssetLogs();
            App.ServiceProvider.GetRequiredService<AssetListViewModel>().AssetLogsChanged += OnAssetLogsChanged;
        }

        public async Task LoadAssetLogs()
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AssetDbContext>();
                var logsList = await context.AssetLogs
                    .Include(a => a.Asset)
                    .Select(a => new AssetLog
                    {
                        LogID = a.LogID,
                        AssetID = a.AssetID,
                        Asset = a.Asset,
                        Action = a.Action,
                        Timestamp = a.Timestamp,
                        PerformedBy = a.PerformedBy
                    })
                    .ToListAsync();
                AssetLogs = new ObservableCollection<AssetLog>(logsList);
                OnPropertyChanged(nameof(AssetLogs));

                AssetLogsChangedReapplyFilters?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnAssetLogsChanged(object sender, EventArgs e)
        {
            OnAssetLogsChangedReapplyFilters();
        }

        protected virtual void OnAssetLogsChangedReapplyFilters()
        {
            AssetLogsChangedReapplyFilters?.Invoke(this, EventArgs.Empty);
        }        

        public void NotifyAssetLogsChanged()
        {
            OnPropertyChanged(nameof(AssetLogs));
        } 
    }
}
