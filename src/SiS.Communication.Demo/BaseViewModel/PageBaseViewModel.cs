namespace SiS.Communication.Demo
{
    public abstract class PageBaseViewModel : NotifyBase
    {
        public PageBaseViewModel(string title)
        {
            _title = title;
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    NotifyPropertyChanged(nameof(Title));
                }
            }
        }

        public virtual ServerBaseViewModel ServerVM { get { return null; } }
        public virtual ClientBaseViewModel ClientVM { get { return null; } }

        public virtual void Initialize()
        {

        }
    }
}
