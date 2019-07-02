namespace SiS.Communication.Process
{
    /// <summary>
    /// Provides data for the DataMessageReceived event.
    /// </summary>
    public class DataMessageReceivedEventArgs
    {
        /// <summary>
        /// Gets or sets the data of the event args.
        /// </summary>
        /// <returns>The data of the event args.</returns>
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Represents the method that will handle the DataMessageReceived event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="args">A SiS.Communication.Process.DataMessageReceivedEventArgs object that contains the event data.</param>
    public delegate void DataMessageReceivedEventHandler(object sender, DataMessageReceivedEventArgs args);
}
