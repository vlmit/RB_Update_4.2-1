using System.Collections.ObjectModel;
using Tessa.UI;

namespace Tessa.Extensions.Default.Client.UI.TaskHistory
{
    public class TaskHistoryViewDetailsViewModel : ViewModel<EmptyModel>
    {
        public ObservableCollection<TaskHistoryViewDetailsElementViewModel> LeftRow { get; set; } = [];
        public ObservableCollection<TaskHistoryViewDetailsElementViewModel> RightRow { get; set; } = [];
        public ObservableCollection<TaskHistoryViewDetailsElementViewModel> BottomElements { get; set; } = [];
    }
}
