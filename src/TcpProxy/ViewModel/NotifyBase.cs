using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpProxy.ViewModel
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
