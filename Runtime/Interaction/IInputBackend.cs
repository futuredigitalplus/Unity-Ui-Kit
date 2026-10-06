namespace FutureDigitalPlus.UIKit.Interaction
{
    /// <summary>
    /// A source of device-level input facts: which device the player touched, and whether a back key was pressed. The legacy Input
    /// Manager backend is built in; the Input System backend lives in the optional <c>FutureDigitalPlus.UIKit.Interaction.InputSystem</c>
    /// assembly; a game can add its own with <see cref="InputFeed.RegisterBackend"/>.
    /// A backend is started when the first <see cref="InputProfile"/> or <see cref="BackKeySource"/> exists and stopped when the last one is gone,
    /// so a game that never binds one pays nothing.
    /// </summary>
    public interface IInputBackend
    {
        /// <summary>Starts listening. Called on the main thread, once per acquire cycle.</summary>
        void Start();

        /// <summary>Stops listening and releases everything taken in <see cref="Start"/>.</summary>
        void Stop();

        /// <summary>Called once per frame while started. Report with <see cref="InputFeed.ReportDevice"/> and <see cref="InputFeed.ReportBack"/>.</summary>
        void Poll();
    }
}
