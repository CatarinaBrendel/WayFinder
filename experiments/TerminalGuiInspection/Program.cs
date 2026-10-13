using Terminal.Gui.Editor.Document;

var document = new TextDocument();

document.Insert(document.TextLength, "User: Hello!\n");
document.Insert(document.TextLength, "Assistant: Hello! How can I help?\n");

Console.WriteLine(document.Text);

Console.WriteLine($"Document length: {document.TextLength}");
