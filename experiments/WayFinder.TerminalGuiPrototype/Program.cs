using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Editor.Document;

using ConversationEditor = Terminal.Gui.Editor.Editor;

using var app = Application.Create();
app.Init();

var processing = false;

var window = new Window
{
    Title = "WayFinder — Editor Test",
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

// Conversation document
var document = new TextDocument();

// Read-only conversation viewer
var output = new ConversationEditor
{
    X = 1,
    Y = 1,
    Width = Dim.Fill(2),
    Height = Dim.Fill(4),

    ReadOnly = true,
    WordWrap = true,
    Document = document
};

// Persistent input
var input = new TextField
{
    X = 1,
    Y = Pos.Bottom(output),
    Width = Dim.Fill(2),
    Text = ""
};

var send = new Button
{
    X = 1,
    Y = Pos.Bottom(input),
    Text = "Send"
};

send.Accepting += (_, e) =>
{
    SubmitMessage();
    e.Handled = true;
};

input.KeyDown += (_, e) =>
{
    if (e.KeyCode.ToString() == "Enter")
    {
        SubmitMessage();
        e.Handled = true;
    }
};

window.Add(output, input, send);

input.SetFocus();

void AppendConversation(string text)
{
    var previousViewport = output.Viewport;

    var wasAtBottom =
        previousViewport.Y > 0 &&
        previousViewport.Y + previousViewport.Height >=
        output.GetContentSize().Height;

    document.Insert(document.TextLength, text);

    if (wasAtBottom)
    {
        output.CaretOffset = document.TextLength;
    }
    else
    {
        output.Viewport = previousViewport;
    }
}

void SubmitMessage()
{
    if (processing)
        return;

    var message = input.Text.ToString();

    if (string.IsNullOrWhiteSpace(message))
        return;

    input.Text = "";
    input.SetFocus();

    AppendConversation($"\n◇ You: {message}\n\n");
    AppendConversation("✦ Copilot: Processing...\n");

    processing = true;

    _ = StreamResponseAsync();
}

async Task StreamResponseAsync()
{
    var lines = new[]
    {
        "Analyzing your request...",
        "Checking registered projects...",
        "Preparing response...",
        "Done."
    };

    foreach (var line in lines)
    {
        await Task.Delay(1200);

        app.Invoke(() =>
        {
            AppendConversation($"│ {line}\n");
        });
    }

    app.Invoke(() =>
    {
        processing = false;
    });
}

app.Run(window);
