using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ControlLibrary.Helpers
{
    /// <summary>
    /// Simple implementation of INotifyPropertyChanged
    /// </summary>
    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        #region Property Changed Event Handling
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary> 
        /// This method can be called manually by the Set accessor of each property.
        /// The CallerMemberName attribute that is applied to the optional propertyName
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// Extended implementation of INotifyPropertyChanged that records value changes.
    /// </summary>
    public class PropertyChangedExtendedEventArgs : PropertyChangedEventArgs
    {
        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public PropertyChangedExtendedEventArgs(string propertyName) : base(propertyName)
        { }

        public PropertyChangedExtendedEventArgs(string propertyName, string oldValue, string newValue) : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Notifies clients that a property value has changed.
    /// </summary>
    public interface INotifyPropertyChangedEnhanced
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedEventHandlerEnhanced PropertyChanged;
    }

    public delegate void PropertyChangedEventHandlerEnhanced(
        object sender, PropertyChangedExtendedEventArgs e);

    public abstract class BindableBase : INotifyPropertyChangedEnhanced
    {
        public event PropertyChangedEventHandlerEnhanced? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "") where T : notnull
        {
            if (Equals(storage, value))
            {
                return false;
            }

            var oldValue = storage;
            storage = value;
            this.OnPropertyChanged(oldValue, value, propertyName);
            return true;
        }

        #region Property Changed Event Handling
        protected void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string propertyName = "") where T : notnull
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedExtendedEventArgs(propertyName, oldValue.ToString(), newValue.ToString()));
        }

        /// <summary> 
        /// This method can be called manually by the Set accessor of each property.
        /// The CallerMemberName attribute that is applied to the optional propertyName
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedExtendedEventArgs(propertyName));
        }
        #endregion
    }
}
