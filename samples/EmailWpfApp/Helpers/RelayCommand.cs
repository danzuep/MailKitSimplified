using System;
using System.Windows.Input;
using System.Threading.Tasks;

namespace EmailWpfApp.Helpers
{
    /// <summary>
    /// A basic command that runs an Action
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Private Members
        /// <summary>
        /// The action to run
        /// </summary>
        private Action _actionToRun;
        #endregion

        #region Public Events
        /// <summary>
        /// The event that's fired when the <see cref="CanExecute(object)"/> value has changed
        /// </summary>
        public event EventHandler? CanExecuteChanged = (sender, e) => { };
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        public RelayCommand(Action action) => _actionToRun = action;
        #endregion

        #region Command Methods
        /// <summary>
        /// A relay command can always execute
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object? parameter) => true;

        /// <summary>
        /// Executes the commands Action
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object? parameter) => _actionToRun();
        #endregion
    }

    /// <summary>
    /// Marks a type as requiring asynchronous initialisation and provides the result of that initialisation.
    /// https://blog.stephencleary.com/2013/01/async-oop-2-constructors.html
    /// </summary>
    public interface IAsyncInitialisation
    {
        /// <summary>
        /// The result of the asynchronous initialisation of this instance.
        /// </summary>
        Task Initialisation { get; }
    }
}
