using System;
using System.ComponentModel;

namespace SiS.Communication.Demo
{
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        // Summary:
        //     Implements INotifyPropertyChanged, PropertyChanged event.
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
