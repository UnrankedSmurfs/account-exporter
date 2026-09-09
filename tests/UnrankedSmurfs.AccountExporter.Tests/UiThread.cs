using System.Windows;
using System.Windows.Threading;

namespace UnrankedSmurfs.AccountExporter.Tests;

/// <summary>
///     One STA thread with a running dispatcher, shared by every WPF test.
///
///     WPF permits a single <see cref="Application" /> per process and ties a
///     window to the thread that built it, so the alternative — a fresh STA
///     thread per test — would throw on the second test rather than on a real
///     defect. Creating the <c>App</c> here is the point of the exercise: it
///     loads App.xaml's merged dictionaries, which is what makes StaticResource
///     lookups resolve the way they do when a user launches the exe.
/// </summary>
internal static class UiThread
{
    private static readonly object Gate = new();
    private static Dispatcher? dispatcher;

    private static Dispatcher Instance
    {
        get
        {
            lock (Gate)
            {
                if (dispatcher != null) return dispatcher;

                var ready = new ManualResetEventSlim();
                Exception? startupFailure = null;

                var thread = new Thread(() =>
                {
                    try
                    {
                        var app = new App();
                        app.InitializeComponent();
                        dispatcher = Dispatcher.CurrentDispatcher;
                    }
                    catch (Exception ex)
                    {
                        startupFailure = ex;
                    }
                    finally
                    {
                        ready.Set();
                    }

                    if (startupFailure == null) Dispatcher.Run();
                })
                {
                    IsBackground = true,
                };

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                if (!ready.Wait(TimeSpan.FromSeconds(60)))
                    throw new TimeoutException("The WPF test thread did not start within 60 seconds.");

                if (startupFailure != null)
                    throw new InvalidOperationException(
                        "Application resources failed to load. A merged dictionary in App.xaml or "
                        + "Resources/Theme.xaml is malformed.", startupFailure);

                return dispatcher!;
            }
        }
    }

    /// <summary>Runs <paramref name="action" /> on the UI thread, rethrowing whatever it throws.</summary>
    public static void Run(Action action) => Instance.Invoke(action);
}
