namespace VelvetCLI;

internal static class Program
{
    private static void Main()
    {
        // Play startup: unfold then shine. Uses Live updates to avoid flicker.
        HeaderRenderer.RenderStartupSequence(unfoldDelayMs: 80, shineDelayMs: 10, shineWidth: 10);

        var app = new VelvetCliApp();
        app.Run();
    }
}
