using Tessa.UI;

namespace Tessa.Extensions.Default.Client.UI.TaskHistory
{
    public class TaskHistoryViewDetailsElementViewModel :
         ViewModel<EmptyModel>
    {
        private string capton;

        private string text;

        private string prefix;
        
        /// <summary>
        /// Создаёт модель представления для элемента отображения в диалоге 
        /// расширенных сведений об элементе истории заданий.
        /// </summary>
        /// <param name="alias">Псевдоним колонки.</param>
        /// <param name="capton">Текст заголовка.</param>
        /// <param name="text">Отображаемый текст.</param>
        /// <param name="prefix">Префикс местоположения.</param>
        public TaskHistoryViewDetailsElementViewModel(string alias, string capton, string text, string prefix)
        {
            this.Alias = alias;
            this.capton = capton;
            this.text = text;
            this.prefix = prefix;
        }

        /// <summary>
        /// Идентификатор объекта для автоматизации.
        /// </summary>
        public string AutomationId => $"{this.Prefix}:{this.Alias}";

        /// <summary>
        /// Префикс, соответствует положению колонки.
        /// </summary>
        public string Prefix
        {
            get => this.prefix;
            set
            {
                if (this.prefix == value)
                {
                    return;
                }
                this.prefix = value;
                this.OnPropertyChanged(nameof(Prefix));
                this.OnPropertyChanged(nameof(AutomationId));
            }
        }

        /// <summary>
        /// Псевдоним колонки модели.
        /// </summary>
        public string Alias { get; private set; }

        public string Text
        {
            get => this.text;
            set
            {
                if (value == this.text)
                {
                    return;
                }

                this.text = value;
                this.OnPropertyChanged(nameof(TaskHistoryViewDetailsElementViewModel.Text));
            }
        }

        public string Caption
        {
            get => this.capton;
            set
            {
                if (value == this.capton)
                {
                    return;
                }

                this.capton = value;
                this.OnPropertyChanged(nameof(TaskHistoryViewDetailsElementViewModel.Caption));
            }
        }
    }
}
